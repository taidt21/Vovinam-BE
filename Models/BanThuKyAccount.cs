namespace VovinamApi.Models;

// Tài khoản Bàn thư ký nội bộ — KHÁC HẲN ApplicationUser (đó là cho đội
// trưởng tự đăng ký ở Portal, gắn với TeamId). Đây là do Admin tự tạo ở
// trang Thiết lập giải, gắn với 1 sân cụ thể để khỏi phải chọn tay mỗi
// lần đăng nhập, tránh thao tác nhầm sang sân khác.
//
// Mật khẩu băm bằng PasswordHasher<T> có sẵn của ASP.NET Core (không tự
// viết hàm hash) — KHÔNG BAO GIỜ trả PasswordHash ra ngoài qua API.
public class BanThuKyAccount
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty; // duy nhất, không phân biệt hoa/thường khi so
    public string PasswordHash { get; set; } = string.Empty;
    public string TenHienThi { get; set; } = string.Empty; // VD "Cô Lan - Sân 2"

    // "c1", "c2"... khớp đúng id sân do fetchCourts() sinh ra (frontend) —
    // null = chưa gán sân, tự chọn tay như trước (không khoá dropdown).
    public string? CourtId { get; set; }
}
