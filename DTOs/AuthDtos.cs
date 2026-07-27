using System.ComponentModel.DataAnnotations;

namespace VovinamApi.DTOs;

public class RegisterDto
{
    [Required] public string TenDoan { get; set; } = string.Empty;
    [Required] public string TenNguoiDaiDien { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string MatKhau { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string MatKhau { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string TenDoan { get; set; } = string.Empty;
    public string TenNguoiDaiDien { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
