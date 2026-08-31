using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vovinam_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDonViVaAnhTrongTai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnhDaiDien",
                table: "TrongTais",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DonVi",
                table: "TrongTais",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnhDaiDien",
                table: "TrongTais");

            migrationBuilder.DropColumn(
                name: "DonVi",
                table: "TrongTais");
        }
    }
}
