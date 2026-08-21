using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vovinam_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddHeSoHuyChuong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HeSoBac",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HeSoDong",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HeSoVang",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.AddForeignKey(
                name: "FK_MatchLiveSnapshots_Matches_Id",
                table: "MatchLiveSnapshots",
                column: "Id",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyenJudgeScores_Athletes_AthleteId",
                table: "QuyenJudgeScores",
                column: "AthleteId",
                principalTable: "Athletes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyenJudgeScores_Events_EventId",
                table: "QuyenJudgeScores",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyenJudgeScores_Teams_TeamId",
                table: "QuyenJudgeScores",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyenLuotHoanThanhs_Athletes_AthleteId",
                table: "QuyenLuotHoanThanhs",
                column: "AthleteId",
                principalTable: "Athletes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyenLuotHoanThanhs_Events_EventId",
                table: "QuyenLuotHoanThanhs",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyenLuotHoanThanhs_Teams_TeamId",
                table: "QuyenLuotHoanThanhs",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyenResults_Athletes_AthleteId",
                table: "QuyenResults",
                column: "AthleteId",
                principalTable: "Athletes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyenResults_Events_EventId",
                table: "QuyenResults",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyenResults_Teams_TeamId",
                table: "QuyenResults",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchLiveSnapshots_Matches_Id",
                table: "MatchLiveSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyenJudgeScores_Athletes_AthleteId",
                table: "QuyenJudgeScores");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyenJudgeScores_Events_EventId",
                table: "QuyenJudgeScores");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyenJudgeScores_Teams_TeamId",
                table: "QuyenJudgeScores");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyenLuotHoanThanhs_Athletes_AthleteId",
                table: "QuyenLuotHoanThanhs");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyenLuotHoanThanhs_Events_EventId",
                table: "QuyenLuotHoanThanhs");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyenLuotHoanThanhs_Teams_TeamId",
                table: "QuyenLuotHoanThanhs");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyenResults_Athletes_AthleteId",
                table: "QuyenResults");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyenResults_Events_EventId",
                table: "QuyenResults");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyenResults_Teams_TeamId",
                table: "QuyenResults");

            migrationBuilder.DropIndex(
                name: "IX_QuyenResults_AthleteId",
                table: "QuyenResults");

            migrationBuilder.DropIndex(
                name: "IX_QuyenResults_EventId",
                table: "QuyenResults");

            migrationBuilder.DropIndex(
                name: "IX_QuyenResults_TeamId",
                table: "QuyenResults");

            migrationBuilder.DropIndex(
                name: "IX_QuyenLuotHoanThanhs_AthleteId",
                table: "QuyenLuotHoanThanhs");

            migrationBuilder.DropIndex(
                name: "IX_QuyenLuotHoanThanhs_EventId",
                table: "QuyenLuotHoanThanhs");

            migrationBuilder.DropIndex(
                name: "IX_QuyenLuotHoanThanhs_TeamId",
                table: "QuyenLuotHoanThanhs");

            migrationBuilder.DropIndex(
                name: "IX_QuyenJudgeScores_AthleteId",
                table: "QuyenJudgeScores");

            migrationBuilder.DropIndex(
                name: "IX_QuyenJudgeScores_EventId",
                table: "QuyenJudgeScores");

            migrationBuilder.DropIndex(
                name: "IX_QuyenJudgeScores_TeamId",
                table: "QuyenJudgeScores");

            migrationBuilder.DropColumn(
                name: "HeSoBac",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "HeSoDong",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "HeSoVang",
                table: "Tournaments");
        }
    }
}
