using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class AddRiotPendingMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RiotPendingMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TournamentCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RiotTournamentCodeId = table.Column<int>(type: "int", nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImportedMatchDbId = table.Column<int>(type: "int", nullable: true),
                    SeriesId = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiotPendingMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiotPendingMatches_Matches_ImportedMatchDbId",
                        column: x => x.ImportedMatchDbId,
                        principalTable: "Matches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RiotPendingMatches_RiotTournamentCodes_RiotTournamentCodeId",
                        column: x => x.RiotTournamentCodeId,
                        principalTable: "RiotTournamentCodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RiotPendingMatches_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiotPendingMatches_ImportedMatchDbId",
                table: "RiotPendingMatches",
                column: "ImportedMatchDbId");

            migrationBuilder.CreateIndex(
                name: "IX_RiotPendingMatches_MatchId",
                table: "RiotPendingMatches",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RiotPendingMatches_RiotTournamentCodeId",
                table: "RiotPendingMatches",
                column: "RiotTournamentCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_RiotPendingMatches_SeriesId",
                table: "RiotPendingMatches",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_RiotPendingMatches_Status_CreatedAt",
                table: "RiotPendingMatches",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiotPendingMatches");
        }
    }
}
