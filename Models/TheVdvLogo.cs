namespace VovinamApi.Models;

// 1 logo hiện trên thẻ VĐV (logo liên đoàn, logo nhà tài trợ...) — số
// lượng linh hoạt (không cố định 3 như mẫu tham khảo cũ), BTC tự
// thêm/xoá/sắp thứ tự theo từng giải. Ảnh lưu trong wwwroot/uploads/logos/,
// giống hệt cách ảnh đại diện VĐV đang lưu.
public class TheVdvLogo
{
    public Guid Id { get; set; }
    public string DuongDan { get; set; } = string.Empty; // "/uploads/logos/xxx.png"
    public int ThuTu { get; set; } // Thứ tự hiển thị trái→phải trên thẻ
}
