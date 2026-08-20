namespace VovinamApi.Models;

public class Tournament
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public int SoSan { get; set; }
    public bool ChoPhepHiepPhu { get; set; }
}