namespace VovinamApi.Models;

// Bản sao lưu dữ liệu SỐNG của 1 lượt quyền đang thi (đồng hồ, trạng
// thái cho_bat_dau/dang_thi/tam_dung...) — LiveCourtStateStore chỉ sống
// trong RAM, restart backend là mất sạch. Lưu xuống đây mỗi khi trạng
// thái sống đổi, để lúc restart tự đọc lại và khôi phục thẳng vào RAM —
// không cần Bàn thư ký tự bấm "Bắt đầu" lại.
//
// Khác MatchLiveSnapshot (khoá theo Match.Id, vì đối kháng có 1 bản ghi
// Match ổn định cho mỗi trận) — quyền không có "Id" ổn định cho từng
// lượt thi (PerformanceOrder không mang trạng thái sống), nên khoá
// thẳng theo CourtId: tại 1 thời điểm, mỗi sân chỉ có đúng 1 lượt quyền
// đang sống.
public class QuyenLiveSnapshot
{
    public string CourtId { get; set; } = "";
    public string StateJson { get; set; } = "";
    public DateTimeOffset CapNhatLuc { get; set; }
}
