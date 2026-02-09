using FluentAssertions;
using MAA.Application.Eligibility.Caching;
using MAA.Application.Eligibility.Repositories;
using MAA.Application.Eligibility.Services;
using MAA.Domain.Rules;
using MAA.Domain.Rules.Exceptions;
using Moq;
using Xunit;

namespace MAA.Tests.Unit.Eligibility;

/// <summary>
/// Unit tests for StateRuleLoader service
/// 
/// Phase 5 Implementation: T047
/// 
/// Purpose:
/// - Tests state-scoped rule loading functionality
/// - Validates caching behavior for multi-program evaluation
/// - Tests cache hits and misses
/// - Tests state validation (IL, CA, NY, TX, FL)
/// - Tests error scenarios (invalid state, no rules found)
/// 
/// Test Categories:
/// - Happy path: Load IL rules, CA rules, etc.
/// - Cache behavior: Hit on second call, invalid on state change
/// - Error handling: Invalid state codes, missing data
/// - State validation: Supported vs unsupported states
/// 
/// Mocking Strategy:
/// - Mock IRuleRepository to control database responses
/// - Mock IRuleCacheService to verify cache calls
/// - Test loader logic in isolation without database
/// 
/// Reference:
/// - Spec: specs/002-rules-engine/spec.md US3
/// - Phase 5 Task: T047 State-Specific Rule Evaluation
/// </summary>
[Trait("Category", "Unit")]
public class StateRuleLoaderTests
{
    private readonly Mock<IRuleRepository> _mockRepository;
    private readonly Mock<IRuleCacheService> _mockCacheService;
    private readonly StateRuleLoader _loader;

    public StateRuleLoaderTests()
    {
        _mockRepository = new Mock<IRuleRepository>();
        _mockCacheService = new Mock<IRuleCacheService>();
        _loader = new StateRuleLoader(_mockRepository.Object, _mockCacheService.Object);
    }

    #region Load IL Rules

    [Fact]
    public async Task LoadRulesForStateAsync_IL_ReturnsILRulesOnly()
    {
        // Arrange
        var ilRules = CreateILRules(2);
        _mockCacheService.Setup(c => c.GetCachedRulesByState("IL")).Returns((IEnumerable<EligibilityRule>)null!);
        _mockRepository.Setup(r => r.GetRulesByStateAsync("IL")).ReturnsAsync(ilRules);

        // Act
        var result = await _loader.LoadRulesForStateAsync("IL");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.StateCode.Should().Be("IL"));
    }

    #endregion

    #region Load CA Rules

    [Fact]
    public async Task LoadRulesForStateAsync_CA_ReturnsCArulesOnly()
    {
        // Arrange
        var caRules = CreateCAKeyRules(2);
        _mockCacheService.Setup(c => c.GetCachedRulesByState("CA")).Returns((IEnumerable<EligibilityRule>)null!);
        _mockRepository.Setup(r => r.GetRulesByStateAsync("CA")).ReturnsAsync(caRules);

        // Act
        var result = await _loader.LoadRulesForStateAsync("CA");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.StateCode.Should().Be("CA"));
    }

    #endregion

    #region Cache Hit

    [Fact]
    public async Task LoadRulesForStateAsync_CacheHit_ReturnsFromCacheWithoutDatabaseQuery()
    {
        // Arrange
        var cachedRules = CreateILRules(2);
        _mockCacheService.Setup(c => c.GetCachedRulesByState("IL")).Returns(cachedRules);

        // Act
        var result = await _loader.LoadRulesForStateAsync("IL");

        // Assert - Cache hit should prevent database call
        result.Should().HaveCount(2);
        _mockRepository.Verify(r => r.GetRulesByStateAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Cache Miss

    [Fact]
    public async Task LoadRulesForStateAsync_CacheMiss_PopulatesCacheAfterDatabaseQuery()
    {
        // Arrange
        var ilRules = CreateILRules(2);
        _mockCacheService.Setup(c => c.GetCachedRulesByState("IL")).Returns(new List<EligibilityRule>());
        _mockRepository.Setup(r => r.GetRulesByStateAsync("IL")).ReturnsAsync(ilRules);

        // Act
        var result = await _loader.LoadRulesForStateAsync("IL");

        // Assert - Verify cache was populated
        result.Should().HaveCount(2);
        _mockCacheService.Verify(c => c.SetCachedRule("IL", It.IsAny<Guid>(), It.IsAny<EligibilityRule>()), Times.Exactly(2));
    }

    #endregion

    #region Invalid State Code

    [Fact]
    public async Task LoadRulesForStateAsync_InvalidStateCode_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _loader.LoadRulesForStateAsync("XX"));
    }

    [Fact]
    public async Task LoadRulesForStateAsync_UnsupportedState_IncludesValidStatesList()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _loader.LoadRulesForStateAsync("AZ"));
        ex.Message.Should().Contain("Supported states");
    }

    #endregion

    #region Null or Empty State Code

    [Fact]
    public async Task LoadRulesForStateAsync_NullStateCode_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _loader.LoadRulesForStateAsync(null!));
    }

    [Fact]
    public async Task LoadRulesForStateAsync_EmptyStateCode_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _loader.LoadRulesForStateAsync(""));
    }

    [Fact]
    public async Task LoadRulesForStateAsync_WhitespaceStateCode_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _loader.LoadRulesForStateAsync("   "));
    }

    #endregion

    #region No Rules Found

    [Fact]
    public async Task LoadRulesForStateAsync_NoRulesInDatabase_ThrowsEligibilityEvaluationException()
    {
        // Arrange
        _mockCacheService.Setup(c => c.GetCachedRulesByState("IL")).Returns(new List<EligibilityRule>());
        _mockRepository.Setup(r => r.GetRulesByStateAsync("IL")).ReturnsAsync(new List<EligibilityRule>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EligibilityEvaluationException>(() => _loader.LoadRulesForStateAsync("IL"));
        ex.Message.Should().Contain("No active rules found");
    }

    #endregion

    #region Load Programs With Rules

    [Fact]
    public async Task LoadProgramsWithRulesForStateAsync_TX_ReturnsTXProgramsWithRules()
    {
        // Arrange
        var programsWithRules = CreateTXProgramsWithRules(3);
        _mockRepository.Setup(r => r.GetProgramsWithActiveRulesByStateAsync("TX"))
            .ReturnsAsync(programsWithRules);

        // Act
        var result = await _loader.LoadProgramsWithRulesForStateAsync("TX");

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(x => x.program.StateCode.Should().Be("TX"));
    }

    [Fact]
    public async Task LoadProgramsWithRulesForStateAsync_NoProgramsForState_ThrowsException()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetProgramsWithActiveRulesByStateAsync("NY"))
            .ReturnsAsync(new List<(MedicaidProgram, EligibilityRule)>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EligibilityEvaluationException>(() => _loader.LoadProgramsWithRulesForStateAsync("NY"));
        ex.Message.Should().Contain("No programs with active rules found");
    }

    #endregion

    #region Case Insensitivity

    [Fact]
    public async Task LoadRulesForStateAsync_LowercaseStateCode_IsNormalized()
    {
        // Arrange
        var ilRules = CreateILRules(1);
        _mockCacheService.Setup(c => c.GetCachedRulesByState("IL")).Returns((IEnumerable<EligibilityRule>)null!);
        _mockRepository.Setup(r => r.GetRulesByStateAsync("IL")).ReturnsAsync(ilRules);

        // Act
        var result = await _loader.LoadRulesForStateAsync("il");

        // Assert
        result.Should().HaveCount(1);
        _mockRepository.Verify(r => r.GetRulesByStateAsync("IL"), Times.Once);
    }

    [Fact]
    public async Task LoadRulesForStateAsync_MixedCaseStateCode_IsNormalized()
    {
        // Arrange
        var caRules = CreateCAKeyRules(1);
        _mockCacheService.Setup(c => c.GetCachedRulesByState("CA")).Returns((IEnumerable<EligibilityRule>)null!);
        _mockRepository.Setup(r => r.GetRulesByStateAsync("CA")).ReturnsAsync(caRules);

        // Act
        var result = await _loader.LoadRulesForStateAsync("Ca");

        // Assert
        result.Should().HaveCount(1);
        _mockRepository.Verify(r => r.GetRulesByStateAsync("CA"), Times.Once);
    }

    #endregion

    #region Invalidate Cache

    [Fact]
    public void InvalidateCacheForState_IL_CallsCacheServiceInvalidateState()
    {
        // Act
        _loader.InvalidateCacheForState("IL");

        // Assert
        _mockCacheService.Verify(c => c.InvalidateState("IL"), Times.Once);
    }

    [Fact]
    public void InvalidateCacheForState_LowercaseStateCode_IsNormalized()
    {
        // Act
        _loader.InvalidateCacheForState("ca");

        // Assert
        _mockCacheService.Verify(c => c.InvalidateState("CA"), Times.Once);
    }

    #endregion

    #region Refresh All States Cache

    [Fact]
    public async Task RefreshAllStatesCacheAsync_SuccessfullyRefunctionshesAllStates()
    {
        // Arrange
        var ilRules = CreateILRules(1);
        var caRules = CreateCAKeyRules(1);
        var nyRules = CreateNYRules(1);
        var txRules = CreateTXRules(1);
        var flRules = CreateFLRules(1);

        _mockCacheService.Setup(c => c.GetCachedRulesByState(It.IsAny<string>())).Returns(new List<EligibilityRule>());
        _mockRepository.Setup(r => r.GetRulesByStateAsync("IL")).ReturnsAsync(ilRules);
        _mockRepository.Setup(r => r.GetRulesByStateAsync("CA")).ReturnsAsync(caRules);
        _mockRepository.Setup(r => r.GetRulesByStateAsync("NY")).ReturnsAsync(nyRules);
        _mockRepository.Setup(r => r.GetRulesByStateAsync("TX")).ReturnsAsync(txRules);
        _mockRepository.Setup(r => r.GetRulesByStateAsync("FL")).ReturnsAsync(flRules);

        // Act
        await _loader.RefreshAllStatesCacheAsync();

        // Assert - All states should have been loaded
        _mockRepository.Verify(r => r.GetRulesByStateAsync("IL"), Times.Once);
        _mockRepository.Verify(r => r.GetRulesByStateAsync("CA"), Times.Once);
        _mockRepository.Verify(r => r.GetRulesByStateAsync("NY"), Times.Once);
        _mockRepository.Verify(r => r.GetRulesByStateAsync("TX"), Times.Once);
        _mockRepository.Verify(r => r.GetRulesByStateAsync("FL"), Times.Once);
    }

    #endregion

    #region Test Helpers

    private List<EligibilityRule> CreateILRules(int count)
    {
        var rules = new List<EligibilityRule>();
        for (int i = 0; i < count; i++)
        {
            rules.Add(new EligibilityRule
            {
                RuleId = Guid.NewGuid(),
                ProgramId = Guid.NewGuid(),
                StateCode = "IL",
                RuleName = $"IL MAGI Adult {i + 1}",
                Version = 1.0m,
                RuleLogic = "{\"<=\": [{\"var\": \"monthly_income_cents\"}, 300000]}",
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                EndDate = null,
                Description = $"Test rule {i + 1}"
            });
        }
        return rules;
    }

    private List<EligibilityRule> CreateCAKeyRules(int count)
    {
        var rules = new List<EligibilityRule>();
        for (int i = 0; i < count; i++)
        {
            rules.Add(new EligibilityRule
            {
                RuleId = Guid.NewGuid(),
                ProgramId = Guid.NewGuid(),
                StateCode = "CA",
                RuleName = $"CA MAGI Adult {i + 1}",
                Version = 1.0m,
                RuleLogic = "{\"<=\": [{\"var\": \"monthly_income_cents\"}, 320000]}",
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                EndDate = null,
                Description = $"Test rule {i + 1}"
            });
        }
        return rules;
    }

    private List<EligibilityRule> CreateNYRules(int count)
    {
        var rules = new List<EligibilityRule>();
        for (int i = 0; i < count; i++)
        {
            rules.Add(new EligibilityRule
            {
                RuleId = Guid.NewGuid(),
                ProgramId = Guid.NewGuid(),
                StateCode = "NY",
                RuleName = $"NY MAGI Adult {i + 1}",
                Version = 1.0m,
                RuleLogic = "{\"<=\": [{\"var\": \"monthly_income_cents\"}, 310000]}",
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                EndDate = null,
                Description = $"Test rule {i + 1}"
            });
        }
        return rules;
    }

    private List<EligibilityRule> CreateTXRules(int count)
    {
        var rules = new List<EligibilityRule>();
        for (int i = 0; i < count; i++)
        {
            rules.Add(new EligibilityRule
            {
                RuleId = Guid.NewGuid(),
                ProgramId = Guid.NewGuid(),
                StateCode = "TX",
                RuleName = $"TX MAGI Adult {i + 1}",
                Version = 1.0m,
                RuleLogic = "{\"<=\": [{\"var\": \"monthly_income_cents\"}, 280000]}",
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                EndDate = null,
                Description = $"Test rule {i + 1}"
            });
        }
        return rules;
    }

    private List<EligibilityRule> CreateFLRules(int count)
    {
        var rules = new List<EligibilityRule>();
        for (int i = 0; i < count; i++)
        {
            rules.Add(new EligibilityRule
            {
                RuleId = Guid.NewGuid(),
                ProgramId = Guid.NewGuid(),
                StateCode = "FL",
                RuleName = $"FL MAGI Adult {i + 1}",
                Version = 1.0m,
                RuleLogic = "{\"<=\": [{\"var\": \"monthly_income_cents\"}, 300000]}",
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                EndDate = null,
                Description = $"Test rule {i + 1}"
            });
        }
        return rules;
    }

    private List<(MedicaidProgram program, EligibilityRule rule)> CreateTXProgramsWithRules(int count)
    {
        var programsWithRules = new List<(MedicaidProgram, EligibilityRule)>();
        for (int i = 0; i < count; i++)
        {
            var programId = Guid.NewGuid();
            var program = new MedicaidProgram
            {
                ProgramId = programId,
                StateCode = "TX",
                ProgramName = $"TX Program {i + 1}",
                ProgramCode = $"TX_PROG_{i + 1}",
                EligibilityPathway = EligibilityPathway.MAGI,
                Description = $"Test program {i + 1}"
            };

            var rule = new EligibilityRule
            {
                RuleId = Guid.NewGuid(),
                ProgramId = programId,
                StateCode = "TX",
                RuleName = $"TX Rule {i + 1}",
                Version = 1.0m,
                RuleLogic = "{\"<=\": [{\"var\": \"monthly_income_cents\"}, 280000]}",
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                EndDate = null,
                Description = $"Test rule {i + 1}"
            };

            programsWithRules.Add((program, rule));
        }
        return programsWithRules;
    }

    #endregion
}
