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

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
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
        await _db.PerformanceOrders.ExecuteDeleteAsync();
        await _db.Matches.ExecuteDeleteAsync();
        await _db.Registrations.ExecuteDeleteAsync();
        await _db.Athletes.ExecuteDeleteAsync();
        await _db.Events.ExecuteDeleteAsync();
        await _db.Teams.ExecuteDeleteAsync();
        await _db.Tournaments.ExecuteDeleteAsync();

        await tx.CommitAsync();

        return NoContent();
    }
}