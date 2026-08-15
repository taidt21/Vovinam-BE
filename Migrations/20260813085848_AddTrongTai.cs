using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vovinam_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTrongTai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrongTais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourtId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ThuTuGiamDinh = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrongTais", x => x.Id);
                });

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
                name: "TrongTais");
        }
    }
}
