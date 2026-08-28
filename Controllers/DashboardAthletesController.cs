using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;
using VovinamApi.Services;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/dashboard/athletes")]
public class DashboardAthletesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AthleteImageService _athleteImages;

    public DashboardAthletesController(ApplicationDbContext db, AthleteImageService athleteImages)
    {
        _db = db;
        _athleteImages = athleteImages;
    }

    [HttpGet]
    public async Task<ActionResult<List<DashboardAthleteDto>>> GetAll()
    {
        var athletes = await _db.Athletes.Include(a => a.Registrations).ToListAsync();
        return Ok(athletes.Select(ToDto));
    }
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<DashboardAthleteDto>> Create(DashboardAthleteUpsertDto dto)
    {
        if (!await _db.Teams.AnyAsync(t => t.Id == dto.TeamId))
            return BadRequest("Đoàn không tồn tại");

        // URL WordPress chỉ là nguồn ban đầu. Backend cố tải ảnh về local;
        // nếu URL lỗi thì vẫn tạo VĐV bình thường với ảnh = null.
        var localImage = await _athleteImages.TryDownloadAsync(
            NormalizeAnhDaiDien(dto.AnhDaiDien),
            HttpContext.RequestAborted);

        var athlete = new Athlete
        {
            Id = Guid.NewGuid(),
            TeamId = dto.TeamId,
            HoTen = dto.HoTen,
            NamSinh = dto.NamSinh,
            GioiTinh = ParseGioiTinh(dto.GioiTinh),
            NhomTuoi = dto.NhomTuoi,
            AnhDaiDien = localImage,
        };
        _db.Athletes.Add(athlete);

        foreach (var eventId in dto.EventIds)
            _db.Registrations.Add(new Registration { Id = Guid.NewGuid(), AthleteId = athlete.Id, EventId = eventId });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Ảnh đã tải xong nhưng DB không lưu được thì dọn file để không rác.
            _athleteImages.DeleteLocalFile(localImage);
            throw;
        }

        return CreatedAtAction(nameof(GetAll), new DashboardAthleteDto
        {
            Id = athlete.Id,
            HoTen = athlete.HoTen,
            NamSinh = athlete.NamSinh,
            GioiTinh = dto.GioiTinh,
            NhomTuoi = athlete.NhomTuoi,
            AnhDaiDien = athlete.AnhDaiDien,
            TeamId = athlete.TeamId,
            EventIds = dto.EventIds,
        });
    }
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, DashboardAthleteUpsertDto dto)
    {
        var athlete = await _db.Athletes.Include(a => a.Registrations).FirstOrDefaultAsync(a => a.Id == id);
        if (athlete is null) return NotFound();

        if (!await _db.Teams.AnyAsync(t => t.Id == dto.TeamId))
            return BadRequest("Đoàn không tồn tại");

        var oldImage = athlete.AnhDaiDien;
        var requestedImage = NormalizeAnhDaiDien(dto.AnhDaiDien);
        var imageToSave = oldImage;
        var shouldDeleteOldImage = false;

        if (requestedImage is null)
        {
            // Người dùng xóa link ảnh trong form.
            imageToSave = null;
            shouldDeleteOldImage = true;
        }
        else if (_athleteImages.IsManagedLocalPath(requestedImage))
        {
            // Frontend gửi lại đúng đường dẫn local đang có.
            imageToSave = requestedImage;
        }
        else
        {
            // Người dùng nhập URL WordPress mới: tải file mới trước. Nếu tải
            // thất bại thì giữ ảnh cũ để PUT không vô tình làm mất ảnh.
            var downloaded = await _athleteImages.TryDownloadAsync(
                requestedImage,
                HttpContext.RequestAborted);
            if (downloaded is not null)
            {
                imageToSave = downloaded;
                shouldDeleteOldImage = true;
            }
        }

        athlete.TeamId = dto.TeamId;
        athlete.HoTen = dto.HoTen;
        athlete.NamSinh = dto.NamSinh;
        athlete.GioiTinh = ParseGioiTinh(dto.GioiTinh);
        athlete.NhomTuoi = dto.NhomTuoi;
        athlete.AnhDaiDien = imageToSave;

        _db.Registrations.RemoveRange(athlete.Registrations);
        foreach (var eventId in dto.EventIds)
            _db.Registrations.Add(new Registration { Id = Guid.NewGuid(), AthleteId = athlete.Id, EventId = eventId });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Nếu vừa tải ảnh mới mà DB update lỗi, dọn ảnh mới; ảnh cũ vẫn còn.
            if (!string.Equals(imageToSave, oldImage, StringComparison.OrdinalIgnoreCase))
                _athleteImages.DeleteLocalFile(imageToSave);
            throw;
        }

        if (shouldDeleteOldImage
            && !string.Equals(oldImage, imageToSave, StringComparison.OrdinalIgnoreCase))
        {
            _athleteImages.DeleteLocalFile(oldImage);
        }

        return NoContent();
    }
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var athlete = await _db.Athletes.FindAsync(id);
        if (athlete is null) return NotFound();

        var coDiem = await _db.QuyenJudgeScores.AnyAsync(s => s.AthleteId == id)
            || await _db.QuyenLuotHoanThanhs.AnyAsync(l => l.AthleteId == id)
            || await _db.QuyenResults.AnyAsync(r => r.AthleteId == id);
        if (coDiem)
            return Conflict($"Không thể xóa \"{athlete.HoTen}\" — đã có điểm/kết quả quyền liên quan tới VĐV này.");

        var imageToDelete = athlete.AnhDaiDien;
        _db.Athletes.Remove(athlete);
        await _db.SaveChangesAsync();
        _athleteImages.DeleteLocalFile(imageToDelete);
        return NoContent();
    }

    private static DashboardAthleteDto ToDto(Athlete a) => new()
    {
        Id = a.Id,
        HoTen = a.HoTen,
        NamSinh = a.NamSinh,
        GioiTinh = GioiTinhToString(a.GioiTinh),
        NhomTuoi = a.NhomTuoi,
        AnhDaiDien = a.AnhDaiDien,
        TeamId = a.TeamId,
        EventIds = a.Registrations.Select(r => r.EventId).ToList(),
    };

    private static string? NormalizeAnhDaiDien(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static GioiTinh ParseGioiTinh(string s) => s switch
    {
        "nam" => GioiTinh.Nam,
        "nu" => GioiTinh.Nu,
        _ => throw new ArgumentException($"Giá trị 'gioiTinh' không hợp lệ: {s}"),
    };

    private static string GioiTinhToString(GioiTinh g) => g switch
    {
        GioiTinh.Nam => "nam",
        GioiTinh.Nu => "nu",
        _ => throw new ArgumentException($"GioiTinh không hợp lệ: {g}"),
    };
}