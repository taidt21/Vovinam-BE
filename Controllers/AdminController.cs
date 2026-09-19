using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;

namespace VovinamApi.Controllers;

// CHỈ chứa thao tác quản trị mang tính hủy diệt — tách hẳn khỏi controller
// nghiệp vụ bình thường để dễ thấy ngay đây là vùng nguy hiểm.
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public AdminController(ApplicationDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    // Xóa sạch dữ liệu giải đấu — KHÔNG đụng tới AspNetUsers (tài khoản
    // trưởng đoàn) và BanThuKyAccounts (tài khoản Bàn thư ký), khác phạm
    // vi với trang Thiết lập giải này.
    //
    // Bọc transaction: nếu 1 bảng lỗi giữa chừng thì rollback hết, không
    // để lại dữ liệu xoá dở dang. Các bảng Quyền/TrongTai/Snapshot không
    // có FK constraint thật tới Event/Athlete/Team/Match (EventId/AthleteId
    // ở đó chỉ là Guid trơn, không khai báo navigation property) nên thứ
    // tự xoá dưới đây không bắt buộc phải đúng theo FK — vẫn xếp "chi
    // tiết trước, gốc sau" cho rõ ràng.
    [HttpDelete("reset-all")]
    public async Task<IActionResult> ResetAll()
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        await _db.QuyenJudgeScores.ExecuteDeleteAsync();
        await _db.QuyenResults.ExecuteDeleteAsync();
        await _db.QuyenLuotHoanThanhs.ExecuteDeleteAsync();
        await _db.MatchLiveSnapshots.ExecuteDeleteAsync();
        await _db.QuyenLiveSnapshots.ExecuteDeleteAsync();
        await _db.TrongTais.ExecuteDeleteAsync();
        await _db.TheVdvLogos.ExecuteDeleteAsync();
        await _db.PerformanceOrders.ExecuteDeleteAsync();
        await _db.Matches.ExecuteDeleteAsync();
        await _db.Registrations.ExecuteDeleteAsync();
        await _db.Athletes.ExecuteDeleteAsync();
        await _db.CanBoDoans.ExecuteDeleteAsync();
        await _db.Events.ExecuteDeleteAsync();
        await _db.Teams.ExecuteDeleteAsync();
        await _db.Tournaments.ExecuteDeleteAsync();

        await tx.CommitAsync();

        ClearUploadsDirectory();

        return NoContent();
    }

    // Chỉ dọn nội dung do ứng dụng tạo ra trong wwwroot/uploads. Giữ lại
    // chính thư mục uploads để static-file middleware và các lần upload sau
    // vẫn hoạt động bình thường.
    private void ClearUploadsDirectory()
    {
        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var uploadsPath = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
        var webRootPath = Path.GetFullPath(webRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        // Chặn việc vô tình xóa ra ngoài wwwroot nếu cấu hình đường dẫn sai.
        if (!uploadsPath.StartsWith(webRootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Đường dẫn uploads không hợp lệ.");
        }

        if (Directory.Exists(uploadsPath))
        {
            Directory.Delete(uploadsPath, recursive: true);
        }

        Directory.CreateDirectory(uploadsPath);
    }
}
