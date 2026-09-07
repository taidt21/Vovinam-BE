namespace VovinamApi.Models;

// Bản LƯU LÂU DÀI của từng dòng Nhật ký trận đấu — khác hẳn LogEntry
// trong LiveCourtStateStore (chỉ sống trong RAM, mất sạch khi restart
// backend HOẶC khi trận bị dọn/kết thúc). Lưu xuống đây để hỗ trợ tính
// năng "Xem lại trận đã kết thúc" — cần còn nguyên nhật ký dù đã qua
// rất lâu sau khi trận đó xong, backend đã restart nhiều lần, hay BTK
// đã chuyển sang xử lý hàng chục trận khác rồi.
//
// Ghi xuống đây NGAY LÚC tạo log trong RAM (PressLight/GhiLogDieuChinhDiem
// trong MatchHub.cs) — không đợi tới lúc trận kết thúc mới lưu 1 lần,
// để dù backend có restart giữa chừng trận cũng không mất nhật ký từ
// trước đó.
public class MatchLogEntryRecord
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public DateTimeOffset Luc { get; set; }
    public string NoiDung { get; set; } = "";
    public string? MatchTimeLabel { get; set; }
    public string? GiamDinhId { get; set; }
}
