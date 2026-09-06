using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.NetworkInformation;
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

    // Trạng thái hiện tại CỦA ĐÚNG SÂN NÀY — cho frontend biết để hiện
    // đúng nút "Mở" hay "Đóng" của sân đang xem. Không cần đăng nhập vì
    // chỉ đọc, không đổi gì cả.
    //
    // coTheDungMayChu: request này có phải gọi từ ĐÚNG cái máy đang chạy
    // backend không — nút "Mở màn hình công khai (máy chủ)" (mở kiosk
    // TRÊN MÁY CHỦ) chỉ có tác dụng nếu BTC đang ngồi đúng máy đó; ngồi
    // máy khác (setup nhiều sân, mỗi sân 1 máy riêng cùng trỏ về 1
    // backend) thì nút đó vô dụng — frontend dựa vào cờ này để chỉ hiện
    // đúng 1 trong 2 lựa chọn, không hiện cả 2 gây rối.
    [HttpGet("trang-thai")]
    public IActionResult TrangThai([FromQuery] string san)
    {
        if (string.IsNullOrWhiteSpace(san))
        {
            return BadRequest(new { message = "Thiếu sân cần kiểm tra." });
        }
        return Ok(new
        {
            dangChay = _launcher.DangChay(san),
            coTheDungMayChu = DangGoiTuMayChu(HttpContext.Connection.RemoteIpAddress),
        });
    }

    // So địa chỉ IP thật của request đang gọi lên với chính máy đang
    // chạy backend — KHÔNG dựa vào Request.Host (đó là tên/IP người
    // dùng GÕ vào thanh địa chỉ, tự sửa được, không đáng tin cho việc
    // kiểm tra này).
    private static bool DangGoiTuMayChu(IPAddress? diaChiGoiLen)
    {
        if (diaChiGoiLen == null) return false;
        if (IPAddress.IsLoopback(diaChiGoiLen)) return true;

        // BTC ngồi ĐÚNG máy chủ vẫn có thể gõ địa chỉ LAN của máy đó
        // (VD 192.168.0.3) thay vì "localhost" — so thêm với TOÀN BỘ
        // địa chỉ IP thật gán trên MỌI card mạng của máy này (Wi-Fi,
        // Ethernet, VPN ảo...) để nhận diện đúng cả trường hợp đó.
        //
        // Đọc trực tiếp từ từng card mạng (NetworkInterface) thay vì
        // tra qua DNS theo hostname máy — cách tra DNS cũ không đáng tin
        // 100% trên mọi máy (phụ thuộc cấu hình DNS nội bộ, có thể bỏ
        // sót IP của 1 số card mạng phụ/ảo không đăng ký theo hostname).
        var quyDoi = diaChiGoiLen.IsIPv4MappedToIPv6 ? diaChiGoiLen.MapToIPv4() : diaChiGoiLen;
        try
        {
            foreach (var card in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var ip in card.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.Equals(quyDoi) || ip.Address.Equals(diaChiGoiLen))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Không tra được danh sách IP của máy (hiếm gặp) — coi như
            // không chắc chắn, để frontend rơi về lựa chọn an toàn hơn
            // (mở tab thường, luôn hoạt động bất kể đang ở máy nào).
            return false;
        }

        // Chạy hết mọi card mạng mà không khớp IP nào — không phải máy
        // chủ.
        return false;
    }

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
        var (thanhCong, thongBao) = _launcher.Mo(req.San, url);
        return thanhCong
            ? Ok(new { message = thongBao, dangChay = _launcher.DangChay(req.San) })
            : BadRequest(new { message = thongBao, dangChay = _launcher.DangChay(req.San) });
    }

    [Authorize(Roles = "Admin,BanThuKy")]
    [HttpPost("dong")]
    public IActionResult Dong([FromBody] DongManHinhCongKhaiRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.San))
        {
            return BadRequest(new { message = "Thiếu sân cần đóng." });
        }

        var (thanhCong, thongBao) = _launcher.Dong(req.San);
        return thanhCong
            ? Ok(new { message = thongBao, dangChay = _launcher.DangChay(req.San) })
            : BadRequest(new { message = thongBao, dangChay = _launcher.DangChay(req.San) });
    }
}

public class MoManHinhCongKhaiRequest
{
    public string San { get; set; } = string.Empty;
}

public class DongManHinhCongKhaiRequest
{
    public string San { get; set; } = string.Empty;
}
