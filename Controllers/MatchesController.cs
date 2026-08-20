using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Hubs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<MatchHub> _hub;

    public MatchesController(ApplicationDbContext db, IHubContext<MatchHub> hub)
    {
        _db = db;
        _hub = hub;
    }
    // Bàn thư ký dùng thẳng 2 endpoint này hàng ngày (đọc danh sách trận,
    // sửa từng trận khi vận hành) — cho phép cả 2 vai trò.
    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpGet]
    public async Task<ActionResult<List<MatchDto>>> GetAll()
    {
        var matches = await _db.Matches.ToListAsync();
        return Ok(matches.Select(ToDto));
    }
    [Authorize(Roles = "Admin,BanThuKy")]
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
        // Báo ngay cho mọi trang BTK đang mở, bất kể đang ở sân nào — thay
        // vì để họ tự phát hiện qua vòng thăm dò 3 giây. Không kèm dữ liệu
        // trong tín hiệu này, ai nhận được tự gọi lại GetAll — đơn giản
        // hơn, không phải lo đồng bộ hình dạng dữ liệu ở 2 nơi.
        await _hub.Clients.All.SendAsync("MatchesChanged");
        return NoContent();
    }
    // Thay TOÀN BỘ trận của 1 nội dung = bốc thăm — chỉ Admin (khớp đúng
    // nút "Bốc thăm" chỉ hiện cho Admin ở giao diện).
    [Authorize(Roles = "Admin")]
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
        await _hub.Clients.All.SendAsync("MatchesChanged");
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