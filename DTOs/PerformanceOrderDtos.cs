namespace VovinamApi.DTOs;

public class PerformanceOrderDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    public int ThuTu { get; set; }
}

public class PerformanceOrderUpsertDto
{
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    public int ThuTu { get; set; }
}