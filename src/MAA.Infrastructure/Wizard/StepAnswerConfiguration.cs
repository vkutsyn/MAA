using MAA.Domain.Wizard;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MAA.Infrastructure.Wizard;

/// <summary>
/// Entity Framework Core configuration for StepAnswer entity.
/// </summary>
public class StepAnswerConfiguration : IEntityTypeConfiguration<StepAnswer>
{
    public void Configure(EntityTypeBuilder<StepAnswer> builder)
    {
        builder.ToTable("step_answers");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.SessionId)
            .IsRequired()
            .HasColumnName("session_id");

        builder.Property(e => e.StepId)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("step_id");

        builder.Property(e => e.AnswerData)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("answer_data");

        builder.Property(e => e.SchemaVersion)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("schema_version");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasColumnName("status");

        builder.Property(e => e.SubmittedAt)
            .IsRequired()
            .HasColumnName("submitted_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(e => e.Version)
            .HasDefaultValue(1)
            .IsConcurrencyToken()
            .HasColumnName("version");

        builder.HasIndex(e => new { e.SessionId, e.StepId })
            .HasDatabaseName("IX_StepAnswers_SessionId_StepId")
            .IsUnique();

        builder.HasIndex(e => e.SessionId)
            .HasDatabaseName("IX_StepAnswers_SessionId");
    }
}
