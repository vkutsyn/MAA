using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedPilotStateRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pilot State Configuration
            // 5 states: IL, CA, NY, TX, FL
            // 6+ programs per state (MAGI Adult, Aged, Disabled, Pregnancy, SSI, State-specific)
            // ~30 programs total with base rules

            // ============= ILLINOIS (IL) =============
            migrationBuilder.InsertData(
                table: "medicaid_programs",
                columns: new[] { "ProgramId", "StateCode", "ProgramName", "ProgramCode", "EligibilityPathway", "Description", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { Guid.NewGuid(), "IL", "MAGI Adult", "IL_MAGI_ADULT", "MAGI", "Income-based Medicaid for working-age adults (18-64) without dependent children", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "IL", "MAGI Parent/Caregiver", "IL_MAGI_PARENT", "MAGI", "Income-based Medicaid for parents/caregivers with dependent children", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "IL", "Aged Medicaid", "IL_AGED", "NonMAGI_Aged", "Non-MAGI Medicaid for individuals 65 years and older", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "IL", "Disabled Medicaid", "IL_DISABLED", "NonMAGI_Disabled", "Non-MAGI Medicaid for individuals with disabilities", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "IL", "Pregnancy-Related", "IL_PREGNANCY", "Pregnancy", "Medicaid for pregnant individuals and postpartum care (60-days postpartum)", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "IL", "SSI-Linked Medicaid", "IL_SSI", "SSI_Linked", "Automatic Medicaid for SSI (Social Security Income) recipients", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) }
                });

            // ============= CALIFORNIA (CA) =============
            migrationBuilder.InsertData(
                table: "medicaid_programs",
                columns: new[] { "ProgramId", "StateCode", "ProgramName", "ProgramCode", "EligibilityPathway", "Description", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { Guid.NewGuid(), "CA", "CalMediConnect", "CA_CALMEDICONNECT", "MAGI", "California's MAGI Medicaid expansion program for working-age adults", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "CA", "Family Medicaid", "CA_FAMILY", "MAGI", "Medicaid for families with dependent children", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "CA", "Aged/Blind/Disabled", "CA_ABD", "NonMAGI_Aged", "Non-MAGI Medicaid for aged (65+), blind, and disabled populations", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "CA", "Pregnancy-Related", "CA_PREGNANCY", "Pregnancy", "Coverage for pregnant individuals and 60-day postpartum period", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "CA", "SSI/SSP Medicaid", "CA_SSI", "SSI_Linked", "Automatic Medicaid tied to SSI/State Supplementary Payment", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "CA", "Emergency Medicaid", "CA_EMERGENCY", "Other", "Limited Medicaid for emergency services (non-citizens, undocumented)", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) }
                });

            // ============= NEW YORK (NY) =============
            migrationBuilder.InsertData(
                table: "medicaid_programs",
                columns: new[] { "ProgramId", "StateCode", "ProgramName", "ProgramCode", "EligibilityPathway", "Description", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { Guid.NewGuid(), "NY", "MAGI Medicaid", "NY_MAGI", "MAGI", "Modified Adjusted Gross Income Medicaid for working-age adults", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "NY", "MAGI-Related Family", "NY_MAGIREL", "MAGI", "MAGI Medicaid for families with dependent children at enhanced income limits", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "NY", "Aged and Blind", "NY_AGED_BLIND", "NonMAGI_Aged", "Non-MAGI Medicaid for individuals 65+ and blind individuals", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "NY", "Disabled Persons Care", "NY_DPC", "NonMAGI_Disabled", "Non-MAGI Medicaid for individuals with disabilities (Persons with Disabilities - PWD)", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "NY", "Emergency Medicaid", "NY_EMERGENCY", "Other", "Emergency-only coverage for undocumented immigrants and emergency situations", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "NY", "Refugee Medicaid", "NY_REFUGEE", "Other", "Medicaid for refugees during first 8 months of US residence", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) }
                });

            // ============= TEXAS (TX) =============
            migrationBuilder.InsertData(
                table: "medicaid_programs",
                columns: new[] { "ProgramId", "StateCode", "ProgramName", "ProgramCode", "EligibilityPathway", "Description", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { Guid.NewGuid(), "TX", "CHIP", "TX_CHIP", "MAGI", "Children's Health Insurance Program for low-income children (CHIP = MAGI pathway)", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "TX", "CHIP Perinatal", "TX_CHIP_PERINATAL", "Pregnancy", "CHIP coverage for pregnancy and childbirth (perinatal Medicaid)", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "TX", "SSI-Related Medicaid", "TX_SSI", "SSI_Linked", "Medicaid for SSI recipients (Texas has SSI-related pathway)", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "TX", "Medically Needy", "TX_MEDNEEDY", "NonMAGI_Disabled", "Medicaid for individuals with high medical costs and limited resources (Non-MAGI)", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "TX", "Emergency Medicaid", "TX_EMERGENCY", "Other", "Limited Medicaid for emergency services only", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "TX", "TANF Medicaid", "TX_TANF", "MAGI", "Medicaid for families receiving TANF (Temporary Assistance for Needy Families)", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) }
                });

            // ============= FLORIDA (FL) =============
            migrationBuilder.InsertData(
                table: "medicaid_programs",
                columns: new[] { "ProgramId", "StateCode", "ProgramName", "ProgramCode", "EligibilityPathway", "Description", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { Guid.NewGuid(), "FL", "MAGI Medicaid", "FL_MAGI", "MAGI", "Modified Adjusted Gross Income Medicaid for working-age adults and families", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "FL", "Aged and Disabled", "FL_AGED_DISABLED", "NonMAGI_Aged", "Medicaid for individuals 65+ and disabled (Non-MAGI pathway with lower income limits)", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "FL", "Blind", "FL_BLIND", "NonMAGI_Disabled", "Medicaid for blind individuals (Non-MAGI pathway)", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "FL", "Pregnancy Medicaid", "FL_PREGNANCY", "Pregnancy", "Medicaid for pregnant women and postpartum coverage (60 days postpartum)", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "FL", "Medically Needy", "FL_MEDNEEDY", "NonMAGI_Disabled", "Medicaid for individuals who spend down income through medical expenses", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), "FL", "SSI-Related", "FL_SSI", "SSI_Linked", "Automatic Medicaid for SSI (Social Security Income) recipients", new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) }
                });

            // ============= ELIGIBILITY RULES (Base Rules for Each Program) =============
            // Note: Rules are inserted in two phases to avoid FK constraint issues
            // Phase 1: Insert programs only
            // Phase 2 (T010): Insert FPL data
            // Rules for each program will be created in Phase 3 (US1 evaluation logic)
            
            // This migration focuses on program setup; actual rule logic follows in Phase 3
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete all programs (cascade deletes rules)
            migrationBuilder.Sql("TRUNCATE TABLE medicaid_programs CASCADE;");
        }
    }
}
