namespace VovinamApi.Models;

public class Team
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;

    public ICollection<Athlete> Athletes { get; set; } = new List<Athlete>();
}
