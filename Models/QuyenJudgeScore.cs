namespace VovinamApi.Models;

public class QuyenJudgeScore
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    public string GiamKhaoId { get; set; } = string.Empty;
    public string TenGiamKhao { get; set; } = string.Empty;
    public decimal Diem { get; set; }
    // Danh sách lỗi bị trừ, dạng JSON thô — chỉ để LƯU LẠI phục vụ tra
    // cứu/giải trình khi có khiếu nại, không dùng để tính toán gì (Diem ở
    // trên mới là con số chính thức dùng để tổng hợp). Không tách bảng
    // riêng vì chưa cần truy vấn/thống kê theo từng loại lỗi.
    public string? ChiTietJson { get; set; }
    public DateTime CapNhatLuc { get; set; }
}