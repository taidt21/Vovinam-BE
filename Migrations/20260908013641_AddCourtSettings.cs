using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vovinam_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCourtSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourtSettings",
                columns: table => new
                {
                    CourtId = table.Column<string>(type: "TEXT", nullable: false),
                    TongSoHiep = table.Column<int>(type: "INTEGER", nullable: false),
                    ThoiGianHiepGiay = table.Column<int>(type: "INTEGER", nullable: false),
                    ThoiGianNghiGiay = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourtSettings", x => x.CourtId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourtSettings");
        }
    }
}
