using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public MatchesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<MatchDto>>> GetAll()
    {
        var matches = await _db.Matches.ToListAsync();
        return Ok(matches.Select(ToDto));
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOne(Guid id, MatchUpdateDto dto)
    {
        var match = await _db.Matches.FindAsync(id);
        if (match is null) return NotFound();

        match.AthleteRedId = dto.AthleteRedId;
        match.AthleteBlueId = dto.AthleteBlueId;
        match.TrangThai = dto.TrangThai;
        match.LyDoKetThuc = dto.LyDoKetThuc;
        match.NguoiThangId = dto.NguoiThangId;
        match.CourtId = dto.CourtId;

        await _db.SaveChangesAsync();
        return NoContent();
    }
    [HttpPut("by-event/{eventId}")]
    public async Task<ActionResult<List<MatchDto>>> ReplaceForEvent(Guid eventId, List<MatchUpsertDto> matches)
    {
        var old = await _db.Matches.Where(m => m.EventId == eventId).ToListAsync();
        _db.Matches.RemoveRange(old);

        var created = matches.Select(m => new Match
        {
            Id = m.Id,
            EventId = eventId,
            AthleteRedId = m.AthleteRedId,
            AthleteBlueId = m.AthleteBlueId,
            NextMatchId = m.NextMatchId,
            NextMatchSlot = m.NextMatchSlot,
            Vong = m.Vong,
            TrangThai = m.TrangThai,
            LyDoKetThuc = m.LyDoKetThuc,
            NguoiThangId = m.NguoiThangId,
            CourtId = m.CourtId,
        }).ToList();
        _db.Matches.AddRange(created);

        await _db.SaveChangesAsync();
        return Ok(created.Select(ToDto));
    }

    private static MatchDto ToDto(Match m) => new()
    {
        Id = m.Id,
        EventId = m.EventId,
        AthleteRedId = m.AthleteRedId,
        AthleteBlueId = m.AthleteBlueId,
        NextMatchId = m.NextMatchId,
        NextMatchSlot = m.NextMatchSlot,
        Vong = m.Vong,
        TrangThai = m.TrangThai,
        LyDoKetThuc = m.LyDoKetThuc,
        NguoiThangId = m.NguoiThangId,
        CourtId = m.CourtId,
    };

}