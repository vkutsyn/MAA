using MAA.Domain.Wizard;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MAA.Infrastructure.Wizard;

/// <summary>
/// Entity Framework Core configuration for WizardSession entity.
/// </summary>
public class WizardSessionConfiguration : IEntityTypeConfiguration<WizardSession>
{
    public void Configure(EntityTypeBuilder<WizardSession> builder)
    {
        builder.ToTable("wizard_sessions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.SessionId)
            .IsRequired()
            .HasColumnName("session_id");

        builder.Property(e => e.CurrentStepId)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("current_step_id");

        builder.Property(e => e.LastActivityAt)
            .IsRequired()
            .HasColumnName("last_activity_at");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(e => e.Version)
            .HasDefaultValue(1)
            .IsConcurrencyToken()
            .HasColumnName("version");

        builder.HasIndex(e => e.SessionId)
            .HasDatabaseName("IX_WizardSessions_SessionId")
            .IsUnique();

        builder.HasIndex(e => e.LastActivityAt)
            .HasDatabaseName("IX_WizardSessions_LastActivityAt");
    }
}
