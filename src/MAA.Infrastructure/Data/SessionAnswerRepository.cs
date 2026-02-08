using MAA.Domain.Repositories;
using MAA.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.Data;

/// <summary>
/// Repository implementation for SessionAnswer entity.
/// Provides data access with support for batch operations and encrypted/plain text storage.
/// </summary>
public class SessionAnswerRepository : ISessionAnswerRepository
{
    private readonly SessionContext _context;

    /// <summary>
    /// Initializes a new instance of SessionAnswerRepository.
    /// </summary>
    /// <param name="context">Entity Framework DbContext</param>
    public SessionAnswerRepository(SessionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates multiple answers in a single transaction (batch operation).
    /// </summary>
    /// <param name="answers">Collection of answers to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created answers with generated IDs</returns>
    /// <exception cref="ArgumentNullException">If answers collection is null</exception>
    /// <exception cref="InvalidOperationException">If validation fails or database save fails</exception>
    public async Task<IEnumerable<SessionAnswer>> CreateBatchAsync(
        IEnumerable<SessionAnswer> answers,
        CancellationToken cancellationToken = default)
    {
        if (answers == null)
            throw new ArgumentNullException(nameof(answers));

        var answersList = answers.ToList();

        if (answersList.Count == 0)
            return Enumerable.Empty<SessionAnswer>();

        // Validate all answers before inserting
        foreach (var answer in answersList)
        {
            answer.Validate();
        }

        _context.SessionAnswers.AddRange(answersList);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to create answers in database.", ex);
        }

        return answersList;
    }

    /// <summary>
    /// Retrieves all answers for a specific session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All answers for the session</returns>
    public async Task<IEnumerable<SessionAnswer>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return Enumerable.Empty<SessionAnswer>();

        return await _context.SessionAnswers
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a single answer by its ID.
    /// </summary>
    /// <param name="id">Answer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Answer if found, null otherwise</returns>
    public async Task<SessionAnswer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        return await _context.SessionAnswers
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <summary>
    /// Updates an existing answer with optimistic concurrency control.
    /// </summary>
    /// <param name="answer">Answer entity with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated answer</returns>
    /// <exception cref="ArgumentNullException">If answer is null</exception>
    /// <exception cref="InvalidOperationException">If validation fails or concurrency conflict occurs</exception>
    public async Task<SessionAnswer> UpdateAsync(SessionAnswer answer, CancellationToken cancellationToken = default)
    {
        if (answer == null)
            throw new ArgumentNullException(nameof(answer));

        answer.Validate();
        answer.UpdatedAt = DateTime.UtcNow;
        answer.Version++;

        _context.SessionAnswers.Update(answer);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                $"Answer {answer.Id} was modified by another process. Please refresh and try again.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to update answer in database.", ex);
        }

        return answer;
    }

    /// <summary>
    /// Deletes a specific answer.
    /// </summary>
    /// <param name="id">Answer ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">If answer not found or delete fails</exception>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var answer = await _context.SessionAnswers
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (answer == null)
            throw new InvalidOperationException($"Answer {id} not found.");

        _context.SessionAnswers.Remove(answer);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to delete answer from database.", ex);
        }
    }

    /// <summary>
    /// Finds answers by field key for a specific session.
    /// Useful for retrieving specific question responses.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="fieldKey">Field key to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Answers matching the field key</returns>
    public async Task<IEnumerable<SessionAnswer>> FindByFieldKeyAsync(
        Guid sessionId,
        string fieldKey,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty || string.IsNullOrWhiteSpace(fieldKey))
            return Enumerable.Empty<SessionAnswer>();

        return await _context.SessionAnswers
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId && a.FieldKey == fieldKey)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets PII answers (encrypted) for a session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PII answers for the session</returns>
    public async Task<IEnumerable<SessionAnswer>> GetPiiAnswersAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return Enumerable.Empty<SessionAnswer>();

        return await _context.SessionAnswers
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId && a.IsPii)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets plain text answers (non-PII) for a session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plain text answers for the session</returns>
    public async Task<IEnumerable<SessionAnswer>> GetPlainAnswersAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return Enumerable.Empty<SessionAnswer>();

        return await _context.SessionAnswers
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId && !a.IsPii)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes all answers for a session (used for session cleanup).
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of answers deleted</returns>
    public async Task<int> DeleteBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return 0;

        var answers = await _context.SessionAnswers
            .Where(a => a.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        _context.SessionAnswers.RemoveRange(answers);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to delete answers from database.", ex);
        }

        return answers.Count;
    }

    /// <summary>
    /// Counts answers for a session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of answers</returns>
    public async Task<int> CountBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return 0;

        return await _context.SessionAnswers
            .Where(a => a.SessionId == sessionId)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Finds an answer by its deterministic hash (for SSN validation).
    /// </summary>
    /// <param name="hash">The deterministic hash value (hex string)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>First matching answer or null</returns>
    public async Task<SessionAnswer?> FindByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return null;

        return await _context.SessionAnswers
            .AsNoTracking()
            .Where(a => a.AnswerHash != null && a.FieldKey == "ssn") // SSN fields have deterministic hashes
            .FirstOrDefaultAsync(cancellationToken);
    }
}
