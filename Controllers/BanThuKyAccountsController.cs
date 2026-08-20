using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

// Quản lý tài khoản Bàn thư ký — CHỈ Admin được xem/tạo/sửa/xoá. Đăng
// nhập thật (kiểm tra username/password) nằm ở AdminAuthController, đọc
// thẳng từ đúng bảng này.
[ApiController]
[Route("api/ban-thu-ky-accounts")]
[Authorize(Roles = "Admin")]
public class BanThuKyAccountsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    // PasswordHasher<T> không có dependency riêng nào — dùng thẳng, không
    // cần đăng ký vào DI container trong Program.cs.
    private readonly IPasswordHasher<BanThuKyAccount> _hasher = new PasswordHasher<BanThuKyAccount>();

    public BanThuKyAccountsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<BanThuKyAccountDto>>> GetAll()
    {
        var accounts = await _db.BanThuKyAccounts.OrderBy(a => a.Username).ToListAsync();
        return Ok(accounts.Select(ToDto));
    }

    [HttpPost]
    public async Task<ActionResult<BanThuKyAccountDto>> Create(BanThuKyAccountCreateDto dto)
    {
        var usernameChuan = dto.Username.Trim().ToLowerInvariant();
        if (await _db.BanThuKyAccounts.AnyAsync(a => a.Username == usernameChuan))
            return Conflict("Tên đăng nhập này đã có người dùng rồi");

        var account = new BanThuKyAccount
        {
            Id = Guid.NewGuid(),
            Username = usernameChuan,
            TenHienThi = dto.TenHienThi,
            CourtId = dto.CourtId,
        };
        account.PasswordHash = _hasher.HashPassword(account, dto.Password);

        _db.BanThuKyAccounts.Add(account);
        await _db.SaveChangesAsync();
        return Ok(ToDto(account));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, BanThuKyAccountUpdateDto dto)
    {
        var account = await _db.BanThuKyAccounts.FindAsync(id);
        if (account is null) return NotFound();

        account.TenHienThi = dto.TenHienThi;
        account.CourtId = dto.CourtId;
        if (!string.IsNullOrWhiteSpace(dto.PasswordMoi))
            account.PasswordHash = _hasher.HashPassword(account, dto.PasswordMoi);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var account = await _db.BanThuKyAccounts.FindAsync(id);
        if (account is null) return NotFound();

        _db.BanThuKyAccounts.Remove(account);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static BanThuKyAccountDto ToDto(BanThuKyAccount a) => new()
    {
        Id = a.Id,
        Username = a.Username,
        TenHienThi = a.TenHienThi,
        CourtId = a.CourtId,
    };
}
