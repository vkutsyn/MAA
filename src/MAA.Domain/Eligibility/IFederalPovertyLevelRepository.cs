namespace MAA.Domain.Eligibility;

public interface IFederalPovertyLevelRepository
{
    Task<FederalPovertyLevel?> GetByYearAndHouseholdSizeAsync(
        int year,
        int householdSize,
        string? stateCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FederalPovertyLevel>> GetByYearAsync(
        int year,
        CancellationToken cancellationToken = default);
}
