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
//
// QUAN TRỌNG — giới hạn không sửa được bằng code: cửa sổ kiosk luôn mở
// TRÊN ĐÚNG CÁI MÁY ĐANG CHẠY BACKEND NÀY, không thể mở hộ trên 1 máy
// khác (VD máy tính riêng của sân 2, nếu 2 sân dùng 2 máy khác nhau
// cùng trỏ về 1 backend chung). Với setup "mỗi sân 1 máy riêng", máy
// của sân đó phải tự mở trình duyệt bình thường tới đúng địa chỉ
// /man-hinh-cong-khai?san=<id>&autoFullscreen=1 — không dùng nút "Mở
// màn hình công khai" (nút đó chỉ có tác dụng nếu backend và màn hình
// cần mở nằm CHUNG 1 máy).
public class ManHinhCongKhaiLauncher
{
    private readonly ILogger<ManHinhCongKhaiLauncher> _logger;
    private readonly object _khoa = new();

    // TRƯỚC ĐÂY: 1 Process duy nhất cho CẢ backend, không phân biệt sân
    // nào — 2 sân cùng dùng chung 1 backend (setup nhiều sân trên cùng 1
    // máy, nhiều màn hình) thì sân này bấm "Mở" sẽ bị hiểu nhầm là "đã
    // mở sẵn rồi", tự đóng mất cửa sổ của sân kia rồi mở đè cửa sổ mới.
    // Giờ theo dõi riêng theo TỪNG courtId — mở/đóng sân này không đụng
    // gì tới sân khác đang mở trên cùng máy.
    private readonly Dictionary<string, (Process TienTrinh, ManHinhDich ManHinh)> _theoSan = new();

    private static string DuongDanFilePid(string courtId) =>
        Path.Combine(AppContext.BaseDirectory, "kiosk-profile", $"kiosk-{SanitizeTenFile(courtId)}.pid");

    // courtId là GUID/id nội bộ nên hầu như luôn an toàn làm tên file
    // sẵn — lọc lại cho chắc, phòng id nào đó lỡ chứa ký tự không hợp lệ
    // trong tên file Windows.
    private static string SanitizeTenFile(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }

    public ManHinhCongKhaiLauncher(ILogger<ManHinhCongKhaiLauncher> logger)
    {
        _logger = logger;
    }

    // Coi là "đang chạy" khi tiến trình của ĐÚNG sân này vẫn còn sống —
    // nếu user tự tay đóng cửa sổ đó (Alt+F4...) thì HasExited tự lên
    // true, lần bấm "Mở" tiếp theo sẽ coi như chưa có gì, mở lại bình
    // thường. KHÔNG có cơ chế nào coi 1 process TRÌNH DUYỆT KHÁC (do
    // user tự mở tay) là "đang chạy" — chỉ theo dõi đúng process do
    // chính hàm Mo() bên dưới tạo ra.
    public bool DangChay(string courtId)
    {
        lock (_khoa)
        {
            return _theoSan.TryGetValue(courtId, out var t) && !t.TienTrinh.HasExited;
        }
    }

    // Đóng đúng tiến trình đã lưu PID trong file của ĐÚNG sân này (nếu
    // có và nếu nó TRÙNG ĐÚNG tên trình duyệt — tránh trường hợp cực
    // hiếm PID cũ đã bị hệ điều hành cấp phát lại cho 1 chương trình
    // khác hoàn toàn không liên quan). Không ném lỗi nếu không tìm
    // thấy — bình thường (đã đóng từ trước, hoặc PID không còn hợp lệ).
    private void DongTienTrinhMoCoi(string courtId)
    {
        var duongDan = DuongDanFilePid(courtId);
        if (!File.Exists(duongDan)) return;
        try
        {
            var noiDung = File.ReadAllText(duongDan).Trim();
            if (int.TryParse(noiDung, out var pidCu))
            {
                var p = Process.GetProcessById(pidCu);
                if (!p.HasExited &&
                    (p.ProcessName.Equals("chrome", StringComparison.OrdinalIgnoreCase) ||
                     p.ProcessName.Equals("msedge", StringComparison.OrdinalIgnoreCase)))
                {
                    p.Kill(entireProcessTree: true);
                    _logger.LogInformation("Đã đóng tiến trình kiosk mồ côi (PID {Pid}) của sân {CourtId} từ lần chạy backend trước", pidCu, courtId);
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
            _logger.LogWarning(ex, "Đóng tiến trình kiosk mồ côi (sân {CourtId}) thất bại — vẫn thử mở cửa sổ mới", courtId);
        }
    }

    [SupportedOSPlatform("windows")]
    public (bool ThanhCong, string ThongBao) Mo(string courtId, string url)
    {
        lock (_khoa)
        {
            if (_theoSan.TryGetValue(courtId, out var cu) && !cu.TienTrinh.HasExited)
            {
                try
                {
                    cu.TienTrinh.Kill(entireProcessTree: true);
                    cu.TienTrinh.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Đóng cửa sổ cũ của sân {CourtId} trước khi mở lại thất bại — vẫn thử mở cửa sổ mới", courtId);
                }
                _theoSan.Remove(courtId);
            }

            // Bù cho trường hợp backend ĐÃ TỪNG restart kể từ lần mở
            // trước — bộ nhớ ở trên chỉ biết trong PHẠM VI lần chạy
            // backend HIỆN TẠI, còn hàm này đọc lại PID đã lưu ra ĐĨA từ
            // TRƯỚC ĐÓ (có thể từ 1 lần chạy backend đã kết thúc).
            DongTienTrinhMoCoi(courtId);

            if (!OperatingSystem.IsWindows())
            {
                return (false, "Tính năng này chỉ hỗ trợ Windows.");
            }

            var trinhDuyet = TimTrinhDuyet();
            if (trinhDuyet == null)
            {
                return (false, "Không tìm thấy Chrome hoặc Edge đã cài trên máy này.");
            }

            // Né các màn hình sân KHÁC đang dùng (nếu nhiều sân cùng
            // chạy trên đúng 1 máy này) — không mở chồng lên nhau.
            var dangDung = _theoSan
                .Where(kv => !kv.Value.TienTrinh.HasExited)
                .Select(kv => (kv.Value.ManHinh.X, kv.Value.ManHinh.Y))
                .ToHashSet();
            var manHinh = ChonManHinhDich(dangDung);

            // Profile riêng THEO TỪNG SÂN — không dùng chung 1 profile
            // cho nhiều sân (2 cửa sổ cùng --user-data-dir sẽ xung đột,
            // Chrome/Edge không cho 2 tiến trình cùng dùng 1 profile
            // cùng lúc).
            var thuMucProfile = Path.Combine(AppContext.BaseDirectory, "kiosk-profile", SanitizeTenFile(courtId));
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
                _theoSan[courtId] = (p, manHinh);
                try
                {
                    File.WriteAllText(DuongDanFilePid(courtId), p.Id.ToString());
                }
                catch (Exception ex)
                {
                    // Ghi file thất bại (VD ổ đĩa readonly) không được
                    // chặn mất việc đã mở màn hình thành công — chỉ mất
                    // đi khả năng tự dọn nếu lỡ backend restart sau này.
                    _logger.LogWarning(ex, "Không ghi được file PID kiosk cho sân {CourtId}", courtId);
                }
                _logger.LogInformation(
                    "Đã mở màn hình công khai cho sân {CourtId} (PID {Pid}) tại màn hình x={X},y={Y}",
                    courtId, p.Id, manHinh.X, manHinh.Y);
                return (true, manHinh.LaManHinhPhu
                    ? "Đã mở màn hình công khai ở màn hình mở rộng."
                    : "Đã mở màn hình công khai (không còn màn hình mở rộng trống nào khác, mở tại màn hình chính).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mở màn hình công khai cho sân {CourtId} thất bại", courtId);
                return (false, $"Mở thất bại: {ex.Message}");
            }
        }
    }

    public (bool ThanhCong, string ThongBao) Dong(string courtId)
    {
        lock (_khoa)
        {
            if (!_theoSan.TryGetValue(courtId, out var t) || t.TienTrinh.HasExited)
            {
                _theoSan.Remove(courtId);
                // Backend có thể đã restart kể từ lần mở trước — thử
                // đóng luôn theo PID đã lưu ra đĩa, phòng còn 1 cửa sổ
                // mồ côi mà bộ nhớ hiện tại không biết gì về nó.
                DongTienTrinhMoCoi(courtId);
                XoaFilePid(courtId);
                return (true, "Không có màn hình công khai nào đang mở cho sân này.");
            }

            try
            {
                // entireProcessTree: true — Chrome/Edge chạy nhiều tiến
                // trình con (renderer, GPU...), chỉ Kill() đúng 1 PID gốc
                // dễ để sót cửa sổ vẫn còn hiển thị. CHỈ kill đúng cây
                // tiến trình này — không đụng tới trình duyệt khác user
                // đang dùng (kể cả kiosk của SÂN KHÁC), vì mỗi sân dùng
                // đúng 1 --user-data-dir riêng, hoàn toàn tách biệt.
                t.TienTrinh.Kill(entireProcessTree: true);
                t.TienTrinh.Dispose();
                _theoSan.Remove(courtId);
                XoaFilePid(courtId);
                return (true, "Đã đóng màn hình công khai.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đóng màn hình công khai cho sân {CourtId} thất bại", courtId);
                return (false, $"Đóng thất bại: {ex.Message}");
            }
        }
    }

    private static void XoaFilePid(string courtId)
    {
        try
        {
            var duongDan = DuongDanFilePid(courtId);
            if (File.Exists(duongDan)) File.Delete(duongDan);
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

    // Nhận thêm danh sách toạ độ CÁC MÀN HÌNH ĐÃ CÓ SÂN KHÁC ĐANG DÙNG —
    // né ra, chọn màn phụ TRỐNG tiếp theo nếu máy này có nhiều hơn 1 màn
    // phụ (VD 1 máy cắm 3 màn hình để chạy cùng lúc 2 sân). Hết màn phụ
    // trống thì đành dùng lại màn chính — còn hơn không mở được gì.
    [SupportedOSPlatform("windows")]
    private static ManHinhDich ChonManHinhDich(HashSet<(int X, int Y)> dangDung)
    {
        var manHinh = MonitorInterop.LayTatCaManHinh();
        var manHinhPhu = manHinh.Where(m => !m.LaPrimary).ToList();

        // KHÔNG hardcode "màn phụ nằm bên phải" — lấy đúng màn hình đầu
        // tiên KHÔNG PHẢI primary và CHƯA sân nào khác đang dùng, theo
        // toạ độ Windows đã tự tính (có thể âm, ở trái/trên/dưới tuỳ
        // cách người dùng sắp xếp trong Windows Display Settings).
        var manTrong = manHinhPhu.FirstOrDefault(m => !dangDung.Contains((m.X, m.Y)));
        if (manTrong != null)
        {
            return new ManHinhDich(manTrong.X, manTrong.Y, LaManHinhPhu: true);
        }

        // Không còn màn phụ nào trống (hết màn, hoặc chỉ có 1 màn phụ mà
        // sân khác đã chiếm) -- mở tại màn hình chính, không có lựa chọn
        // nào khác. Nhiều sân cùng rơi vào đây sẽ chồng cửa sổ lên nhau
        // trên đúng màn chính — đây là giới hạn phần cứng thật (không đủ
        // màn hình vật lý), không phải lỗi phần mềm.
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
