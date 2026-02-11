using MAA.Domain.Eligibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MAA.Infrastructure.Eligibility;

public class RuleSetVersionConfiguration : IEntityTypeConfiguration<RuleSetVersion>
{
    public void Configure(EntityTypeBuilder<RuleSetVersion> builder)
    {
        builder.ToTable("eligibility_rule_set_versions");

        builder.HasKey(e => e.RuleSetVersionId);
        builder.Property(e => e.RuleSetVersionId)
            .HasColumnName("rule_set_version_id")
            .ValueGeneratedNever();

        builder.Property(e => e.StateCode)
            .HasColumnName("state_code")
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(e => e.Version)
            .HasColumnName("version")
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.EffectiveDate)
            .HasColumnName("effective_date")
            .IsRequired();

        builder.Property(e => e.EndDate)
            .HasColumnName("end_date");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RuleSetStatus.Active);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(e => new { e.StateCode, e.EffectiveDate })
            .HasDatabaseName("IX_RuleSetVersions_State_EffectiveDate");

        builder.HasIndex(e => new { e.StateCode, e.Version })
            .IsUnique()
            .HasDatabaseName("IX_RuleSetVersions_State_Version");
    }
}
