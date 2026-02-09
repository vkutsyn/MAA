using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnablePgcrypto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pgcrypto extension for encryption functions
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
            
            migrationBuilder.DropIndex(
                name: "IX_encryption_keys_Algorithm_IsActive",
                table: "encryption_keys");

            migrationBuilder.CreateIndex(
                name: "IX_encryption_keys_Algorithm_IsActive",
                table: "encryption_keys",
                columns: new[] { "Algorithm", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_encryption_keys_Algorithm_IsActive",
                table: "encryption_keys");

            migrationBuilder.CreateIndex(
                name: "IX_encryption_keys_Algorithm_IsActive",
                table: "encryption_keys",
                columns: new[] { "Algorithm", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = TRUE");
                
            // Drop pgcrypto extension 
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS pgcrypto;");
        }
    }
}
