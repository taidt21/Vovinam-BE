using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.Json.Nodes;
using VovinamApi.Services;

namespace VovinamApi.Hubs;

public class MatchHub : Hub
{
    private readonly LiveCourtStateStore _store;

    public MatchHub(LiveCourtStateStore store)
    {
        _store = store;
    }

    public async Task JoinCourt(string courtId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(courtId));
        await Clients.Caller.SendAsync("CourtSnapshot", courtId, _store.GetSnapshot(courtId));
    }

    public async Task PublishMatchState(string courtId, JsonElement matchState)
    {
        var node = JsonNode.Parse(matchState.GetRawText());
        if (node != null) _store.SetMatchState(courtId, node);
        await Clients.OthersInGroup(GroupName(courtId)).SendAsync("MatchStateUpdated", courtId, matchState);
    }

    public async Task ClearMatchState(string courtId)
    {
        _store.ClearMatchState(courtId);
        await Clients.OthersInGroup(GroupName(courtId)).SendAsync("MatchStateCleared", courtId);
    }
    public async Task PublishQuyenState(string courtId, JsonElement quyenState)
    {
        var node = JsonNode.Parse(quyenState.GetRawText());
        if (node != null) _store.SetQuyenState(courtId, node);
        await Clients.OthersInGroup(GroupName(courtId)).SendAsync("QuyenStateUpdated", courtId, quyenState);
    }

    public async Task ClearQuyenState(string courtId)
    {
        _store.ClearQuyenState(courtId);
        await Clients.OthersInGroup(GroupName(courtId)).SendAsync("QuyenStateCleared", courtId);
    }

    // Tab Bàn thư ký đang mở cho sân này ("doi_khang"/"quyen"/null) — quyết
    // định màn hình trọng tài hiện gì, KHÔNG suy ra từ việc có dữ liệu sống
    // hay không (dữ liệu có thể chưa kịp tạo dù đã chọn đúng tab).
    public async Task SetActiveMode(string courtId, string? mode)
    {
        _store.SetActiveMode(courtId, mode);
        await Clients.Group(GroupName(courtId)).SendAsync("ActiveModeUpdated", courtId, mode);
    }
    // mau: "do" | "xanh". diem: 1 hoặc 2 — đúng 4 nút trên màn trọng tài.
    // tenTrongTai: tên hiển thị (backend không có bảng trọng tài riêng,
    // lấy thẳng tên từ chính thiết bị gửi lên để ghi log dễ đọc).
    public async Task PressLight(string courtId, string giamDinhId, string tenTrongTai, string mau, int diem)
    {
        if (mau != "do" && mau != "xanh") return;
        if (diem != 1 && diem != 2) return;

        var matchState = _store.GetMatchState(courtId);
        var trangThai = matchState?["trangThai"]?.GetValue<string>();
        if (trangThai != "dang_thi")
        {
            await Clients.Caller.SendAsync("PressRejected", "Trận chưa bắt đầu hoặc đang tạm dừng — không tính điểm lúc này.");
            return;
        }

        var remaining = _store.ComputeRemainingSeconds(courtId);
        if (remaining != null && remaining <= 0)
        {
            await Clients.Caller.SendAsync("PressRejected", "Đã hết giờ hiệp đấu — chờ thư ký xác nhận hiệp mới.");
            return;
        }

        var luc = DateTimeOffset.UtcNow;
        var mauLabel = mau == "do" ? "Đỏ" : "Xanh";

        var pressLog = _store.AddLog(courtId, $"{tenTrongTai} bấm {mauLabel} +{diem}");
        await Clients.OthersInGroup(GroupName(courtId)).SendAsync("LightPressed", courtId, giamDinhId, tenTrongTai, mau, diem, luc);
        await Clients.Group(GroupName(courtId)).SendAsync("LogEntryAdded", courtId, pressLog);

        var result = _store.RegisterPressAndCheckConsensus(courtId, mau, diem, giamDinhId, luc);
        if (result == null) return;

        var (diemThat, soLuong) = result.Value;

        var scoreKey = mau == "do" ? "diemChinhThucDo" : "diemChinhThucXanh";
        var current = matchState![scoreKey]?.GetValue<int>() ?? 0;
        matchState[scoreKey] = current + diemThat;
        matchState["capNhatLuc"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _store.SetMatchState(courtId, matchState);

        var scoreLog = _store.AddLog(courtId, $"✓ GHI ĐIỂM: {mauLabel} +{diemThat} ({soLuong}/5 trọng tài đồng thuận)");

        await Clients.Group(GroupName(courtId)).SendAsync("MatchStateUpdated", courtId, matchState);
        await Clients.Group(GroupName(courtId)).SendAsync("ConsensusScored", courtId, mau, diemThat, soLuong, luc);
        await Clients.Group(GroupName(courtId)).SendAsync("LogEntryAdded", courtId, scoreLog);
    }

    public async Task SubmitRefereeScore(string courtId, string giamDinhId, JsonElement score)
    {
        var node = JsonNode.Parse(score.GetRawText());
        if (node != null) _store.SetRefereeScore(courtId, giamDinhId, node);
        await Clients.OthersInGroup(GroupName(courtId)).SendAsync("RefereeScoreUpdated", courtId, score);
    }

    public async Task RemoveRefereeScore(string courtId, string giamDinhId)
    {
        _store.RemoveRefereeScore(courtId, giamDinhId);
        await Clients.OthersInGroup(GroupName(courtId)).SendAsync("RefereeScoreRemoved", courtId, giamDinhId);
    }

    private static string GroupName(string courtId) => $"court-{courtId}";
}