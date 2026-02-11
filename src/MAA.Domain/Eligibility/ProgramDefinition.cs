namespace MAA.Domain.Eligibility;

public class ProgramDefinition
{
    public required string ProgramCode { get; set; }
    public required string StateCode { get; set; }
    public required string ProgramName { get; set; }
    public string? Description { get; set; }
    public ProgramCategory Category { get; set; }
    public bool IsActive { get; set; }

    public ICollection<EligibilityRule> Rules { get; set; } = new List<EligibilityRule>();
}

public enum ProgramCategory
{
    Magi,
    NonMagi,
    Pregnancy,
    SsiLinked,
    Other
}
