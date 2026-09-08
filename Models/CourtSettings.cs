namespace VovinamApi.Models;

// Cài đặt riêng của TỪNG sân đối kháng (số hiệp, thời gian hiệp, thời
// gian nghỉ) — trước đây 3 giá trị này nằm trong LiveMatchState (trạng
// thái của TỪNG TRẬN), nên mỗi khi mở trận mới vào sân lại tự reset về
// mặc định cứng, không giữ lại được tuỳ chỉnh riêng của sân đó cho các
// trận sau. Lưu lâu dài ở đây, khoá theo CourtId ("c1", "c2"...) — mở
// trận mới vào sân nào thì tự đọc đúng cài đặt của sân đó áp dụng luôn,
// không cần BTK chỉnh lại tay mỗi trận.
public class CourtSettings
{
    public string CourtId { get; set; } = "";
    public int TongSoHiep { get; set; }
    public int ThoiGianHiepGiay { get; set; }
    public int ThoiGianNghiGiay { get; set; }
}
