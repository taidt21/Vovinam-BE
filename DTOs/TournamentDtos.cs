using System.ComponentModel.DataAnnotations;

namespace VovinamApi.DTOs;

public class TournamentDto
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public int SoSan { get; set; }
    public bool ChoPhepHiepPhu { get; set; }
    public int HeSoVang { get; set; }
    public int HeSoBac { get; set; }
    public int HeSoDong { get; set; }
    public bool ChoPhepDongHangBaQuyen { get; set; }
}

public class TournamentUpsertDto
{
    [Required] public string Ten { get; set; } = string.Empty;
    [Range(1, 50)] public int SoSan { get; set; }
    public bool ChoPhepHiepPhu { get; set; }
    [Range(0, 1000)] public int HeSoVang { get; set; } = 50;
    [Range(0, 1000)] public int HeSoBac { get; set; } = 20;
    [Range(0, 1000)] public int HeSoDong { get; set; } = 10;
    public bool ChoPhepDongHangBaQuyen { get; set; } = true;
}