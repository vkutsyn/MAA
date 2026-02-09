using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAA.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Seeds initial encryption key (version 1) for session data encryption.
    /// This key is referenced by Session.EncryptionKeyVersion and SessionAnswer.KeyVersion.
    /// </summary>
    public partial class SeedEncryptionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed initial encryption key (version 1)
            // KeyVersion serves as the primary lookup key (indexed unique)
            migrationBuilder.InsertData(
                table: "encryption_keys",
                columns: new[] { "Id", "KeyVersion", "KeyIdVault", "Algorithm", "IsActive", "CreatedAt", "RotatedAt", "ExpiresAt", "Metadata" },
                values: new object[]
                {
                    Guid.NewGuid(), // Id
                    1, // KeyVersion
                    "maa-key-v001", // KeyIdVault - reference to Azure Key Vault
                    "AES-256-GCM", // Algorithm
                    true, // IsActive - only one active key per algorithm
                    DateTime.UtcNow, // CreatedAt
                    null, // RotatedAt - null until deactivated
                    null, // ExpiresAt - null means no expiration
                    "{\"purpose\":\"initial_seed\",\"rotationPolicy\":\"annual\"}" // Metadata (JSONB)
                }
            );

            // Create unique index to enforce only one active key per algorithm
            // This prevents accidentally activating multiple keys for the same algorithm
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS idx_active_key_per_algorithm 
                ON encryption_keys(""Algorithm"") 
                WHERE ""IsActive"" = TRUE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the unique index first
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS idx_active_key_per_algorithm;
            ");

            // Remove the seeded encryption key
            migrationBuilder.DeleteData(
                table: "encryption_keys",
                keyColumn: "KeyVersion",
                keyValue: 1
            );
        }
    }
}
