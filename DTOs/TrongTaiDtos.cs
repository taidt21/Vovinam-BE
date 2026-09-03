namespace VovinamApi.DTOs;

public class TrongTaiDto
{
    public Guid Id { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? CourtId { get; set; }
    public int? ThuTuGiamDinh { get; set; }
    public string? DonVi { get; set; }
    public string? AnhDaiDien { get; set; }
    public bool DaChonThietBi { get; set; }
}

public class TrongTaiUpsertDto
{
    public string HoTen { get; set; } = string.Empty;
    public string? CourtId { get; set; }
    public int? ThuTuGiamDinh { get; set; }
    public string? DonVi { get; set; }
}
