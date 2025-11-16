using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class AddRiotTournamentModels2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RiotTournaments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiotTournamentId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderId = table.Column<int>(type: "int", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TournamentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiotTournaments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiotTournaments_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RiotTournamentCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RiotTournamentId = table.Column<int>(type: "int", nullable: false),
                    SeriesId = table.Column<int>(type: "int", nullable: true),
                    TeamAId = table.Column<int>(type: "int", nullable: true),
                    TeamBId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MapType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PickType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SpectatorType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TeamSize = table.Column<int>(type: "int", nullable: false),
                    AllowedSummonerIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    MatchId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MatchDbId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiotTournamentCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiotTournamentCodes_Matches_MatchDbId",
                        column: x => x.MatchDbId,
                        principalTable: "Matches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RiotTournamentCodes_RiotTournaments_RiotTournamentId",
                        column: x => x.RiotTournamentId,
                        principalTable: "RiotTournaments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RiotTournamentCodes_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RiotTournamentCodes_Teams_TeamAId",
                        column: x => x.TeamAId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RiotTournamentCodes_Teams_TeamBId",
                        column: x => x.TeamBId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiotTournamentCodes_Code",
                table: "RiotTournamentCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiotTournamentCodes_MatchDbId",
                table: "RiotTournamentCodes",
                column: "MatchDbId");

            migrationBuilder.CreateIndex(
                name: "IX_RiotTournamentCodes_RiotTournamentId",
                table: "RiotTournamentCodes",
                column: "RiotTournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_RiotTournamentCodes_SeriesId",
                table: "RiotTournamentCodes",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_RiotTournamentCodes_TeamAId",
                table: "RiotTournamentCodes",
                column: "TeamAId");

            migrationBuilder.CreateIndex(
                name: "IX_RiotTournamentCodes_TeamBId",
                table: "RiotTournamentCodes",
                column: "TeamBId");

            migrationBuilder.CreateIndex(
                name: "IX_RiotTournaments_TournamentId",
                table: "RiotTournaments",
                column: "TournamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiotTournamentCodes");

            migrationBuilder.DropTable(
                name: "RiotTournaments");
        }
    }
}
