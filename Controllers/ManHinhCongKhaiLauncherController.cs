using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Versioning;
using VovinamApi.Services;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/man-hinh-cong-khai-launcher")]
public class ManHinhCongKhaiLauncherController : ControllerBase
{
    private readonly ManHinhCongKhaiLauncher _launcher;

    public ManHinhCongKhaiLauncherController(ManHinhCongKhaiLauncher launcher)
    {
        _launcher = launcher;
    }

    // Trạng thái hiện tại — cho frontend biết để hiện đúng nút "Mở" hay
    // "Đóng". Không cần đăng nhập vì chỉ đọc, không đổi gì cả.
    [HttpGet("trang-thai")]
    public IActionResult TrangThai() => Ok(new { dangChay = _launcher.DangChay });

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPost("mo")]
    [SupportedOSPlatform("windows")]
    public IActionResult Mo([FromBody] MoManHinhCongKhaiRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.San))
        {
            return BadRequest(new { message = "Thiếu sân cần hiện." });
        }

        // Dùng đúng scheme/host của chính request đang gọi lên (khớp
        // đúng IP/cổng backend hiện tại) thay vì tự đoán — luôn đúng dù
        // sau này đổi cổng hay đổi IP máy chủ.
        var url = $"{Request.Scheme}://{Request.Host}/man-hinh-cong-khai?san={Uri.EscapeDataString(req.San)}";
        var (thanhCong, thongBao) = _launcher.Mo(url);
        return thanhCong
            ? Ok(new { message = thongBao, dangChay = _launcher.DangChay })
            : BadRequest(new { message = thongBao, dangChay = _launcher.DangChay });
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPost("dong")]
    public IActionResult Dong()
    {
        var (thanhCong, thongBao) = _launcher.Dong();
        return thanhCong
            ? Ok(new { message = thongBao, dangChay = _launcher.DangChay })
            : BadRequest(new { message = thongBao, dangChay = _launcher.DangChay });
    }
}

public class MoManHinhCongKhaiRequest
{
    public string San { get; set; } = string.Empty;
}
