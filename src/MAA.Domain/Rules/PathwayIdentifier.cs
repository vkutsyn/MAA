namespace MAA.Domain.Rules;

/// <summary>
/// Pure function to determine applicable eligibility pathways based on user characteristics.
/// No I/O, no dependencies - contains only logic for pathway determination.
/// Used by PathwayRouter to filter programs by applicable pathways.
/// </summary>
public class PathwayIdentifier
{
    /// <summary>
    /// Determines all applicable eligibility pathways for a user based on their characteristics.
    /// Returns sorted list for deterministic output.
    /// </summary>
    public List<EligibilityPathway> DetermineApplicablePathways(
        int age,
        bool hasDisability,
        bool receivesSsi,
        bool isPregnant = false,
        bool isFemale = false)
    {
        if (age < 0 || age > 120)
            throw new ArgumentException("Age must be between 0 and 120", nameof(age));

        var pathways = new List<EligibilityPathway>();

        // SSI pathway
        if (receivesSsi)
        {
            pathways.Add(EligibilityPathway.SSI_Linked);
        }

        // Age 65+ → Aged pathway (non-MAGI)
        if (age >= 65)
        {
            pathways.Add(EligibilityPathway.NonMAGI_Aged);
        }
        else if (hasDisability && !receivesSsi)
        {
            // Age <65 with disability (non-SSI) → Disabled pathway
            pathways.Add(EligibilityPathway.NonMAGI_Disabled);
        }

        // Age 19-64, no disability, not SSI → MAGI pathway
        if (age >= 19 && age < 65 && !hasDisability && !receivesSsi)
        {
            pathways.Add(EligibilityPathway.MAGI);
        }

        // Pregnancy pathway (if female AND pregnant)
        if (isFemale && isPregnant)
        {
            pathways.Add(EligibilityPathway.Pregnancy);
        }

        // Return sorted for deterministic output
        return pathways.OrderBy(p => p.ToString()).ToList();
    }

    /// <summary>
    /// Simplified overload without pregnancy parameters.
    /// </summary>
    public List<EligibilityPathway> DetermineApplicablePathways(
        int age,
        bool hasDisability,
        bool receivesSsi)
    {
        return DetermineApplicablePathways(age, hasDisability, receivesSsi, false, false);
    }

    /// <summary>
    /// Determines if  a specific pathway applies to a user.
    /// </summary>
    public bool IsPathwayApplicable(
        EligibilityPathway pathway,
        int age,
        bool hasDisability,
        bool receivesSsi,
        bool isPregnant = false,
        bool isFemale = false)
    {
        var applicablePathways = DetermineApplicablePathways(age, hasDisability, receivesSsi, isPregnant, isFemale);
        return applicablePathways.Contains(pathway);
    }
}
