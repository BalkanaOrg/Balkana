using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class Branding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BrandId",
                table: "Teams",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Brandings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tag = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    yearFounded = table.Column<int>(type: "int", nullable: false),
                    LogoURL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FounderId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ManagerId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Brandings_AspNetUsers_FounderId",
                        column: x => x.FounderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Brandings_AspNetUsers_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_BrandId",
                table: "Teams",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Brandings_FounderId",
                table: "Brandings",
                column: "FounderId");

            migrationBuilder.CreateIndex(
                name: "IX_Brandings_ManagerId",
                table: "Brandings",
                column: "ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Brandings_BrandId",
                table: "Teams",
                column: "BrandId",
                principalTable: "Brandings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Brandings_BrandId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "Brandings");

            migrationBuilder.DropIndex(
                name: "IX_Teams_BrandId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "Teams");
        }
    }
}
