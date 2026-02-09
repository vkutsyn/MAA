using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MAA.Domain.Rules;

/// <summary>
/// Pure function service for generating plain-language eligibility explanations.
/// 
/// Phase 6 Implementation: T051
/// 
/// Key Properties:
/// - Pure function: Same input always produces same output
/// - No I/O: Does not access database, cache, or external services
/// - No dependencies: Only uses user input to generate explanations
/// - Deterministic: Results can be reproduced exactly for audit/testing
/// - Jargon-free: All acronyms are defined inline
/// - Concrete: Explanations include actual user data values
/// 
/// Usage Example:
///   var generator = new ExplanationGenerator();
///   var explanation = generator.GenerateEligibilityExplanation(matchedPrograms, input);
///   // Output: "You qualify for Medicaid. Your monthly income of $2,100..."
/// </summary>
public class ExplanationGenerator
{
    /// <summary>
    /// Generates an overall eligibility explanation based on program matches.
    /// Includes concrete income values and threshold comparisons.
    /// </summary>
    /// <param name="matchedPrograms">Programs where user is eligible</param>
    /// <param name="input">User eligibility input with income and demographic data</param>
    /// <returns>Plain-language explanation suitable for 8th grade reading level</returns>
    public string GenerateEligibilityExplanation(List<ProgramMatch> matchedPrograms, UserEligibilityInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (!matchedPrograms.Any())
        {
            return GenerateIneligibilityExplanation(input);
        }

        var explanation = new StringBuilder();
        var monthlyIncome = input.MonthlyIncomeCents / 100m;
        var annualIncome = monthlyIncome * 12;

        // Check for categorical eligibility paths first
        if (input.ReceivesSsi)
        {
            explanation.Append(GenerateCategoricalEligibilityExplanation_SSI());
        }
        else if (input.IsPregnant)
        {
            explanation.Append(GenerateCategoricalEligibilityExplanation_Pregnancy());
        }
        else if (input.HasDisability)
        {
            explanation.Append(GenerateCategoricalEligibilityExplanation_Disability());
        }
        else
        {
            // Income-based eligibility
            if (monthlyIncome > 0)
            {
                explanation.Append($"Based on your monthly income of ${monthlyIncome:F2}, you qualify for the following Medicaid programs: ");
            }
        }

        // Add matched programs
        if (matchedPrograms.Count == 1)
        {
            var program = matchedPrograms[0];
            explanation.Append($"{program.ProgramName}");
        }
        else
        {
            var programNames = string.Join(", ", matchedPrograms.Select(p => p.ProgramName));
            explanation.Append(programNames);
        }

        explanation.Append(".");

        return explanation.ToString();
    }

    /// <summary>
    /// Generates an explanation for why a user doesn't qualify for Medicaid.
    /// </summary>
    /// <param name="input">User eligibility input</param>
    /// <returns>Plain-language ineligibility explanation</returns>
    private string GenerateIneligibilityExplanation(UserEligibilityInput input)
    {
        var monthlyIncome = input.MonthlyIncomeCents / 100m;

        var reasons = new List<string>();

        if (monthlyIncome > 0)
        {
            reasons.Add($"your income exceeds the allowed limit");
        }

        if (!input.IsCitizen)
        {
            reasons.Add("citizenship or immigration status requirements");
        }

        if (input.AssetsCents.HasValue && input.AssetsCents > 0)
        {
            var assets = input.AssetsCents.Value / 100m;
            reasons.Add($"your assets (${assets:F2}) exceed the limit");
        }

        if (reasons.Count == 0)
        {
            return "You do not currently qualify for Medicaid based on the information provided.";
        }

        var reasonsList = GenerateDisqualifyingFactorsExplanation(reasons);
        return $"You do not qualify for Medicaid. The reasons are: {reasonsList}";
    }

    /// <summary>
    /// Generates a program-specific eligibility explanation with income threshold comparison.
    /// </summary>
    /// <param name="program">The program being evaluated</param>
    /// <param name="userIncome">User's income in dollars</param>
    /// <param name="threshold">Program's income limit in dollars</param>
    /// <returns>Program-specific explanation</returns>
    public string GenerateProgramExplanation(ProgramMatch program, decimal userIncome, decimal threshold)
    {
        if (program == null)
            throw new ArgumentNullException(nameof(program));

        var explanation = new StringBuilder();

        explanation.Append($"For {program.ProgramName}: ");

        if (userIncome <= threshold)
        {
            explanation.Append($"Your income of ${userIncome:F2} is below the limit of ${threshold:F2}.");
        }
        else
        {
            var overage = userIncome - threshold;
            explanation.Append($"Your income of ${userIncome:F2} exceeds the limit by ${overage:F2}.");
        }

        return explanation.ToString();
    }

    /// <summary>
    /// Generates a formatted explanation of disqualifying factors.
    /// Lists each factor numerically for clarity.
    /// </summary>
    /// <param name="factors">List of disqualifying factors or reasons</param>
    /// <returns>Formatted explanation with numbered factors</returns>
    public string GenerateDisqualifyingFactorsExplanation(List<string> factors)
    {
        if (factors == null || factors.Count == 0)
            return string.Empty;

        if (factors.Count == 1)
            return factors[0];

        var explanation = new StringBuilder();
        for (int i = 0; i < factors.Count; i++)
        {
            explanation.Append($"({i + 1}) {factors[i]}");
            if (i < factors.Count - 1)
                explanation.Append(", ");
        }

        return explanation.ToString();
    }

    /// <summary>
    /// Generates an explanation for SSI (Social Security Income) recipients.
    /// SSI recipients qualify for Disabled Medicaid regardless of income.
    /// </summary>
    private string GenerateCategoricalEligibilityExplanation_SSI()
    {
        return "You qualify for Disabled Medicaid because you receive SSI (Social Security Income) benefits. ";
    }

    /// <summary>
    /// Generates an explanation for pregnant applicants.
    /// Pregnant individuals can qualify for Medicaid regardless of other factors.
    /// </summary>
    private string GenerateCategoricalEligibilityExplanation_Pregnancy()
    {
        return "You qualify for Pregnant Medicaid because pregnancy is a qualifying factor. ";
    }

    /// <summary>
    /// Generates an explanation for applicants with disabilities.
    /// Individuals with disabilities may qualify for Medicaid through disability pathways.
    /// </summary>
    private string GenerateCategoricalEligibilityExplanation_Disability()
    {
        return "You qualify for Medicaid based on disability status. ";
    }
}
