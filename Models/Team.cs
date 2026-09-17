namespace VovinamApi.Models;

public class Team
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public ICollection<Athlete> Athletes { get; set; } = new List<Athlete>();
    public ICollection<CanBoDoan> CanBoDoans { get; set; } = new List<CanBoDoan>();

}
