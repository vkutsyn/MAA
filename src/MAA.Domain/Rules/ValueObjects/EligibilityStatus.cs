namespace MAA.Domain.Rules;

/// <summary>
/// Eligibility Status Value Object
/// Represents the possible outcomes of an eligibility evaluation
/// 
/// Possible values:
/// - Likely Eligible: User meets all criteria for the program
/// - Possibly Eligible: User may qualify with verification/documentation
/// - Unlikely Eligible: User does not meet criteria
/// 
/// Phase 2 Implementation: T015
/// </summary>
public enum EligibilityStatus
{
    /// <summary>User likely qualifies for the program</summary>
    LikelyEligible,

    /// <summary>User may qualify pending verification</summary>
    PossiblyEligible,

    /// <summary>User unlikely qualifies for the program</summary>
    UnlikelyEligible
}
