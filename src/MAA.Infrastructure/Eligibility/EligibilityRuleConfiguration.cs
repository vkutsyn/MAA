using MAA.Domain.Eligibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MAA.Infrastructure.Eligibility;

public class EligibilityRuleConfiguration : IEntityTypeConfiguration<EligibilityRule>
{
    public void Configure(EntityTypeBuilder<EligibilityRule> builder)
    {
        builder.ToTable("eligibility_rules_v2");

        builder.HasKey(e => e.EligibilityRuleId);
        builder.Property(e => e.EligibilityRuleId)
            .HasColumnName("eligibility_rule_id")
            .ValueGeneratedNever();

        builder.Property(e => e.RuleSetVersionId)
            .HasColumnName("rule_set_version_id")
            .IsRequired();

        builder.Property(e => e.ProgramCode)
            .HasColumnName("program_code")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.RuleLogic)
            .HasColumnName("rule_logic")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.Priority)
            .HasColumnName("priority")
            .HasDefaultValue(0);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(e => e.RuleSetVersion)
            .WithMany(rs => rs.Rules)
            .HasForeignKey(e => e.RuleSetVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Program)
            .WithMany(p => p.Rules)
            .HasForeignKey(e => e.ProgramCode)
            .HasPrincipalKey(p => p.ProgramCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.RuleSetVersionId)
            .HasDatabaseName("IX_EligibilityRulesV2_RuleSetVersion");

        builder.HasIndex(e => e.ProgramCode)
            .HasDatabaseName("IX_EligibilityRulesV2_ProgramCode");
    }
}
