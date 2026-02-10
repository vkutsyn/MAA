using MAA.Application.Eligibility.Repositories;
using MAA.Domain.Rules;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.DataAccess;

/// <summary>
/// Repository for accessing Medicaid programs from the database.
/// </summary>
public class MedicaidProgramRepository : IMedicaidProgramRepository
{
    private readonly SessionContext _context;

    public MedicaidProgramRepository(SessionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<MedicaidProgram?> GetByIdAsync(Guid programId)
    {
        ArgumentException.ThrowIfNullOrEmpty(programId.ToString(), nameof(programId));

        return await _context.MedicaidPrograms
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProgramId == programId)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<MedicaidProgram>> GetByStateAsync(string stateCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));

        return await _context.MedicaidPrograms
            .AsNoTracking()
            .Where(p => p.StateCode == stateCode)
            .OrderBy(p => p.ProgramName)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<MedicaidProgram>> GetByPathwayAsync(string stateCode, EligibilityPathway pathway)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));

        return await _context.MedicaidPrograms
            .AsNoTracking()
            .Where(p => p.StateCode == stateCode && p.EligibilityPathway == pathway)
            .OrderBy(p => p.ProgramName)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<MedicaidProgram>> GetAllAsync()
    {
        return await _context.MedicaidPrograms
            .AsNoTracking()
            .OrderBy(p => p.StateCode)
            .ThenBy(p => p.ProgramName)
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
