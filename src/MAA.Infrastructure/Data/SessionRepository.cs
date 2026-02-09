using MAA.Domain.Repositories;
using MAA.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.Data;

/// <summary>
/// Repository implementation for Session entity.
/// Provides data access with optimistic locking support for concurrent updates.
/// </summary>
public class SessionRepository : ISessionRepository
{
    private readonly SessionContext _context;

    /// <summary>
    /// Initializes a new instance of SessionRepository.
    /// </summary>
    /// <param name="context">Entity Framework DbContext</param>
    public SessionRepository(SessionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates a new session in the database.
    /// </summary>
    /// <param name="session">Session entity to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session with generated ID</returns>
    /// <exception cref="ArgumentNullException">If session is null</exception>
    /// <exception cref="InvalidOperationException">If validation fails or database save fails</exception>
    public async Task<Session> CreateAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        session.Validate();

        _context.Sessions.Add(session);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to create session in database.", ex);
        }

        return session;
    }

    /// <summary>
    /// Retrieves a session by its ID.
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session if found, null otherwise</returns>
    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        return await _context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <summary>
    /// Updates an existing session with optimistic concurrency control.
    /// </summary>
    /// <param name="session">Session entity with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated session</returns>
    /// <exception cref="ArgumentNullException">If session is null</exception>
    /// <exception cref="InvalidOperationException">If validation fails, session not found, or concurrency conflict occurs</exception>
    public async Task<Session> UpdateAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        session.Validate();
        session.UpdatedAt = DateTime.UtcNow;
        session.Version++;

        _context.Sessions.Update(session);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                $"Session {session.Id} was modified by another process. Please refresh and try again.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to update session in database.", ex);
        }

        return session;
    }

    /// <summary>
    /// Deletes a session and all associated data.
    /// </summary>
    /// <param name="id">Session ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">If session not found or delete fails</exception>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (session == null)
            throw new InvalidOperationException($"Session {id} not found.");

        _context.Sessions.Remove(session);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to delete session from database.", ex);
        }
    }

    /// <summary>
    /// Lists all expired sessions for cleanup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expired sessions</returns>
    public async Task<IEnumerable<Session>> ListExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.Sessions
            .AsNoTracking()
            .Where(s => s.ExpiresAt <= now || s.InactivityTimeoutAt <= now)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets sessions by user ID (Phase 5 feature).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sessions for the user</returns>
    public async Task<IEnumerable<Session>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Enumerable.Empty<Session>();

        return await _context.Sessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all active sessions (not expired, not revoked).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active sessions</returns>
    public async Task<IEnumerable<Session>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.Sessions
            .AsNoTracking()
            .Where(s => !s.IsRevoked
                && s.ExpiresAt > now
                && s.InactivityTimeoutAt > now
                && s.State != SessionState.Abandoned)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Counts sessions by state.
    /// </summary>
    /// <param name="state">Session state to count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions in the specified state</returns>
    public async Task<int> CountByStateAsync(SessionState state, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .Where(s => s.State == state)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if session exists and is valid.
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if session exists and is valid</returns>
    public async Task<bool> ExistsAndIsValidAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return false;

        var now = DateTime.UtcNow;

        return await _context.Sessions
            .AsNoTracking()
            .AnyAsync(s => s.Id == id
                && !s.IsRevoked
                && s.ExpiresAt > now
                && s.InactivityTimeoutAt > now
                && s.State != SessionState.Abandoned,
                cancellationToken);
    }
}
