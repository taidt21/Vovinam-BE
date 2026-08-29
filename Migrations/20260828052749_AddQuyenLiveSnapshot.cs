using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vovinam_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddQuyenLiveSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuyenLiveSnapshots",
                columns: table => new
                {
                    CourtId = table.Column<string>(type: "TEXT", nullable: false),
                    StateJson = table.Column<string>(type: "TEXT", nullable: false),
                    CapNhatLuc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyenLiveSnapshots", x => x.CourtId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuyenLiveSnapshots");
        }
    }
}
