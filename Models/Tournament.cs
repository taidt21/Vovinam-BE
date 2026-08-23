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

    // Quyền: 2 lượt bằng điểm tổng có được cùng nhận HCĐ (đồng hạng ba)
    // không, hay phải phân định bằng điểm giám khảo cao nhất (hiệu số
    // phụ). Mặc định cho phép — khớp luật đối kháng vốn luôn cho đồng
    // hạng ba (2 người thua bán kết, không có trận tranh hạng 3).
    public bool ChoPhepDongHangBaQuyen { get; set; } = true;
}