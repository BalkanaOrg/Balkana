using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class CommunityTeamsAndInvites4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunityJoinRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommunityTeamId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityJoinRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommunityJoinRequests_CommunityTeams_CommunityTeamId",
                        column: x => x.CommunityTeamId,
                        principalTable: "CommunityTeams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityTeams_Tag_GameId",
                table: "CommunityTeams",
                columns: new[] { "Tag", "GameId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityJoinRequests_CommunityTeamId",
                table: "CommunityJoinRequests",
                column: "CommunityTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityJoinRequests_UserId",
                table: "CommunityJoinRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityJoinRequests");

            migrationBuilder.DropIndex(
                name: "IX_CommunityTeams_Tag_GameId",
                table: "CommunityTeams");
        }
    }
}
