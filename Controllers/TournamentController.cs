using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

// Hiện chỉ có ĐÚNG 1 giải tại 1 thời điểm — API hoạt động kiểu "singleton":
// GET luôn trả về giải duy nhất (tự tạo bản ghi mặc định nếu chưa từng
// có), PUT luôn cập nhật đúng bản ghi đó.
[ApiController]
[Route("api/tournament")]
public class TournamentController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public TournamentController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<TournamentDto>> Get()
    {
        var t = await GetOrCreateSingleton();
        return Ok(new TournamentDto { Id = t.Id, Ten = t.Ten, SoSan = t.SoSan });
    }

    [HttpPut]
    public async Task<ActionResult<TournamentDto>> Update(TournamentUpsertDto dto)
    {
        var t = await GetOrCreateSingleton();
        t.Ten = dto.Ten;
        t.SoSan = dto.SoSan;
        await _db.SaveChangesAsync();
        return Ok(new TournamentDto { Id = t.Id, Ten = t.Ten, SoSan = t.SoSan });
    }

    private async Task<Tournament> GetOrCreateSingleton()
    {
        var existing = await _db.Tournaments.FirstOrDefaultAsync();
        if (existing != null) return existing;

        var created = new Tournament { Id = Guid.NewGuid(), Ten = "", SoSan = 1 };
        _db.Tournaments.Add(created);
        await _db.SaveChangesAsync();
        return created;
    }
}