using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VovinamApi.Data;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/court-settings")]
public class CourtSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    // Y hệt các giá trị mặc định cứng trước đây trong makeLiveState()
    // (helpers.ts phía frontend) — dùng làm giá trị trả về nếu sân đó
    // CHƯA TỪNG được tuỳ chỉnh riêng lần nào.
    private const int MacDinhTongSoHiep = 2;
    private const int MacDinhThoiGianHiepGiay = 60;
    private const int MacDinhThoiGianNghiGiay = 30;

    public CourtSettingsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // Đọc cài đặt riêng của 1 sân — CỐ TÌNH mở công khai (không yêu cầu
    // đăng nhập), y hệt lý do các GET chỉ-đọc khác trong dự án: không
    // sửa được gì qua đây, không phát sinh rủi ro.
    //
    // CHƯA TỪNG lưu cho đúng sân này -> trả về giá trị mặc định (KHÔNG
    // phải lỗi 404) — để phía frontend luôn có số hợp lệ để dùng ngay
    // cả lần đầu mở trận vào 1 sân mới toanh, không cần tự xử lý
    // trường hợp "chưa có gì" riêng.
    [HttpGet("{courtId}")]
    public async Task<ActionResult<CourtSettings>> Get(string courtId)
    {
        var settings = await _db.CourtSettings.FindAsync(courtId);
        return Ok(
            settings
                ?? new CourtSettings
                {
                    CourtId = courtId,
                    TongSoHiep = MacDinhTongSoHiep,
                    ThoiGianHiepGiay = MacDinhThoiGianHiepGiay,
                    ThoiGianNghiGiay = MacDinhThoiGianNghiGiay,
                }
        );
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPut("{courtId}")]
    public async Task<IActionResult> Upsert(string courtId, CourtSettings dto)
    {
        var existing = await _db.CourtSettings.FindAsync(courtId);
        if (existing != null)
        {
            existing.TongSoHiep = dto.TongSoHiep;
            existing.ThoiGianHiepGiay = dto.ThoiGianHiepGiay;
            existing.ThoiGianNghiGiay = dto.ThoiGianNghiGiay;
        }
        else
        {
            _db.CourtSettings.Add(
                new CourtSettings
                {
                    CourtId = courtId,
                    TongSoHiep = dto.TongSoHiep,
                    ThoiGianHiepGiay = dto.ThoiGianHiepGiay,
                    ThoiGianNghiGiay = dto.ThoiGianNghiGiay,
                }
            );
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
