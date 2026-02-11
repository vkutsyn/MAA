using MAA.Domain.Eligibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MAA.Infrastructure.Eligibility;

public class FederalPovertyLevelConfiguration : IEntityTypeConfiguration<FederalPovertyLevel>
{
    public void Configure(EntityTypeBuilder<FederalPovertyLevel> builder)
    {
        builder.ToTable("federal_poverty_levels_v2");

        builder.HasKey(e => e.FplId);
        builder.Property(e => e.FplId)
            .HasColumnName("fpl_id")
            .ValueGeneratedNever();

        builder.Property(e => e.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(e => e.HouseholdSize)
            .HasColumnName("household_size")
            .IsRequired();

        builder.Property(e => e.AnnualAmount)
            .HasColumnName("annual_amount")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(e => e.StateCode)
            .HasColumnName("state_code")
            .HasMaxLength(2);

        builder.HasIndex(e => new { e.Year, e.HouseholdSize, e.StateCode })
            .IsUnique()
            .HasDatabaseName("IX_FplV2_Year_Size_State");
    }
}
