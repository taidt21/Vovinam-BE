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
    [HttpPut]
    public async Task<ActionResult<QuyenJudgeScoreDto>> Upsert(QuyenJudgeScoreUpsertDto dto)
    {
        var existing = await _db.QuyenJudgeScores.FirstOrDefaultAsync(s =>
            s.EventId == dto.EventId &&
            s.AthleteId == dto.AthleteId &&
            s.TeamId == dto.TeamId &&
            s.GiamKhaoId == dto.GiamKhaoId);

        if (existing != null)
        {
            existing.Diem = dto.Diem;
            existing.TenGiamKhao = dto.TenGiamKhao;
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
                CapNhatLuc = DateTime.UtcNow,
            };
            _db.QuyenJudgeScores.Add(existing);
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(existing));
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
        CapNhatLuc = s.CapNhatLuc,
    };
}