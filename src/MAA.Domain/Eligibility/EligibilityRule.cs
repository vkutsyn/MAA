namespace MAA.Domain.Eligibility;

public class EligibilityRule
{
    public Guid EligibilityRuleId { get; set; }
    public Guid RuleSetVersionId { get; set; }
    public RuleSetVersion? RuleSetVersion { get; set; }
    public required string ProgramCode { get; set; }
    public ProgramDefinition? Program { get; set; }
    public required string RuleLogic { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
}
