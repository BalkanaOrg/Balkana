using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class FixCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Series_SeriesId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_csMaps_MapId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Pictures_Players_PlayerId",
                table: "Pictures");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Nationalities_NationalityId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStatistics_CS2_Matches_MatchId",
                table: "PlayerStatistics_CS2");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStatistics_CS2_Players_PlayerId",
                table: "PlayerStatistics_CS2");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTeamTransfers_Players_PlayerId",
                table: "PlayerTeamTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTeamTransfers_Positions_PositionId",
                table: "PlayerTeamTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTeamTransfers_Teams_TeamId",
                table: "PlayerTeamTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Games_GameId",
                table: "Positions");

            migrationBuilder.DropForeignKey(
                name: "FK_Series_Games_GameId",
                table: "Series");

            migrationBuilder.DropForeignKey(
                name: "FK_Series_Teams_TeamAId",
                table: "Series");

            migrationBuilder.DropForeignKey(
                name: "FK_Series_Teams_TeamBId",
                table: "Series");

            migrationBuilder.DropForeignKey(
                name: "FK_Series_Tournaments_TournamentId",
                table: "Series");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Games_GameId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Organizers_OrganizerId",
                table: "Tournaments");

            migrationBuilder.DropTable(
                name: "csMaps");

            migrationBuilder.DropIndex(
                name: "IX_Series_GameId",
                table: "Series");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerStatistics_CS2",
                table: "PlayerStatistics_CS2");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "Series");

            migrationBuilder.RenameTable(
                name: "PlayerStatistics_CS2",
                newName: "PlayerStats");

            migrationBuilder.RenameColumn(
                name: "VOD",
                table: "Matches",
                newName: "TeamBSourceSlot");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerStatistics_CS2_PlayerId",
                table: "PlayerStats",
                newName: "IX_PlayerStats_PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerStatistics_CS2_MatchId",
                table: "PlayerStats",
                newName: "IX_PlayerStats_MatchId");

            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "MapId",
                table: "Matches",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CompetitionType",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalMatchId",
                table: "Matches",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GameMode",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameVersion",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MatchLoL_MapId",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchType",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PlayedAt",
                table: "Matches",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Matches",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TeamAId",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamASourceSlot",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TeamBId",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "_5k",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "_4k",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "_3k",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "_2k",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "_1v5",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "_1v4",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "_1v3",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "_1v2",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "_1v1",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "_1k",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "WallbangKills",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "UD",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TsideRoundsWon",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TK",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TD",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "NoScopeKills",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Kills",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "KAST",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "HSkills",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "HLTV2",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "HLTV1",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "FK",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "FD",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Deaths",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Damage",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CollateralKills",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CTsideRoundsWon",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Assists",
                table: "PlayerStats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ChampionId",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChampionName",
                table: "PlayerStats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreepScore",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GoldEarned",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HLTV3",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWinner",
                table: "PlayerStats",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Item0",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Item1",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Item2",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Item3",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Item4",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Item5",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Item6",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lane",
                table: "PlayerStats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayerStatistic_LoL_Assists",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayerStatistic_LoL_Deaths",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayerStatistic_LoL_Kills",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "PlayerStats",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatType",
                table: "PlayerStats",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Team",
                table: "PlayerStats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalDamageToChampions",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalDamageToObjectives",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VisionScore",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerStats",
                table: "PlayerStats",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "GameMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    PictureURL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isActiveDuty = table.Column<bool>(type: "bit", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameMaps_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GameProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UUID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameProfiles_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatchVersion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_GameId",
                table: "Tournaments",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ExternalMatchId_Source",
                table: "Matches",
                columns: new[] { "ExternalMatchId", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamAId",
                table: "Matches",
                column: "TeamAId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamBId",
                table: "Matches",
                column: "TeamBId");

            migrationBuilder.CreateIndex(
                name: "IX_GameMaps_GameId",
                table: "GameMaps",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameProfiles_PlayerId",
                table: "GameProfiles",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameProfiles_UUID_Provider",
                table: "GameProfiles",
                columns: new[] { "UUID", "Provider" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_GameMaps_MapId",
                table: "Matches",
                column: "MapId",
                principalTable: "GameMaps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Series_SeriesId",
                table: "Matches",
                column: "SeriesId",
                principalTable: "Series",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamAId",
                table: "Matches",
                column: "TeamAId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamBId",
                table: "Matches",
                column: "TeamBId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pictures_Players_PlayerId",
                table: "Pictures",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Nationalities_NationalityId",
                table: "Players",
                column: "NationalityId",
                principalTable: "Nationalities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStats_Matches_MatchId",
                table: "PlayerStats",
                column: "MatchId",
                principalTable: "Matches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStats_Players_PlayerId",
                table: "PlayerStats",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTeamTransfers_Players_PlayerId",
                table: "PlayerTeamTransfers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTeamTransfers_Positions_PositionId",
                table: "PlayerTeamTransfers",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTeamTransfers_Teams_TeamId",
                table: "PlayerTeamTransfers",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Games_GameId",
                table: "Positions",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Teams_TeamAId",
                table: "Series",
                column: "TeamAId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Teams_TeamBId",
                table: "Series",
                column: "TeamBId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Tournaments_TournamentId",
                table: "Series",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Games_GameId",
                table: "Teams",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Games_GameId",
                table: "Tournaments",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Organizers_OrganizerId",
                table: "Tournaments",
                column: "OrganizerId",
                principalTable: "Organizers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_GameMaps_MapId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Series_SeriesId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamAId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamBId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Pictures_Players_PlayerId",
                table: "Pictures");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Nationalities_NationalityId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStats_Matches_MatchId",
                table: "PlayerStats");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStats_Players_PlayerId",
                table: "PlayerStats");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTeamTransfers_Players_PlayerId",
                table: "PlayerTeamTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTeamTransfers_Positions_PositionId",
                table: "PlayerTeamTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTeamTransfers_Teams_TeamId",
                table: "PlayerTeamTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Games_GameId",
                table: "Positions");

            migrationBuilder.DropForeignKey(
                name: "FK_Series_Teams_TeamAId",
                table: "Series");

            migrationBuilder.DropForeignKey(
                name: "FK_Series_Teams_TeamBId",
                table: "Series");

            migrationBuilder.DropForeignKey(
                name: "FK_Series_Tournaments_TournamentId",
                table: "Series");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Games_GameId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Games_GameId",
                table: "Tournaments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Organizers_OrganizerId",
                table: "Tournaments");

            migrationBuilder.DropTable(
                name: "GameMaps");

            migrationBuilder.DropTable(
                name: "GameProfiles");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_GameId",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Matches_ExternalMatchId_Source",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TeamAId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TeamBId",
                table: "Matches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerStats",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "CompetitionType",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ExternalMatchId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "GameMode",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "GameVersion",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "MatchLoL_MapId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "MatchType",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PlayedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamAId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamASourceSlot",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamBId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ChampionId",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "ChampionName",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "CreepScore",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "GoldEarned",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "HLTV3",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "IsWinner",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "Item0",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "Item1",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "Item2",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "Item3",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "Item4",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "Item5",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "Item6",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "Lane",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "PlayerStatistic_LoL_Assists",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "PlayerStatistic_LoL_Deaths",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "PlayerStatistic_LoL_Kills",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "StatType",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "Team",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "TotalDamageToChampions",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "TotalDamageToObjectives",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "VisionScore",
                table: "PlayerStats");

            migrationBuilder.RenameTable(
                name: "PlayerStats",
                newName: "PlayerStatistics_CS2");

            migrationBuilder.RenameColumn(
                name: "TeamBSourceSlot",
                table: "Matches",
                newName: "VOD");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerStats_PlayerId",
                table: "PlayerStatistics_CS2",
                newName: "IX_PlayerStatistics_CS2_PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerStats_MatchId",
                table: "PlayerStatistics_CS2",
                newName: "IX_PlayerStatistics_CS2_MatchId");

            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "Series",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "MapId",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "_5k",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "_4k",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "_3k",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "_2k",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "_1v5",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "_1v4",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "_1v3",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "_1v2",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "_1v1",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "_1k",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WallbangKills",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UD",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TsideRoundsWon",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TK",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TD",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "NoScopeKills",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Kills",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "KAST",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HSkills",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HLTV2",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HLTV1",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FK",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FD",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Deaths",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Damage",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CollateralKills",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CTsideRoundsWon",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Assists",
                table: "PlayerStatistics_CS2",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerStatistics_CS2",
                table: "PlayerStatistics_CS2",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "csMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    PictureURL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isActiveDuty = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_csMaps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Series_GameId",
                table: "Series",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Series_SeriesId",
                table: "Matches",
                column: "SeriesId",
                principalTable: "Series",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_csMaps_MapId",
                table: "Matches",
                column: "MapId",
                principalTable: "csMaps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pictures_Players_PlayerId",
                table: "Pictures",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Nationalities_NationalityId",
                table: "Players",
                column: "NationalityId",
                principalTable: "Nationalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStatistics_CS2_Matches_MatchId",
                table: "PlayerStatistics_CS2",
                column: "MatchId",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStatistics_CS2_Players_PlayerId",
                table: "PlayerStatistics_CS2",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTeamTransfers_Players_PlayerId",
                table: "PlayerTeamTransfers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTeamTransfers_Positions_PositionId",
                table: "PlayerTeamTransfers",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTeamTransfers_Teams_TeamId",
                table: "PlayerTeamTransfers",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Games_GameId",
                table: "Positions",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Games_GameId",
                table: "Series",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Teams_TeamAId",
                table: "Series",
                column: "TeamAId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Teams_TeamBId",
                table: "Series",
                column: "TeamBId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Tournaments_TournamentId",
                table: "Series",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Games_GameId",
                table: "Teams",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Organizers_OrganizerId",
                table: "Tournaments",
                column: "OrganizerId",
                principalTable: "Organizers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
