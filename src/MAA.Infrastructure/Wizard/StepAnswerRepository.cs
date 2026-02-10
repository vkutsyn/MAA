using MAA.Application.Wizard.Repositories;
using MAA.Domain.Wizard;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.Wizard;

/// <summary>
/// Repository implementation for wizard step answers.
/// </summary>
public class StepAnswerRepository : IStepAnswerRepository
{
    private readonly SessionContext _context;

    public StepAnswerRepository(SessionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<StepAnswer?> GetBySessionAndStepIdAsync(Guid sessionId, string stepId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty || string.IsNullOrWhiteSpace(stepId))
            return null;

        return await _context.StepAnswers
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.StepId == stepId, cancellationToken);
    }

    public async Task<IReadOnlyList<StepAnswer>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return Array.Empty<StepAnswer>();

        return await _context.StepAnswers
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<StepAnswer> UpsertAsync(StepAnswer stepAnswer, CancellationToken cancellationToken = default)
    {
        if (stepAnswer == null)
            throw new ArgumentNullException(nameof(stepAnswer));

        stepAnswer.Validate();

        var existing = await _context.StepAnswers
            .FirstOrDefaultAsync(a => a.SessionId == stepAnswer.SessionId && a.StepId == stepAnswer.StepId, cancellationToken);

        if (existing == null)
        {
            _context.StepAnswers.Add(stepAnswer);
        }
        else
        {
            existing.AnswerData = stepAnswer.AnswerData;
            existing.SchemaVersion = stepAnswer.SchemaVersion;
            existing.Status = stepAnswer.Status;
            existing.SubmittedAt = stepAnswer.SubmittedAt;
            existing.UpdatedAt = stepAnswer.UpdatedAt;
            existing.Version++;
            _context.StepAnswers.Update(existing);
            stepAnswer = existing;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to upsert step answer in database.", ex);
        }

        return stepAnswer;
    }
}
