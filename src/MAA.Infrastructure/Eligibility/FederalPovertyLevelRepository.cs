using EligibilityDomain = MAA.Domain.Eligibility;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.Eligibility;

public class FederalPovertyLevelRepository : EligibilityDomain.IFederalPovertyLevelRepository
{
    private readonly SessionContext _context;

    public FederalPovertyLevelRepository(SessionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<EligibilityDomain.FederalPovertyLevel?> GetByYearAndHouseholdSizeAsync(
        int year,
        int householdSize,
        string? stateCode,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FederalPovertyLevelsV2
            .AsNoTracking()
            .Where(record => record.Year == year && record.HouseholdSize == householdSize);

        if (!string.IsNullOrWhiteSpace(stateCode))
        {
            var stateRecord = await query
                .Where(record => record.StateCode == stateCode)
                .FirstOrDefaultAsync(cancellationToken);

            if (stateRecord != null)
            {
                return stateRecord;
            }
        }

        return await query
            .Where(record => record.StateCode == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EligibilityDomain.FederalPovertyLevel>> GetByYearAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.FederalPovertyLevelsV2
            .AsNoTracking()
            .Where(record => record.Year == year)
            .ToListAsync(cancellationToken);
    }
}
