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
                columns: new[] { "id", "key_version", "key_id_vault", "algorithm", "is_active", "created_at", "rotated_at", "expires_at", "metadata" },
                values: new object[]
                {
                    Guid.NewGuid(), // id
                    1, // key_version
                    "maa-key-v001", // key_id_vault - reference to Azure Key Vault
                    "AES-256-GCM", // algorithm
                    true, // is_active - only one active key per algorithm
                    DateTime.UtcNow, // created_at
                    null, // rotated_at - null until deactivated
                    null, // expires_at - null means no expiration
                    "{\"purpose\":\"initial_seed\",\"rotationPolicy\":\"annual\"}" // metadata (JSONB)
                }
            );

            // Create unique index to enforce only one active key per algorithm
            // This prevents accidentally activating multiple keys for the same algorithm
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS idx_active_key_per_algorithm 
                ON encryption_keys(algorithm) 
                WHERE is_active = TRUE;
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
                keyColumn: "key_version",
                keyValue: 1
            );
        }
    }
}
