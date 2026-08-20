using System.ComponentModel.DataAnnotations;

namespace VovinamApi.DTOs;

public class AdminLoginDto
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class AdminAuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? CourtId { get; set; }
}