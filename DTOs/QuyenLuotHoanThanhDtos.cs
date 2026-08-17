namespace VovinamApi.DTOs;

public class QuyenLuotHoanThanhDto
{
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    public string LyDo { get; set; } = "hoan_thanh";
}

public class QuyenLuotHoanThanhUpsertDto
{
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    public string LyDo { get; set; } = "hoan_thanh";
}
