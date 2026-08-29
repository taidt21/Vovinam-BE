using Microsoft.EntityFrameworkCore;
using VovinamApi.Models;

namespace VovinamApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Tournament> Tournaments => Set<Tournament>(); public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<CompetitionEvent> Events => Set<CompetitionEvent>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<PerformanceOrder> PerformanceOrders => Set<PerformanceOrder>();
    public DbSet<QuyenResult> QuyenResults => Set<QuyenResult>();
    public DbSet<QuyenJudgeScore> QuyenJudgeScores => Set<QuyenJudgeScore>();
    public DbSet<TrongTai> TrongTais => Set<TrongTai>();
    public DbSet<QuyenLuotHoanThanh> QuyenLuotHoanThanhs => Set<QuyenLuotHoanThanh>();
    public DbSet<MatchLiveSnapshot> MatchLiveSnapshots => Set<MatchLiveSnapshot>();
    public DbSet<QuyenLiveSnapshot> QuyenLiveSnapshots => Set<QuyenLiveSnapshot>();
    public DbSet<BanThuKyAccount> BanThuKyAccounts => Set<BanThuKyAccount>();
    public DbSet<TheVdvLogo> TheVdvLogos => Set<TheVdvLogo>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<BanThuKyAccount>()
            .HasIndex(a => a.Username)
            .IsUnique();

        builder.Entity<Athlete>()
            .Property(a => a.AnhDaiDien)
            .HasMaxLength(2048);

        builder.Entity<Athlete>()
            .HasOne(a => a.Team)
            .WithMany(t => t.Athletes)
            .HasForeignKey(a => a.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Registration>()
            .HasOne(r => r.Athlete)
            .WithMany(a => a.Registrations)
            .HasForeignKey(r => r.AthleteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Registration>()
            .HasOne(r => r.Event)
            .WithMany(e => e.Registrations)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1 VĐV không đăng ký trùng 1 nội dung 2 lần — đúng ràng buộc đã
        // bàn lúc thiết kế bảng Registrations, giờ DB tự chặn thay vì phải
        // tự kiểm tra bằng tay ở tầng ứng dụng.
        builder.Entity<Registration>()
            .HasIndex(r => new { r.AthleteId, r.EventId })
            .IsUnique();

        builder.Entity<QuyenResult>().Property(r => r.Diem).HasPrecision(5, 2);
        builder.Entity<QuyenResult>().Property(r => r.DiemTru).HasPrecision(5, 2);
        builder.Entity<QuyenJudgeScore>().Property(s => s.Diem).HasPrecision(5, 2);

        // 1 sân không được có 2 người cùng là Giám định số N — nhiều người
        // ThuTuGiamDinh = null (dự bị/chưa gán) vẫn thoải mái tồn tại song
        // song vì filter loại các dòng null ra khỏi ràng buộc unique.
        builder.Entity<TrongTai>()
            .HasIndex(t => new { t.CourtId, t.ThuTuGiamDinh })
            .IsUnique()
            .HasFilter("[ThuTuGiamDinh] IS NOT NULL");

        // QuyenJudgeScore/QuyenLuotHoanThanh/QuyenResult trước đây chỉ có
        // field Guid thường, không có khoá ngoại thật — DB không chặn được
        // EventId/AthleteId/TeamId sai hoặc trỏ vào bản ghi đã xoá. Dùng
        // Restrict (không phải Cascade): đây là dữ liệu điểm/kết quả thi
        // đấu THẬT, có giá trị lưu trữ độc lập — xoá nhầm 1 VĐV/nội dung
        // đã có điểm không nên ÂM THẦM xoá luôn điểm số theo, phải chặn
        // lại và báo rõ (đã thêm kiểm tra tương ứng ở EventsController/
        // DashboardAthletesController/DashboardTeamsController).
        foreach (var t in new[] { typeof(QuyenJudgeScore), typeof(QuyenLuotHoanThanh), typeof(QuyenResult) })
        {
            builder.Entity(t).HasOne(typeof(CompetitionEvent)).WithMany()
                .HasForeignKey("EventId").OnDelete(DeleteBehavior.Restrict);
            builder.Entity(t).HasOne(typeof(Athlete)).WithMany()
                .HasForeignKey("AthleteId").OnDelete(DeleteBehavior.Restrict);
            builder.Entity(t).HasOne(typeof(Team)).WithMany()
                .HasForeignKey("TeamId").OnDelete(DeleteBehavior.Restrict);
        }

        // MatchLiveSnapshot.Id CHÍNH LÀ Match.Id (xem comment trong Model)
        // — quan hệ 1-1 dùng chung khoá chính. Khác QuyenResult ở trên:
        // đây thuần là bản sao lưu/cache trạng thái sống, không có giá trị
        // độc lập gì khi Match gốc không còn — Cascade là đúng, xoá Match
        // (VD lúc bốc thăm lại 1 nội dung) tự dọn theo, không để rác lại.
        builder.Entity<MatchLiveSnapshot>()
            .HasOne<Match>()
            .WithOne()
            .HasForeignKey<MatchLiveSnapshot>(s => s.Id)
            .OnDelete(DeleteBehavior.Cascade);

        // Quyền không có 1 entity "gốc" ổn định để làm quan hệ 1-1 như
        // Match ở trên (xem comment trong Model) — chỉ cần CourtId làm
        // khoá chính thẳng, không có FK nào để cascade theo.
        builder.Entity<QuyenLiveSnapshot>()
            .HasKey(s => s.CourtId);
    }
}
