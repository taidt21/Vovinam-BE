using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vovinam_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddChoPhepDongHangBaQuyen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChoPhepDongHangBaQuyen",
                table: "Tournaments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChoPhepDongHangBaQuyen",
                table: "Tournaments");
        }
    }
}
