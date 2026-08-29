using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vovinam_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTieuDeTheVaLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TieuDeThe",
                table: "Tournaments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TheVdvLogos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DuongDan = table.Column<string>(type: "TEXT", nullable: false),
                    ThuTu = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheVdvLogos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TheVdvLogos");

            migrationBuilder.DropColumn(
                name: "TieuDeThe",
                table: "Tournaments");
        }
    }
}
