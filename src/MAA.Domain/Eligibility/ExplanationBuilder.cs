namespace MAA.Domain.Eligibility;

/// <summary>
/// Builds plain-language explanations for eligibility determination results.
/// Uses templates and a glossary to ensure clear, consistent communication.
/// </summary>
public class ExplanationBuilder
{
    private static readonly Dictionary<string, string> CriterionGlossary = new()
    {
        ["citizenship_requirement"] = "You must be a U.S. citizen or qualified immigrant.",
        ["income_threshold"] = "Your household income must be below the limit for your household size.",
        ["asset_limit"] = "Your household assets must be below the program limit.",
        ["residency_requirement"] = "You must be a resident of this state.",
        ["age_requirement"] = "You must meet the age requirement for the program.",
        ["employment_status"] = "Your employment status affects your eligibility.",
        ["family_structure"] = "Your family structure affects the benefits you receive.",
        ["medical_status"] = "Certain medical conditions may affect your eligibility."
    };

    private static readonly Dictionary<string, string> CriterionShortName = new()
    {
        ["citizenship_requirement"] = "Citizenship",
        ["income_threshold"] = "Income Limit",
        ["asset_limit"] = "Asset Limit",
        ["residency_requirement"] = "State Residency",
        ["age_requirement"] = "Age",
        ["employment_status"] = "Employment Status",
        ["family_structure"] = "Family Structure",
        ["medical_status"] = "Medical Status"
    };

    /// <summary>
    /// Builds a list of explanation items for the given criteria groups.
    /// </summary>
    /// <param name="metCriteria">Criteria that were satisfied.</param>
    /// <param name="unmetCriteria">Criteria that were not satisfied.</param>
    /// <param name="missingCriteria">Criteria that could not be evaluated due to missing data.</param>
    /// <returns>A list of explanation items in a consistent order.</returns>
    public List<ExplanationItem> BuildExplanationItems(
        IEnumerable<string> metCriteria,
        IEnumerable<string> unmetCriteria,
        IEnumerable<string> missingCriteria)
    {
        var items = new List<ExplanationItem>();

        // Add met criteria
        foreach (var criterion in metCriteria.OrderBy(c => c))
        {
            items.Add(new ExplanationItem
            {
                CriterionId = criterion,
                Message = GetMetMessage(criterion),
                Status = ExplanationItemStatus.Met,
                GlossaryReference = GetGlossaryReference(criterion)
            });
        }

        // Add unmet criteria
        foreach (var criterion in unmetCriteria.OrderBy(c => c))
        {
            items.Add(new ExplanationItem
            {
                CriterionId = criterion,
                Message = GetUnmetMessage(criterion),
                Status = ExplanationItemStatus.Unmet,
                GlossaryReference = GetGlossaryReference(criterion)
            });
        }

        // Add missing criteria
        foreach (var criterion in missingCriteria.OrderBy(c => c))
        {
            items.Add(new ExplanationItem
            {
                CriterionId = criterion,
                Message = GetMissingMessage(criterion),
                Status = ExplanationItemStatus.Missing,
                GlossaryReference = GetGlossaryReference(criterion)
            });
        }

        return items;
    }

    /// <summary>
    /// Generates a plain-language summary explanation for the determination.
    /// </summary>
    public string GenerateExplanation(
        IEnumerable<string> metCriteria,
        IEnumerable<string> unmetCriteria,
        IEnumerable<string> missingCriteria)
    {
        var metList = metCriteria.ToList();
        var unmetList = unmetCriteria.ToList();
        var missingList = missingCriteria.ToList();

        // Determine overall status
        var isEligible = unmetList.Count == 0 && missingList.Count == 0 && metList.Count > 0;
        var hasUnmet = unmetList.Count > 0;
        var hasMissing = missingList.Count > 0;

        if (isEligible)
        {
            return BuildEligibleExplanation(metList);
        }

        if (hasUnmet && !hasMissing)
        {
            return BuildIneligibleExplanation(unmetList);
        }

        if (hasMissing)
        {
            return BuildPartialExplanation(metList, unmetList, missingList);
        }

        return "Unable to determine eligibility with the provided information.";
    }

    private string BuildEligibleExplanation(List<string> metCriteria)
    {
        var criteria = string.Join(", ", metCriteria.Select(c => GetShortName(c)).OrderBy(c => c));
        return $"Based on the information provided, you appear to be eligible. You meet all the requirements, including {criteria}.";
    }

    private string BuildIneligibleExplanation(List<string> unmetCriteria)
    {
        var criteria = string.Join(" and ", unmetCriteria.Select(c => GetShortName(c)).OrderBy(c => c));
        var verb = unmetCriteria.Count > 1 ? "requirements" : "requirement";
        return $"Based on the information provided, you do not appear to be eligible. You do not meet the {criteria} {verb}.";
    }

    private string BuildPartialExplanation(
        List<string> metCriteria,
        List<string> unmetCriteria,
        List<string> missingCriteria)
    {
        var parts = new List<string>();

        if (metCriteria.Count > 0)
        {
            var metStr = string.Join(", ", metCriteria.Select(c => GetShortName(c)).OrderBy(c => c));
            parts.Add($"You meet the {metStr} requirement(s).");
        }

        if (unmetCriteria.Count > 0)
        {
            var unmetStr = string.Join(", ", unmetCriteria.Select(c => GetShortName(c)).OrderBy(c => c));
            parts.Add($"You do not meet the {unmetStr} requirement(s).");
        }

        if (missingCriteria.Count > 0)
        {
            var missingStr = string.Join(", ", missingCriteria.Select(c => GetShortName(c)).OrderBy(c => c));
            parts.Add($"We could not evaluate your {missingStr} requirement(s) due to missing information.");
        }

        return string.Join(" ", parts) + " Please review the details above for more information about each requirement.";
    }

    private string GetMetMessage(string criterionId)
    {
        var name = GetShortName(criterionId);
        return $"✓ {name}: Requirement met";
    }

    private string GetUnmetMessage(string criterionId)
    {
        var name = GetShortName(criterionId);
        return $"✗ {name}: Requirement not met";
    }

    private string GetMissingMessage(string criterionId)
    {
        var name = GetShortName(criterionId);
        return $"? {name}: Cannot determine (missing information)";
    }

    private string GetShortName(string criterionId)
    {
        return CriterionShortName.TryGetValue(criterionId, out var name) 
            ? name 
            : CamelCaseToHumanReadable(criterionId);
    }

    private string? GetGlossaryReference(string criterionId)
    {
        return CriterionGlossary.TryGetValue(criterionId, out var glossary) ? glossary : null;
    }

    private string CamelCaseToHumanReadable(string text)
    {
        // Convert "someRequirement" to "Some Requirement"
        var chars = text.Select((c, i) => 
            char.IsUpper(c) && i > 0 ? $" {c}" : c.ToString()
        );
        var result = string.Concat(chars);
        return char.ToUpper(result[0]) + result.Substring(1);
    }
}
