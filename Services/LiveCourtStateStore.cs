using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace VovinamApi.Services;

public class LiveCourtStateStore
{
    public class CourtState
    {
        public JsonNode? MatchState { get; set; }
        public JsonNode? QuyenState { get; set; }
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

    public object GetSnapshot(string courtId)
    {
        var s = GetOrCreate(courtId);
        return new
        {
            matchState = s.MatchState,
            quyenState = s.QuyenState,
            refereeScores = s.RefereeScores.Values.ToList(),
            log = GetLog(courtId),
        };
    }

    public void SetMatchState(string courtId, JsonNode matchState) => GetOrCreate(courtId).MatchState = matchState;
    public JsonNode? GetMatchState(string courtId) => GetOrCreate(courtId).MatchState;

    // Trạng thái sống của quyền — TÁCH RIÊNG khỏi MatchState (đối kháng),
    // vì 1 khu vực có thể lần lượt dùng cho cả 2 mục đích khác nhau tuỳ
    // lịch, không nên lẫn vào chung 1 chỗ.
    public void SetQuyenState(string courtId, JsonNode quyenState) => GetOrCreate(courtId).QuyenState = quyenState;
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

    private string? ComputeMatchTimeLabel(string courtId)
    {
        if (!_courts.TryGetValue(courtId, out var state) || state.MatchState == null) return null;
        var ms = state.MatchState;

        var hiepNode = ms["hiepHienTai"];
        var thoiGianHiepNode = ms["thoiGianHiepGiay"];
        if (hiepNode == null || thoiGianHiepNode == null) return null;

        var currentRemaining = ComputeRemainingSeconds(courtId);
        if (currentRemaining == null) return null;

        try
        {
            var hiep = hiepNode.GetValue<int>();
            var thoiGianHiep = thoiGianHiepNode.GetValue<double>();
            var elapsedInRound = Math.Max(0, thoiGianHiep - currentRemaining.Value);

            var totalSeconds = (int)Math.Round(elapsedInRound);
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
    // cùng màu trong cửa sổ 1.5s là ĐỦ ĐIỀU KIỆN, mức điểm thật (1 hay 2)
    // quyết định bằng đa số NGAY TRONG đúng 3 người đó. Luôn chốt lại
    // đúng lúc vừa đủ 3 (không đợi thêm người thứ 4/5), nên nhóm xét luôn
    // có số lẻ — luôn ra đa số rõ ràng, không thể hòa.
    private static readonly TimeSpan ConsensusWindow = TimeSpan.FromSeconds(1.5);
    private const int ConsensusThreshold = 3;
    private readonly ConcurrentDictionary<string, List<PressRecord>> _pressWindows = new();

    public (int diem, int soLuong)? RegisterPressAndCheckConsensus(
        string courtId, string mau, int diem, string giamDinhId, DateTimeOffset luc)
    {
        var bucketKey = $"{courtId}::{mau}";
        var list = _pressWindows.GetOrAdd(bucketKey, _ => new List<PressRecord>());
        lock (list)
        {
            list.RemoveAll(p => luc - p.Luc > ConsensusWindow);
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