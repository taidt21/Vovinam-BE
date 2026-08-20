using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/admin-auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<BanThuKyAccount> _hasher = new PasswordHasher<BanThuKyAccount>();

    public AdminAuthController(IConfiguration config, ApplicationDbContext db)
    {
        _config = config;
        _db = db;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AdminAuthResponseDto>> Login(AdminLoginDto dto)
    {
        var adminUsername = _config["AdminAuth:Username"];
        var adminPassword = _config["AdminAuth:Password"];

        // Tài khoản Admin tối cao — luôn có sẵn trong cấu hình, không nằm
        // trong database (cần có cách đăng nhập được TRƯỚC KHI có bất kỳ
        // tài khoản Bàn thư ký nào được tạo ra). Chỉ khớp nhánh này khi
        // ĐÃ cấu hình VÀ đúng — chưa cấu hình thì rơi thẳng xuống thử
        // tài khoản BanThuKy trong DB, không được chặn hết mọi đăng nhập
        // chỉ vì riêng tài khoản admin chưa cấu hình.
        if (!string.IsNullOrEmpty(adminUsername) && !string.IsNullOrEmpty(adminPassword)
            && dto.Username == adminUsername && dto.Password == adminPassword)
            return Ok(new AdminAuthResponseDto
            {
                Token = GenerateJwt("admin", "Admin", null),
                Role = "Admin",
                CourtId = null,
            });

        // Tài khoản Bàn thư ký — do Admin tự tạo ở Thiết lập giải, nằm
        // trong database, mật khẩu đã băm (không so sánh chuỗi thô).
        var usernameChuan = dto.Username.Trim().ToLowerInvariant();
        var account = await _db.BanThuKyAccounts.FirstOrDefaultAsync(a => a.Username == usernameChuan);
        if (account is not null)
        {
            var ketQua = _hasher.VerifyHashedPassword(account, account.PasswordHash, dto.Password);
            if (ketQua == PasswordVerificationResult.Success || ketQua == PasswordVerificationResult.SuccessRehashNeeded)
                return Ok(new AdminAuthResponseDto
                {
                    Token = GenerateJwt(account.Username, "BanThuKy", account.CourtId),
                    Role = "BanThuKy",
                    CourtId = account.CourtId,
                });
        }

        return Unauthorized("Sai tên đăng nhập hoặc mật khẩu");
    }

    private string GenerateJwt(string ten, string vaiTro, string? courtId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, ten),
            new(ClaimTypes.Role, vaiTro),
        };
        if (!string.IsNullOrEmpty(courtId))
            claims.Add(new Claim("courtId", courtId));

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
