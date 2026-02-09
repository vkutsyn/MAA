using FluentAssertions;
using MAA.Domain.Rules;
using Xunit;

namespace MAA.Tests.Unit.Rules;

/// <summary>
/// Unit tests for JargonDefinition static dictionary.
/// Tests acronym to definition mapping and lookup functionalities.
/// 
/// Phase 6 Implementation: T055
/// </summary>
public class JargonDefinitionTests
{
    [Fact]
    public void GetDefinition_WithValidAcronym_ReturnsCorrectDefinition()
    {
        // Arrange
        const string acronym = "MAGI";

        // Act
        var definition = JargonDefinition.GetDefinition(acronym);

        // Assert
        definition.Should().NotBeNull();
        definition.Should().Contain("Modified Adjusted Gross Income");
        definition.Should().Contain("MAGI");
    }

    [Fact]
    public void GetDefinition_WithFPLAcronym_ReturnsCorrectDefinition()
    {
        // Arrange
        const string acronym = "FPL";

        // Act
        var definition = JargonDefinition.GetDefinition(acronym);

        // Assert
        definition.Should().NotBeNull();
        definition.Should().Contain("Federal Poverty Level");
    }

    [Fact]
    public void GetDefinition_WithSSIAcronym_ReturnsCorrectDefinition()
    {
        // Arrange
        const string acronym = "SSI";

        // Act
        var definition = JargonDefinition.GetDefinition(acronym);

        // Assert
        definition.Should().NotBeNull();
        definition.Should().Contain("Social Security Income");
    }

    [Fact]
    public void GetDefinition_WithInvalidAcronym_ReturnsNull()
    {
        // Arrange
        const string acronym = "XYZ";

        // Act
        var definition = JargonDefinition.GetDefinition(acronym);

        // Assert
        definition.Should().BeNull();
    }

    [Fact]
    public void GetDefinition_WithNullAcronym_ReturnsNull()
    {
        // Act
        var definition = JargonDefinition.GetDefinition(null!);

        // Assert
        definition.Should().BeNull();
    }

    [Fact]
    public void GetDefinition_WithEmptyAcronym_ReturnsNull()
    {
        // Act
        var definition = JargonDefinition.GetDefinition(string.Empty);

        // Assert
        definition.Should().BeNull();
    }

    [Fact]
    public void GetDefinition_IsCaseInsensitive_FindsAcronymInAnyCase()
    {
        // Act
        var definitionLower = JargonDefinition.GetDefinition("magi");
        var definitionUpper = JargonDefinition.GetDefinition("MAGI");
        var definitionMixed = JargonDefinition.GetDefinition("MaGi");

        // Assert
        definitionLower.Should().NotBeNull();
        definitionUpper.Should().NotBeNull();
        definitionMixed.Should().NotBeNull();
        definitionLower.Should().Be(definitionUpper);
    }

    [Fact]
    public void HasDefinition_WithValidAcronym_ReturnsTrue()
    {
        // Arrange
        const string acronym = "MAGI";

        // Act
        var hasDefinition = JargonDefinition.HasDefinition(acronym);

        // Assert
        hasDefinition.Should().BeTrue();
    }

    [Fact]
    public void HasDefinition_WithInvalidAcronym_ReturnsFalse()
    {
        // Arrange
        const string acronym = "INVALID123";

        // Act
        var hasDefinition = JargonDefinition.HasDefinition(acronym);

        // Assert
        hasDefinition.Should().BeFalse();
    }

    [Fact]
    public void HasDefinition_WithNullAcronym_ReturnsFalse()
    {
        // Act
        var hasDefinition = JargonDefinition.HasDefinition(null!);

        // Assert
        hasDefinition.Should().BeFalse();
    }

    [Fact]
    public void GetAllAcronyms_ReturnsAtLeastMinimumCount()
    {
        // Arrange
        const int minimumExpectedCount = 10;

        // Act
        var acronyms = JargonDefinition.GetAllAcronyms();

        // Assert
        acronyms.Should().NotBeNull();
        acronyms.Should().HaveCountGreaterThanOrEqualTo(minimumExpectedCount);
    }

    [Fact]
    public void GetAllAcronyms_IncludesRequiredDefinitions()
    {
        // Arrange
        var requiredAcronyms = new[] { "MAGI", "FPL", "SSI", "SSDI", "TANF" };

        // Act
        var acronyms = JargonDefinition.GetAllAcronyms();

        // Assert
        foreach (var acronym in requiredAcronyms)
        {
            acronyms.Should().Contain(acronym, $"because {acronym} is a required definition");
        }
    }

    [Fact]
    public void GetDefinitionCount_ReturnsAtLeastTenDefinitions()
    {
        // Act
        var count = JargonDefinition.GetDefinitionCount();

        // Assert
        count.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Definition_ContainsAcronymAndFullForm()
    {
        // Arrange
        const string acronym = "FPL";

        // Act
        var definition = JargonDefinition.GetDefinition(acronym);

        // Assert
        definition.Should().Contain("FPL");
        definition.Should().Contain("Federal Poverty Level");
    }
}
