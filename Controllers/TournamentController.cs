using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Hubs;
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
    private readonly IHubContext<MatchHub> _hub;

    public TournamentController(ApplicationDbContext db, IHubContext<MatchHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [HttpGet]
    public async Task<ActionResult<TournamentDto>> Get()
    {
        var t = await GetOrCreateSingleton();
        return Ok(ToDto(t));
    }
    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<ActionResult<TournamentDto>> Update(TournamentUpsertDto dto)
    {
        var t = await GetOrCreateSingleton();
        t.Ten = dto.Ten;
        t.SoSan = dto.SoSan;
        t.ChoPhepHiepPhu = dto.ChoPhepHiepPhu;
        t.HeSoVang = dto.HeSoVang;
        t.HeSoBac = dto.HeSoBac;
        t.HeSoDong = dto.HeSoDong;
        t.ChoPhepDongHangBaQuyen = dto.ChoPhepDongHangBaQuyen;
        t.CuaSoDongThuanGiay = dto.CuaSoDongThuanGiay;
        await _db.SaveChangesAsync();
        // Trang Bàn thư ký chỉ tải Tournament đúng 1 lần lúc mở — nếu tab
        // đó đã mở sẵn từ trước lúc BTC đổi cài đặt (VD tích "cho phép
        // hiệp phụ"), báo ngay để nó tự tải lại, khỏi phải nhớ F5 tay.
        await _hub.Clients.All.SendAsync("TournamentChanged");
        return Ok(ToDto(t));
    }

    private static TournamentDto ToDto(Tournament t) => new()
    {
        Id = t.Id,
        Ten = t.Ten,
        SoSan = t.SoSan,
        ChoPhepHiepPhu = t.ChoPhepHiepPhu,
        HeSoVang = t.HeSoVang,
        HeSoBac = t.HeSoBac,
        HeSoDong = t.HeSoDong,
        ChoPhepDongHangBaQuyen = t.ChoPhepDongHangBaQuyen,
        CuaSoDongThuanGiay = t.CuaSoDongThuanGiay,
    };

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