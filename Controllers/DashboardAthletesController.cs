using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/dashboard/athletes")]
public class DashboardAthletesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DashboardAthletesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<DashboardAthleteDto>>> GetAll()
    {
        var athletes = await _db.Athletes.Include(a => a.Registrations).ToListAsync();
        return Ok(athletes.Select(ToDto));
    }

    [HttpPost]
    public async Task<ActionResult<DashboardAthleteDto>> Create(DashboardAthleteUpsertDto dto)
    {
        if (!await _db.Teams.AnyAsync(t => t.Id == dto.TeamId))
            return BadRequest("Đoàn không tồn tại");

        var athlete = new Athlete
        {
            Id = Guid.NewGuid(),
            TeamId = dto.TeamId,
            HoTen = dto.HoTen,
            NamSinh = dto.NamSinh,
            GioiTinh = ParseGioiTinh(dto.GioiTinh),
            NhomTuoi = dto.NhomTuoi,
        };
        _db.Athletes.Add(athlete);

        foreach (var eventId in dto.EventIds)
            _db.Registrations.Add(new Registration { Id = Guid.NewGuid(), AthleteId = athlete.Id, EventId = eventId });

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new DashboardAthleteDto
        {
            Id = athlete.Id,
            HoTen = athlete.HoTen,
            NamSinh = athlete.NamSinh,
            GioiTinh = dto.GioiTinh,
            NhomTuoi = athlete.NhomTuoi,
            TeamId = athlete.TeamId,
            EventIds = dto.EventIds,
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, DashboardAthleteUpsertDto dto)
    {
        var athlete = await _db.Athletes.Include(a => a.Registrations).FirstOrDefaultAsync(a => a.Id == id);
        if (athlete is null) return NotFound();

        if (!await _db.Teams.AnyAsync(t => t.Id == dto.TeamId))
            return BadRequest("Đoàn không tồn tại");

        athlete.TeamId = dto.TeamId;
        athlete.HoTen = dto.HoTen;
        athlete.NamSinh = dto.NamSinh;
        athlete.GioiTinh = ParseGioiTinh(dto.GioiTinh);
        athlete.NhomTuoi = dto.NhomTuoi;

        _db.Registrations.RemoveRange(athlete.Registrations);
        foreach (var eventId in dto.EventIds)
            _db.Registrations.Add(new Registration { Id = Guid.NewGuid(), AthleteId = athlete.Id, EventId = eventId });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var athlete = await _db.Athletes.FindAsync(id);
        if (athlete is null) return NotFound();

        _db.Athletes.Remove(athlete);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static DashboardAthleteDto ToDto(Athlete a) => new()
    {
        Id = a.Id,
        HoTen = a.HoTen,
        NamSinh = a.NamSinh,
        GioiTinh = GioiTinhToString(a.GioiTinh),
        NhomTuoi = a.NhomTuoi,
        TeamId = a.TeamId,
        EventIds = a.Registrations.Select(r => r.EventId).ToList(),
    };

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