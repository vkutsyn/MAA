# Database Migration Rollback Procedures

## Overview

This document describes rollback procedures for database migrations in production environments.

## Local Development Rollback

To rollback all migrations to initial state:

```bash
dotnet ef database update 0 --project src/MAA.Infrastructure --startup-project src/MAA.API
```

To rollback to a specific migration:

```bash
dotnet ef database update <MigrationName> --project src/MAA.Infrastructure --startup-project src/MAA.API
```

## Production Rollback Strategy

### Pre-Rollback Checklist

1. **Backup Database**: Always create a backup before rollback
   ```bash
   pg_dump -h <host> -U <user> -d <database> -F c -b -v -f backup_$(date +%Y%m%d_%H%M%S).dump
   ```

2. **Check Active Sessions**: Ensure no active user sessions will be affected
   ```sql
   SELECT COUNT(*) FROM sessions WHERE is_revoked = FALSE AND expires_at > NOW();
   ```

3. **Notify Team**: Alert operations team of planned rollback

### Rollback Execution

1. **Stop Application**: Scale down to 0 instances to prevent new connections
   ```bash
   az webapp stop --name <app-name> --resource-group <rg-name>
   ```

2. **Execute Rollback**: Run EF Core migration rollback
   ```bash
   dotnet ef database update <PreviousMigrationName> --connection "<connection-string>"
   ```

3. **Verify Rollback**: Check database schema matches expected state
   ```sql
   SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';
   ```

4. **Restart Application**: Scale back up
   ```bash
   az webapp start --name <app-name> --resource-group <rg-name>
   ```

### Data Preservation During Rollback

- **Sessions Table**: Data preserved unless migration explicitly drops table
- **Encryption Keys**: Never rollback migrations that deactivate keys (data loss risk)
- **User Accounts**: Always preserve user data; use migration scripts to transform, not drop

### Recovery from Failed Rollback

If rollback fails:

1. **Restore from Backup**:
   ```bash
   pg_restore -h <host> -U <user> -d <database> -v backup_<timestamp>.dump
   ```

2. **Re-apply Migrations**: Start fresh from backup state
   ```bash
   dotnet ef database update --project src/MAA.Infrastructure
   ```

3. **Validate Data Integrity**: Run smoke tests
   ```bash
   dotnet test --filter Category=Smoke
   ```

## Migration Best Practices

- Always write `Down()` methods for reversible migrations
- Test rollback in staging before production
- Document breaking changes in migration comments
- Use transactions for data migrations
- Never drop tables with `HttpOnly` production data without explicit approval

## Contact

For production rollback assistance, contact:
- DevOps Team: devops@example.com
- Database Admin: dba@example.com
