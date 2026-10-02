using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMutationOutboxAndScopedUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rehearsed on disposable SQL. Never guess unknown or structurally inconsistent legacy values.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Projects WHERE LEN(Slug) > 100)
                    THROW 51000, 'Project slugs longer than 100 require explicit reconciliation.', 1;
                IF EXISTS (SELECT UserId FROM TimeEntries WHERE EndTime IS NULL GROUP BY UserId HAVING COUNT(*) > 1)
                    THROW 51000, 'Duplicate active timers require explicit reconciliation.', 1;
                IF EXISTS (SELECT 1 FROM Projects WHERE ProjectType NOT IN (N'0', N'1', N'Personal', N'Team'))
                    THROW 51000, 'Unknown legacy ProjectType requires explicit reconciliation.', 1;
                IF EXISTS (SELECT 1 FROM Projects WHERE (ProjectType IN (N'0', N'Personal') AND WorkspaceId IS NOT NULL)
                    OR (ProjectType IN (N'1', N'Team') AND WorkspaceId IS NULL))
                    THROW 51000, 'ProjectType and workspace disagree; review legacy intent before migration.', 1;
                UPDATE Projects SET ProjectType = CASE ProjectType WHEN N'0' THEN N'Personal' WHEN N'1' THEN N'Team' ELSE ProjectType END
                    WHERE ProjectType IN (N'0', N'1');
                """);
            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Slug",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_WorkspaceId",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_UserId",
                table: "TimeEntries",
                column: "UserId",
                unique: true,
                filter: "[EndTime] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerId_Slug",
                table: "Projects",
                columns: new[] { "OwnerId", "Slug" },
                unique: true,
                filter: "[WorkspaceId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_WorkspaceId_Slug",
                table: "Projects",
                columns: new[] { "WorkspaceId", "Slug" },
                unique: true,
                filter: "[WorkspaceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CompletedAt_CreatedAt",
                table: "OutboxMessages",
                columns: new[] { "CompletedAt", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT Slug FROM Projects GROUP BY Slug HAVING COUNT(*) > 1)
                    THROW 51000, 'Global slug uniqueness cannot be restored until duplicates are reconciled.', 1;
                IF EXISTS (SELECT 1 FROM OutboxMessages WHERE CompletedAt IS NULL)
                    THROW 51000, 'Pending delivery/cleanup must be drained or exported before rollback.', 1;
                """);
            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_UserId",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerId_Slug",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_WorkspaceId_Slug",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Projects",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerId",
                table: "Projects",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Slug",
                table: "Projects",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_WorkspaceId",
                table: "Projects",
                column: "WorkspaceId");
        }
    }
}
