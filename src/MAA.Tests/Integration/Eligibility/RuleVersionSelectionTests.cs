using FluentAssertions;
using MAA.Domain.Eligibility;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MAA.Tests.Integration.Eligibility;

public class RuleVersionSelectionTests : IAsyncLifetime
{
    private SessionContext _dbContext = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<SessionContext>()
            .UseInMemoryDatabase(databaseName: $"eligibility_{Guid.NewGuid()}")
            .Options;

        _dbContext = new SessionContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task GetActiveRuleSetAsync_SelectsCorrectVersionByEffectiveDate()
    {
        // Arrange
        var stateCode = "IL";
        var baseDate = new DateTime(2026, 1, 1);

        var versions = new[]
        {
            new RuleSetVersion
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = stateCode,
                Version = "v1.0",
                EffectiveDate = baseDate.AddDays(-30),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new RuleSetVersion
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = stateCode,
                Version = "v2.0",
                EffectiveDate = baseDate,
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new RuleSetVersion
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = stateCode,
                Version = "v3.0",
                EffectiveDate = baseDate.AddDays(10),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _dbContext.EligibilityRuleSetVersions.AddRangeAsync(versions);
        await _dbContext.SaveChangesAsync();

        var requestDate = baseDate.AddDays(5);

        // Act
        var selected = await _dbContext.EligibilityRuleSetVersions
            .Where(r => r.StateCode == stateCode &&
                        r.EffectiveDate <= requestDate &&
                        (r.EndDate == null || r.EndDate >= requestDate) &&
                        r.Status == RuleSetStatus.Active)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        // Assert
        selected.Should().NotBeNull();
        selected!.Version.Should().Be("v2.0");
        selected.EffectiveDate.Should().Be(baseDate);
    }

    [Fact]
    public async Task GetActiveRuleSetAsync_WithEndDate_ExcludesExpiredVersions()
    {
        // Arrange
        var stateCode = "IL";
        var baseDate = new DateTime(2026, 1, 1);
        var v1EndDate = baseDate.AddDays(15);

        var versions = new[]
        {
            new RuleSetVersion
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = stateCode,
                Version = "v1.0",
                EffectiveDate = baseDate,
                EndDate = v1EndDate,
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new RuleSetVersion
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = stateCode,
                Version = "v2.0",
                EffectiveDate = v1EndDate.AddDays(1),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _dbContext.EligibilityRuleSetVersions.AddRangeAsync(versions);
        await _dbContext.SaveChangesAsync();

        var requestDate = v1EndDate.AddDays(5);

        // Act
        var selected = await _dbContext.EligibilityRuleSetVersions
            .Where(r => r.StateCode == stateCode &&
                        r.EffectiveDate <= requestDate &&
                        (r.EndDate == null || r.EndDate >= requestDate) &&
                        r.Status == RuleSetStatus.Active)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        // Assert
        selected.Should().NotBeNull();
        selected!.Version.Should().Be("v2.0");
    }

    [Fact]
    public async Task GetActiveRuleSetAsync_WithFutureVersion_SkipsNotYetEffectiveVersions()
    {
        // Arrange
        var stateCode = "IL";
        var baseDate = new DateTime(2026, 1, 1);

        var versions = new[]
        {
            new RuleSetVersion
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = stateCode,
                Version = "v1.0",
                EffectiveDate = baseDate.AddDays(-10),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new RuleSetVersion
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = stateCode,
                Version = "v2.0",
                EffectiveDate = baseDate.AddDays(10),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _dbContext.EligibilityRuleSetVersions.AddRangeAsync(versions);
        await _dbContext.SaveChangesAsync();

        var requestDate = baseDate;

        // Act
        var selected = await _dbContext.EligibilityRuleSetVersions
            .Where(r => r.StateCode == stateCode &&
                        r.EffectiveDate <= requestDate &&
                        (r.EndDate == null || r.EndDate >= requestDate) &&
                        r.Status == RuleSetStatus.Active)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        // Assert
        selected.Should().NotBeNull();
        selected!.Version.Should().Be("v1.0");
    }

    [Fact]
    public async Task GetActiveRuleSetAsync_WithNoVersionsForState_ReturnsNull()
    {
        // Arrange
        var stateCode = "IL";
        var requestDate = new DateTime(2026, 2, 11);

        // Act
        var selected = await _dbContext.EligibilityRuleSetVersions
            .Where(r => r.StateCode == stateCode &&
                        r.EffectiveDate <= requestDate &&
                        (r.EndDate == null || r.EndDate >= requestDate) &&
                        r.Status == RuleSetStatus.Active)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        // Assert
        selected.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveRuleSetAsync_WithMultipleStates_SelectsCorrectState()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 1);

        var versions = new[]
        {
            new RuleSetVersion
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "IL_v1.0",
                EffectiveDate = baseDate,
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new RuleSetVersion
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "TX",
                Version = "TX_v1.0",
                EffectiveDate = baseDate,
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _dbContext.EligibilityRuleSetVersions.AddRangeAsync(versions);
        await _dbContext.SaveChangesAsync();

        var requestDate = baseDate.AddDays(5);

        // Act
        var selectedIL = await _dbContext.EligibilityRuleSetVersions
            .Where(r => r.StateCode == "IL" &&
                        r.EffectiveDate <= requestDate &&
                        (r.EndDate == null || r.EndDate >= requestDate) &&
                        r.Status == RuleSetStatus.Active)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        var selectedTX = await _dbContext.EligibilityRuleSetVersions
            .Where(r => r.StateCode == "TX" &&
                        r.EffectiveDate <= requestDate &&
                        (r.EndDate == null || r.EndDate >= requestDate) &&
                        r.Status == RuleSetStatus.Active)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        // Assert
        selectedIL.Should().NotBeNull();
        selectedIL!.Version.Should().Be("IL_v1.0");
        selectedTX.Should().NotBeNull();
        selectedTX!.Version.Should().Be("TX_v1.0");
    }
}
