namespace MAA.Domain.Rules.Exceptions;

/// <summary>
/// Custom exception for eligibility evaluation errors
/// Phoenix Medicaid Application - Rules Engine
/// 
/// Phase 2 Implementation: T014
/// </summary>
public class EligibilityEvaluationException : Exception
{
    public EligibilityEvaluationException(string message) : base(message)
    {
    }

    public EligibilityEvaluationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
