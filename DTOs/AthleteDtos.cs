using System.ComponentModel.DataAnnotations;

namespace VovinamApi.DTOs;

public class AthleteDto
{
    public Guid Id { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public int NamSinh { get; set; }
    public string GioiTinh { get; set; } = string.Empty;
    public int NhomTuoi { get; set; }
    public List<Guid> EventIds { get; set; } = new();
}

public class AthleteUpsertDto
{
    [Required] public string HoTen { get; set; } = string.Empty;
    [Range(1970, 2100)] public int NamSinh { get; set; }
    [Required] public string GioiTinh { get; set; } = string.Empty;
    public int NhomTuoi { get; set; }
    public List<Guid> EventIds { get; set; } = new();
}