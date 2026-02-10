using MAA.Application.Wizard.Repositories;
using MAA.Domain.Wizard;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.Wizard;

/// <summary>
/// Repository implementation for wizard sessions and progress tracking.
/// </summary>
public class WizardSessionRepository : IWizardSessionRepository
{
    private readonly SessionContext _context;

    public WizardSessionRepository(SessionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<WizardSession?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return null;

        return await _context.WizardSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
    }

    public async Task<WizardSession> AddAsync(WizardSession wizardSession, CancellationToken cancellationToken = default)
    {
        if (wizardSession == null)
            throw new ArgumentNullException(nameof(wizardSession));

        wizardSession.Validate();

        _context.WizardSessions.Add(wizardSession);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to create wizard session in database.", ex);
        }

        return wizardSession;
    }

    public async Task<WizardSession> UpdateAsync(WizardSession wizardSession, CancellationToken cancellationToken = default)
    {
        if (wizardSession == null)
            throw new ArgumentNullException(nameof(wizardSession));

        wizardSession.Validate();
        wizardSession.UpdatedAt = DateTime.UtcNow;
        wizardSession.Version++;

        _context.WizardSessions.Update(wizardSession);

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
            throw new InvalidOperationException("Failed to update wizard session in database.", ex);
        }

        return wizardSession;
    }

    public async Task<bool> ExistsBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return false;

        return await _context.WizardSessions
            .AsNoTracking()
            .AnyAsync(s => s.SessionId == sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<StepProgress>> GetProgressBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return Array.Empty<StepProgress>();

        return await _context.StepProgress
            .AsNoTracking()
            .Where(p => p.SessionId == sessionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<StepProgress?> GetProgressBySessionAndStepIdAsync(Guid sessionId, string stepId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty || string.IsNullOrWhiteSpace(stepId))
            return null;

        return await _context.StepProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.StepId == stepId, cancellationToken);
    }

    public async Task<StepProgress> UpsertProgressAsync(StepProgress progress, CancellationToken cancellationToken = default)
    {
        if (progress == null)
            throw new ArgumentNullException(nameof(progress));

        progress.Validate();

        var existing = await _context.StepProgress
            .FirstOrDefaultAsync(p => p.SessionId == progress.SessionId && p.StepId == progress.StepId, cancellationToken);

        if (existing == null)
        {
            _context.StepProgress.Add(progress);
        }
        else
        {
            existing.Status = progress.Status;
            existing.LastUpdatedAt = progress.LastUpdatedAt;
            existing.Version++;
            _context.StepProgress.Update(existing);
            progress = existing;
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
            throw new InvalidOperationException("Failed to upsert step progress in database.", ex);
        }

        return progress;
    }

    public async Task<IReadOnlyList<StepProgress>> UpdateProgressStatusesAsync(
        Guid sessionId,
        IReadOnlyCollection<string> stepIds,
        StepStatus status,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty || stepIds == null || stepIds.Count == 0)
            return Array.Empty<StepProgress>();

        var progressItems = await _context.StepProgress
            .Where(p => p.SessionId == sessionId && stepIds.Contains(p.StepId) && p.Status != StepStatus.NotStarted)
            .ToListAsync(cancellationToken);

        if (progressItems.Count == 0)
            return Array.Empty<StepProgress>();

        var now = DateTime.UtcNow;
        foreach (var progress in progressItems)
        {
            progress.Status = status;
            progress.LastUpdatedAt = now;
            progress.Version++;
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
            throw new InvalidOperationException("Failed to update step progress statuses in database.", ex);
        }

        return progressItems;
    }
}
