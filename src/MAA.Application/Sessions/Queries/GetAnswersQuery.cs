using MAA.Application.Services;
using MAA.Application.Sessions.DTOs;
using MAA.Domain.Repositories;

namespace MAA.Application.Sessions.Queries;

/// <summary>
/// Query to retrieve all answers for a session.
/// Handles decryption for PII fields before returning DTOs.
/// US2: Wizard answers saved → page refresh → answers restored.
/// </summary>
public class GetAnswersQuery
{
    public Guid SessionId { get; set; }
}

/// <summary>
/// Handler for GetAnswersQuery.
/// Retrieves answers from database and decrypts PII fields.
/// </summary>
public class GetAnswersQueryHandler
{
    private readonly ISessionAnswerRepository _answerRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IEncryptionService _encryptionService;

    public GetAnswersQueryHandler(
        ISessionAnswerRepository answerRepository,
        ISessionRepository sessionRepository,
        IEncryptionService encryptionService)
    {
        _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    /// <summary>
    /// Executes query and returns decrypted answers.
    /// </summary>
    /// <param name="query">Query containing session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of answer DTOs with decrypted values</returns>
    /// <exception cref="InvalidOperationException">If session not found or invalid</exception>
    public async Task<List<SessionAnswerDto>> HandleAsync(GetAnswersQuery query, CancellationToken cancellationToken = default)
    {
        // Validate session exists and is valid
        var session = await _sessionRepository.GetByIdAsync(query.SessionId, cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"Session {query.SessionId} not found.");

        if (!session.IsValid())
            throw new InvalidOperationException("Session has expired or is no longer valid.");

        // Retrieve all answers for session
        var answers = await _answerRepository.GetBySessionAsync(query.SessionId, cancellationToken);

        var dtos = new List<SessionAnswerDto>();

        foreach (var answer in answers)
        {
            string? answerValue;

            // Decrypt PII fields; use plain text for non-PII
            if (answer.IsPii && !string.IsNullOrWhiteSpace(answer.AnswerEncrypted) && answer.KeyVersion.HasValue)
            {
                // US2 Acceptance Scenario 3: Decryption happens on application side, returns plain value
                answerValue = await _encryptionService.DecryptAsync(
                    answer.AnswerEncrypted,
                    answer.KeyVersion.Value,
                    cancellationToken);
            }
            else
            {
                answerValue = answer.AnswerPlain;
            }

            dtos.Add(new SessionAnswerDto
            {
                Id = answer.Id,
                SessionId = answer.SessionId,
                FieldKey = answer.FieldKey,
                FieldType = answer.FieldType,
                AnswerValue = answerValue,
                IsPii = answer.IsPii,
                KeyVersion = answer.KeyVersion,
                ValidationErrors = answer.ValidationErrors,
                CreatedAt = answer.CreatedAt,
                UpdatedAt = answer.UpdatedAt
            });
        }

        return dtos;
    }
}
