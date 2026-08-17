namespace VovinamApi.Models;

// Đánh dấu 1 lượt thi quyền ĐÃ KẾT THÚC — độc lập với việc có đủ 5/5 điểm
// giám định hay không. Cần tách riêng vì luật thi đấu thật cho phép 1 lượt
// kết thúc ngay lập tức mà KHÔNG cần đủ điểm (rớt vũ khí, té ngã, dừng bài
// rõ rệt — theo thủ lệnh của giám định 1, không đợi 5 giám định chấm
// xong). Suy "đã xong" chỉ từ số điểm sẽ sai đúng những lúc này.
public class QuyenLuotHoanThanh
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    // "hoan_thanh" | "quen_bai" | "dung_bai" | "roi_vu_khi" | "chan_thuong"
    // | "loi_may" — khớp đúng LyDoKetThucQuyen bên frontend.
    public string LyDo { get; set; } = "hoan_thanh";
}
