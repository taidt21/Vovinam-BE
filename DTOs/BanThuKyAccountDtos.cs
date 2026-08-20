using System.ComponentModel.DataAnnotations;

namespace VovinamApi.DTOs;

// Trả về danh sách/từng tài khoản — CỐ Ý không có field mật khẩu/hash
// nào cả, kể cả đã băm cũng không lộ ra ngoài.
public class BanThuKyAccountDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string TenHienThi { get; set; } = string.Empty;
    public string? CourtId { get; set; }
}

public class BanThuKyAccountCreateDto
{
    [Required, MinLength(3)] public string Username { get; set; } = string.Empty;
    [Required, MinLength(4)] public string Password { get; set; } = string.Empty;
    [Required] public string TenHienThi { get; set; } = string.Empty;
    public string? CourtId { get; set; }
}

public class BanThuKyAccountUpdateDto
{
    [Required] public string TenHienThi { get; set; } = string.Empty;
    public string? CourtId { get; set; }
    // Để trống = giữ nguyên mật khẩu cũ, điền vào = đổi mật khẩu mới.
    public string? PasswordMoi { get; set; }
}
