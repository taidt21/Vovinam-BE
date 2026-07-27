using System.ComponentModel.DataAnnotations;

namespace VovinamApi.DTOs;

public class TeamDto
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public int SoVdv { get; set; }
}

public class TeamUpsertDto
{
    [Required] public string Ten { get; set; } = string.Empty;
}