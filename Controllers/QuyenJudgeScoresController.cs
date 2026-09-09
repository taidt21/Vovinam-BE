using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/quyen-judge-scores")]
public class QuyenJudgeScoresController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public QuyenJudgeScoresController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<QuyenJudgeScoreDto>>> GetAll()
    {
        var scores = await _db.QuyenJudgeScores.ToListAsync();
        return Ok(scores.Select(ToDto));
    }

    // 1 trọng tài chấm lại (bấm nhầm, sửa điểm) -> ghi đè đúng bản cũ của
    // chính họ, không tạo thêm dòng mới -> không làm sai đếm "đã có mấy
    // người chấm" (luôn tính theo GiamKhaoId duy nhất, không theo số dòng).
    //
    // Trang trọng tài (thiết bị riêng, KHÔNG đăng nhập theo đúng thiết
    // kế — xem TrongTai/QuyenView.tsx) gọi thẳng PUT này để gửi điểm, nên
    // KHÔNG được để [Authorize] ở đây — có JWT đâu mà xác thực. Bảo vệ
    // endpoint này cần cơ chế khác (mã sân) chứ không phải role-based auth.
    //
    // CHÍNH VÌ endpoint này không có auth, "khoá điểm" (yêu cầu mới của
    // Bàn thư ký) không thể chặn bằng role — chặn ngay tại đây, bất kể ai
    // gọi: đã khoá thì từ chối thẳng bằng 409, không quan tâm nguồn gọi.
    [HttpPut]
    public async Task<ActionResult<QuyenJudgeScoreDto>> Upsert(QuyenJudgeScoreUpsertDto dto)
    {
        var daKhoa = await _db.QuyenScoreLocks.AnyAsync(l =>
            l.EventId == dto.EventId && l.AthleteId == dto.AthleteId && l.TeamId == dto.TeamId);
        if (daKhoa)
            return Conflict("Điểm của lượt này đã bị Bàn thư ký khoá, không thể gửi/sửa thêm.");

        var existing = await _db.QuyenJudgeScores.FirstOrDefaultAsync(s =>
            s.EventId == dto.EventId &&
            s.AthleteId == dto.AthleteId &&
            s.TeamId == dto.TeamId &&
            s.GiamKhaoId == dto.GiamKhaoId);

        if (existing != null)
        {
            existing.Diem = dto.Diem;
            existing.TenGiamKhao = dto.TenGiamKhao;
            existing.ChiTietJson = dto.ChiTietJson;
            existing.CapNhatLuc = DateTime.UtcNow;
        }
        else
        {
            existing = new QuyenJudgeScore
            {
                Id = Guid.NewGuid(),
                EventId = dto.EventId,
                AthleteId = dto.AthleteId,
                TeamId = dto.TeamId,
                GiamKhaoId = dto.GiamKhaoId,
                TenGiamKhao = dto.TenGiamKhao,
                Diem = dto.Diem,
                ChiTietJson = dto.ChiTietJson,
                CapNhatLuc = DateTime.UtcNow,
            };
            _db.QuyenJudgeScores.Add(existing);
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(existing));
    }

    // Danh sách MỌI lượt đang bị khoá — CỐ TÌNH mở công khai (không auth),
    // y hệt GetAll() điểm ở trên: cả màn hình BTK lẫn màn hình trọng tài
    // (QuyenView.tsx, cũng không đăng nhập) đều cần đọc được để tự biết
    // khoá/mở khoá mà hiện đúng giao diện — đây chỉ là ĐỌC, không sửa được
    // gì qua endpoint này.
    [HttpGet("locks")]
    public async Task<ActionResult<List<QuyenScoreLockDto>>> GetLocks()
    {
        var locks = await _db.QuyenScoreLocks.ToListAsync();
        return Ok(locks.Select(l => new QuyenScoreLockDto
        {
            EventId = l.EventId,
            AthleteId = l.AthleteId,
            TeamId = l.TeamId,
        }));
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPut("lock")]
    public async Task<IActionResult> Lock(QuyenScoreLockDto dto)
    {
        var existing = await _db.QuyenScoreLocks.FirstOrDefaultAsync(l =>
            l.EventId == dto.EventId && l.AthleteId == dto.AthleteId && l.TeamId == dto.TeamId);
        if (existing == null)
        {
            _db.QuyenScoreLocks.Add(new QuyenScoreLock
            {
                Id = Guid.NewGuid(),
                EventId = dto.EventId,
                AthleteId = dto.AthleteId,
                TeamId = dto.TeamId,
                KhoaLuc = DateTimeOffset.UtcNow,
            });
            await _db.SaveChangesAsync();
        }
        return NoContent();
    }

    // Mở khoá — dành cho lúc BTK lỡ khoá nhầm, hoặc cần cho giám định sửa
    // lại sau khi đã khoá (VD phát hiện lỗi sau khi khoá).
    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpDelete("lock")]
    public async Task<IActionResult> Unlock(
        [FromQuery] Guid eventId, [FromQuery] Guid? athleteId, [FromQuery] Guid? teamId)
    {
        var existing = await _db.QuyenScoreLocks.FirstOrDefaultAsync(l =>
            l.EventId == eventId && l.AthleteId == athleteId && l.TeamId == teamId);
        if (existing != null)
        {
            _db.QuyenScoreLocks.Remove(existing);
            await _db.SaveChangesAsync();
        }
        return NoContent();
    }

    // Cho thi lại 1 lượt = xoá sạch điểm CŨ của tất cả giám định cho ĐÚNG
    // lượt đó — không xoá thì giám định chấm lại sẽ trộn lẫn với điểm của
    // lần thi hỏng trước, ra kết quả sai.
    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpDelete]
    public async Task<IActionResult> DeleteForPerformance(
        [FromQuery] Guid eventId, [FromQuery] Guid? athleteId, [FromQuery] Guid? teamId)
    {
        var scores = await _db.QuyenJudgeScores.Where(s =>
            s.EventId == eventId && s.AthleteId == athleteId && s.TeamId == teamId).ToListAsync();
        _db.QuyenJudgeScores.RemoveRange(scores);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static QuyenJudgeScoreDto ToDto(QuyenJudgeScore s) => new()
    {
        Id = s.Id,
        EventId = s.EventId,
        AthleteId = s.AthleteId,
        TeamId = s.TeamId,
        GiamKhaoId = s.GiamKhaoId,
        TenGiamKhao = s.TenGiamKhao,
        Diem = s.Diem,
        ChiTietJson = s.ChiTietJson,
        CapNhatLuc = s.CapNhatLuc,
    };
}