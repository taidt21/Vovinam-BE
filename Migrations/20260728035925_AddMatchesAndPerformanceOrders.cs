using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vovinam_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchesAndPerformanceOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AthleteRedId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AthleteBlueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NextMatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NextMatchSlot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Vong = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LyDoKetThuc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NguoiThangId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourtId = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AthleteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ThuTu = table.Column<int>(type: "int", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "PerformanceOrders");
        }
    }
}
