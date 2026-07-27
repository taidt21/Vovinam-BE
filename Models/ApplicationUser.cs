using Microsoft.AspNetCore.Identity;

namespace VovinamApi.Models;

// Mở rộng IdentityUser — Identity đã tự lo UserName, Email, PasswordHash
// (hash thật, không phải chữ thô như bản localStorage cũ), khóa tài khoản
// sau nhiều lần đăng nhập sai... chỉ thêm đúng phần riêng của bài toán
// này: tên người đại diện, và đội nào họ quản lý.
public class ApplicationUser : IdentityUser
{
    public string TenNguoiDaiDien { get; set; } = string.Empty;

    public Guid TeamId { get; set; }
    public Team? Team { get; set; }
}
