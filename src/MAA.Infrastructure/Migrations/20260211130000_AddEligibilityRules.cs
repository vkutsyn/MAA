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
            // Create medicaid_programs table for MAA.Domain.Rules.MedicaidProgram
            migrationBuilder.CreateTable(
                name: "medicaid_programs",
                columns: table => new
                {
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    ProgramName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProgramCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EligibilityPathway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicaid_programs", x => x.ProgramId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicaidPrograms_ProgramCode",
                table: "medicaid_programs",
                column: "ProgramCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicaidPrograms_State_Pathway",
                table: "medicaid_programs",
                columns: new[] { "StateCode", "EligibilityPathway" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicaidPrograms_State_ProgramName",
                table: "medicaid_programs",
                columns: new[] { "StateCode", "ProgramName" },
                unique: true);

            // Create eligibility_rules table for MAA.Domain.Rules.EligibilityRule
            migrationBuilder.CreateTable(
                name: "eligibility_rules",
                columns: table => new
                {
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    RuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    RuleLogic = table.Column<string>(type: "jsonb", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eligibility_rules", x => x.RuleId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityRules_Program_Version",
                table: "eligibility_rules",
                columns: new[] { "ProgramId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityRules_State_EffectiveDate",
                table: "eligibility_rules",
                columns: new[] { "StateCode", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityRules_Program_Dates",
                table: "eligibility_rules",
                columns: new[] { "ProgramId", "EffectiveDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eligibility_rules");

            migrationBuilder.DropTable(
                name: "medicaid_programs");
        }
    }
}
