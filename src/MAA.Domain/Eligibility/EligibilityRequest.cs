namespace MAA.Domain.Eligibility;

public class EligibilityRequest
{
    public required string StateCode { get; set; }
    public DateTime EffectiveDate { get; set; }
    public IReadOnlyDictionary<string, object?> Answers { get; set; } =
        new Dictionary<string, object?>();
}
