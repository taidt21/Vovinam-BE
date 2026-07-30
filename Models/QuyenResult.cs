namespace VovinamApi.Models;

public class QuyenResult
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; } // quyền cá nhân
    public Guid? TeamId { get; set; } // quyền đồng đội
    public decimal Diem { get; set; }
    public decimal DiemTru { get; set; }
    public DateTime CapNhatLuc { get; set; }
}