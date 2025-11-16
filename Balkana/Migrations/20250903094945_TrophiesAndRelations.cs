using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class TrophiesAndRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trophy_Tournaments_TournamentId",
                table: "Trophy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trophy",
                table: "Trophy");

            migrationBuilder.RenameTable(
                name: "Trophy",
                newName: "Trophies");

            migrationBuilder.RenameIndex(
                name: "IX_Trophy_TournamentId",
                table: "Trophies",
                newName: "IX_Trophies_TournamentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trophies",
                table: "Trophies",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "PlayerTrophy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    TrophyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTrophy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerTrophy_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayerTrophy_Trophies_TrophyId",
                        column: x => x.TrophyId,
                        principalTable: "Trophies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeamTrophy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    TrophyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTrophy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamTrophy_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamTrophy_Trophies_TrophyId",
                        column: x => x.TrophyId,
                        principalTable: "Trophies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTrophy_PlayerId",
                table: "PlayerTrophy",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTrophy_TrophyId",
                table: "PlayerTrophy",
                column: "TrophyId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTrophy_TeamId",
                table: "TeamTrophy",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTrophy_TrophyId",
                table: "TeamTrophy",
                column: "TrophyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trophies_Tournaments_TournamentId",
                table: "Trophies",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trophies_Tournaments_TournamentId",
                table: "Trophies");

            migrationBuilder.DropTable(
                name: "PlayerTrophy");

            migrationBuilder.DropTable(
                name: "TeamTrophy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trophies",
                table: "Trophies");

            migrationBuilder.RenameTable(
                name: "Trophies",
                newName: "Trophy");

            migrationBuilder.RenameIndex(
                name: "IX_Trophies_TournamentId",
                table: "Trophy",
                newName: "IX_Trophy_TournamentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trophy",
                table: "Trophy",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Trophy_Tournaments_TournamentId",
                table: "Trophy",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id");
        }
    }
}
