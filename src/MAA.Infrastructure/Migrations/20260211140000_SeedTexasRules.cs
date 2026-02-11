using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedTexasRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop v2 tables if they exist
            migrationBuilder.Sql("DROP TABLE IF EXISTS eligibility_rules_v2 CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS federal_poverty_levels_v2 CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS eligibility_rule_set_versions CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS program_definitions CASCADE;");

            // Seed Texas Medicaid Programs
            var txProgram1 = Guid.NewGuid();
            var txProgram2 = Guid.NewGuid();
            var txProgram3 = Guid.NewGuid();
            var txProgram4 = Guid.NewGuid();
            var txProgram5 = Guid.NewGuid();

            migrationBuilder.InsertData(
                table: "medicaid_programs",
                columns: new[] { "ProgramId", "StateCode", "ProgramName", "ProgramCode", "EligibilityPathway", "Description", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { txProgram1, "TX", "MAGI Adults", "TX_MAGI_ADULT", "MAGI", "Texas Medicaid for adults using MAGI-based income calculations", DateTime.UtcNow, DateTime.UtcNow },
                    { txProgram2, "TX", "MAGI Children", "TX_MAGI_CHILD", "MAGI", "Texas Medicaid and CHIP for children using MAGI-based income calculations", DateTime.UtcNow, DateTime.UtcNow },
                    { txProgram3, "TX", "MAGI Pregnant Women", "TX_MAGI_PREGNANT", "Pregnancy", "Texas Medicaid for pregnant women", DateTime.UtcNow, DateTime.UtcNow },
                    { txProgram4, "TX", "Aged 65+", "TX_NONMAGI_AGED", "NonMAGI_Aged", "Texas Medicaid for individuals aged 65 and older", DateTime.UtcNow, DateTime.UtcNow },
                    { txProgram5, "TX", "Disabled Adults", "TX_NONMAGI_DISABLED", "NonMAGI_Disabled", "Texas Medicaid for disabled adults", DateTime.UtcNow, DateTime.UtcNow }
                });

            // Seed Eligibility Rules for Texas
            var now = DateTime.UtcNow;
            var effectiveDate = new DateTime(2026, 1, 1).ToUniversalTime();

            migrationBuilder.InsertData(
                table: "eligibility_rules",
                columns: new[] { "RuleId", "ProgramId", "StateCode", "RuleName", "Version", "RuleLogic", "EffectiveDate", "EndDate", "CreatedBy", "CreatedAt", "UpdatedAt", "Description" },
                values: new object[,]
                {
                    // Rule 1: MAGI Adults - Age and Income
                    {
                        Guid.NewGuid(),
                        txProgram1,
                        "TX",
                        "TX MAGI Adult Age and Income 2026",
                        1.0m,
                        @"{""and"":[{"">="":[{""var"":""age""},19]},{""<="":[{""var"":""age""},64]},{""<="":[{""var"":""monthly_income""},1800]}]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Eligible if age 19-64 and monthly income <= $1,800 (138% FPL for household of 1 in TX)"
                    },
                    // Rule 2: MAGI Adults - Citizenship Required
                    {
                        Guid.NewGuid(),
                        txProgram1,
                        "TX",
                        "TX MAGI Adult Citizenship 2026",
                        1.1m,
                        @"{""=="":[{""var"":""citizenship_status""},""US_CITIZEN""]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Must be a US Citizen or qualified immigrant"
                    },
                    // Rule 3: MAGI Children - Age and Income
                    {
                        Guid.NewGuid(),
                        txProgram2,
                        "TX",
                        "TX MAGI Child Basic Eligibility 2026",
                        1.0m,
                        @"{""and"":[{""<"":[{""var"":""age""},19]},{""<="":[{""var"":""household_income_percent_fpl""},205]}]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Eligible if under 19 and household income <= 205% FPL (CHIP threshold)"
                    },
                    // Rule 4: MAGI Children - Residency
                    {
                        Guid.NewGuid(),
                        txProgram2,
                        "TX",
                        "TX MAGI Child Residency 2026",
                        1.1m,
                        @"{""=="":[{""var"":""state_of_residence""},""TX""]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Must be a Texas resident"
                    },
                    // Rule 5: Pregnant Women - Basic Eligibility
                    {
                        Guid.NewGuid(),
                        txProgram3,
                        "TX",
                        "TX Pregnant Women Eligibility 2026",
                        1.0m,
                        @"{""and"":[{""=="":[{""var"":""is_pregnant""},true]},{""<="":[{""var"":""household_income_percent_fpl""},198]}]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Eligible if pregnant and household income <= 198% FPL"
                    },
                    // Rule 6: Pregnant Women - Age Range
                    {
                        Guid.NewGuid(),
                        txProgram3,
                        "TX",
                        "TX Pregnant Women Age 2026",
                        1.1m,
                        @"{""and"":[{"">="":[{""var"":""age""},15]},{""<="":[{""var"":""age""},50]}]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Pregnant applicant must be aged 15-50"
                    },
                    // Rule 7: Aged 65+ - Basic Eligibility
                    {
                        Guid.NewGuid(),
                        txProgram4,
                        "TX",
                        "TX Aged Basic Eligibility 2026",
                        1.0m,
                        @"{""and"":[{"">="":[{""var"":""age""},65]},{""<="":[{""var"":""countable_resources""},4000]}]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Eligible if 65+ and countable resources <= $4,000"
                    },
                    // Rule 8: Aged 65+ - Income Limit
                    {
                        Guid.NewGuid(),
                        txProgram4,
                        "TX",
                        "TX Aged Income Limit 2026",
                        1.1m,
                        @"{""<="":[{""var"":""monthly_income""},1000]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Monthly income must be <= $1,000 for aged category"
                    },
                    // Rule 9: Disabled Adults - Disability Status
                    {
                        Guid.NewGuid(),
                        txProgram5,
                        "TX",
                        "TX Disabled SSI Criteria 2026",
                        1.0m,
                        @"{""or"":[{""=="":[{""var"":""receives_ssi""},true]},{""=="":[{""var"":""receives_ssdi""},true]}]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Eligible if receiving SSI or SSDI benefits"
                    },
                    // Rule 10: Disabled Adults - Resource Limit
                    {
                        Guid.NewGuid(),
                        txProgram5,
                        "TX",
                        "TX Disabled Resource Limit 2026",
                        1.1m,
                        @"{""<="":[{""var"":""countable_resources""},4000]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Countable resources must be <= $4,000 for disabled category"
                    },
                    // Rule 11: MAGI Adults - No Other Coverage
                    {
                        Guid.NewGuid(),
                        txProgram1,
                        "TX",
                        "TX MAGI Adult No Other Coverage 2026",
                        1.2m,
                        @"{""!"":[{""var"":""has_other_insurance""}]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Must not have other health insurance coverage"
                    },
                    // Rule 12: MAGI Children - No Medicare
                    {
                        Guid.NewGuid(),
                        txProgram2,
                        "TX",
                        "TX MAGI Child No Medicare 2026",
                        1.2m,
                        @"{""!"":[{""var"":""has_medicare""}]}",
                        effectiveDate,
                        null,
                        null,
                        now,
                        now,
                        "Children must not be eligible for Medicare"
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the seeded rules for Texas
            migrationBuilder.Sql("DELETE FROM eligibility_rules WHERE \"StateCode\" = 'TX';");
            
            // Remove the seeded programs for Texas
            migrationBuilder.Sql("DELETE FROM medicaid_programs WHERE \"StateCode\" = 'TX';");
        }
    }
}
