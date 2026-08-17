using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using VovinamApi.Data;
using VovinamApi.Models;
using VovinamApi.Services;

namespace VovinamApi.Hubs;

public class MatchHub : Hub
{
    private readonly LiveCourtStateStore _store;
    private readonly ApplicationDbContext _db;

    public MatchHub(LiveCourtStateStore store, ApplicationDbContext db)
    {
        _store = store;
        _db = db;
    }

    // Lưu lại state sống của ĐÚNG trận đang có (dựa vào field matchId ngay
    // trong chính JSON đó) — gọi sau MỌI lần SetMatchState, để restart
    // backend có gì đọc lại tự khôi phục, không cần Bàn thư ký gõ tay.
    private async Task LuuSnapshotAsync(JsonNode matchState)
    {
        var matchIdNode = matchState["matchId"];
        if (matchIdNode == null) return;
        if (!Guid.TryParse(matchIdNode.GetValue<string>(), out var matchId)) return;

        var existing = await _db.MatchLiveSnapshots.FindAsync(matchId);
        var json = matchState.ToJsonString();
        if (existing != null)
        {
            existing.StateJson = json;
            existing.CapNhatLuc = DateTimeOffset.UtcNow;
        }
        else
        {
            _db.MatchLiveSnapshots.Add(new MatchLiveSnapshot
            {
                Id = matchId,
                StateJson = json,
                CapNhatLuc = DateTimeOffset.UtcNow,
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task JoinCourt(string courtId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(courtId));

        // Chưa có gì sống trong RAM cho sân này — kiểm tra xem có đúng 1
        // trận đang "dang_thi" tại sân này trong DB không, và nếu có bản
        // lưu snapshot của đúng trận đó thì tự khôi phục thẳng vào RAM
        // TRƯỚC khi gửi snapshot cho client — âm thầm, không ai phải làm
        // gì cả.
        //
        // CHỈ khôi phục nếu ActiveMode KHÔNG phải "quyen" — nếu BTK đã
        // chủ động chuyển sang quyền (không phải do crash), "dang_thi"
        // trong DB có thể chỉ là dữ liệu cũ chưa kịp cập nhật (đổi tab
        // không tự đổi TrangThai của Match), khôi phục lúc này sẽ vô tình
        // đè mất đúng lượt quyền đang thật sự sống (do 2 loại tự xoá lẫn
        // nhau).
        if (_store.GetMatchState(courtId) == null && _store.GetActiveMode(courtId) != "quyen")
        {
            var dangThi = await _db.Matches.FirstOrDefaultAsync(m =>
                m.CourtId == courtId && m.TrangThai == "dang_thi");
            if (dangThi != null)
            {
                var snapshot = await _db.MatchLiveSnapshots.FindAsync(dangThi.Id);
                if (snapshot != null)
                {
                    var node = JsonNode.Parse(snapshot.StateJson);
                    if (node != null) _store.SetMatchState(courtId, node);
                }
            }
        }

        await Clients.Caller.SendAsync("CourtSnapshot", courtId, _store.GetSnapshot(courtId));
    }

    public async Task PublishMatchState(string courtId, JsonElement matchState)
    {
        var node = JsonNode.Parse(matchState.GetRawText());
        if (node != null)
        {
            _store.SetMatchState(courtId, node);
            await LuuSnapshotAsync(node);
        }
        await Clients.OthersInGroup(GroupName(courtId)).SendAsync("MatchStateUpdated", courtId, matchState);
    }

    public async Task ClearMatchState(string courtId)
    {
        var matchState = _store.GetMatchState(courtId);
        var matchIdNode = matchState?["matchId"];
        if (matchIdNode != null && Guid.TryParse(matchIdNode.GetValue<string>(), out var matchId))
        {
            var snapshot = await _db.MatchLiveSnapshots.FindAsync(matchId);
            if (snapshot != null)
            {
                _db.MatchLiveSnapshots.Remove(snapshot);
                await _db.SaveChangesAsync();
            }
        }
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
        await LuuSnapshotAsync(matchState);

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