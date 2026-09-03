using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;
using VovinamApi.Services;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/can-bo-doan")]
public class CanBoDoanController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly AthleteImageService _anhService;

    // Ảnh Trưởng đoàn/HLV lưu riêng thư mục này (không lẫn với VĐV) —
    // dùng chung đúng AthleteImageService (tên gọi có chữ "Athlete"
    // nhưng logic tải-về/kiểm tra an toàn không riêng gì cho VĐV), chỉ
    // khác tham số folder truyền vào.
    private const string ThuMucAnh = "can-bo-doan";

    private static readonly HashSet<string> DuoiAnhChoPhep = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp",
    };
    private const long AnhKichThuocToiDaByte = 5 * 1024 * 1024;

    public CanBoDoanController(ApplicationDbContext db, IWebHostEnvironment env, AthleteImageService anhService)
    {
        _db = db;
        _env = env;
        _anhService = anhService;
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpGet]
    public async Task<ActionResult<List<CanBoDoanDto>>> GetAll()
    {
        var list = await _db.CanBoDoans.Include(c => c.Team).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPost]
    public async Task<ActionResult<CanBoDoanDto>> Create(CanBoDoanUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.HoTen))
        {
            return BadRequest("Thiếu họ tên.");
        }
        var team = await _db.Teams.FindAsync(dto.TeamId);
        if (team is null)
        {
            return BadRequest("Không tìm thấy đơn vị.");
        }

        // URL ảnh (VD từ Excel import, cột "Link ảnh") chỉ là nguồn ban
        // đầu — tự tải về lưu local NGAY LÚC TẠO, y hệt cách VĐV đang
        // làm. Lý do: PDF in thẻ (html2canvas) cần ảnh cùng gốc mới chụp
        // được ổn định, ảnh nằm thẳng bên WordPress dễ dính lỗi CORS.
        // Tải lỗi thì vẫn tạo bình thường với ảnh = null, không chặn.
        var localImage = await _anhService.TryDownloadAsync(
            dto.AnhDaiDien, HttpContext.RequestAborted, ThuMucAnh);

        var canBo = new CanBoDoan
        {
            Id = Guid.NewGuid(),
            HoTen = dto.HoTen,
            VaiTro = dto.VaiTro,
            TeamId = dto.TeamId,
            AnhDaiDien = localImage,
        };
        _db.CanBoDoans.Add(canBo);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            _anhService.DeleteLocalFile(localImage, ThuMucAnh);
            throw;
        }

        canBo.Team = team;
        return CreatedAtAction(nameof(GetAll), ToDto(canBo));
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CanBoDoanUpsertDto dto)
    {
        var canBo = await _db.CanBoDoans.FirstOrDefaultAsync(c => c.Id == id);
        if (canBo is null) return NotFound();

        canBo.HoTen = dto.HoTen;
        canBo.VaiTro = dto.VaiTro;
        canBo.TeamId = dto.TeamId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Tách riêng khỏi Update (JSON) vì ảnh cần multipart/form-data — cùng
    // cách làm với trọng tài/logo thẻ VĐV, lưu vào đúng thư mục riêng.
    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPost("{id}/anh")]
    [RequestSizeLimit(AnhKichThuocToiDaByte)]
    public async Task<ActionResult<CanBoDoanDto>> UploadAnh(Guid id, IFormFile file)
    {
        var canBo = await _db.CanBoDoans.Include(c => c.Team).FirstOrDefaultAsync(c => c.Id == id);
        if (canBo is null) return NotFound();

        if (file == null || file.Length == 0) return BadRequest("Chưa chọn file.");
        if (file.Length > AnhKichThuocToiDaByte) return BadRequest("File quá 5MB.");
        var ext = Path.GetExtension(file.FileName);
        if (!DuoiAnhChoPhep.Contains(ext)) return BadRequest("Chỉ nhận file ảnh (PNG, JPG, WEBP).");

        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
        var thuMuc = Path.Combine(webRoot, "uploads", ThuMucAnh);
        Directory.CreateDirectory(thuMuc);

        var tenFile = $"{Guid.NewGuid()}{ext}";
        var duongDanDayDu = Path.Combine(thuMuc, tenFile);
        await using (var stream = System.IO.File.Create(duongDanDayDu))
        {
            await file.CopyToAsync(stream);
        }

        var anhCu = canBo.AnhDaiDien;
        canBo.AnhDaiDien = $"/uploads/{ThuMucAnh}/{tenFile}";
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(anhCu))
        {
            try
            {
                var duongDanCu = Path.Combine(webRoot, anhCu.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(duongDanCu)) System.IO.File.Delete(duongDanCu);
            }
            catch
            {
                // Bỏ qua — không để lỗi xoá ảnh cũ chặn mất việc đã lưu ảnh mới.
            }
        }

        return Ok(ToDto(canBo));
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var canBo = await _db.CanBoDoans.FirstOrDefaultAsync(c => c.Id == id);
        if (canBo is null) return NotFound();

        _db.CanBoDoans.Remove(canBo);
        await _db.SaveChangesAsync();
        _anhService.DeleteLocalFile(canBo.AnhDaiDien, ThuMucAnh);
        return NoContent();
    }

    private static CanBoDoanDto ToDto(CanBoDoan c) => new()
    {
        Id = c.Id,
        HoTen = c.HoTen,
        VaiTro = c.VaiTro,
        AnhDaiDien = c.AnhDaiDien,
        TeamId = c.TeamId,
        TeamTen = c.Team?.Ten ?? "",
    };
}
