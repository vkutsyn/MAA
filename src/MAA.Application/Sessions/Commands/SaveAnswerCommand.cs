using MAA.Application.Services;
using MAA.Application.Sessions.DTOs;
using MAA.Domain.Repositories;
using MAA.Domain.Sessions;

namespace MAA.Application.Sessions.Commands;

/// <summary>
/// Command to save or update a session answer.
/// Handles encryption for PII fields and plain text storage for non-PII.
/// Implements upsert logic: updates existing answer if FieldKey exists, creates new otherwise.
/// US2 Acceptance Scenario 1: Income stored encrypted before database insert.
/// </summary>
public class SaveAnswerCommand
{
    public Guid SessionId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string AnswerValue { get; set; } = string.Empty;
    public bool IsPii { get; set; }
}

/// <summary>
/// Handler for SaveAnswerCommand.
/// Coordinates encryption, database persistence, and answer lifecycle.
/// </summary>
public class SaveAnswerCommandHandler
{
    private readonly ISessionAnswerRepository _answerRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IEncryptionService _encryptionService;

    public SaveAnswerCommandHandler(
        ISessionAnswerRepository answerRepository,
        ISessionRepository sessionRepository,
        IEncryptionService encryptionService)
    {
        _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    /// <summary>
    /// Executes save/update operation with encryption.
    /// </summary>
    /// <param name="command">Command containing answer data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved answer DTO</returns>
    /// <exception cref="InvalidOperationException">If session not found or validation fails</exception>
    public async Task<SessionAnswerDto> HandleAsync(SaveAnswerCommand command, CancellationToken cancellationToken = default)
    {
        // Validate session exists and is valid
        var session = await _sessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"Session {command.SessionId} not found.");

        if (!session.IsValid())
            throw new InvalidOperationException("Session has expired or is no longer valid.");

        // Check if answer already exists (upsert logic)
        var existingAnswer = await _answerRepository.FindBySessionAndFieldAsync(
            command.SessionId,
            command.FieldKey,
            cancellationToken);

        SessionAnswer answer;

        if (existingAnswer != null)
        {
            // Update existing answer
            answer = existingAnswer;
            answer.FieldType = command.FieldType;
            answer.UpdatedAt = DateTime.UtcNow;
            answer.Version++;
        }
        else
        {
            // Create new answer
            answer = new SessionAnswer
            {
                Id = Guid.NewGuid(),
                SessionId = command.SessionId,
                FieldKey = command.FieldKey,
                FieldType = command.FieldType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };
        }

        // Set PII flag
        answer.IsPii = command.IsPii;

        // Handle encryption based on PII flag
        if (command.IsPii)
        {
            // Encrypt PII data (US2 Acceptance Scenario 1)
            var keyVersion = await _encryptionService.GetCurrentKeyVersionAsync(cancellationToken);
            answer.AnswerEncrypted = await _encryptionService.EncryptAsync(
                command.AnswerValue,
                keyVersion,
                cancellationToken);
            answer.AnswerPlain = null; // Clear plain text for PII
            answer.KeyVersion = keyVersion;

            // Generate deterministic hash for SSN fields (US2: deterministic encryption for exact-match queries)
            if (command.FieldKey == "ssn")
            {
                answer.AnswerHash = await _encryptionService.HashAsync(
                    command.AnswerValue,
                    keyVersion,
                    cancellationToken);
            }
        }
        else
        {
            // Store non-PII as plain text (US2: demographics not encrypted)
            answer.AnswerPlain = command.AnswerValue;
            answer.AnswerEncrypted = null;
            answer.AnswerHash = null;
            answer.KeyVersion = 0; // No encryption key needed
        }

        // Save or update
        if (existingAnswer != null)
        {
            await _answerRepository.UpdateAsync(answer, cancellationToken);
        }
        else
        {
            await _answerRepository.CreateAsync(answer, cancellationToken);
        }

        // Return DTO with decrypted value
        return new SessionAnswerDto
        {
            Id = answer.Id,
            SessionId = answer.SessionId,
            FieldKey = answer.FieldKey,
            FieldType = answer.FieldType,
            AnswerValue = command.AnswerValue, // Return original value (not encrypted)
            IsPii = answer.IsPii,
            KeyVersion = answer.KeyVersion,
            ValidationErrors = answer.ValidationErrors,
            CreatedAt = answer.CreatedAt,
            UpdatedAt = answer.UpdatedAt
        };
    }
}
