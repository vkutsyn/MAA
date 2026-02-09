using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitializeRulesEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create medicaid_programs table
            // Stores the different Medicaid programs offered by each state (e.g., MAGI Adult, Aged Medicaid)
            migrationBuilder.CreateTable(
                name: "medicaid_programs",
                columns: table => new
                {
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    program_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    program_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    eligibility_pathway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicaid_programs", x => x.program_id);
                    table.UniqueConstraint("AK_medicaid_programs_program_code", x => x.program_code);
                    table.UniqueConstraint("AK_medicaid_programs_state_program", x => new { x.state_code, x.program_name });
                });

            // Create eligibility_rules table
            // Stores versioned eligibility rules for each program with effective date tracking
            migrationBuilder.CreateTable(
                name: "eligibility_rules",
                columns: table => new
                {
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    rule_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    rule_logic = table.Column<string>(type: "jsonb", nullable: false),
                    effective_date = table.Column<DateTime>(type: "date", nullable: false),
                    end_date = table.Column<DateTime>(type: "date", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eligibility_rules", x => x.rule_id);
                    table.ForeignKey(
                        name: "FK_eligibility_rules_medicaid_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "medicaid_programs",
                        principalColumn: "program_id",
                        onDelete: ReferentialAction.Cascade);
                    table.UniqueConstraint("AK_eligibility_rules_program_version", x => new { x.program_id, x.version });
                });

            // Create federal_poverty_levels table
            // Stores FPL thresholds for income calculations, updated annually
            migrationBuilder.CreateTable(
                name: "federal_poverty_levels",
                columns: table => new
                {
                    fpl_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    household_size = table.Column<int>(type: "integer", nullable: false),
                    annual_income_cents = table.Column<long>(type: "bigint", nullable: false),
                    state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    adjustment_multiplier = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_federal_poverty_levels", x => x.fpl_id);
                    table.UniqueConstraint("AK_fpl_year_size_state", x => new { x.year, x.household_size, x.state_code });
                });

            // Create indexes for query performance

            // Index for finding active rules by program and effective date
            migrationBuilder.CreateIndex(
                name: "IX_eligibility_rules_program_effective",
                table: "eligibility_rules",
                columns: new[] { "program_id", "effective_date", "end_date" });

            // Index for state-scoped rule queries (used by rule dashboards and evaluations)
            migrationBuilder.CreateIndex(
                name: "IX_eligibility_rules_state_effective",
                table: "eligibility_rules",
                columns: new[] { "state_code", "effective_date" });

            // Index for state/pathway program lookups
            migrationBuilder.CreateIndex(
                name: "IX_medicaid_programs_state_pathway",
                table: "medicaid_programs",
                columns: new[] { "state_code", "eligibility_pathway" });

            // Index for FPL baseline lookups (NULL state_code = baseline for all states)
            migrationBuilder.CreateIndex(
                name: "IX_fpl_year_size",
                table: "federal_poverty_levels",
                columns: new[] { "year", "household_size" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "federal_poverty_levels");
            migrationBuilder.DropTable(name: "eligibility_rules");
            migrationBuilder.DropTable(name: "medicaid_programs");
        }
    }
}
