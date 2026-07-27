using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(UserManager<ApplicationUser> userManager, ApplicationDbContext db, IConfiguration config)
    {
        _userManager = userManager;
        _db = db;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            return Conflict("Email này đã đăng ký tài khoản rồi");

        var team = new Team { Id = Guid.NewGuid(), Ten = dto.TenDoan };
        _db.Teams.Add(team);

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            TenNguoiDaiDien = dto.TenNguoiDaiDien,
            TeamId = team.Id,
        };

        var result = await _userManager.CreateAsync(user, dto.MatKhau);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        await _db.SaveChangesAsync();

        return Ok(new AuthResponseDto
        {
            Token = GenerateJwt(user),
            TenDoan = team.Ten,
            TenNguoiDaiDien = user.TenNguoiDaiDien,
            Email = user.Email!,
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.MatKhau))
            return Unauthorized("Sai email hoặc mật khẩu");

        var team = await _db.Teams.FindAsync(user.TeamId);

        return Ok(new AuthResponseDto
        {
            Token = GenerateJwt(user),
            TenDoan = team?.Ten ?? "",
            TenNguoiDaiDien = user.TenNguoiDaiDien,
            Email = user.Email!,
        });
    }

    // teamId được nhúng thẳng vào token — AthletesController đọc lại từ
    // đây, không bao giờ tin teamId do client tự gửi lên trong body request.
    private string GenerateJwt(ApplicationUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("teamId", user.TeamId.ToString()),
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
