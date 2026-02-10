using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWizardSessionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "step_answers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    answer_data = table.Column<string>(type: "jsonb", nullable: false),
                    schema_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_step_answers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "step_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_step_progress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wizard_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_step_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wizard_sessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StepAnswers_SessionId",
                table: "step_answers",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_StepAnswers_SessionId_StepId",
                table: "step_answers",
                columns: new[] { "session_id", "step_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StepProgress_SessionId_Status",
                table: "step_progress",
                columns: new[] { "session_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_StepProgress_SessionId_StepId",
                table: "step_progress",
                columns: new[] { "session_id", "step_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WizardSessions_LastActivityAt",
                table: "wizard_sessions",
                column: "last_activity_at");

            migrationBuilder.CreateIndex(
                name: "IX_WizardSessions_SessionId",
                table: "wizard_sessions",
                column: "session_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "step_answers");

            migrationBuilder.DropTable(
                name: "step_progress");

            migrationBuilder.DropTable(
                name: "wizard_sessions");
        }
    }
}
