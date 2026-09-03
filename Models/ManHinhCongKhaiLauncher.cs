using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace VovinamApi.Services;

// Toàn bộ việc "tìm màn hình phụ + mở trình duyệt kiosk + theo dõi/đóng
// đúng đúng tiến trình đó" nằm ở ĐÂY (backend, chạy như 1 tiến trình
// Windows thật) — KHÔNG làm ở frontend/JS nữa, vì trình duyệt (nơi
// frontend đang chạy) cố tình khoá chặt quyền enum màn hình + điều
// khiển cửa sổ của chính nó, còn 1 tiến trình .NET native thì không bị
// giới hạn đó, gọi thẳng Win32 API được.
public class ManHinhCongKhaiLauncher
{
    private readonly ILogger<ManHinhCongKhaiLauncher> _logger;
    private Process? _tienTrinh;
    private readonly object _khoa = new();

    // _tienTrinh ở trên chỉ sống trong bộ nhớ của ĐÚNG LẦN CHẠY backend
    // hiện tại — nếu backend từng bị tắt/khởi động lại (rất hay xảy ra
    // lúc đang test) trong khi cửa sổ kiosk CŨ vẫn còn sống, backend MỚI
    // khởi động lại sẽ có _tienTrinh = null, HOÀN TOÀN không biết cửa sổ
    // cũ đó tồn tại — "Mở" sẽ mở thêm 1 cửa sổ MỚI chồng lên, cửa sổ cũ
    // "mồ côi" không ai đóng được nữa, và cửa sổ đang hiện ra trước mắt
    // rất có thể lại là cửa sổ CŨ (che khuất/đứng trước cửa sổ mới).
    // Ghi PID ra 1 file cạnh profile kiosk để BẤT KỲ lần chạy backend
    // nào (kể cả sau khi restart) cũng đọc lại được, tìm và đóng đúng
    // tiến trình cũ trước khi mở tiến trình mới.
    private static string DuongDanFilePid =>
        Path.Combine(AppContext.BaseDirectory, "kiosk-profile", "kiosk.pid");

    public ManHinhCongKhaiLauncher(ILogger<ManHinhCongKhaiLauncher> logger)
    {
        _logger = logger;
    }

    // Coi là "đang chạy" khi tiến trình mình từng mở vẫn còn sống —
    // nếu user tự tay đóng cửa sổ đó (Alt+F4...) thì HasExited tự lên
    // true, lần bấm "Mở" tiếp theo sẽ coi như chưa có gì, mở lại bình
    // thường. KHÔNG có cơ chế nào coi 1 process TRÌNH DUYỆT KHÁC (do
    // user tự mở tay) là "đang chạy" — chỉ theo dõi đúng process do
    // chính hàm Mo() bên dưới tạo ra.
    public bool DangChay
    {
        get
        {
            lock (_khoa)
            {
                return _tienTrinh != null && !_tienTrinh.HasExited;
            }
        }
    }

    // Đóng đúng tiến trình đã lưu PID trong file (nếu có và nếu nó
    // TRÙNG ĐÚNG tên trình duyệt — tránh trường hợp cực hiếm PID cũ đã
    // bị hệ điều hành cấp phát lại cho 1 chương trình khác hoàn toàn
    // không liên quan). Không ném lỗi nếu không tìm thấy — bình thường
    // (đã đóng từ trước, hoặc PID không còn hợp lệ).
    private void DongTienTrinhMoCoi()
    {
        if (!File.Exists(DuongDanFilePid)) return;
        try
        {
            var noiDung = File.ReadAllText(DuongDanFilePid).Trim();
            if (int.TryParse(noiDung, out var pidCu))
            {
                var p = Process.GetProcessById(pidCu);
                if (!p.HasExited &&
                    (p.ProcessName.Equals("chrome", StringComparison.OrdinalIgnoreCase) ||
                     p.ProcessName.Equals("msedge", StringComparison.OrdinalIgnoreCase)))
                {
                    p.Kill(entireProcessTree: true);
                    _logger.LogInformation("Đã đóng tiến trình kiosk mồ côi (PID {Pid}) từ lần chạy backend trước", pidCu);
                }
            }
        }
        catch (ArgumentException)
        {
            // Process.GetProcessById ném lỗi này khi PID không còn tồn
            // tại — nghĩa là tiến trình đó đã tự đóng từ trước, bỏ qua.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Đóng tiến trình kiosk mồ côi thất bại — vẫn thử mở cửa sổ mới");
        }
    }

    [SupportedOSPlatform("windows")]
    public (bool ThanhCong, string ThongBao) Mo(string url)
    {
        lock (_khoa)
        {
            // TRƯỚC ĐÂY: đã mở sẵn thì coi như xong việc, không làm gì
            // thêm — nghe hợp lý (khỏi mở trùng cửa sổ), NHƯNG lại vô
            // tình khiến "Mở" mất tác dụng nếu có 1 cửa sổ CŨ (build cũ)
            // vẫn đang chạy từ trước — bấm "Mở" chẳng có gì xảy ra, vẫn
            // nhìn thấy đúng cửa sổ cũ, hiểu lầm là "chưa cập nhật giao
            // diện" dù backend đã build đúng bản mới. Giờ ĐÓNG cửa sổ cũ
            // (nếu có) rồi LUÔN mở lại 1 cửa sổ MỚI — bấm "Mở" là chắc
            // chắn có cửa sổ mới, tải lại từ đầu, không bao giờ bị kẹt ở
            // bản cũ nữa.
            if (_tienTrinh != null && !_tienTrinh.HasExited)
            {
                try
                {
                    _tienTrinh.Kill(entireProcessTree: true);
                    _tienTrinh.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Đóng cửa sổ cũ trước khi mở lại thất bại — vẫn thử mở cửa sổ mới");
                }
                _tienTrinh = null;
            }

            // Bù cho trường hợp backend ĐÃ TỪNG restart kể từ lần mở
            // trước — _tienTrinh ở trên chỉ biết trong PHẠM VI lần chạy
            // backend HIỆN TẠI, còn hàm này đọc lại PID đã lưu ra ĐĨA từ
            // TRƯỚC ĐÓ (có thể từ 1 lần chạy backend đã kết thúc).
            DongTienTrinhMoCoi();

            if (!OperatingSystem.IsWindows())
            {
                return (false, "Tính năng này chỉ hỗ trợ Windows.");
            }

            var trinhDuyet = TimTrinhDuyet();
            if (trinhDuyet == null)
            {
                return (false, "Không tìm thấy Chrome hoặc Edge đã cài trên máy này.");
            }

            var manHinh = ChonManHinhDich();

            // Profile riêng, KHÔNG đụng gì tới profile Chrome/Edge cá
            // nhân của người dùng (lịch sử, đăng nhập, extension...) —
            // nằm cạnh chỗ chạy .exe, tự tạo nếu chưa có.
            var thuMucProfile = Path.Combine(AppContext.BaseDirectory, "kiosk-profile");
            Directory.CreateDirectory(thuMucProfile);

            // Thêm tham số vô hại vào cuối URL, đổi giá trị mỗi lần mở —
            // buộc trình duyệt coi đây là 1 địa chỉ MỚI, không lấy lại
            // trang đã cache từ lần mở trước trong CÙNG profile kiosk
            // này (dù đã đóng+mở cửa sổ mới ở trên, HTML gốc vẫn có thể
            // bị cache theo URL nếu server không set header chặn cache).
            var urlKhongCache = url + (url.Contains('?') ? "&" : "?") + "_t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var psi = new ProcessStartInfo
            {
                FileName = trinhDuyet.DuongDan,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("--kiosk");
            psi.ArgumentList.Add(urlKhongCache);
            psi.ArgumentList.Add($"--window-position={manHinh.X},{manHinh.Y}");
            psi.ArgumentList.Add($"--user-data-dir={thuMucProfile}");
            psi.ArgumentList.Add("--no-first-run");
            psi.ArgumentList.Add("--noerrdialogs");
            // Màn hình công khai có tiếng chuông báo hiệp — cửa sổ kiosk
            // này là 1 profile HOÀN TOÀN MỚI, chưa từng có click/gõ phím
            // nào bên trong, nên mặc định trình duyệt có thể chặn phát
            // âm thanh (chính sách autoplay). Cờ này bỏ hẳn yêu cầu đó,
            // chỉ áp dụng cho đúng cửa sổ kiosk riêng biệt này.
            psi.ArgumentList.Add("--autoplay-policy=no-user-gesture-required");
            if (trinhDuyet.LaEdge)
            {
                // Riêng Edge cần thêm cờ này thì --kiosk mới thật sự full
                // màn hình (Chrome không có/không cần cờ tương đương).
                psi.ArgumentList.Add("--edge-kiosk-type=fullscreen");
            }

            try
            {
                var p = Process.Start(psi);
                if (p == null)
                {
                    return (false, "Không khởi động được trình duyệt.");
                }
                _tienTrinh = p;
                try
                {
                    File.WriteAllText(DuongDanFilePid, p.Id.ToString());
                }
                catch (Exception ex)
                {
                    // Ghi file thất bại (VD ổ đĩa readonly) không được
                    // chặn mất việc đã mở màn hình thành công — chỉ mất
                    // đi khả năng tự dọn nếu lỡ backend restart sau này.
                    _logger.LogWarning(ex, "Không ghi được file PID kiosk");
                }
                _logger.LogInformation(
                    "Đã mở màn hình công khai (PID {Pid}) tại màn hình x={X},y={Y}",
                    p.Id, manHinh.X, manHinh.Y);
                return (true, manHinh.LaManHinhPhu
                    ? "Đã mở màn hình công khai ở màn hình mở rộng."
                    : "Đã mở màn hình công khai (chỉ phát hiện 1 màn hình, mở tại màn hình chính).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mở màn hình công khai thất bại");
                return (false, $"Mở thất bại: {ex.Message}");
            }
        }
    }

    public (bool ThanhCong, string ThongBao) Dong()
    {
        lock (_khoa)
        {
            if (_tienTrinh == null || _tienTrinh.HasExited)
            {
                _tienTrinh = null;
                // Backend có thể đã restart kể từ lần mở trước — thử
                // đóng luôn theo PID đã lưu ra đĩa, phòng còn 1 cửa sổ
                // mồ côi mà bộ nhớ hiện tại không biết gì về nó.
                DongTienTrinhMoCoi();
                XoaFilePid();
                return (true, "Không có màn hình công khai nào đang mở.");
            }

            try
            {
                // entireProcessTree: true — Chrome/Edge chạy nhiều tiến
                // trình con (renderer, GPU...), chỉ Kill() đúng 1 PID gốc
                // dễ để sót cửa sổ vẫn còn hiển thị. CHỈ kill đúng cây
                // tiến trình này — không đụng tới trình duyệt khác user
                // đang dùng, vì đây là tiến trình dùng --user-data-dir
                // riêng, hoàn toàn tách biệt.
                _tienTrinh.Kill(entireProcessTree: true);
                _tienTrinh.Dispose();
                _tienTrinh = null;
                XoaFilePid();
                return (true, "Đã đóng màn hình công khai.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đóng màn hình công khai thất bại");
                return (false, $"Đóng thất bại: {ex.Message}");
            }
        }
    }

    private static void XoaFilePid()
    {
        try
        {
            if (File.Exists(DuongDanFilePid)) File.Delete(DuongDanFilePid);
        }
        catch
        {
            // Không xoá được file PID không phải lỗi nghiêm trọng — lần
            // "Mở" kế tiếp vẫn tự phát hiện PID cũ không còn hợp lệ
            // (process đã đóng) và bỏ qua bình thường.
        }
    }

    private sealed record TrinhDuyet(string DuongDan, bool LaEdge);

    // Cố tình dùng đường dẫn cài đặt THÔNG THƯỜNG thay vì đọc registry
    // (App Paths) — tránh phụ thuộc registry theo đúng yêu cầu, đủ dùng
    // cho tuyệt đại đa số máy Windows cài Chrome/Edge kiểu mặc định.
    private static TrinhDuyet? TimTrinhDuyet()
    {
        string[] duongDanChrome =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
        ];
        string[] duongDanEdge =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
        ];

        // Ưu tiên Chrome, không có mới rơi về Edge -- đúng yêu cầu.
        var chrome = duongDanChrome.FirstOrDefault(File.Exists);
        if (chrome != null) return new TrinhDuyet(chrome, LaEdge: false);

        var edge = duongDanEdge.FirstOrDefault(File.Exists);
        if (edge != null) return new TrinhDuyet(edge, LaEdge: true);

        return null;
    }

    private sealed record ManHinhDich(int X, int Y, bool LaManHinhPhu);

    [SupportedOSPlatform("windows")]
    private static ManHinhDich ChonManHinhDich()
    {
        var manHinh = MonitorInterop.LayTatCaManHinh();
        // KHÔNG hardcode "màn phụ nằm bên phải" — lấy đúng màn hình đầu
        // tiên KHÔNG PHẢI primary theo toạ độ Windows đã tự tính (có
        // thể âm, ở trái/trên/dưới tuỳ cách người dùng sắp xếp trong
        // Windows Display Settings).
        var manHinhPhu = manHinh.FirstOrDefault(m => !m.LaPrimary);
        if (manHinhPhu != null)
        {
            return new ManHinhDich(manHinhPhu.X, manHinhPhu.Y, LaManHinhPhu: true);
        }

        // Chỉ có 1 màn hình (không Extend) -- mở tại màn hình chính,
        // không có lựa chọn nào khác.
        var chinh = manHinh.FirstOrDefault(m => m.LaPrimary) ?? manHinh.FirstOrDefault();
        return new ManHinhDich(chinh?.X ?? 0, chinh?.Y ?? 0, LaManHinhPhu: false);
    }
}

// Bọc riêng phần P/Invoke Win32 (EnumDisplayMonitors/GetMonitorInfo) —
// đây là cách chuẩn, không cần System.Windows.Forms (vốn chỉ có ở
// project WinForms) để lấy đúng toạ độ THẬT của từng màn hình đang cắm,
// theo hệ toạ độ "virtual desktop" của Windows (primary luôn ở gốc 0,0,
// màn khác có thể âm nếu đặt bên trái/trên primary).
[SupportedOSPlatform("windows")]
internal static class MonitorInterop
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private const uint MONITORINFOF_PRIMARY = 0x1;

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    public sealed record ManHinhInfo(int X, int Y, int Rong, int Cao, bool LaPrimary);

    public static List<ManHinhInfo> LayTatCaManHinh()
    {
        var ketQua = new List<ManHinhInfo>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcKhongDung, ref RECT rect, IntPtr duLieuKhongDung) =>
        {
            var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(hMonitor, ref info))
            {
                ketQua.Add(new ManHinhInfo(
                    info.rcMonitor.Left,
                    info.rcMonitor.Top,
                    info.rcMonitor.Right - info.rcMonitor.Left,
                    info.rcMonitor.Bottom - info.rcMonitor.Top,
                    (info.dwFlags & MONITORINFOF_PRIMARY) != 0));
            }
            return true;
        }, IntPtr.Zero);
        return ketQua;
    }
}
