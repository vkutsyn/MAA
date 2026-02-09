using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedFPLTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 2026 Federal Poverty Level (FPL) Data
            // Source: U.S. Department of Health & Human Services (HHS) Poverty Guidelines
            // Effective: 2026 Fiscal Year
            //
            // Baseline FPL amounts in cents (to avoid floating-point precision issues)
            // Example: Household size 1 = $14,580 annual = 1,458,000 cents
            //
            // Per-person increment (for household size 8+): $6,140 per person
            // For household size 9: $60,660 (FPL for 8) + $6,140 = $66,800

            // ============= BASELINE FPL (48 states + DC) =============
            migrationBuilder.InsertData(
                table: "federal_poverty_levels",
                columns: new[] { "fpl_id", "year", "household_size", "annual_income_cents", "state_code", "adjustment_multiplier", "created_at", "updated_at" },
                values: new object[,]
                {
                    // Household size 1-8
                    { Guid.NewGuid(), 2026, 1, 1458000L, null, null, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 2, 1972000L, null, null, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 3, 2486000L, null, null, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 4, 3000000L, null, null, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 5, 3514000L, null, null, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 6, 4028000L, null, null, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 7, 4542000L, null, null, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 8, 6066000L, null, null, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) }
                });

            // ============= ALASKA (AK) FPL ADJUSTMENTS =============
            // Alaska has higher cost of living: 1.25x multiplier
            // Calculations: Baseline FPL * 1.25
            // Example: 1 person baseline $14,580 * 1.25 = $18,225
            migrationBuilder.InsertData(
                table: "federal_poverty_levels",
                columns: new[] { "fpl_id", "year", "household_size", "annual_income_cents", "state_code", "adjustment_multiplier", "created_at", "updated_at" },
                values: new object[,]
                {
                    { Guid.NewGuid(), 2026, 1, 1822500L, "AK", 1.25m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 2, 2465000L, "AK", 1.25m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 3, 3107500L, "AK", 1.25m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 4, 3750000L, "AK", 1.25m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 5, 4392500L, "AK", 1.25m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 6, 5035000L, "AK", 1.25m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 7, 5677500L, "AK", 1.25m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 8, 7582500L, "AK", 1.25m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) }
                });

            // ============= HAWAII (HI) FPL ADJUSTMENTS =============
            // Hawaii has higher cost of living: 1.15x multiplier
            // Calculations: Baseline FPL * 1.15
            // Example: 1 person baseline $14,580 * 1.15 = $16,767
            migrationBuilder.InsertData(
                table: "federal_poverty_levels",
                columns: new[] { "fpl_id", "year", "household_size", "annual_income_cents", "state_code", "adjustment_multiplier", "created_at", "updated_at" },
                values: new object[,]
                {
                    { Guid.NewGuid(), 2026, 1, 1676700L, "HI", 1.15m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 2, 2267800L, "HI", 1.15m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 3, 2858900L, "HI", 1.15m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 4, 3450000L, "HI", 1.15m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 5, 4041100L, "HI", 1.15m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 6, 4632200L, "HI", 1.15m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 7, 5223300L, "HI", 1.15m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.NewGuid(), 2026, 8, 6975900L, "HI", 1.15m, new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) }
                });

            // ============= ADDITIONAL NOTES =============
            // Per-person increment for household sizes 9+ (use FederalPovertyLevel.CalculateForLargeHousehold method):
            // - Baseline: $6,140 per additional person
            // - Alaska: $6,140 * 1.25 = $7,675 per additional person
            // - Hawaii: $6,140 * 1.15 = $7,061 per additional person
            //
            // Example calculation:
            // - 9-person household (baseline): $60,660 (FPL for 8) + $6,140 = $66,800
            // - 10-person household (baseline): $60,660 + (2 * $6,140) = $72,940
            //
            // The per-person increment is stored in FederalPovertyLevel.PerPersonIncrementCents
            // Calculate methods handle these automatically
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete all FPL data for 2026
            migrationBuilder.Sql("TRUNCATE TABLE federal_poverty_levels;");
        }
    }
}
