using MAA.Domain.Wizard;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MAA.Infrastructure.Wizard;

/// <summary>
/// Entity Framework Core configuration for StepProgress entity.
/// </summary>
public class StepProgressConfiguration : IEntityTypeConfiguration<StepProgress>
{
    public void Configure(EntityTypeBuilder<StepProgress> builder)
    {
        builder.ToTable("step_progress");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.SessionId)
            .IsRequired()
            .HasColumnName("session_id");

        builder.Property(e => e.StepId)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("step_id");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasColumnName("status");

        builder.Property(e => e.LastUpdatedAt)
            .IsRequired()
            .HasColumnName("last_updated_at");

        builder.Property(e => e.Version)
            .HasDefaultValue(1)
            .IsConcurrencyToken()
            .HasColumnName("version");

        builder.HasIndex(e => new { e.SessionId, e.StepId })
            .HasDatabaseName("IX_StepProgress_SessionId_StepId")
            .IsUnique();

        builder.HasIndex(e => new { e.SessionId, e.Status })
            .HasDatabaseName("IX_StepProgress_SessionId_Status");
    }
}
