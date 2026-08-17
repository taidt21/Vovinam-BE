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
        var adminUsername = _config["AdminAuth:Username"];
        var adminPassword = _config["AdminAuth:Password"];
        var btkUsername = _config["BanThuKyAuth:Username"];
        var btkPassword = _config["BanThuKyAuth:Password"];

        if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
            return StatusCode(500, "Chưa cấu hình tài khoản admin trong appsettings.json");

        if (dto.Username == adminUsername && dto.Password == adminPassword)
            return Ok(new AdminAuthResponseDto { Token = GenerateJwt("admin", "Admin"), Role = "Admin" });

        if (!string.IsNullOrEmpty(btkUsername) && !string.IsNullOrEmpty(btkPassword)
            && dto.Username == btkUsername && dto.Password == btkPassword)
            return Ok(new AdminAuthResponseDto { Token = GenerateJwt("banthuky", "BanThuKy"), Role = "BanThuKy" });

        return Unauthorized("Sai tên đăng nhập hoặc mật khẩu");
    }

    private string GenerateJwt(string ten, string vaiTro)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, ten),
            new Claim(ClaimTypes.Role, vaiTro),
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