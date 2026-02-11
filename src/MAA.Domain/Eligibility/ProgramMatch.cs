namespace MAA.Domain.Eligibility;

public class ProgramMatch
{
    public required string ProgramCode { get; set; }
    public required string ProgramName { get; set; }
    public int ConfidenceScore { get; set; }
    public required string Explanation { get; set; }
}
