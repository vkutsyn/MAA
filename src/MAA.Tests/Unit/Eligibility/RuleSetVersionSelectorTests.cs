using FluentAssertions;
using MAA.Domain.Eligibility;
using Xunit;

namespace MAA.Tests.Unit.Eligibility;

public class RuleSetVersionSelectorTests
{
    [Fact]
    public void SelectRuleSetVersion_WithMultipleVersions_SelectsMostRecentByEffectiveDate()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 1);
        var selector = new RuleSetVersionSelector();
        
        var versions = new List<RuleSetVersion>
        {
            new()
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "v1.0",
                EffectiveDate = baseDate.AddDays(-30),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "v2.0",
                EffectiveDate = baseDate.AddDays(-15),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "v3.0",
                EffectiveDate = baseDate,
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "v4.0",
                EffectiveDate = baseDate.AddDays(10),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        var requestDate = baseDate.AddDays(5);

        // Act
        var selected = selector.SelectRuleSetVersion(versions, requestDate);

        // Assert
        selected.Should().NotBeNull();
        selected!.Version.Should().Be("v3.0");
        selected.EffectiveDate.Should().Be(baseDate);
    }

    [Fact]
    public void SelectRuleSetVersion_WithExactDateMatch_SelectsVersion()
    {
        // Arrange
        var effectiveDate = new DateTime(2026, 2, 11);
        var selector = new RuleSetVersionSelector();
        
        var versions = new List<RuleSetVersion>
        {
            new()
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "v1.0",
                EffectiveDate = effectiveDate,
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var selected = selector.SelectRuleSetVersion(versions, effectiveDate);

        // Assert
        selected.Should().NotBeNull();
        selected!.Version.Should().Be("v1.0");
    }

    [Fact]
    public void SelectRuleSetVersion_WithFutureDateRequest_SkipsNotYetEffectiveVersions()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 1);
        var selector = new RuleSetVersionSelector();
        
        var versions = new List<RuleSetVersion>
        {
            new()
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "v1.0",
                EffectiveDate = baseDate.AddDays(-10),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "v2.0",
                EffectiveDate = baseDate.AddDays(10),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        var requestDate = baseDate;

        // Act
        var selected = selector.SelectRuleSetVersion(versions, requestDate);

        // Assert
        selected.Should().NotBeNull();
        selected!.Version.Should().Be("v1.0");
    }

    [Fact]
    public void SelectRuleSetVersion_WithNoVersionsBeforeDate_ReturnsNull()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 1);
        var selector = new RuleSetVersionSelector();
        
        var versions = new List<RuleSetVersion>
        {
            new()
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "v1.0",
                EffectiveDate = baseDate.AddDays(10),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        var requestDate = baseDate;

        // Act
        var selected = selector.SelectRuleSetVersion(versions, requestDate);

        // Assert
        selected.Should().BeNull();
    }

    [Fact]
    public void SelectRuleSetVersion_WithRetiredVersions_IgnoresRetiredStatus()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 1);
        var selector = new RuleSetVersionSelector();
        
        var guidV1 = Guid.NewGuid();
        var versions = new List<RuleSetVersion>
        {
            new()
            {
                RuleSetVersionId = guidV1,
                StateCode = "IL",
                Version = "v1.0",
                EffectiveDate = baseDate.AddDays(-10),
                Status = RuleSetStatus.Retired,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "v2.0",
                EffectiveDate = baseDate,
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        var requestDate = baseDate.AddDays(5);

        // Act
        var selected = selector.SelectRuleSetVersion(versions, requestDate);

        // Assert
        selected.Should().NotBeNull();
        selected!.Version.Should().Be("v2.0");
    }

    [Fact]
    public void SelectRuleSetVersion_WithEndDate_IgnoresVersionsAfterEndDate()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 1);
        var selector = new RuleSetVersionSelector();
        
        var endDate = baseDate.AddDays(5);
        var versions = new List<RuleSetVersion>
        {
            new()
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "v1.0",
                EffectiveDate = baseDate,
                EndDate = endDate,
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                RuleSetVersionId = Guid.NewGuid(),
                StateCode = "IL",
                Version = "v2.0",
                EffectiveDate = endDate.AddDays(1),
                Status = RuleSetStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        var requestDate = endDate.AddDays(5);

        // Act
        var selected = selector.SelectRuleSetVersion(versions, requestDate);

        // Assert
        selected.Should().NotBeNull();
        selected!.Version.Should().Be("v2.0");
    }

    [Fact]
    public void SelectRuleSetVersion_WithEmptyVersionsList_ReturnsNull()
    {
        // Arrange
        var selector = new RuleSetVersionSelector();
        var versions = new List<RuleSetVersion>();
        var requestDate = new DateTime(2026, 2, 11);

        // Act
        var selected = selector.SelectRuleSetVersion(versions, requestDate);

        // Assert
        selected.Should().BeNull();
    }

    [Fact]
    public void SelectRuleSetVersion_WithNullVersionsList_ThrowsArgumentNullException()
    {
        // Arrange
        var selector = new RuleSetVersionSelector();
        var requestDate = new DateTime(2026, 2, 11);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            selector.SelectRuleSetVersion(null!, requestDate));
    }
}
