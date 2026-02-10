using MAA.Application.StateContext;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.StateContext;

/// <summary>
/// Repository implementation for StateContext entity
/// </summary>
public class StateContextRepository : IStateContextRepository
{
    private readonly SessionContext _context;

    public StateContextRepository(SessionContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets a state context by session ID, including related state configuration
    /// </summary>
    public async Task<Domain.StateContext.StateContext?> GetBySessionIdAsync(Guid sessionId)
    {
        return await _context.StateContexts
            .Include(sc => sc.StateConfiguration)
            .FirstOrDefaultAsync(sc => sc.SessionId == sessionId);
    }

    /// <summary>
    /// Adds a new state context to the database
    /// </summary>
    public async Task<Domain.StateContext.StateContext> AddAsync(Domain.StateContext.StateContext stateContext)
    {
        _context.StateContexts.Add(stateContext);
        await _context.SaveChangesAsync();
        return stateContext;
    }

    /// <summary>
    /// Updates an existing state context
    /// </summary>
    public async Task UpdateAsync(Domain.StateContext.StateContext stateContext)
    {
        _context.StateContexts.Update(stateContext);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Checks if a state context exists for a session
    /// </summary>
    public async Task<bool> ExistsBySessionIdAsync(Guid sessionId)
    {
        return await _context.StateContexts.AnyAsync(sc => sc.SessionId == sessionId);
    }
}
