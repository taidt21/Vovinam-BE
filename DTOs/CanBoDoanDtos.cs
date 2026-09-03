namespace VovinamApi.DTOs;

public class CanBoDoanDto
{
    public Guid Id { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public string? AnhDaiDien { get; set; }
    public Guid TeamId { get; set; }
    public string TeamTen { get; set; } = string.Empty;
}

public class CanBoDoanUpsertDto
{
    public string HoTen { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public Guid TeamId { get; set; }

    // Chỉ dùng lúc TẠO MỚI (import Excel) — URL nguồn để backend tự tải
    // về lưu local. Update (PUT) không đụng gì tới ảnh, đổi ảnh sau đó
    // luôn qua endpoint /anh riêng (upload file).
    public string? AnhDaiDien { get; set; }
}
