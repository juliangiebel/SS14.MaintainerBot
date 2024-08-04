using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.MaintainerBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PullRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallationId = table.Column<long>(type: "bigint", nullable: false),
                    GhRepoId = table.Column<long>(type: "bigint", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MergeProcesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PullRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MergeDelay = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MergeProcesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MergeProcesses_PullRequest_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PullRequestComment",
                columns: table => new
                {
                    PullRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<long>(type: "bigint", nullable: false),
                    CommentType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequestComment", x => new { x.PullRequestId, x.CommentId });
                    table.ForeignKey(
                        name: "FK_PullRequestComment_PullRequest_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviewer",
                columns: table => new
                {
                    PullRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    GhUserId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviewer", x => new { x.PullRequestId, x.GhUserId });
                    table.ForeignKey(
                        name: "FK_Reviewer_PullRequest_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MergeProcesses_PullRequestId",
                table: "MergeProcesses",
                column: "PullRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PullRequest_InstallationId_GhRepoId_Number",
                table: "PullRequest",
                columns: new[] { "InstallationId", "GhRepoId", "Number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MergeProcesses");

            migrationBuilder.DropTable(
                name: "PullRequestComment");

            migrationBuilder.DropTable(
                name: "Reviewer");

            migrationBuilder.DropTable(
                name: "PullRequest");
        }
    }
}
