using System.ComponentModel.DataAnnotations;

namespace VovinamApi.DTOs;

public class TournamentDto
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public int SoSan { get; set; }
}

public class TournamentUpsertDto
{
    [Required] public string Ten { get; set; } = string.Empty;
    [Range(1, 50)] public int SoSan { get; set; }
}