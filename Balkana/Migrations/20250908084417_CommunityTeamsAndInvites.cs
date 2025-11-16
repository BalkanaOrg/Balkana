using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class CommunityTeamsAndInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunityTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityTeams_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommunityInvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommunityTeamId = table.Column<int>(type: "int", nullable: false),
                    InviterUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InviteeUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityInvites_AspNetUsers_InviteeUserId",
                        column: x => x.InviteeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommunityInvites_AspNetUsers_InviterUserId",
                        column: x => x.InviterUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommunityInvites_CommunityTeams_CommunityTeamId",
                        column: x => x.CommunityTeamId,
                        principalTable: "CommunityTeams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommunityTeamMembers",
                columns: table => new
                {
                    CommunityTeamId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityTeamMembers", x => new { x.CommunityTeamId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CommunityTeamMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommunityTeamMembers_CommunityTeams_CommunityTeamId",
                        column: x => x.CommunityTeamId,
                        principalTable: "CommunityTeams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommunityTeamTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommunityTeamId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityTeamTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityTeamTransfers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommunityTeamTransfers_CommunityTeams_CommunityTeamId",
                        column: x => x.CommunityTeamId,
                        principalTable: "CommunityTeams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommunityTeamTransfers_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityInvites_CommunityTeamId",
                table: "CommunityInvites",
                column: "CommunityTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityInvites_InviteeUserId",
                table: "CommunityInvites",
                column: "InviteeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityInvites_InviterUserId",
                table: "CommunityInvites",
                column: "InviterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityTeamMembers_UserId",
                table: "CommunityTeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityTeams_GameId",
                table: "CommunityTeams",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityTeamTransfers_CommunityTeamId",
                table: "CommunityTeamTransfers",
                column: "CommunityTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityTeamTransfers_PositionId",
                table: "CommunityTeamTransfers",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityTeamTransfers_UserId",
                table: "CommunityTeamTransfers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityInvites");

            migrationBuilder.DropTable(
                name: "CommunityTeamMembers");

            migrationBuilder.DropTable(
                name: "CommunityTeamTransfers");

            migrationBuilder.DropTable(
                name: "CommunityTeams");
        }
    }
}
