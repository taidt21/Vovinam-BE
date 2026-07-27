using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/athletes")]
[Authorize]
public class AthletesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AthletesController(ApplicationDbContext db)
    {
        _db = db;
    }

    private Guid CurrentTeamId => Guid.Parse(User.FindFirst("teamId")!.Value);

    [HttpGet]
    public async Task<ActionResult<List<AthleteDto>>> GetMine()
    {
        var athletes = await _db.Athletes
            .Where(a => a.TeamId == CurrentTeamId)
            .Include(a => a.Registrations)
            .ToListAsync();

        return Ok(athletes.Select(ToDto));
    }

    [HttpPost]
    public async Task<ActionResult<AthleteDto>> Create(AthleteUpsertDto dto)
    {
        var athlete = new Athlete
        {
            Id = Guid.NewGuid(),
            TeamId = CurrentTeamId,
            HoTen = dto.HoTen,
            NamSinh = dto.NamSinh,
            GioiTinh = ParseGioiTinh(dto.GioiTinh),
            NhomTuoi = dto.NhomTuoi,
        };
        _db.Athletes.Add(athlete);

        foreach (var eventId in dto.EventIds)
            _db.Registrations.Add(new Registration { Id = Guid.NewGuid(), AthleteId = athlete.Id, EventId = eventId });

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMine), new AthleteDto
        {
            Id = athlete.Id,
            HoTen = athlete.HoTen,
            NamSinh = athlete.NamSinh,
            GioiTinh = GioiTinhToString(athlete.GioiTinh),
            NhomTuoi = athlete.NhomTuoi,
            EventIds = dto.EventIds,
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, AthleteUpsertDto dto)
    {
        var athlete = await _db.Athletes
            .Include(a => a.Registrations)
            .FirstOrDefaultAsync(a => a.Id == id && a.TeamId == CurrentTeamId);
        if (athlete is null) return NotFound();

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
        var athlete = await _db.Athletes.FirstOrDefaultAsync(a => a.Id == id && a.TeamId == CurrentTeamId);
        if (athlete is null) return NotFound();

        _db.Athletes.Remove(athlete);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static AthleteDto ToDto(Athlete a) => new()
    {
        Id = a.Id,
        HoTen = a.HoTen,
        NamSinh = a.NamSinh,
        GioiTinh = GioiTinhToString(a.GioiTinh),
        NhomTuoi = a.NhomTuoi,
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