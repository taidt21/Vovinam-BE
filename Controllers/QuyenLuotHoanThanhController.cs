using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/quyen-luot-hoan-thanh")]
public class QuyenLuotHoanThanhController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public QuyenLuotHoanThanhController(ApplicationDbContext db)
    {
        _db = db;
    }

    // Mở — Bàn thư ký cần đọc để biết lượt nào đã xong (kể cả bị loại,
    // chưa đủ điểm) mà không tự động đưa lại vào sân.
    [HttpGet]
    public async Task<ActionResult<List<QuyenLuotHoanThanhDto>>> GetAll()
    {
        var list = await _db.QuyenLuotHoanThanhs.ToListAsync();
        return Ok(
            list.Select(x => new QuyenLuotHoanThanhDto
            {
                EventId = x.EventId,
                AthleteId = x.AthleteId,
                TeamId = x.TeamId,
                LyDo = x.LyDo,
            })
        );
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPost]
    public async Task<IActionResult> MarkDone(QuyenLuotHoanThanhUpsertDto dto)
    {
        var existing = await _db.QuyenLuotHoanThanhs.FirstOrDefaultAsync(x =>
            x.EventId == dto.EventId && x.AthleteId == dto.AthleteId && x.TeamId == dto.TeamId);
        if (existing != null)
        {
            existing.LyDo = dto.LyDo;
        }
        else
        {
            _db.QuyenLuotHoanThanhs.Add(new QuyenLuotHoanThanh
            {
                Id = Guid.NewGuid(),
                EventId = dto.EventId,
                AthleteId = dto.AthleteId,
                TeamId = dto.TeamId,
                LyDo = dto.LyDo,
            });
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Cho thi lại = bỏ đánh dấu "đã xong" — không bỏ thì lịch tự động vẫn
    // coi lượt này đã hoàn thành, không đưa lại vào hàng chờ.
    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpDelete]
    public async Task<IActionResult> UnmarkDone(
        [FromQuery] Guid eventId, [FromQuery] Guid? athleteId, [FromQuery] Guid? teamId)
    {
        var existing = await _db.QuyenLuotHoanThanhs.Where(x =>
            x.EventId == eventId && x.AthleteId == athleteId && x.TeamId == teamId).ToListAsync();
        _db.QuyenLuotHoanThanhs.RemoveRange(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
