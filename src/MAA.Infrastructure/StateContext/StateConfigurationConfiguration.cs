using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MAA.Infrastructure.StateContext;

/// <summary>
/// Entity Framework Core configuration for StateConfiguration entity
/// </summary>
public class StateConfigurationConfiguration : IEntityTypeConfiguration<Domain.StateContext.StateConfiguration>
{
    public void Configure(EntityTypeBuilder<Domain.StateContext.StateConfiguration> builder)
    {
        builder.ToTable("state_configurations");

        // Primary key
        builder.HasKey(e => e.StateCode);

        builder.Property(e => e.StateCode)
            .IsRequired()
            .HasMaxLength(2)
            .HasColumnName("state_code");

        // Properties
        builder.Property(e => e.StateName)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("state_name");

        builder.Property(e => e.MedicaidProgramName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("medicaid_program_name");

        builder.Property(e => e.ConfigData)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("config_data"); // PostgreSQL JSONB for efficient querying

        builder.Property(e => e.EffectiveDate)
            .IsRequired()
            .HasColumnName("effective_date");

        builder.Property(e => e.Version)
            .IsRequired()
            .HasDefaultValue(1)
            .HasColumnName("version");

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_StateConfigurations_IsActive")
            .HasFilter("is_active = true"); // Partial index for active configs only

        builder.HasIndex(e => new { e.StateCode, e.Version })
            .HasDatabaseName("IX_StateConfigurations_StateCode_Version")
            .IsUnique();

        // Relationships (StateContexts navigation configured in StateContextConfiguration)
    }
}
