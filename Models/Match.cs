namespace VovinamApi.Models;

public class Match
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public CompetitionEvent? Event { get; set; }

    public Guid? AthleteRedId { get; set; }
    public Athlete? AthleteRed { get; set; }
    public Guid? AthleteBlueId { get; set; }
    public Athlete? AthleteBlue { get; set; }

    // Không ép khóa ngoại thật cho 2 trường dưới — tự quản lý ở tầng ứng
    // dụng, tránh rắc rối tự tham chiếu (1 trận trỏ tới 1 trận khác cùng
    // bảng) lúc xóa/ghi lại cả nhánh đấu mỗi lần bốc thăm lại.
    public Guid? NextMatchId { get; set; }
    public string? NextMatchSlot { get; set; } // "do" | "xanh"

    public string Vong { get; set; } = string.Empty;
    public string TrangThai { get; set; } = "cho_thi";
    public string? LyDoKetThuc { get; set; }
    public Guid? NguoiThangId { get; set; }
    public string? CourtId { get; set; } // chưa có bảng Courts riêng, để dành lúc làm Bàn thư ký
}