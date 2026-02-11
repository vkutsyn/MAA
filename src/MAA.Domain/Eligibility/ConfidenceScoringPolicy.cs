namespace MAA.Domain.Eligibility;

public class ConfidenceScoringPolicy
{
    private const double DefaultCompleteness = 0.5;
    private const double MatchedCertainty = 1.0;
    private const double UnmatchedCertainty = 0.5;

    public int CalculateScore(IReadOnlyDictionary<string, object?> answers, bool ruleMatched)
    {
        var completeness = CalculateCompleteness(answers);
        var certainty = ruleMatched ? MatchedCertainty : UnmatchedCertainty;
        var score = (int)Math.Round(100 * completeness * certainty, MidpointRounding.AwayFromZero);
        return Math.Clamp(score, 0, 100);
    }

    public EligibilityStatus GetStatus(int confidenceScore)
    {
        return confidenceScore switch
        {
            >= 85 => EligibilityStatus.Likely,
            >= 60 => EligibilityStatus.Possibly,
            _ => EligibilityStatus.Unlikely
        };
    }

    private static double CalculateCompleteness(IReadOnlyDictionary<string, object?> answers)
    {
        if (answers == null || answers.Count == 0)
        {
            return DefaultCompleteness;
        }

        var hasValue = answers.Any(entry => entry.Value != null);
        return hasValue ? 1.0 : DefaultCompleteness;
    }
}
