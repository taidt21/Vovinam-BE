namespace VovinamApi.Models;

public class PerformanceOrder
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }

    public Guid? AthleteId { get; set; } // quyền cá nhân
    public Athlete? Athlete { get; set; }
    public Guid? TeamId { get; set; } // quyền đồng đội — "đội" = đơn vị, đã đơn giản hóa
    public Team? Team { get; set; }

    public int ThuTu { get; set; }
}