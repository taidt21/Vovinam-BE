using System.ComponentModel.DataAnnotations;

namespace VovinamApi.DTOs;

public class DashboardAthleteDto
{
    public Guid Id { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public int NamSinh { get; set; }
    public string GioiTinh { get; set; } = string.Empty;
    public int NhomTuoi { get; set; }
    public string? AnhDaiDien { get; set; }
    public Guid TeamId { get; set; }
    public List<Guid> EventIds { get; set; } = new();
}

public class DashboardAthleteUpsertDto
{
    [Required] public string HoTen { get; set; } = string.Empty;
    [Range(1970, 2100)] public int NamSinh { get; set; }
    [Required] public string GioiTinh { get; set; } = string.Empty;
    public int NhomTuoi { get; set; }
    [MaxLength(2048)] public string? AnhDaiDien { get; set; }
    public Guid TeamId { get; set; }
    public List<Guid> EventIds { get; set; } = new();
}