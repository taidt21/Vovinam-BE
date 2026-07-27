namespace VovinamApi.Models;

public enum GioiTinh
{
    Nam,
    Nu,
}

public class Athlete
{
    public Guid Id { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public int NamSinh { get; set; }
    public GioiTinh GioiTinh { get; set; }
    public int NhomTuoi { get; set; }

    public Guid TeamId { get; set; }
    public Team? Team { get; set; }

    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
