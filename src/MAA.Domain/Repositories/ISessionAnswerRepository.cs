using MAA.Domain.Sessions;

namespace MAA.Domain.Repositories;

/// <summary>
/// Repository interface for SessionAnswer entity operations.
/// </summary>
public interface ISessionAnswerRepository
{
    /// <summary>
    /// Creates multiple answers in a single transaction (batch operation).
    /// </summary>
    /// <param name="answers">Collection of answers to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created answers with generated IDs</returns>
    Task<IEnumerable<SessionAnswer>> CreateBatchAsync(
        IEnumerable<SessionAnswer> answers,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all answers for a specific session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All answers for the session</returns>
    Task<IEnumerable<SessionAnswer>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single answer by its ID.
    /// </summary>
    /// <param name="id">Answer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Answer if found, null otherwise</returns>
    Task<SessionAnswer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing answer.
    /// </summary>
    /// <param name="answer">Answer entity with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated answer</returns>
    Task<SessionAnswer> UpdateAsync(SessionAnswer answer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific answer.
    /// </summary>
    /// <param name="id">Answer ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an answer by deterministic hash (for SSN lookup).
    /// </summary>
    /// <param name="hash">Deterministic hash to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Answer if found, null otherwise</returns>
    Task<SessionAnswer?> FindByHashAsync(string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an answer by session ID and field key (for upsert operations).
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="fieldKey">Field key (e.g., "income_annual_2025")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Answer if found, null otherwise</returns>
    Task<SessionAnswer?> FindBySessionAndFieldAsync(
        Guid sessionId,
        string fieldKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new answer.
    /// </summary>
    /// <param name="answer">Answer to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created answer with generated ID</returns>
    Task<SessionAnswer> CreateAsync(SessionAnswer answer, CancellationToken cancellationToken = default);
}
