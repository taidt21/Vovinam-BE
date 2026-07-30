namespace VovinamApi.DTOs;

public class QuyenResultDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    public decimal Diem { get; set; }
    public decimal DiemTru { get; set; }
    public DateTime CapNhatLuc { get; set; }
}

public class QuyenResultUpsertDto
{
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    public decimal Diem { get; set; }
    public decimal DiemTru { get; set; }
}