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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop pgcrypto extension 
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS pgcrypto;");
        }
    }
}
