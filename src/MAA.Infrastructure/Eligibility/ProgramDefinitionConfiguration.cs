using MAA.Domain.Eligibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MAA.Infrastructure.Eligibility;

public class ProgramDefinitionConfiguration : IEntityTypeConfiguration<ProgramDefinition>
{
    public void Configure(EntityTypeBuilder<ProgramDefinition> builder)
    {
        builder.ToTable("program_definitions");

        builder.HasKey(e => e.ProgramCode);
        builder.Property(e => e.ProgramCode)
            .HasColumnName("program_code")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.StateCode)
            .HasColumnName("state_code")
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(e => e.ProgramName)
            .HasColumnName("program_name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(e => e.StateCode)
            .HasDatabaseName("IX_ProgramDefinitions_State");
    }
}
