using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public EventsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<EventDto>>> GetAll()
    {
        var events = await _db.Events.ToListAsync();
        return Ok(events.Select(ToDto));
    }
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<EventDto>> Create(EventUpsertDto dto)
    {
        var daTonTai = await _db.Events.AnyAsync(e => e.Ten.ToLower() == dto.Ten.Trim().ToLower() && e.NhomTuoi == dto.NhomTuoi);
        if (daTonTai)
            return Conflict($"Đã có nội dung tên \"{dto.Ten}\" ở Nhóm tuổi {dto.NhomTuoi} rồi.");
        var ev = new CompetitionEvent
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten,
            Loai = ParseLoai(dto.Loai),
            GioiTinh = ParseGioiTinh(dto.GioiTinh),
            HinhThucThi = ParseHinhThucThi(dto.HinhThucThi),
            NhomTuoi = dto.NhomTuoi,
            HangCan = dto.HangCan,
            ThoiGianBaiGiay = dto.ThoiGianBaiGiay,
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), ToDto(ev));
    }
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, EventUpsertDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();

        var daTonTai = await _db.Events.AnyAsync(e => e.Id != id && e.Ten.ToLower() == dto.Ten.Trim().ToLower() && e.NhomTuoi == dto.NhomTuoi);
        if (daTonTai)
            return Conflict($"Đã có nội dung tên \"{dto.Ten}\" ở Nhóm tuổi {dto.NhomTuoi} rồi.");

        ev.Ten = dto.Ten;
        ev.Loai = ParseLoai(dto.Loai);
        ev.GioiTinh = ParseGioiTinh(dto.GioiTinh);
        ev.HinhThucThi = ParseHinhThucThi(dto.HinhThucThi);
        ev.NhomTuoi = dto.NhomTuoi;
        ev.HangCan = dto.HangCan;
        ev.ThoiGianBaiGiay = dto.ThoiGianBaiGiay;
        await _db.SaveChangesAsync();
        return NoContent();
    }
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static EventDto ToDto(CompetitionEvent e) => new()
    {
        Id = e.Id,
        Ten = e.Ten,
        Loai = LoaiToString(e.Loai),
        GioiTinh = GioiTinhToString(e.GioiTinh),
        HinhThucThi = HinhThucThiToString(e.HinhThucThi),
        NhomTuoi = e.NhomTuoi,
        HangCan = e.HangCan,
        ThoiGianBaiGiay = e.ThoiGianBaiGiay,
    };

    private static LoaiNoiDung ParseLoai(string s) => s switch
    {
        "quyen" => LoaiNoiDung.Quyen,
        "doi_khang" => LoaiNoiDung.DoiKhang,
        _ => throw new ArgumentException($"Giá trị 'loai' không hợp lệ: {s}"),
    };

    private static string LoaiToString(LoaiNoiDung l) => l switch
    {
        LoaiNoiDung.Quyen => "quyen",
        LoaiNoiDung.DoiKhang => "doi_khang",
        _ => throw new ArgumentException($"LoaiNoiDung không hợp lệ: {l}"),
    };

    private static GioiTinhNoiDung ParseGioiTinh(string s) => s switch
    {
        "nam" => GioiTinhNoiDung.Nam,
        "nu" => GioiTinhNoiDung.Nu,
        "hon_hop" => GioiTinhNoiDung.HonHop,
        _ => throw new ArgumentException($"Giá trị 'gioiTinh' không hợp lệ: {s}"),
    };

    private static string GioiTinhToString(GioiTinhNoiDung g) => g switch
    {
        GioiTinhNoiDung.Nam => "nam",
        GioiTinhNoiDung.Nu => "nu",
        GioiTinhNoiDung.HonHop => "hon_hop",
        _ => throw new ArgumentException($"GioiTinhNoiDung không hợp lệ: {g}"),
    };

    private static HinhThucThi ParseHinhThucThi(string s) => s switch
    {
        "ca_nhan" => HinhThucThi.CaNhan,
        "doi" => HinhThucThi.Doi,
        _ => throw new ArgumentException($"Giá trị 'hinhThucThi' không hợp lệ: {s}"),
    };

    private static string HinhThucThiToString(HinhThucThi h) => h switch
    {
        HinhThucThi.CaNhan => "ca_nhan",
        HinhThucThi.Doi => "doi",
        _ => throw new ArgumentException($"HinhThucThi không hợp lệ: {h}"),
    };
}