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
[Route("api/trong-tai")]
public class TrongTaiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<MatchHub> _hub;

    public TrongTaiController(ApplicationDbContext db, IHubContext<MatchHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    // Mở — thiết bị trọng tài (không đăng nhập admin) cần đọc danh sách
    // này để chọn đúng tên mình lúc vào chấm.
    [HttpGet]
    public async Task<ActionResult<List<TrongTaiDto>>> GetAll()
    {
        var list = await _db.TrongTais.ToListAsync();
        return Ok(list.Select(ToDto));
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPost]
    public async Task<ActionResult<TrongTaiDto>> Create(TrongTaiUpsertDto dto)
    {
        if (dto.ThuTuGiamDinh is not null)
        {
            var loi = await KiemTraTrungSo(dto.CourtId, dto.ThuTuGiamDinh.Value, null);
            if (loi is not null) return loi;
        }

        var trongTai = new TrongTai
        {
            Id = Guid.NewGuid(),
            HoTen = dto.HoTen,
            CourtId = dto.CourtId,
            ThuTuGiamDinh = dto.ThuTuGiamDinh,
        };
        _db.TrongTais.Add(trongTai);
        await _db.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("TrongTaiChanged");

        return CreatedAtAction(nameof(GetAll), ToDto(trongTai));
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, TrongTaiUpsertDto dto)
    {
        var trongTai = await _db.TrongTais.FirstOrDefaultAsync(t => t.Id == id);
        if (trongTai is null) return NotFound();

        if (dto.ThuTuGiamDinh is not null)
        {
            var loi = await KiemTraTrungSo(dto.CourtId, dto.ThuTuGiamDinh.Value, id);
            if (loi is not null) return loi;
        }

        trongTai.HoTen = dto.HoTen;
        trongTai.CourtId = dto.CourtId;
        trongTai.ThuTuGiamDinh = dto.ThuTuGiamDinh;

        await _db.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("TrongTaiChanged");
        return NoContent();
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var trongTai = await _db.TrongTais.FirstOrDefaultAsync(t => t.Id == id);
        if (trongTai is null) return NotFound();

        _db.TrongTais.Remove(trongTai);
        await _db.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("TrongTaiChanged");
        return NoContent();
    }

    // Báo lỗi rõ ràng thay vì để DB ném lỗi unique-index khó hiểu khi 2
    // người bị gán trùng số Giám định trong cùng 1 sân.
    private async Task<ActionResult?> KiemTraTrungSo(string? courtId, int thuTu, Guid? boQuaId)
    {
        var trung = await _db.TrongTais.AnyAsync(t =>
            t.CourtId == courtId && t.ThuTuGiamDinh == thuTu && t.Id != boQuaId);
        return trung ? BadRequest($"Sân này đã có người là Giám định {thuTu} rồi.") : null;
    }

    private static TrongTaiDto ToDto(TrongTai t) => new()
    {
        Id = t.Id,
        HoTen = t.HoTen,
        CourtId = t.CourtId,
        ThuTuGiamDinh = t.ThuTuGiamDinh,
    };
}
