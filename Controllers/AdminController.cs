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
    // trưởng đoàn), khác phạm vi với trang Thiết lập giải này.
    [HttpDelete("reset-all")]
    public async Task<IActionResult> ResetAll()
    {
        await _db.PerformanceOrders.ExecuteDeleteAsync();
        await _db.Matches.ExecuteDeleteAsync();
        await _db.Registrations.ExecuteDeleteAsync();
        await _db.Athletes.ExecuteDeleteAsync();
        await _db.Events.ExecuteDeleteAsync();
        await _db.Teams.ExecuteDeleteAsync();
        await _db.Tournaments.ExecuteDeleteAsync();

        return NoContent();
    }
}