using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/quyen-results")]
public class QuyenResultsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public QuyenResultsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<QuyenResultDto>>> GetAll()
    {
        var results = await _db.QuyenResults.ToListAsync();
        return Ok(results.Select(ToDto));
    }

    [HttpPut]
    public async Task<ActionResult<QuyenResultDto>> Upsert(QuyenResultUpsertDto dto)
    {
        var existing = await _db.QuyenResults.FirstOrDefaultAsync(r =>
            r.EventId == dto.EventId &&
            r.AthleteId == dto.AthleteId &&
            r.TeamId == dto.TeamId);

        if (existing != null)
        {
            existing.Diem = dto.Diem;
            existing.DiemTru = dto.DiemTru;
            existing.CapNhatLuc = DateTime.UtcNow;
        }
        else
        {
            existing = new QuyenResult
            {
                Id = Guid.NewGuid(),
                EventId = dto.EventId,
                AthleteId = dto.AthleteId,
                TeamId = dto.TeamId,
                Diem = dto.Diem,
                DiemTru = dto.DiemTru,
                CapNhatLuc = DateTime.UtcNow,
            };
            _db.QuyenResults.Add(existing);
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(existing));
    }

    private static QuyenResultDto ToDto(QuyenResult r) => new()
    {
        Id = r.Id,
        EventId = r.EventId,
        AthleteId = r.AthleteId,
        TeamId = r.TeamId,
        Diem = r.Diem,
        DiemTru = r.DiemTru,
        CapNhatLuc = r.CapNhatLuc,
    };
}