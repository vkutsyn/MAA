using FluentAssertions;
using MAA.Domain.Rules;
using MAA.Domain.Rules.ValueObjects;
using System;
using System.Collections.Generic;
using Xunit;

namespace MAA.Tests.Unit.Rules;

/// <summary>
/// Unit tests for ExplanationGenerator pure function service.
/// Tests plain-language explanation generation with concrete values and jargon-free output.
/// 
/// Phase 6 Implementation: T054
/// </summary>
public class ExplanationGeneratorTests
{
    private readonly ExplanationGenerator _generator = new();

    #region GenerateEligibilityExplanation Tests

    [Fact]
    public void GenerateEligibilityExplanation_WithIncomeBelowThreshold_IncludesActualIncomeValue()
    {
        // Arrange
        var matchedPrograms = new List<ProgramMatch>
        {
            new ProgramMatch
            {
                ProgramId = Guid.NewGuid(),
                ProgramName = "MAGI Adult",
                Status = EligibilityStatus.LikelyEligible,
                EligibilityPathway = EligibilityPathway.MAGI,
                ConfidenceScore = new ConfidenceScore(85),
                MatchingFactors = new List<string> { "Income below limit" },
                DisqualifyingFactors = new List<string>()
            }
        };

        var input = new UserEligibilityInput
        {
            StateCode = "IL",
            HouseholdSize = 2,
            MonthlyIncomeCents = 210000, // $2,100
            IsCitizen = true,
            CurrentDate = DateTime.UtcNow
        };

        // Act
        var explanation = _generator.GenerateEligibilityExplanation(matchedPrograms, input);

        // Assert
        explanation.Should().Contain("$2100"); // Should include actual income
        explanation.Should().Contain("MAGI Adult");
    }

    [Fact]
    public void GenerateEligibilityExplanation_WithSSIRecipient_ExplainsDisabledMedicaidBypass()
    {
        // Arrange
        var matchedPrograms = new List<ProgramMatch>
        {
            new ProgramMatch
            {
                ProgramId = Guid.NewGuid(),
                ProgramName = "Disabled Medicaid",
                Status = EligibilityStatus.LikelyEligible,
                EligibilityPathway = EligibilityPathway.SSI_Linked,
                ConfidenceScore = new ConfidenceScore(95),
                MatchingFactors = new List<string> { "SSI recipient" },
                DisqualifyingFactors = new List<string>()
            }
        };

        var input = new UserEligibilityInput
        {
            StateCode = "IL",
            HouseholdSize = 1,
            MonthlyIncomeCents = 0,
            IsCitizen = true,
            ReceivesSsi = true, // SSI flag
            CurrentDate = DateTime.UtcNow
        };

        // Act
        var explanation = _generator.GenerateEligibilityExplanation(matchedPrograms, input);

        // Assert
        explanation.Should().Contain("SSI");
        explanation.Should().Contain("Disabled Medicaid");
        explanation.Should().Contain("Social Security Income"); // Definition should be included
    }

    [Fact]
    public void GenerateEligibilityExplanation_WithPregnancy_ExplainsPregnancyQualifyingRoute()
    {
        // Arrange
        var matchedPrograms = new List<ProgramMatch>
        {
            new ProgramMatch
            {
                ProgramId = Guid.NewGuid(),
                ProgramName = "Pregnant Medicaid",
                Status = EligibilityStatus.LikelyEligible,
                EligibilityPathway = EligibilityPathway.Pregnancy,
                ConfidenceScore = new ConfidenceScore(95),
                MatchingFactors = new List<string> { "Pregnancy" },
                DisqualifyingFactors = new List<string>()
            }
        };

        var input = new UserEligibilityInput
        {
            StateCode = "IL",
            HouseholdSize = 1,
            MonthlyIncomeCents = 350000, // $3,500
            IsCitizen = true,
            IsPregnant = true, // Pregnancy flag
            CurrentDate = DateTime.UtcNow
        };

        // Act
        var explanation = _generator.GenerateEligibilityExplanation(matchedPrograms, input);

        // Assert
        explanation.Should().Contain("pregnancy");
        explanation.Should().Contain("Pregnant Medicaid");
    }

    [Fact]
    public void GenerateEligibilityExplanation_WithMultiplePrograms_ListsAllPrograms()
    {
        // Arrange
        var matchedPrograms = new List<ProgramMatch>
        {
            new ProgramMatch
            {
                ProgramId = Guid.NewGuid(),
                ProgramName = "MAGI Adult",
                Status = EligibilityStatus.LikelyEligible,
                EligibilityPathway = EligibilityPathway.MAGI,
                ConfidenceScore = new ConfidenceScore(90),
                MatchingFactors = new List<string> { "Income below limit" },
                DisqualifyingFactors = new List<string>()
            },
            new ProgramMatch
            {
                ProgramId = Guid.NewGuid(),
                ProgramName = "Medicaid Expansion",
                Status = EligibilityStatus.LikelyEligible,
                EligibilityPathway = EligibilityPathway.MAGI,
                ConfidenceScore = new ConfidenceScore(88),
                MatchingFactors = new List<string> { "Eligible under expansion" },
                DisqualifyingFactors = new List<string>()
            }
        };

        var input = new UserEligibilityInput
        {
            StateCode = "IL",
            HouseholdSize = 3,
            MonthlyIncomeCents = 250000, // $2,500
            IsCitizen = true,
            CurrentDate = DateTime.UtcNow
        };

        // Act
        var explanation = _generator.GenerateEligibilityExplanation(matchedPrograms, input);

        // Assert
        explanation.Should().Contain("MAGI Adult");
        explanation.Should().Contain("Medicaid Expansion");
    }

    [Fact]
    public void GenerateEligibilityExplanation_WithNoMatchedPrograms_ReturnsIneligibilityExplanation()
    {
        // Arrange
        var matchedPrograms = new List<ProgramMatch>();

        var input = new UserEligibilityInput
        {
            StateCode = "IL",
            HouseholdSize = 2,
            MonthlyIncomeCents = 500000, // $5,000 - too high
            IsCitizen = true,
            CurrentDate = DateTime.UtcNow
        };

        // Act
        var explanation = _generator.GenerateEligibilityExplanation(matchedPrograms, input);

        // Assert
        explanation.Should().Contain("do not qualify");
    }

    [Fact]
    public void GenerateEligibilityExplanation_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var matchedPrograms = new List<ProgramMatch>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _generator.GenerateEligibilityExplanation(matchedPrograms, null!));
    }

    #endregion

    #region GenerateProgramExplanation Tests

    [Fact]
    public void GenerateProgramExplanation_WithIncomeBelowThreshold_IncludesActualValues()
    {
        // Arrange
        var program = new ProgramMatch
        {
            ProgramId = Guid.NewGuid(),
            ProgramName = "MAGI Adult",
            Status = EligibilityStatus.LikelyEligible,
            EligibilityPathway = EligibilityPathway.MAGI,
            ConfidenceScore = new ConfidenceScore(85),
            MatchingFactors = new List<string> { "Income below limit" },
            DisqualifyingFactors = new List<string>()
        };

        const decimal userIncome = 2100;
        const decimal threshold = 2500;

        // Act
        var explanation = _generator.GenerateProgramExplanation(program, userIncome, threshold);

        // Assert
        explanation.Should().Contain("Your income of $2100.00");
        explanation.Should().Contain("below the limit of $2500.00");
        explanation.Should().Contain("MAGI Adult");
    }

    [Fact]
    public void GenerateProgramExplanation_WithIncomeAboveThreshold_IncludesOverageAmount()
    {
        // Arrange
        var program = new ProgramMatch
        {
            ProgramId = Guid.NewGuid(),
            ProgramName = "MAGI Adult",
            Status = EligibilityStatus.UnlikelyEligible,
            EligibilityPathway = EligibilityPathway.MAGI,
            ConfidenceScore = new ConfidenceScore(15),
            MatchingFactors = new List<string> { "Income exceeds limit" },
            DisqualifyingFactors = new List<string>()
        };

        const decimal userIncome = 4500;
        const decimal threshold = 2500;
        const decimal expectedOverage = 2000;

        // Act
        var explanation = _generator.GenerateProgramExplanation(program, userIncome, threshold);

        // Assert
        explanation.Should().Contain("exceeds the limit");
        explanation.Should().Contain(expectedOverage.ToString("F2"));
    }

    [Fact]
    public void GenerateProgramExplanation_WithNullProgram_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _generator.GenerateProgramExplanation(null!, 2000, 2500));
    }

    #endregion

    #region GenerateDisqualifyingFactorsExplanation Tests

    [Fact]
    public void GenerateDisqualifyingFactorsExplanation_WithSingleFactor_ReturnsFactor()
    {
        // Arrange
        var factors = new List<string> { "income exceeds limit" };

        // Act
        var explanation = _generator.GenerateDisqualifyingFactorsExplanation(factors);

        // Assert
        explanation.Should().Be("income exceeds limit");
    }

    [Fact]
    public void GenerateDisqualifyingFactorsExplanation_WithMultipleFactors_ListsNumberedFactors()
    {
        // Arrange
        var factors = new List<string>
        {
            "income exceeds limit",
            "not state resident",
            "citizenship requirements not met"
        };

        // Act
        var explanation = _generator.GenerateDisqualifyingFactorsExplanation(factors);

        // Assert
        explanation.Should().Contain("(1) income exceeds limit");
        explanation.Should().Contain("(2) not state resident");
        explanation.Should().Contain("(3) citizenship requirements not met");
    }

    [Fact]
    public void GenerateDisqualifyingFactorsExplanation_WithEmptyList_ReturnsEmptyString()
    {
        // Arrange
        var factors = new List<string>();

        // Act
        var explanation = _generator.GenerateDisqualifyingFactorsExplanation(factors);

        // Assert
        explanation.Should().Be(string.Empty);
    }

    [Fact]
    public void GenerateDisqualifyingFactorsExplanation_WithNullList_ReturnsEmptyString()
    {
        // Act
        var explanation = _generator.GenerateDisqualifyingFactorsExplanation(null!);

        // Assert
        explanation.Should().Be(string.Empty);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void GenerateEligibilityExplanation_WithDisability_ExplainsDisabilityRoute()
    {
        // Arrange
        var matchedPrograms = new List<ProgramMatch>
        {
            new ProgramMatch
            {
                ProgramId = Guid.NewGuid(),
                ProgramName = "Disabled Medicaid",
                Status = EligibilityStatus.LikelyEligible,
                EligibilityPathway = EligibilityPathway.NonMAGI_Disabled,
                ConfidenceScore = new ConfidenceScore(92),
                MatchingFactors = new List<string> { "Disability status" },
                DisqualifyingFactors = new List<string>()
            }
        };

        var input = new UserEligibilityInput
        {
            StateCode = "IL",
            HouseholdSize = 1,
            MonthlyIncomeCents = 0,
            IsCitizen = true,
            HasDisability = true, // Disability flag
            CurrentDate = DateTime.UtcNow
        };

        // Act
        var explanation = _generator.GenerateEligibilityExplanation(matchedPrograms, input);

        // Assert
        explanation.Should().Contain("disability");
        explanation.Should().Contain("Disabled Medicaid");
    }

    [Fact]
    public void GenerateEligibilityExplanation_WithZeroIncome_IncludesIncomeInformation()
    {
        // Arrange
        var matchedPrograms = new List<ProgramMatch>
        {
            new ProgramMatch
            {
                ProgramId = Guid.NewGuid(),
                ProgramName = "Medicaid for Blind",
                Status = EligibilityStatus.LikelyEligible,
                EligibilityPathway = EligibilityPathway.NonMAGI_Aged,
                ConfidenceScore = new ConfidenceScore(95),
                MatchingFactors = new List<string> { "No income" },
                DisqualifyingFactors = new List<string>()
            }
        };

        var input = new UserEligibilityInput
        {
            StateCode = "IL",
            HouseholdSize = 1,
            MonthlyIncomeCents = 0,
            IsCitizen = true,
            CurrentDate = DateTime.UtcNow
        };

        // Act
        var explanation = _generator.GenerateEligibilityExplanation(matchedPrograms, input);

        // Assert
        explanation.Should().NotBeNullOrEmpty();
        explanation.Should().Contain("Medicaid for Blind");
    }

    [Fact]
    public void GenerateProgramExplanation_WithEqualIncomeAndThreshold_ExplainsAtLimit()
    {
        // Arrange
        var program = new ProgramMatch
        {
            ProgramId = Guid.NewGuid(),
            ProgramName = "MAGI Adult",
            Status = EligibilityStatus.LikelyEligible,
            EligibilityPathway = EligibilityPathway.MAGI,
            ConfidenceScore = new ConfidenceScore(85),
            MatchingFactors = new List<string> { "At limit" },
            DisqualifyingFactors = new List<string>()
        };

        const decimal amount = 2500;

        // Act
        var explanation = _generator.GenerateProgramExplanation(program, amount, amount);

        // Assert
        explanation.Should().Contain("below the limit");
    }

    #endregion
}
