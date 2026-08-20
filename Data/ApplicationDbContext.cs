using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Models;

namespace VovinamApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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
    public DbSet<BanThuKyAccount> BanThuKyAccounts => Set<BanThuKyAccount>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Team)
            .WithMany()
            .HasForeignKey(u => u.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BanThuKyAccount>()
            .HasIndex(a => a.Username)
            .IsUnique();

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
    }
}
