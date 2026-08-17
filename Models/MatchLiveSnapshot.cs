namespace VovinamApi.Models;

// Bản sao lưu dữ liệu SỐNG của 1 trận đối kháng (hiệp, điểm, cảnh cáo,
// đồng hồ...) — LiveCourtStateStore chỉ sống trong RAM, restart backend là
// mất sạch. Lưu xuống đây mỗi khi trạng thái sống đổi, để lúc restart tự
// đọc lại và khôi phục thẳng vào RAM — không cần Bàn thư ký tự gõ tay lại.
public class MatchLiveSnapshot
{
    public Guid Id { get; set; } // = đúng Match.Id — 1 bản lưu cho mỗi trận
    public string StateJson { get; set; } = "";
    public DateTimeOffset CapNhatLuc { get; set; }
}
