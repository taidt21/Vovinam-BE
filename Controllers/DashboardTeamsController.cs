using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

// Dùng cho màn Đoàn & VĐV (BTC/thư ký) — thấy và sửa được MỌI đoàn. Khác
// với /api/athletes (cổng đăng ký) vốn chỉ thấy đúng đoàn của người đang
// đăng nhập — 2 route tách riêng vì 2 mức quyền khác hẳn nhau.
[ApiController]
[Route("api/dashboard/teams")]
public class DashboardTeamsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DashboardTeamsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TeamDto>>> GetAll()
    {
        var teams = await _db.Teams
            .Select(t => new TeamDto { Id = t.Id, Ten = t.Ten, SoVdv = t.Athletes.Count })
            .ToListAsync();
        return Ok(teams);
    }
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<TeamDto>> Create(TeamUpsertDto dto)
    {
        var ten = dto.Ten.Trim();
        if (await _db.Teams.AnyAsync(t => t.Ten.ToLower() == ten.ToLower()))
            return Conflict($"Đã có đoàn tên \"{dto.Ten}\" rồi.");

        var team = new Team { Id = Guid.NewGuid(), Ten = ten };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();
        return Ok(new TeamDto { Id = team.Id, Ten = team.Ten, SoVdv = 0 });
    }
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, TeamUpsertDto dto)
    {
        var team = await _db.Teams.FindAsync(id);
        if (team is null) return NotFound();

        var ten = dto.Ten.Trim();
        if (await _db.Teams.AnyAsync(t => t.Id != id && t.Ten.ToLower() == ten.ToLower()))
            return Conflict($"Đã có đoàn tên \"{dto.Ten}\" rồi.");

        team.Ten = ten;
        await _db.SaveChangesAsync();
        return NoContent();
    }
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var team = await _db.Teams.Include(t => t.Athletes).FirstOrDefaultAsync(t => t.Id == id);
        if (team is null) return NotFound();

        if (team.Athletes.Count > 0)
            return Conflict($"Không thể xóa \"{team.Ten}\" — còn {team.Athletes.Count} VĐV thuộc đoàn này. Xóa hoặc chuyển đoàn cho các VĐV đó trước.");

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}