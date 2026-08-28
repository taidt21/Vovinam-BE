using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VovinamApi.Data;

#nullable disable

namespace vovinam_backend.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260826172600_AddAthleteAnhDaiDien")]
    public class AddAthleteAnhDaiDien : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnhDaiDien",
                table: "Athletes",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnhDaiDien",
                table: "Athletes");
        }
    }
}
