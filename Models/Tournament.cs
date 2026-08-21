namespace VovinamApi.Models;

public class Tournament
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public int SoSan { get; set; }
    public bool ChoPhepHiepPhu { get; set; }

    // Hệ số tính điểm tổng đoàn (Tổng sắp huy chương) — tạm thời,
    // BTC tự chỉnh theo quy chế từng giải, không cố định trong code.
    public int HeSoVang { get; set; } = 50;
    public int HeSoBac { get; set; } = 20;
    public int HeSoDong { get; set; } = 10;
}