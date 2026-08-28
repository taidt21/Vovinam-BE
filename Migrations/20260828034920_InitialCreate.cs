using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vovinam_backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BanThuKyAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    TenHienThi = table.Column<string>(type: "TEXT", nullable: false),
                    CourtId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanThuKyAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ten = table.Column<string>(type: "TEXT", nullable: false),
                    Loai = table.Column<int>(type: "INTEGER", nullable: false),
                    GioiTinh = table.Column<int>(type: "INTEGER", nullable: false),
                    HinhThucThi = table.Column<int>(type: "INTEGER", nullable: false),
                    NhomTuoi = table.Column<int>(type: "INTEGER", nullable: false),
                    HangCan = table.Column<int>(type: "INTEGER", nullable: true),
                    ThoiGianBaiGiay = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ten = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ten = table.Column<string>(type: "TEXT", nullable: false),
                    SoSan = table.Column<int>(type: "INTEGER", nullable: false),
                    ChoPhepHiepPhu = table.Column<bool>(type: "INTEGER", nullable: false),
                    HeSoVang = table.Column<int>(type: "INTEGER", nullable: false),
                    HeSoBac = table.Column<int>(type: "INTEGER", nullable: false),
                    HeSoDong = table.Column<int>(type: "INTEGER", nullable: false),
                    ChoPhepDongHangBaQuyen = table.Column<bool>(type: "INTEGER", nullable: false),
                    CuaSoDongThuanGiay = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrongTais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HoTen = table.Column<string>(type: "TEXT", nullable: false),
                    CourtId = table.Column<string>(type: "TEXT", nullable: true),
                    ThuTuGiamDinh = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrongTais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Athletes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HoTen = table.Column<string>(type: "TEXT", nullable: false),
                    NamSinh = table.Column<int>(type: "INTEGER", nullable: false),
                    GioiTinh = table.Column<int>(type: "INTEGER", nullable: false),
                    NhomTuoi = table.Column<int>(type: "INTEGER", nullable: false),
                    AnhDaiDien = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Athletes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Athletes_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AthleteRedId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AthleteBlueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NextMatchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NextMatchSlot = table.Column<string>(type: "TEXT", nullable: true),
                    Vong = table.Column<string>(type: "TEXT", nullable: false),
                    TrangThai = table.Column<string>(type: "TEXT", nullable: false),
                    LyDoKetThuc = table.Column<string>(type: "TEXT", nullable: true),
                    NguoiThangId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CourtId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Athletes_AthleteBlueId",
                        column: x => x.AthleteBlueId,
                        principalTable: "Athletes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Matches_Athletes_AthleteRedId",
                        column: x => x.AthleteRedId,
                        principalTable: "Athletes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Matches_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AthleteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ThuTu = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceOrders_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PerformanceOrders_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuyenJudgeScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AthleteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GiamKhaoId = table.Column<string>(type: "TEXT", nullable: false),
                    TenGiamKhao = table.Column<string>(type: "TEXT", nullable: false),
                    Diem = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    ChiTietJson = table.Column<string>(type: "TEXT", nullable: true),
                    CapNhatLuc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyenJudgeScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuyenJudgeScores_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyenJudgeScores_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyenJudgeScores_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuyenLuotHoanThanhs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AthleteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LyDo = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyenLuotHoanThanhs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuyenLuotHoanThanhs_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyenLuotHoanThanhs_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyenLuotHoanThanhs_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuyenResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AthleteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Diem = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    DiemTru = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    CapNhatLuc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyenResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuyenResults_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyenResults_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyenResults_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AthleteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registrations_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Registrations_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchLiveSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StateJson = table.Column<string>(type: "TEXT", nullable: false),
                    CapNhatLuc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchLiveSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchLiveSnapshots_Matches_Id",
                        column: x => x.Id,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_TeamId",
                table: "Athletes",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_BanThuKyAccounts_Username",
                table: "BanThuKyAccounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AthleteBlueId",
                table: "Matches",
                column: "AthleteBlueId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AthleteRedId",
                table: "Matches",
                column: "AthleteRedId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_EventId",
                table: "Matches",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceOrders_AthleteId",
                table: "PerformanceOrders",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceOrders_TeamId",
                table: "PerformanceOrders",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyenJudgeScores_AthleteId",
                table: "QuyenJudgeScores",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyenJudgeScores_EventId",
                table: "QuyenJudgeScores",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyenJudgeScores_TeamId",
                table: "QuyenJudgeScores",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyenLuotHoanThanhs_AthleteId",
                table: "QuyenLuotHoanThanhs",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyenLuotHoanThanhs_EventId",
                table: "QuyenLuotHoanThanhs",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyenLuotHoanThanhs_TeamId",
                table: "QuyenLuotHoanThanhs",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyenResults_AthleteId",
                table: "QuyenResults",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyenResults_EventId",
                table: "QuyenResults",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyenResults_TeamId",
                table: "QuyenResults",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_AthleteId_EventId",
                table: "Registrations",
                columns: new[] { "AthleteId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId",
                table: "Registrations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_TrongTais_CourtId_ThuTuGiamDinh",
                table: "TrongTais",
                columns: new[] { "CourtId", "ThuTuGiamDinh" },
                unique: true,
                filter: "[ThuTuGiamDinh] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanThuKyAccounts");

            migrationBuilder.DropTable(
                name: "MatchLiveSnapshots");

            migrationBuilder.DropTable(
                name: "PerformanceOrders");

            migrationBuilder.DropTable(
                name: "QuyenJudgeScores");

            migrationBuilder.DropTable(
                name: "QuyenLuotHoanThanhs");

            migrationBuilder.DropTable(
                name: "QuyenResults");

            migrationBuilder.DropTable(
                name: "Registrations");

            migrationBuilder.DropTable(
                name: "Tournaments");

            migrationBuilder.DropTable(
                name: "TrongTais");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Athletes");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
