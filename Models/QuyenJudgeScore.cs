namespace VovinamApi.Models;

public class QuyenJudgeScore
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    public string GiamKhaoId { get; set; } = string.Empty; // random ID sinh ở máy trọng tài, không phải "vị trí 1-5"
    public string TenGiamKhao { get; set; } = string.Empty;
    public decimal Diem { get; set; }
    public DateTime CapNhatLuc { get; set; }
}