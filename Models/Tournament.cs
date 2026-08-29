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

    // Quyền: hạng 4 có được nhận thêm 1 suất HCĐ cùng hạng 3 hay không —
    // THUẦN theo vị trí xếp hạng, không liên quan gì đến việc hạng 3/4 có
    // bằng điểm tổng hay không (hiệu số phụ theo điểm giám khảo cao nhất
    // dùng để phân định THỨ TỰ khi bằng điểm tổng luôn áp dụng sẵn, không
    // phụ thuộc cờ này — xem computeQuyenRanking ở frontend). Mặc định
    // cho phép — khớp luật đối kháng vốn luôn cho đồng hạng ba (2 người
    // thua bán kết, không có trận tranh hạng 3).
    public bool ChoPhepDongHangBaQuyen { get; set; } = true;

    // Đối kháng: cửa sổ thời gian (giây) để gộp phiếu bấm đèn — đủ 3/5
    // trọng tài bấm cùng màu trong đúng khoảng này thì tính là ĐỒNG
    // THUẬN, chốt điểm ngay. Trước đây đóng cứng 1.5s trong code
    // (LiveCourtStateStore.ConsensusWindow); giờ BTC tự chỉnh theo thực
    // tế từng giải (trọng tài quen tay bấm nhanh/chậm khác nhau).
    public double CuaSoDongThuanGiay { get; set; } = 1.5;

    // Tiêu đề in trên thẻ VĐV — để trống thì trang in không hiện dòng nào
    // cả (mẫu thẻ mới không in cứng tên giải như bản demo cũ nữa, để 1
    // mẫu dùng lại được nhiều giải). Cho phép nhiều dòng (BTC tự xuống
    // dòng khi nhập), không giới hạn độ dài.
    public string TieuDeThe { get; set; } = string.Empty;
}