using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class PlayerStatisticUpdate6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isFinished",
                table: "Series",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isFinished",
                table: "Series");
        }
    }
}
