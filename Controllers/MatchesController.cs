using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;
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
    // Đọc danh sách trận — CỐ TÌNH để mở, không yêu cầu đăng nhập. Màn
    // hình công khai (/man-hinh-cong-khai, không ai đăng nhập gì cả) cần
    // gọi đúng API này để tính "trận số" hiển thị — trước đây chặn
    // Admin/BanThuKy ở GET này khiến hồ sơ trình duyệt kiosk (mới tinh,
    // chưa từng đăng nhập) nhận lỗi 401, bị code xử lý lỗi chung tự đá
    // thẳng về trang đăng nhập admin. Đây chỉ là ĐỌC (không sửa được gì
    // qua endpoint này), nên mở công khai không phát sinh rủi ro ghi đè
    // dữ liệu — các thao tác SỬA (UpdateOne/ReplaceForEvent) bên dưới
    // vẫn giữ nguyên yêu cầu đăng nhập như cũ.
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

    // Dữ liệu đầy đủ cho tính năng "Xem lại trận đã kết thúc" — snapshot
    // (điểm, hiệp, nhắc nhở, cảnh cáo, y tế... tại thời điểm kết thúc)
    // + toàn bộ nhật ký trận đấu, đọc thẳng từ 2 bảng lưu lâu dài
    // (MatchLiveSnapshots, MatchLogEntries) — KHÔNG đụng gì tới
    // LiveCourtStateStore (RAM), vì trận này có thể đã kết thúc từ rất
    // lâu, RAM của sân đó giờ đang phục vụ trận khác hoàn toàn rồi.
    //
    // CỐ TÌNH mở công khai (không yêu cầu đăng nhập) — y hệt lý do ở
    // GetAll phía trên: chỉ đọc, không sửa được gì qua đây.
    [HttpGet("{id}/xem-lai")]
    public async Task<IActionResult> XemLai(Guid id)
    {
        var match = await _db.Matches.FindAsync(id);
        if (match is null) return NotFound();

        var snapshot = await _db.MatchLiveSnapshots.FindAsync(id);
        var matchState = snapshot != null ? JsonNode.Parse(snapshot.StateJson) : null;

        // .OrderBy(Luc) PHẢI đứng SAU ToListAsync() — SQLite không dịch
        // được ORDER BY trên cột kiểu DateTimeOffset thành SQL hợp lệ
        // (lỗi thật đã gặp: "SQLite does not support expressions of
        // type 'DateTimeOffset' in ORDER BY clauses"). Đành lấy hết dữ
        // liệu về trước (số dòng log của 1 trận không nhiều, không đáng
        // lo hiệu năng), rồi sắp xếp lại trên C# (LINQ to Objects) thay
        // vì để EF dịch sang SQL.
        var log = (
            await _db.MatchLogEntries
                .Where(l => l.MatchId == id)
                .Select(l => new
                {
                    id = l.Id.ToString(),
                    luc = l.Luc,
                    noiDung = l.NoiDung,
                    matchTimeLabel = l.MatchTimeLabel,
                    giamDinhId = l.GiamDinhId,
                })
                .ToListAsync()
        )
            .OrderBy(l => l.luc)
            .ToList();

        return Ok(new { matchState, log });
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