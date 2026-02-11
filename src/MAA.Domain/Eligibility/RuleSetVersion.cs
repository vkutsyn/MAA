namespace MAA.Domain.Eligibility;

public class RuleSetVersion
{
    public Guid RuleSetVersionId { get; set; }
    public required string StateCode { get; set; }
    public required string Version { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public RuleSetStatus Status { get; set; } = RuleSetStatus.Active;
    public DateTime CreatedAt { get; set; }

    public ICollection<EligibilityRule> Rules { get; set; } = new List<EligibilityRule>();
}

public enum RuleSetStatus
{
    Active,
    Retired
}
