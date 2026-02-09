using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRulesEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "federal_poverty_levels",
                columns: table => new
                {
                    FplId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    HouseholdSize = table.Column<int>(type: "integer", nullable: false),
                    AnnualIncomeCents = table.Column<long>(type: "bigint", nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    AdjustmentMultiplier = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_federal_poverty_levels", x => x.FplId);
                });

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
                    table.ForeignKey(
                        name: "FK_eligibility_rules_medicaid_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "medicaid_programs",
                        principalColumn: "ProgramId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_eligibility_rules_ProgramId_EffectiveDate_EndDate",
                table: "eligibility_rules",
                columns: new[] { "ProgramId", "EffectiveDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_eligibility_rules_ProgramId_Version",
                table: "eligibility_rules",
                columns: new[] { "ProgramId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_eligibility_rules_StateCode_EffectiveDate",
                table: "eligibility_rules",
                columns: new[] { "StateCode", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_federal_poverty_levels_Year_HouseholdSize",
                table: "federal_poverty_levels",
                columns: new[] { "Year", "HouseholdSize" });

            migrationBuilder.CreateIndex(
                name: "IX_federal_poverty_levels_Year_HouseholdSize_StateCode",
                table: "federal_poverty_levels",
                columns: new[] { "Year", "HouseholdSize", "StateCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medicaid_programs_ProgramCode",
                table: "medicaid_programs",
                column: "ProgramCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medicaid_programs_StateCode_EligibilityPathway",
                table: "medicaid_programs",
                columns: new[] { "StateCode", "EligibilityPathway" });

            migrationBuilder.CreateIndex(
                name: "IX_medicaid_programs_StateCode_ProgramName",
                table: "medicaid_programs",
                columns: new[] { "StateCode", "ProgramName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eligibility_rules");

            migrationBuilder.DropTable(
                name: "federal_poverty_levels");

            migrationBuilder.DropTable(
                name: "medicaid_programs");
        }
    }
}
