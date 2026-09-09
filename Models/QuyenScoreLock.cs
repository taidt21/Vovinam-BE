namespace VovinamApi.Models;

// Đánh dấu 1 lượt quyền (EventId + AthleteId + TeamId — đúng bộ khoá
// định danh y hệt QuyenJudgeScore, quyền không có bản ghi Match nào để
// gắn vào) đã bị Bàn thư ký KHOÁ — giám định không thể gửi/sửa điểm
// cho lượt đó nữa. Tồn tại 1 dòng ở đây = đã khoá; không có dòng = còn
// mở. Không cần thêm cột boolean vì bản chất chỉ có 2 trạng thái, sự
// tồn tại của dòng đã đủ diễn đạt.
//
// Id RIÊNG làm khoá chính (y hệt QuyenJudgeScore) — KHÔNG dùng
// (EventId, AthleteId, TeamId) làm khoá chính tổng hợp như bản đầu
// tiên: EF Core không cho phép bất kỳ phần nào của khoá chính là null,
// trong khi AthleteId/TeamId hoàn toàn hợp lệ là null với lượt cá nhân
// (lỗi thật đã gặp: "Unable to track an entity... primary key property
// 'TeamId' is null"). Việc tránh trùng lặp (1 lượt chỉ nên có tối đa 1
// dòng khoá) do code ở Controller tự kiểm tra bằng FirstOrDefaultAsync
// trước khi thêm mới, không dựa vào ràng buộc khoá chính.
public class QuyenScoreLock
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? TeamId { get; set; }
    public DateTimeOffset KhoaLuc { get; set; }
}
