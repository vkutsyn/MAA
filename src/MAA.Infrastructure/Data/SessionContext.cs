using MAA.Domain;
using MAA.Domain.Rules;
using EligibilityDomain = MAA.Domain.Eligibility;
using MAA.Domain.Sessions;
using MAA.Domain.Wizard;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for session management.
/// Manages sessions, users, encryption-related entities, and rules engine data.
/// </summary>
public class SessionContext : DbContext
{
    public SessionContext(DbContextOptions<SessionContext> options) : base(options)
    {
    }

    /// <summary>
    /// Sessions: anonymous and authenticated user sessions
    /// </summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>
    /// Session answers: encrypted/plain wizard responses
    /// </summary>
    public DbSet<SessionAnswer> SessionAnswers => Set<SessionAnswer>();

    /// <summary>
    /// Encryption keys: versioned keys for data encryption
    /// </summary>
    public DbSet<EncryptionKey> EncryptionKeys => Set<EncryptionKey>();

    /// <summary>
    /// Users: registered user accounts (Phase 5)
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Medicaid Programs: Programs offered by each state (Phase 2)
    /// </summary>
    public DbSet<MedicaidProgram> MedicaidPrograms => Set<MedicaidProgram>();

    /// <summary>
    /// Eligibility Rules: Versioned rules for program eligibility (Phase 2)
    /// </summary>
    public DbSet<EligibilityRule> EligibilityRules => Set<EligibilityRule>();

    /// <summary>
    /// Federal Poverty Levels: Annual FPL thresholds (Phase 2)
    /// </summary>
    public DbSet<FederalPovertyLevel> FederalPovertyLevels => Set<FederalPovertyLevel>();

    /// <summary>
    /// Eligibility rule sets: versioned rule bundles (V2)
    /// </summary>
    public DbSet<EligibilityDomain.RuleSetVersion> EligibilityRuleSetVersions =>
        Set<EligibilityDomain.RuleSetVersion>();

    /// <summary>
    /// Eligibility rules (V2)
    /// </summary>
    public DbSet<EligibilityDomain.EligibilityRule> EligibilityRulesV2 =>
        Set<EligibilityDomain.EligibilityRule>();

    /// <summary>
    /// Program definitions (V2)
    /// </summary>
    public DbSet<EligibilityDomain.ProgramDefinition> ProgramDefinitions =>
        Set<EligibilityDomain.ProgramDefinition>();

    /// <summary>
    /// Federal Poverty Levels (V2)
    /// </summary>
    public DbSet<EligibilityDomain.FederalPovertyLevel> FederalPovertyLevelsV2 =>
        Set<EligibilityDomain.FederalPovertyLevel>();

    /// <summary>
    /// State Contexts: Medicaid jurisdiction context per session
    /// </summary>
    public DbSet<Domain.StateContext.StateContext> StateContexts => Set<Domain.StateContext.StateContext>();

    /// <summary>
    /// State Configurations: State-specific Medicaid program configurations
    /// </summary>
    public DbSet<Domain.StateContext.StateConfiguration> StateConfigurations => Set<Domain.StateContext.StateConfiguration>();

    /// <summary>
    /// Wizard Sessions: per-session wizard state
    /// </summary>
    public DbSet<WizardSession> WizardSessions => Set<WizardSession>();

    /// <summary>
    /// Step Answers: per-step wizard answer data
    /// </summary>
    public DbSet<StepAnswer> StepAnswers => Set<StepAnswer>();

    /// <summary>
    /// Step Progress: per-step completion status
    /// </summary>
    public DbSet<StepProgress> StepProgress => Set<StepProgress>();

    /// <summary>
    /// Questions: eligibility question definitions
    /// </summary>
    public DbSet<Question> Questions => Set<Question>();

    /// <summary>
    /// Conditional Rules: visibility conditions for questions
    /// </summary>
    public DbSet<ConditionalRule> ConditionalRules => Set<ConditionalRule>();

    /// <summary>
    /// Question Options: selectable options for questions
    /// </summary>
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSessionEntity(modelBuilder);
        ConfigureSessionAnswerEntity(modelBuilder);
        ConfigureEncryptionKeyEntity(modelBuilder);
        ConfigureUserEntity(modelBuilder);
        ConfigureMedicaidProgramEntity(modelBuilder);
        ConfigureEligibilityRuleEntity(modelBuilder);
        ConfigureFederalPovertyLevelEntity(modelBuilder);
        ConfigureQuestionEntities(modelBuilder);

        // Apply configuration from separate configuration classes
        modelBuilder.ApplyConfiguration(new Infrastructure.StateContext.StateContextConfiguration());
        modelBuilder.ApplyConfiguration(new Infrastructure.StateContext.StateConfigurationConfiguration());
        modelBuilder.ApplyConfiguration(new Infrastructure.Wizard.WizardSessionConfiguration());
        modelBuilder.ApplyConfiguration(new Infrastructure.Wizard.StepAnswerConfiguration());
        modelBuilder.ApplyConfiguration(new Infrastructure.Wizard.StepProgressConfiguration());
        modelBuilder.ApplyConfiguration(new Infrastructure.Eligibility.RuleSetVersionConfiguration());
        modelBuilder.ApplyConfiguration(new Infrastructure.Eligibility.ProgramDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new Infrastructure.Eligibility.EligibilityRuleConfiguration());
        modelBuilder.ApplyConfiguration(new Infrastructure.Eligibility.FederalPovertyLevelConfiguration());
    }

    private void ConfigureSessionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("sessions");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Generated by application

            entity.Property(e => e.State)
                .IsRequired()
                .HasConversion<string>() // Store as VARCHAR
                .HasMaxLength(20);

            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.UserAgent).IsRequired().HasMaxLength(500);
            entity.Property(e => e.SessionType).IsRequired().HasMaxLength(50).HasDefaultValue("anonymous");
            entity.Property(e => e.EncryptionKeyVersion).IsRequired();
            entity.Property(e => e.Data).IsRequired().HasColumnType("jsonb").HasDefaultValue("{}");
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.InactivityTimeoutAt).IsRequired();
            entity.Property(e => e.LastActivityAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.IsRevoked).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Version).HasDefaultValue(1).IsConcurrencyToken();

            // Indexes
            entity.HasIndex(e => e.UserId).HasFilter("user_id IS NOT NULL");
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.Id).IsUnique();

            // Relationships
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<EncryptionKey>()
                .WithMany()
                .HasForeignKey(e => e.EncryptionKeyVersion)
                .HasPrincipalKey(k => k.KeyVersion)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureSessionAnswerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionAnswer>(entity =>
        {
            entity.ToTable("session_answers");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Generated by application

            entity.Property(e => e.SessionId).IsRequired();
            entity.Property(e => e.FieldKey).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FieldType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AnswerPlain).HasMaxLength(1000);
            entity.Property(e => e.AnswerEncrypted).HasColumnType("text");
            entity.Property(e => e.AnswerHash).HasMaxLength(256);
            entity.Property(e => e.KeyVersion); // Nullable for non-PII answers
            entity.Property(e => e.IsPii).HasDefaultValue(false);
            entity.Property(e => e.ValidationErrors).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Version).HasDefaultValue(1).IsConcurrencyToken();

            // Indexes
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.AnswerHash)
                .IsUnique()
                .HasFilter("answer_hash IS NOT NULL");

            // Relationships
            entity.HasOne<Session>()
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<EncryptionKey>()
                .WithMany()
                .HasForeignKey(e => e.KeyVersion)
                .HasPrincipalKey(k => k.KeyVersion)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureEncryptionKeyEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EncryptionKey>(entity =>
        {
            entity.ToTable("encryption_keys");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Generated by application

            entity.Property(e => e.KeyVersion).IsRequired();
            entity.Property(e => e.KeyIdVault).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Algorithm).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Metadata).HasColumnType("jsonb");

            // Indexes - unique KeyVersion, only one active key per algorithm
            entity.HasIndex(e => e.KeyVersion).IsUnique();
            entity.HasIndex(e => new { e.Algorithm, e.IsActive })
                .IsUnique()
                .HasFilter("\"IsActive\" = TRUE");
        });
    }

    private void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Generated by application

            entity.Property(e => e.Email).IsRequired().HasMaxLength(320); // RFC 5321 max
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Role).IsRequired(); // User role for RBAC
            entity.Property(e => e.EmailVerified).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Version).HasDefaultValue(1).IsConcurrencyToken();

            // Indexes
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }

    private void ConfigureMedicaidProgramEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MedicaidProgram>(entity =>
        {
            entity.ToTable("medicaid_programs");

            entity.HasKey(e => e.ProgramId);
            entity.Property(e => e.ProgramId).ValueGeneratedNever(); // Generated by application

            entity.Property(e => e.StateCode).IsRequired().HasMaxLength(2);
            entity.Property(e => e.ProgramName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProgramCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.EligibilityPathway).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            // Unique constraints
            entity.HasIndex(e => e.ProgramCode).IsUnique();
            entity.HasIndex(e => new { e.StateCode, e.ProgramName }).IsUnique();

            // Query optimization indexes
            entity.HasIndex(e => new { e.StateCode, e.EligibilityPathway });

            // Relationships
            entity.HasMany(e => e.Rules)
                .WithOne(r => r.Program)
                .HasForeignKey(r => r.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureEligibilityRuleEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EligibilityRule>(entity =>
        {
            entity.ToTable("eligibility_rules");

            entity.HasKey(e => e.RuleId);
            entity.Property(e => e.RuleId).ValueGeneratedNever(); // Generated by application

            entity.Property(e => e.ProgramId).IsRequired();
            entity.Property(e => e.StateCode).IsRequired().HasMaxLength(2);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Version).IsRequired().HasColumnType("numeric(4,2)");
            entity.Property(e => e.RuleLogic).IsRequired().HasColumnType("jsonb");
            entity.Property(e => e.EffectiveDate).IsRequired().HasColumnType("date");
            entity.Property(e => e.EndDate).HasColumnType("date");
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Description).HasColumnType("text");

            // Unique constraints
            entity.HasIndex(e => new { e.ProgramId, e.Version }).IsUnique();

            // Query optimization indexes
            entity.HasIndex(e => new { e.ProgramId, e.EffectiveDate, e.EndDate });
            entity.HasIndex(e => new { e.StateCode, e.EffectiveDate });

            // Relationships
            entity.HasOne(e => e.Program)
                .WithMany(p => p.Rules)
                .HasForeignKey(e => e.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureFederalPovertyLevelEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FederalPovertyLevel>(entity =>
        {
            entity.ToTable("federal_poverty_levels");

            entity.HasKey(e => e.FplId);
            entity.Property(e => e.FplId).ValueGeneratedNever(); // Generated by application

            entity.Property(e => e.Year).IsRequired();
            entity.Property(e => e.HouseholdSize).IsRequired();
            entity.Property(e => e.AnnualIncomeCents).IsRequired();
            entity.Property(e => e.StateCode).HasMaxLength(2);
            entity.Property(e => e.AdjustmentMultiplier).HasColumnType("numeric(3,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            // Unique constraint for (year, household_size, state_code)
            entity.HasIndex(e => new { e.Year, e.HouseholdSize, e.StateCode }).IsUnique();

            // Query optimization indexes
            entity.HasIndex(e => new { e.Year, e.HouseholdSize });
        });
    }

    private void ConfigureQuestionEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("questions");

            entity.HasKey(e => e.QuestionId);
            entity.Property(e => e.QuestionId).ValueGeneratedNever();

            entity.Property(e => e.StateCode).IsRequired().HasMaxLength(2);
            entity.Property(e => e.ProgramCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.QuestionText).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.FieldType).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.IsRequired).HasDefaultValue(false);
            entity.Property(e => e.HelpText).HasMaxLength(2000);
            entity.Property(e => e.ValidationRegex).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            entity.HasIndex(e => new { e.StateCode, e.ProgramCode });
            entity.HasIndex(e => new { e.StateCode, e.ProgramCode, e.DisplayOrder }).IsUnique();
            entity.HasIndex(e => e.ConditionalRuleId);

            entity.HasOne(e => e.ConditionalRule)
                .WithMany(r => r.Questions)
                .HasForeignKey(e => e.ConditionalRuleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Options)
                .WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConditionalRule>(entity =>
        {
            entity.ToTable("conditional_rules");

            entity.HasKey(e => e.ConditionalRuleId);
            entity.Property(e => e.ConditionalRuleId).ValueGeneratedNever();

            entity.Property(e => e.RuleExpression).IsRequired().HasMaxLength(5000);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.ToTable("question_options");

            entity.HasKey(e => e.OptionId);
            entity.Property(e => e.OptionId).ValueGeneratedNever();

            entity.Property(e => e.OptionLabel).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OptionValue).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            entity.HasIndex(e => new { e.QuestionId, e.OptionValue }).IsUnique();
            entity.HasIndex(e => new { e.QuestionId, e.DisplayOrder }).IsUnique();
        });
    }
}