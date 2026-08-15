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
}
