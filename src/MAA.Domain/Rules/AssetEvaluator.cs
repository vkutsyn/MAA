namespace MAA.Domain.Rules;

/// <summary>
/// Pure Function: Asset Eligibility Evaluator
/// Determines whether a household's total assets meet program-specific limits
/// 
/// Phase 4 Implementation: T031a
/// 
/// Purpose:
/// - Evaluates assets against state-specific limits for non-MAGI pathways
/// - Provides deterministic assessment: same input always produces same output
/// - Pure function: no I/O, no database access, no side effects
/// - Supports audit trail: reasoning explains which limit was applied
/// 
/// Key Properties:
/// - Immutable: Does not modify state or inputs
/// - Fast: Simple dictionary lookup and comparison
/// - Testable in isolation: No external dependencies
/// - Deterministic: Results reproducible for same input
/// 
/// Scope:
/// - Applies asset tests to: NonMAGI_Aged, NonMAGI_Disabled pathways only
/// - Passes through: MAGI, SSI_Linked, Pregnancy, Other (no asset test)
/// - Per-state asset limits defined in specification: 2026 pilot states (IL, CA, NY, TX, FL)
/// 
/// Asset Limits by State (in cents, updated annually):
/// - IL: $2,000 per person / $3,000 for married couples
/// - CA: $3,000
/// - NY: $4,500
/// - TX: $2,000
/// - FL: $2,500
/// 
/// Reference: specs/002-rules-engine/data-model.md section 4b
/// </summary>
public class AssetEvaluator
{
    /// <summary>
    /// Asset limits by state and pathway (in cents)
    /// Used for non-MAGI Aged and Disabled pathway evaluations
    /// 
    /// Note: MAGI pathway has no asset test
    /// Pregnancy pathway: No official asset test (defer to program rules)
    /// SSI linked: Categorical eligibility bypasses asset test
    /// </summary>
    private static readonly Dictionary<string, long> StateAssetLimitsCents = new(StringComparer.OrdinalIgnoreCase)
    {
        // IL: $2,000 limit for Aged/Disabled pathways
        { "IL", 200_000L },  // $2,000.00 in cents

        // CA: $3,000 limit (higher than federal baseline)
        { "CA", 300_000L },  // $3,000.00 in cents

        // NY: $4,500 limit (highest among pilot states)
        { "NY", 450_000L },  // $4,500.00 in cents

        // TX: $2,000 limit (federal baseline)
        { "TX", 200_000L },  // $2,000.00 in cents

        // FL: $2,500 limit (hybrid approach between low and high limits)
        { "FL", 250_000L }   // $2,500.00 in cents
    };

    /// <summary>
    /// Evaluates whether household assets meet the requirement for their eligibility pathway
    /// </summary>
    /// <param name="assetsCents">Total household assets in cents (e.g., $5,000 = 500_000 cents)</param>
    /// <param name="pathway">The eligibility pathway (MAGI, NonMAGI_Aged, NonMAGI_Disabled, SSI, Pregnancy, Other)</param>
    /// <param name="stateCode">Two-character state code (IL, CA, NY, TX, FL)</param>
    /// <param name="currentYear">Current year for future limit adjustments (2026+)</param>
    /// <returns>
    /// Tuple of (isEligible: bool, reason: string)
    /// - isEligible: true if assets pass the test (or no test applies)
    /// - reason: Explanation of the assessment (for UI/explanations)
    /// </returns>
    public static (bool isEligible, string reason) EvaluateAssets(
        long assetsCents,
        EligibilityPathway pathway,
        string stateCode,
        int currentYear)
    {
        // Pathways that are NOT subject to asset tests
        if (pathway is EligibilityPathway.MAGI
            or EligibilityPathway.SSI_Linked
            or EligibilityPathway.Pregnancy
            or EligibilityPathway.Other)
        {
            return (true, GetNoAssetTestReason(pathway));
        }

        // Asset tests apply only to NonMAGI pathways
        if (pathway is not (EligibilityPathway.NonMAGI_Aged or EligibilityPathway.NonMAGI_Disabled))
        {
            // Unknown pathway - no test applies
            return (true, $"No asset test applies to {pathway} pathway.");
        }

        // Validate state code
        if (string.IsNullOrEmpty(stateCode) || !StateAssetLimitsCents.ContainsKey(stateCode))
        {
            return (false, $"Unknown state code '{stateCode}' for asset evaluation.");
        }

        // Get the asset limit for this state
        long limitCents = StateAssetLimitsCents[stateCode];

        // Null assets treated as $0
        long assetsToEvaluate = assetsCents > 0 ? assetsCents : 0;

        // Determine eligibility based on asset test
        bool isEligible = assetsToEvaluate <= limitCents;

        // Build explanation with actual values
        string reason = BuildAssetExplanation(
            isEligible,
            assetsToEvaluate,
            limitCents,
            pathway,
            stateCode);

        return (isEligible, reason);
    }

    /// <summary>
    /// Gets a reason string for pathways that don't have asset tests
    /// </summary>
    private static string GetNoAssetTestReason(EligibilityPathway pathway)
    {
        return pathway switch
        {
            EligibilityPathway.MAGI =>
                "MAGI (Modified Adjusted Gross Income) pathway does not include an asset test.",

            EligibilityPathway.SSI_Linked =>
                "Supplemental Security Income (SSI) recipients are categorically eligible and do not face asset limits.",

            EligibilityPathway.Pregnancy =>
                "Pregnancy-related Medicaid pathway does not include an asset test.",

            _ => "No asset test applies to this eligibility pathway."
        };
    }

    /// <summary>
    /// Builds a human-readable explanation of the asset evaluation result
    /// </summary>
    private static string BuildAssetExplanation(
        bool isEligible,
        long assetsCents,
        long limitCents,
        EligibilityPathway pathway,
        string stateCode)
    {
        // Format amounts as dollars for readability
        decimal assetsAmount = assetsCents / 100m;
        decimal limitAmount = limitCents / 100m;

        string pathwayName = GetPathwayName(pathway);

        if (isEligible)
        {
            return $"Your assets (${assetsAmount:F2}) are within the ${limitAmount:F2} limit " +
                   $"for {pathwayName} Medicaid in {stateCode}.";
        }
        else
        {
            decimal overage = (assetsCents - limitCents) / 100m;
            return $"Your assets (${assetsAmount:F2}) exceed the ${limitAmount:F2} limit " +
                   $"for {pathwayName} Medicaid in {stateCode} by ${overage:F2} overage. " +
                   $"You may regain eligibility by reducing assets below the limit.";
        }
    }

    /// <summary>
    /// Gets a plain-language name for the eligibility pathway
    /// </summary>
    private static string GetPathwayName(EligibilityPathway pathway)
    {
        return pathway switch
        {
            EligibilityPathway.NonMAGI_Aged => "Aged",
            EligibilityPathway.NonMAGI_Disabled => "Disabled",
            _ => pathway.ToString()
        };
    }

    /// <summary>
    /// Gets the asset limit in dollars for a given state and pathway
    /// Useful for computing thresholds and explanations
    /// </summary>
    /// <param name="stateCode">State code</param>
    /// <param name="pathway">Eligibility pathway</param>
    /// <returns>Asset limit in cents, or null if no test applies</returns>
    public static long? GetAssetLimitCents(string stateCode, EligibilityPathway pathway)
    {
        // No asset test for these pathways
        if (pathway is EligibilityPathway.MAGI
            or EligibilityPathway.SSI_Linked
            or EligibilityPathway.Pregnancy)
        {
            return null;
        }

        // Return the limit if state is recognized
        return StateAssetLimitsCents.TryGetValue(stateCode, out var limit)
            ? limit
            : null;
    }
}
