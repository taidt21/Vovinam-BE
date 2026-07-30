namespace VovinamApi.DTOs;

public class QuyenJudgeScoreDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    public string GiamKhaoId { get; set; } = string.Empty;
    public string TenGiamKhao { get; set; } = string.Empty;
    public decimal Diem { get; set; }
    public DateTime CapNhatLuc { get; set; }
}

public class QuyenJudgeScoreUpsertDto
{
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    public string GiamKhaoId { get; set; } = string.Empty;
    public string TenGiamKhao { get; set; } = string.Empty;
    public decimal Diem { get; set; }
}