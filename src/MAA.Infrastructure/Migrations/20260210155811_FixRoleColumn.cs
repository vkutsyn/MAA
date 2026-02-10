using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRoleColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure the Role column exists in users table
            // This handles cases where the column might not exist or has wrong case
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Drop the column if it exists with any case variation
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                              WHERE table_name = 'users' AND column_name = 'role') THEN
                        ALTER TABLE users DROP COLUMN role;
                    END IF;
                    
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                              WHERE table_name = 'users' AND column_name = 'Role') THEN
                        ALTER TABLE users DROP COLUMN ""Role"";
                    END IF;
                    
                    -- Add the column with proper case (quoted to preserve case)
                    ALTER TABLE users ADD COLUMN ""Role"" integer NOT NULL DEFAULT 1;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");
        }
    }
}
