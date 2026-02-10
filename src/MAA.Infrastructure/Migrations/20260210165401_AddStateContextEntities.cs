using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStateContextEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "state_configurations",
                columns: table => new
                {
                    state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    state_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    medicaid_program_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    config_data = table.Column<string>(type: "jsonb", nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_state_configurations", x => x.state_code);
                });

            migrationBuilder.CreateTable(
                name: "state_contexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    state_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    zip_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    is_manual_override = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_state_contexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_state_contexts_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_state_contexts_state_configurations_state_code",
                        column: x => x.state_code,
                        principalTable: "state_configurations",
                        principalColumn: "state_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StateConfigurations_IsActive",
                table: "state_configurations",
                column: "is_active",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_StateConfigurations_StateCode_Version",
                table: "state_configurations",
                columns: new[] { "state_code", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateContexts_SessionId",
                table: "state_contexts",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateContexts_StateCode",
                table: "state_contexts",
                column: "state_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "state_contexts");

            migrationBuilder.DropTable(
                name: "state_configurations");
        }
    }
}
