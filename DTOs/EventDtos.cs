using System.ComponentModel.DataAnnotations;

namespace VovinamApi.DTOs;

public class EventDto
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string Loai { get; set; } = string.Empty;
    public string GioiTinh { get; set; } = string.Empty;
    public string HinhThucThi { get; set; } = string.Empty;
    public int NhomTuoi { get; set; }
    public int? HangCan { get; set; }
    public int? ThoiGianBaiGiay { get; set; }
}

public class EventUpsertDto
{
    [Required] public string Ten { get; set; } = string.Empty;
    [Required] public string Loai { get; set; } = string.Empty;
    [Required] public string GioiTinh { get; set; } = string.Empty;
    public string HinhThucThi { get; set; } = "ca_nhan";
    public int NhomTuoi { get; set; }
    public int? HangCan { get; set; }
    public int? ThoiGianBaiGiay { get; set; }
}