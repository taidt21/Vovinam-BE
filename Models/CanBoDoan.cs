namespace VovinamApi.Models;

// "Cán bộ đoàn" — gộp chung Trưởng đoàn + Huấn luyện viên vì cùng luồng
// dữ liệu (đăng ký trên website Vector Sport, admin xuất Excel, nhập vào
// đây), chỉ khác đúng VaiTro và mẫu thẻ khi in.
public class CanBoDoan
{
    public Guid Id { get; set; }
    public string HoTen { get; set; } = string.Empty;

    // "truong_doan" | "huan_luyen_vien" — khớp đúng giá trị xuất ra từ
    // WordPress, xem vs_staff_role_label() bên theme.
    public string VaiTro { get; set; } = string.Empty;

    public string? AnhDaiDien { get; set; }

    public Guid TeamId { get; set; }
    public Team? Team { get; set; }
}
