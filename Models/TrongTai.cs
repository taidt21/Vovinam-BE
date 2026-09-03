namespace VovinamApi.Models;

public class TrongTai
{
    public Guid Id { get; set; }
    public string HoTen { get; set; } = string.Empty;

    // "c1", "c2"... khớp với CourtBasic.id bên frontend (sân không phải
    // bảng riêng — sinh ra từ Tournament.SoSan). Null = chưa gán sân nào.
    public string? CourtId { get; set; }

    // 1-5 = đang là Giám định số mấy tại sân đó (đang hoạt động).
    // Null = có trong danh sách của sân nhưng đang dự bị, không active.
    public int? ThuTuGiamDinh { get; set; }

    // 2 trường dưới đây chỉ phục vụ in thẻ trọng tài — không liên quan
    // gì tới việc gán sân/chấm điểm ở trên.
    public string? DonVi { get; set; }
    public string? AnhDaiDien { get; set; } // "/uploads/trong-tai/xxx.jpg"

    // true = đã có 1 thiết bị chọn đúng tên này ở màn "Trọng tài" — chặn
    // thiết bị KHÁC chọn trùng tên (2 máy cùng tưởng mình là 1 người,
    // gửi điểm lẫn lộn không biết của ai). Không liên quan gì tới
    // CourtId/ThuTuGiamDinh (đó là BTC gán sân, cái này là THIẾT BỊ tự
    // nhận diện danh tính).
    public bool DaChonThietBi { get; set; }
}
