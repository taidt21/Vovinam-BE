using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace VovinamApi.Services;

public class LiveCourtStateStore
{
    public class CourtState
    {
        public JsonNode? MatchState { get; set; }
        public JsonNode? QuyenState { get; set; }
        public string? ActiveMode { get; set; } // "doi_khang" | "quyen" | null — tab BTK đang mở cho sân này
        // BTC bấm "X" gỡ trận/lượt đang chờ ở đúng 1 bên (đối kháng hoặc
        // quyền) — tạm ngưng tự động nhận trận/lượt kế tiếp cho ĐÚNG bên
        // đó, bên còn lại không ảnh hưởng.
        public bool DangNghiDoiKhang { get; set; }
        public bool DangNghiQuyen { get; set; }
        public ConcurrentDictionary<string, JsonNode> RefereeScores { get; } = new();
    }

    public class LogEntry
    {
        public DateTimeOffset Luc { get; set; }
        public string NoiDung { get; set; } = "";
        public string? MatchTimeLabel { get; set; } // "Hiệp 1 - 00:30" — null nếu không tính được
    }

    private readonly ConcurrentDictionary<string, CourtState> _courts = new();
    private readonly ConcurrentDictionary<string, List<LogEntry>> _logs = new();
    private CourtState GetOrCreate(string courtId) => _courts.GetOrAdd(courtId, _ => new CourtState());

    // Khoá theo từng sân — dùng khi 1 thao tác cần đọc-sửa-ghi MatchState
    // (hoặc phát broadcast dựa trên state đó) mà không được để thao tác
    // khác trên ĐÚNG sân này chen vào giữa chừng. Ví dụ: 2 màu cùng đạt
    // đồng thuận gần như đồng thời trong PressLight — nếu không khoá, 2
    // task có thể cùng sửa chung 1 JsonNode cùng lúc, dễ lỗi/ném exception.
    // Sân khác nhau dùng khoá khác nhau, không ảnh hưởng lẫn nhau.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _courtLocks = new();

    public async Task WithCourtLockAsync(string courtId, Func<Task> action)
    {
        var sem = _courtLocks.GetOrAdd(courtId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            sem.Release();
        }
    }

    public object GetSnapshot(string courtId)
    {
        var s = GetOrCreate(courtId);
        return new
        {
            matchState = s.MatchState,
            quyenState = s.QuyenState,
            activeMode = s.ActiveMode,
            dangNghiDoiKhang = s.DangNghiDoiKhang,
            dangNghiQuyen = s.DangNghiQuyen,
            refereeScores = s.RefereeScores.Values.ToList(),
            log = GetLog(courtId),
        };
    }

    public void SetActiveMode(string courtId, string? mode) => GetOrCreate(courtId).ActiveMode = mode;
    public string? GetActiveMode(string courtId) => GetOrCreate(courtId).ActiveMode;

    public void SetDangNghiDoiKhang(string courtId, bool val) => GetOrCreate(courtId).DangNghiDoiKhang = val;
    public bool GetDangNghiDoiKhang(string courtId) => GetOrCreate(courtId).DangNghiDoiKhang;
    public void SetDangNghiQuyen(string courtId, bool val) => GetOrCreate(courtId).DangNghiQuyen = val;
    public bool GetDangNghiQuyen(string courtId) => GetOrCreate(courtId).DangNghiQuyen;

    public void SetMatchState(string courtId, JsonNode matchState)
    {
        var s = GetOrCreate(courtId);
        s.MatchState = matchState;
        s.QuyenState = null; // 1 sân không thể vừa đối kháng vừa quyền cùng lúc
    }
    public JsonNode? GetMatchState(string courtId) => GetOrCreate(courtId).MatchState;

    // Trạng thái sống của quyền — TÁCH RIÊNG khỏi MatchState (đối kháng),
    // vì 1 khu vực có thể lần lượt dùng cho cả 2 mục đích khác nhau tuỳ
    // lịch, không nên lẫn vào chung 1 chỗ.
    public void SetQuyenState(string courtId, JsonNode quyenState)
    {
        var s = GetOrCreate(courtId);
        s.QuyenState = quyenState;
        s.MatchState = null; // ditto — ngược lại
    }
    public JsonNode? GetQuyenState(string courtId) => GetOrCreate(courtId).QuyenState;
    public void ClearQuyenState(string courtId) => GetOrCreate(courtId).QuyenState = null;

    public void ClearMatchState(string courtId)
    {
        var s = GetOrCreate(courtId);
        s.MatchState = null;
        s.RefereeScores.Clear();
        _pressWindows.Keys.Where(k => k.StartsWith(courtId + "::")).ToList()
            .ForEach(k => _pressWindows.TryRemove(k, out _));
        _logs.TryRemove(courtId, out _);
    }

    public void SetRefereeScore(string courtId, string giamDinhId, JsonNode score) =>
        GetOrCreate(courtId).RefereeScores[giamDinhId] = score;
    public void RemoveRefereeScore(string courtId, string giamDinhId) =>
        GetOrCreate(courtId).RefereeScores.TryRemove(giamDinhId, out _);

    // ===== Log thời gian thực =====
    public List<LogEntry> GetLog(string courtId) => _logs.GetOrAdd(courtId, _ => new List<LogEntry>());

    public LogEntry AddLog(string courtId, string noiDung)
    {
        var entry = new LogEntry
        {
            Luc = DateTimeOffset.UtcNow,
            NoiDung = noiDung,
            MatchTimeLabel = ComputeMatchTimeLabel(courtId),
        };
        var log = _logs.GetOrAdd(courtId, _ => new List<LogEntry>());
        lock (log)
        {
            log.Add(entry);
            if (log.Count > 300) log.RemoveAt(0); // chặn phình vô hạn nếu quên clear giữa các trận
        }
        return entry;
    }

    // Tính đúng "còn bao nhiêu giây" tại THỜI ĐIỂM GỌI HÀM NÀY — dùng
    // chung cho cả việc ghi log (Hiệp mấy - phút:giây) lẫn việc chặn bấm
    // đèn khi hết giờ. Chỉ "dang_thi"/"nghi_giua_hiep" mới cần trừ thời
    // gian đã trôi qua kể từ capNhatDongHoLuc; các trạng thái khác (tạm
    // dừng, chờ bắt đầu...) thì số đã lưu sẵn chính là đúng, không trừ gì
    // thêm — khớp đúng logic tinhThoiGianConLai() bên frontend.
    public double? ComputeRemainingSeconds(string courtId)
    {
        if (!_courts.TryGetValue(courtId, out var state) || state.MatchState == null) return null;
        var ms = state.MatchState;

        var trangThaiNode = ms["trangThai"];
        var conLaiNode = ms["thoiGianConLaiGiay"];
        var capNhatNode = ms["capNhatDongHoLuc"];
        if (trangThaiNode == null || conLaiNode == null || capNhatNode == null) return null;

        try
        {
            var trangThai = trangThaiNode.GetValue<string>();
            var conLai = conLaiNode.GetValue<double>();
            var capNhat = capNhatNode.GetValue<long>();

            if (trangThai != "dang_thi" && trangThai != "nghi_giua_hiep") return conLai;

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var elapsed = (nowMs - capNhat) / 1000.0;
            return Math.Max(0, conLai - elapsed);
        }
        catch
        {
            return null;
        }
    }

    // MM:SS ở đây là số giây CÒN LẠI — đúng số mà đồng hồ đếm ngược trên
    // màn hình đang hiện tại đúng thời điểm ghi log, không phải số giây
    // đã trôi từ đầu hiệp.
    private string? ComputeMatchTimeLabel(string courtId)
    {
        if (!_courts.TryGetValue(courtId, out var state) || state.MatchState == null) return null;
        var ms = state.MatchState;

        var hiepNode = ms["hiepHienTai"];
        if (hiepNode == null) return null;

        var currentRemaining = ComputeRemainingSeconds(courtId);
        if (currentRemaining == null) return null;

        try
        {
            var hiep = hiepNode.GetValue<int>();
            var totalSeconds = (int)Math.Round(currentRemaining.Value);
            var mm = totalSeconds / 60;
            var ss = totalSeconds % 60;
            return $"Hiệp {hiep} - {mm:D2}:{ss:D2}";
        }
        catch
        {
            return null;
        }
    }

    // ===== Bấm đèn đối kháng =====
    private class PressRecord
    {
        public string GiamDinhId = "";
        public int Diem;
        public DateTimeOffset Luc;
    }

    // Gộp theo MÀU (không tách theo mức điểm nữa) — đủ 3 người đồng ý
    // cùng màu trong cửa sổ thời gian (đọc từ Tournament.CuaSoDongThuanGiay,
    // BTC tự chỉnh ở Thiết lập giải — trước đây đóng cứng 1.5s ở đây) là
    // ĐỦ ĐIỀU KIỆN, mức điểm thật (1 hay 2) quyết định bằng đa số NGAY
    // TRONG đúng 3 người đó. Luôn chốt lại đúng lúc vừa đủ 3 (không đợi
    // thêm người thứ 4/5), nên nhóm xét luôn có số lẻ — luôn ra đa số rõ
    // ràng, không thể hòa.
    private const int ConsensusThreshold = 3;
    private readonly ConcurrentDictionary<string, List<PressRecord>> _pressWindows = new();

    public (int diem, int soLuong)? RegisterPressAndCheckConsensus(
        string courtId, string mau, int diem, string giamDinhId, DateTimeOffset luc, double cuaSoDongThuanGiay)
    {
        var consensusWindow = TimeSpan.FromSeconds(cuaSoDongThuanGiay);
        var bucketKey = $"{courtId}::{mau}";
        var list = _pressWindows.GetOrAdd(bucketKey, _ => new List<PressRecord>());
        lock (list)
        {
            list.RemoveAll(p => luc - p.Luc > consensusWindow);
            list.RemoveAll(p => p.GiamDinhId == giamDinhId); // 1 trọng tài chỉ tính lần bấm gần nhất trong cửa sổ
            list.Add(new PressRecord { GiamDinhId = giamDinhId, Diem = diem, Luc = luc });

            if (list.Count >= ConsensusThreshold)
            {
                var group = list.Take(ConsensusThreshold).ToList();
                var diemDaSo = group.GroupBy(p => p.Diem).OrderByDescending(g => g.Count()).First().Key;
                list.Clear(); // dùng xong đợt này — đếm lại từ đầu cho pha ra đòn kế tiếp
                return (diemDaSo, group.Count);
            }
        }
        return null;
    }
}