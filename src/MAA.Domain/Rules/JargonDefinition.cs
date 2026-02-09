using System;
using System.Collections.Generic;

namespace MAA.Domain.Rules;

/// <summary>
/// Static dictionary mapping government and program-related acronyms to their plain-language definitions.
/// 
/// Phase 6 Implementation: T052
/// 
/// Key Properties:
/// - Static collection: Pre-defined list of all acronyms used in explanations
/// - Deterministic: Same acronym always maps to same definition
/// - No I/O: Pure data structure with no external dependencies
/// - Comprehensive: ≥10 acronyms with clear, simple definitions
/// 
/// Usage Example:
///   var definition = JargonDefinition.GetDefinition("MAGI");
///   // Returns: "Modified Adjusted Gross Income (MAGI)"
/// </summary>
public static class JargonDefinition
{
    /// <summary>
    /// Dictionary mapping acronyms to their full definitions.
    /// Definitions are formatted for inline inclusion in explanations.
    /// </summary>
    private static readonly Dictionary<string, string> Definitions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Income-related acronyms
        { "MAGI", "Modified Adjusted Gross Income (MAGI)" },
        { "FPL", "Federal Poverty Level (FPL)" },
        { "AMI", "Area Median Income (AMI)" },
        { "AGI", "Adjusted Gross Income (AGI)" },
        
        // Government benefits acronyms
        { "SSI", "Social Security Income (SSI)" },
        { "SSDI", "Social Security Disability Insurance (SSDI)" },
        { "TANF", "Temporary Assistance for Needy Families (TANF)" },
        
        // Program-related acronyms
        { "CONST", "Continuous eligibility (CONST)" },
        { "MEDICALLY NEEDY", "Medical need exemption" },
        { "SHARE OF COST", "Share of Cost (SOC)" },
        
        // State/Administrative acronyms
        { "DHHS", "Department of Health and Human Services (DHHS)" },
        { "DHS", "Department of Human Services (DHS)" },
        
        // Additional acronyms
        { "CHIP", "Children's Health Insurance Program (CHIP)" },
        { "COBRA", "Temporary health coverage (COBRA)" }
    };

    /// <summary>
    /// Gets the full definition for a given acronym.
    /// </summary>
    /// <param name="acronym">The acronym to look up (case-insensitive)</param>
    /// <returns>The definition if found; otherwise null</returns>
    public static string? GetDefinition(string acronym)
    {
        if (string.IsNullOrWhiteSpace(acronym))
            return null;

        return Definitions.TryGetValue(acronym, out var definition) ? definition : null;
    }

    /// <summary>
    /// Checks if an acronym is defined in the dictionary.
    /// </summary>
    /// <param name="acronym">The acronym to check (case-insensitive)</param>
    /// <returns>True if the acronym is defined; otherwise false</returns>
    public static bool HasDefinition(string acronym)
    {
        if (string.IsNullOrWhiteSpace(acronym))
            return false;

        return Definitions.ContainsKey(acronym);
    }

    /// <summary>
    /// Gets all defined acronyms.
    /// </summary>
    /// <returns>Collection of all acronyms in the dictionary</returns>
    public static IEnumerable<string> GetAllAcronyms()
    {
        return Definitions.Keys;
    }

    /// <summary>
    /// Gets the count of defined acronyms.
    /// </summary>
    /// <returns>Total number of defined acronyms</returns>
    public static int GetDefinitionCount()
    {
        return Definitions.Count;
    }
}
