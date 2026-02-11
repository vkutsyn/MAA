namespace MAA.Domain.Eligibility;

public class FederalPovertyLevel
{
    public Guid FplId { get; set; }
    public int Year { get; set; }
    public int HouseholdSize { get; set; }
    public decimal AnnualAmount { get; set; }
    public string? StateCode { get; set; }
}
