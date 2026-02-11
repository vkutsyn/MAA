using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEligibilityRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eligibility_rule_set_versions",
                columns: table => new
                {
                    rule_set_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eligibility_rule_set_versions", x => x.rule_set_version_id);
                });

            migrationBuilder.CreateTable(
                name: "program_definitions",
                columns: table => new
                {
                    program_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    program_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_definitions", x => x.program_code);
                });

            migrationBuilder.CreateTable(
                name: "federal_poverty_levels_v2",
                columns: table => new
                {
                    fpl_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    household_size = table.Column<int>(type: "integer", nullable: false),
                    annual_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_federal_poverty_levels_v2", x => x.fpl_id);
                });

            migrationBuilder.CreateTable(
                name: "eligibility_rules_v2",
                columns: table => new
                {
                    eligibility_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_set_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rule_logic = table.Column<string>(type: "jsonb", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eligibility_rules_v2", x => x.eligibility_rule_id);
                    table.ForeignKey(
                        name: "FK_eligibility_rules_v2_eligibility_rule_set_versions_rule_set_version_id",
                        column: x => x.rule_set_version_id,
                        principalTable: "eligibility_rule_set_versions",
                        principalColumn: "rule_set_version_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_eligibility_rules_v2_program_definitions_program_code",
                        column: x => x.program_code,
                        principalTable: "program_definitions",
                        principalColumn: "program_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuleSetVersions_State_EffectiveDate",
                table: "eligibility_rule_set_versions",
                columns: new[] { "state_code", "effective_date" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleSetVersions_State_Version",
                table: "eligibility_rule_set_versions",
                columns: new[] { "state_code", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramDefinitions_State",
                table: "program_definitions",
                column: "state_code");

            migrationBuilder.CreateIndex(
                name: "IX_FplV2_Year_Size_State",
                table: "federal_poverty_levels_v2",
                columns: new[] { "year", "household_size", "state_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityRulesV2_ProgramCode",
                table: "eligibility_rules_v2",
                column: "program_code");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityRulesV2_RuleSetVersion",
                table: "eligibility_rules_v2",
                column: "rule_set_version_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eligibility_rules_v2");

            migrationBuilder.DropTable(
                name: "federal_poverty_levels_v2");

            migrationBuilder.DropTable(
                name: "eligibility_rule_set_versions");

            migrationBuilder.DropTable(
                name: "program_definitions");
        }
    }
}
