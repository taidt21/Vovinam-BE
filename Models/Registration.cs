namespace VovinamApi.Models;

// Bảng nối — đúng vai trò đã bàn khi thiết kế sơ đồ DB: 1 VĐV có thể đăng
// ký nhiều nội dung, 1 nội dung có nhiều VĐV, không nhét mảng vào 1 cột.
public class Registration
{
    public Guid Id { get; set; }

    public Guid AthleteId { get; set; }
    public Athlete? Athlete { get; set; }

    public Guid EventId { get; set; }
    public CompetitionEvent? Event { get; set; }
}
