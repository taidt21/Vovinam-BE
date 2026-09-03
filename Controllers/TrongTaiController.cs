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
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> DuoiAnhChoPhep = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp",
    };
    private const long AnhKichThuocToiDaByte = 5 * 1024 * 1024;

    public TrongTaiController(ApplicationDbContext db, IHubContext<MatchHub> hub, IWebHostEnvironment env)
    {
        _db = db;
        _hub = hub;
        _env = env;
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

        // Không chỉ định sân (trường hợp thêm từ trang quản lý ở Thiết
        // lập giải) thì tự chia đều vòng tròn qua các sân hiện có — làm
        // ở BACKEND (không phải để frontend tự tính) vì cần đúng SoSan
        // + số trọng tài hiện tại NGAY LÚC LƯU, không lệ thuộc frontend
        // đã tải xong danh sách sân hay chưa (dễ bị race condition, lỡ
        // bấm "Thêm" trước khi tải xong thì lại rơi về null như cũ).
        var courtId = dto.CourtId;
        if (string.IsNullOrEmpty(courtId))
        {
            var tournament = await _db.Tournaments.FirstOrDefaultAsync();
            var soSan = Math.Max(1, tournament?.SoSan ?? 1);
            var soLuongHienCo = await _db.TrongTais.CountAsync();
            courtId = $"c{(soLuongHienCo % soSan) + 1}";
        }

        var trongTai = new TrongTai
        {
            Id = Guid.NewGuid(),
            HoTen = dto.HoTen,
            CourtId = courtId,
            ThuTuGiamDinh = dto.ThuTuGiamDinh,
            DonVi = dto.DonVi,
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

        // CHỈ nhả khoá đúng lúc frontend cũng coi thiết lập cũ hết hạn —
        // frontend giờ CHỈ so courtId để quyết định bắt chọn lại (xem
        // TrongTai.tsx) — kể cả chuyển về dự bị (ThuTuGiamDinh = null,
        // CÙNG sân) máy đó vẫn ở lại (chỉ đổi màn hiển thị, không bị bắt
        // chọn lại nữa), nên KHÔNG được nhả khoá lúc đó — nhả nhầm sẽ mở
        // đường cho người khác chọn TRÙNG tên trong khi họ vẫn đang hoạt
        // động (dù đang là dự bị) trên đúng máy đó.
        var matSan = trongTai.CourtId != dto.CourtId;
        if (matSan)
        {
            trongTai.DaChonThietBi = false;
        }

        trongTai.HoTen = dto.HoTen;
        trongTai.CourtId = dto.CourtId;
        trongTai.ThuTuGiamDinh = dto.ThuTuGiamDinh;
        trongTai.DonVi = dto.DonVi;

        await _db.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("TrongTaiChanged");
        return NoContent();
    }

    // Tách riêng khỏi Update ở trên (JSON) vì ảnh cần multipart/form-data
    // — cùng cách làm với TheVdvLogosController, lưu vào đúng
    // wwwroot/uploads/trong-tai/ (không chung thư mục với ảnh VĐV/logo).
    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPost("{id}/anh")]
    [RequestSizeLimit(AnhKichThuocToiDaByte)]
    public async Task<ActionResult<TrongTaiDto>> UploadAnh(Guid id, IFormFile file)
    {
        var trongTai = await _db.TrongTais.FirstOrDefaultAsync(t => t.Id == id);
        if (trongTai is null) return NotFound();

        if (file == null || file.Length == 0) return BadRequest("Chưa chọn file.");
        if (file.Length > AnhKichThuocToiDaByte) return BadRequest("File quá 5MB.");
        var ext = Path.GetExtension(file.FileName);
        if (!DuoiAnhChoPhep.Contains(ext)) return BadRequest("Chỉ nhận file ảnh (PNG, JPG, WEBP).");

        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
        var thuMuc = Path.Combine(webRoot, "uploads", "trong-tai");
        Directory.CreateDirectory(thuMuc);

        // Ảnh cũ (nếu có) không xoá ngay — ghi đè bằng tên file MỚI, để
        // lỡ ghi lỗi giữa chừng vẫn còn ảnh cũ dùng tạm, không mất trắng.
        var tenFile = $"{Guid.NewGuid()}{ext}";
        var duongDanDayDu = Path.Combine(thuMuc, tenFile);
        await using (var stream = System.IO.File.Create(duongDanDayDu))
        {
            await file.CopyToAsync(stream);
        }

        var anhCu = trongTai.AnhDaiDien;
        trongTai.AnhDaiDien = $"/uploads/trong-tai/{tenFile}";
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(anhCu))
        {
            try
            {
                var duongDanCu = Path.Combine(webRoot, anhCu.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(duongDanCu)) System.IO.File.Delete(duongDanCu);
            }
            catch
            {
                // Bỏ qua — không để lỗi xoá ảnh cũ chặn mất việc đã lưu ảnh mới.
            }
        }

        await _hub.Clients.All.SendAsync("TrongTaiChanged");
        return Ok(ToDto(trongTai));
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

    // Mở — thiết bị trọng tài tự gọi lúc chọn tên mình ở màn thiết lập,
    // không đăng nhập admin. Cập nhật kiểu "UPDATE ... WHERE chưa ai
    // chọn" ngay ở tầng SQL (ExecuteUpdateAsync) thay vì đọc lên rồi ghi
    // xuống — để 2 thiết bị lỡ bấm trúng cùng 1 tên trong đúng 1 tích
    // tắc thì CHỈ ĐÚNG 1 request thắng, không có kẽ hở đọc-rồi-ghi khiến
    // cả 2 đều tưởng mình chọn thành công.
    [HttpPost("{id}/chon")]
    public async Task<IActionResult> Chon(Guid id)
    {
        var soHangDoi = await _db.TrongTais
            .Where(t => t.Id == id && !t.DaChonThietBi)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.DaChonThietBi, true));

        if (soHangDoi == 0)
        {
            var tonTai = await _db.TrongTais.AnyAsync(t => t.Id == id);
            return tonTai
                ? Conflict("Tên này vừa được người khác chọn mất — tải lại danh sách rồi chọn tên khác.")
                : NotFound();
        }

        await _hub.Clients.All.SendAsync("TrongTaiChanged");
        return NoContent();
    }

    // Gọi lúc thiết bị bấm "Đổi tên/thiết lập lại" — nhả tên ra cho
    // người khác (hoặc chính người này trên máy khác) chọn lại được.
    [HttpPost("{id}/bo-chon")]
    public async Task<IActionResult> BoChon(Guid id)
    {
        await _db.TrongTais
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.DaChonThietBi, false));

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
        DonVi = t.DonVi,
        AnhDaiDien = t.AnhDaiDien,
        DaChonThietBi = t.DaChonThietBi,
    };
}
