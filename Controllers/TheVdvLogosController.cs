using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/the-vdv-logos")]
public class TheVdvLogosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    // Chỉ nhận đúng vài đuôi ảnh phổ biến — logo thường là PNG nền trong
    // suốt, nhưng cho cả JPG/WEBP phòng khi BTC có sẵn file khác định dạng.
    private static readonly HashSet<string> DuoiChoPhep = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".svg",
    };
    private const long KichThuocToiDaByte = 5 * 1024 * 1024; // 5MB — logo không cần to hơn

    public TheVdvLogosController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<List<TheVdvLogoDto>>> GetAll()
    {
        var logos = await _db.TheVdvLogos.OrderBy(l => l.ThuTu).ToListAsync();
        return Ok(logos.Select(l => new TheVdvLogoDto { Id = l.Id, DuongDan = l.DuongDan, ThuTu = l.ThuTu }));
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPost]
    [RequestSizeLimit(KichThuocToiDaByte)]
    public async Task<ActionResult<TheVdvLogoDto>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Chưa chọn file.");
        if (file.Length > KichThuocToiDaByte) return BadRequest("File quá 5MB.");

        var ext = Path.GetExtension(file.FileName);
        if (!DuoiChoPhep.Contains(ext))
            return BadRequest("Chỉ nhận file ảnh (PNG, JPG, WEBP, SVG).");

        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
        var thuMucLogo = Path.Combine(webRoot, "uploads", "logos");
        Directory.CreateDirectory(thuMucLogo);

        var tenFile = $"{Guid.NewGuid()}{ext}";
        var duongDanDayDu = Path.Combine(thuMucLogo, tenFile);
        await using (var stream = System.IO.File.Create(duongDanDayDu))
        {
            await file.CopyToAsync(stream);
        }

        var thuTuKeTiep = 1 + (await _db.TheVdvLogos.Select(l => (int?)l.ThuTu).MaxAsync() ?? 0);
        var logo = new TheVdvLogo
        {
            Id = Guid.NewGuid(),
            DuongDan = $"/uploads/logos/{tenFile}",
            ThuTu = thuTuKeTiep,
        };
        _db.TheVdvLogos.Add(logo);
        await _db.SaveChangesAsync();

        return Ok(new TheVdvLogoDto { Id = logo.Id, DuongDan = logo.DuongDan, ThuTu = logo.ThuTu });
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var logo = await _db.TheVdvLogos.FirstOrDefaultAsync(l => l.Id == id);
        if (logo is null) return NotFound();

        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
        var duongDanDayDu = Path.Combine(webRoot, logo.DuongDan.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        try
        {
            if (System.IO.File.Exists(duongDanDayDu)) System.IO.File.Delete(duongDanDayDu);
        }
        catch
        {
            // Xoá file lỗi (đang mở, quyền truy cập...) thì vẫn xoá bản ghi
            // DB bình thường — không để sót file cũ chặn mất chức năng chính.
        }

        _db.TheVdvLogos.Remove(logo);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class TheVdvLogoDto
{
    public Guid Id { get; set; }
    public string DuongDan { get; set; } = string.Empty;
    public int ThuTu { get; set; }
}
