using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VovinamApi.DTOs;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/admin-auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AdminAuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    public ActionResult<AdminAuthResponseDto> Login(AdminLoginDto dto)
    {
        var validUsername = _config["AdminAuth:Username"];
        var validPassword = _config["AdminAuth:Password"];

        if (string.IsNullOrEmpty(validUsername) || string.IsNullOrEmpty(validPassword))
            return StatusCode(500, "Chưa cấu hình tài khoản admin trong appsettings.json");

        if (dto.Username != validUsername || dto.Password != validPassword)
            return Unauthorized("Sai tên đăng nhập hoặc mật khẩu");

        return Ok(new AdminAuthResponseDto { Token = GenerateAdminJwt() });
    }

    private string GenerateAdminJwt()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Admin"),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}