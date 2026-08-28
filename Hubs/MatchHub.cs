using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<MatchHub> _logger;

    public MatchHub(LiveCourtStateStore store, ApplicationDbContext db, ILogger<MatchHub> logger)
    {
        _store = store;
        _db = db;
        _logger = logger;
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
        if (node == null) return;

        await _store.WithCourtLockAsync(courtId, async () =>
        {
            _store.SetMatchState(courtId, node);
            await Clients.OthersInGroup(GroupName(courtId)).SendAsync("MatchStateUpdated", courtId, matchState);

            try
            {
                await LuuSnapshotAsync(node);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lưu snapshot thất bại cho sân {CourtId} (PublishMatchState)", courtId);
            }
        });
    }

    public async Task ClearMatchState(string courtId)
    {
        await _store.WithCourtLockAsync(courtId, async () =>
        {
            var matchState = _store.GetMatchState(courtId);
            var matchIdNode = matchState?["matchId"];
            Guid? matchIdToDelete = null;
            if (matchIdNode != null && Guid.TryParse(matchIdNode.GetValue<string>(), out var parsedId))
                matchIdToDelete = parsedId;

            _store.ClearMatchState(courtId);
            await Clients.OthersInGroup(GroupName(courtId)).SendAsync("MatchStateCleared", courtId);

            if (matchIdToDelete != null)
            {
                try
                {
                    var snapshot = await _db.MatchLiveSnapshots.FindAsync(matchIdToDelete);
                    if (snapshot != null)
                    {
                        _db.MatchLiveSnapshots.Remove(snapshot);
                        await _db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Xoá snapshot thất bại cho trận {MatchId} tại sân {CourtId} (ClearMatchState)", matchIdToDelete, courtId);
                }
            }
        });
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

    // BTC bấm "X" gỡ trận/lượt đang chờ ở ĐÚNG 1 bên (doi_khang hoặc
    // quyen) tại 1 sân — dọn sạch trạng thái sống của đúng bên đó và tạm
    // ngưng tự động nhận trận/lượt kế tiếp CHO ĐÚNG bên đó, bên còn lại
    // không bị đụng tới. Bắt đầu thủ công 1 trận/lượt mới (bên tương ứng)
    // sẽ tự tắt lại cờ này — xem chỗ gọi ở BanThuKy.tsx.
    public async Task SetCourtResting(string courtId, string mode, bool dangNghi)
    {
        if (mode != "doi_khang" && mode != "quyen") return;
        if (mode == "doi_khang")
        {
            _store.SetDangNghiDoiKhang(courtId, dangNghi);
            if (dangNghi) await ClearMatchState(courtId);
        }
        else
        {
            _store.SetDangNghiQuyen(courtId, dangNghi);
            if (dangNghi) await ClearQuyenState(courtId);
        }
        await Clients.Group(GroupName(courtId)).SendAsync("CourtRestingUpdated", courtId, mode, dangNghi);
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

        // Đọc cửa sổ đồng thuận hiện hành từ Thiết lập giải mỗi lần bấm —
        // để BTC đổi giữa chừng giải là có hiệu lực ngay từ pha kế tiếp,
        // không cần restart hay đợi cache nào hết hạn.
        var tournament = await _db.Tournaments.FirstOrDefaultAsync();
        var cuaSoGiay = tournament?.CuaSoDongThuanGiay ?? 1.5;

        var result = _store.RegisterPressAndCheckConsensus(courtId, mau, diem, giamDinhId, luc, cuaSoGiay);
        if (result == null) return;

        var (diemThat, soLuong) = result.Value;

        await _store.WithCourtLockAsync(courtId, async () =>
        {
            var scoreKey = mau == "do" ? "diemChinhThucDo" : "diemChinhThucXanh";
            var current = matchState![scoreKey]?.GetValue<int>() ?? 0;
            matchState[scoreKey] = current + diemThat;
            matchState["capNhatLuc"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _store.SetMatchState(courtId, matchState);

            var scoreLog = _store.AddLog(courtId, $"✓ GHI ĐIỂM: {mauLabel} +{diemThat} ({soLuong}/5 trọng tài đồng thuận)");

            await Clients.Group(GroupName(courtId)).SendAsync("MatchStateUpdated", courtId, matchState);
            await Clients.Group(GroupName(courtId)).SendAsync("ConsensusScored", courtId, mau, diemThat, soLuong, luc);
            await Clients.Group(GroupName(courtId)).SendAsync("LogEntryAdded", courtId, scoreLog);

            try
            {
                await LuuSnapshotAsync(matchState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lưu snapshot thất bại cho sân {CourtId} sau khi ghi điểm (PressLight)", courtId);
            }
        });
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