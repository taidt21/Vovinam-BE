namespace VovinamApi.Models;

public enum LoaiNoiDung
{
    Quyen,
    DoiKhang,
}

public enum HinhThucThi
{
    CaNhan,
    Doi,
}

public enum GioiTinhNoiDung
{
    Nam,
    Nu,
    HonHop,
}

public class CompetitionEvent
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public LoaiNoiDung Loai { get; set; }
    public GioiTinhNoiDung GioiTinh { get; set; }
    public HinhThucThi HinhThucThi { get; set; } = HinhThucThi.CaNhan;
    public int NhomTuoi { get; set; }

    public int? HangCan { get; set; } // chỉ đối kháng
    public int? ThoiGianBaiGiay { get; set; } // chỉ quyền

    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}