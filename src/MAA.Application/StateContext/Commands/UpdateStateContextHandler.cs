using MAA.Application.StateContext.Commands;
using MAA.Application.StateContext.DTOs;
using MAA.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace MAA.Application.StateContext.Commands;

/// <summary>
/// Handler for UpdateStateContextCommand
/// </summary>
public class UpdateStateContextHandler
{
    private readonly IStateContextRepository _stateContextRepository;
    private readonly IStateConfigurationRepository _stateConfigRepository;
    private readonly ILogger<UpdateStateContextHandler> _logger;

    public UpdateStateContextHandler(
        IStateContextRepository stateContextRepository,
        IStateConfigurationRepository stateConfigRepository,
        ILogger<UpdateStateContextHandler> logger)
    {
        _stateContextRepository = stateContextRepository;
        _stateConfigRepository = stateConfigRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the update of state context
    /// </summary>
    public async Task<UpdateStateContextResult> HandleAsync(UpdateStateContextCommand command)
    {
        _logger.LogInformation("Updating state context for session {SessionId} to state {StateCode}",
            command.SessionId, command.StateCode);

        // Get existing state context
        var existingContext = await _stateContextRepository.GetBySessionIdAsync(command.SessionId);
        if (existingContext == null)
        {
            _logger.LogWarning("State context not found for session {SessionId}", command.SessionId);
            throw new ValidationException("State context not found for this session");
        }

        // Verify the state code exists and has a configuration
        var stateConfig = await _stateConfigRepository.GetActiveByStateCodeAsync(command.StateCode);
        if (stateConfig == null)
        {
            _logger.LogError("State configuration not found for state code: {StateCode}", command.StateCode);
            throw new ValidationException($"State configuration not found for state: {command.StateCode}");
        }

        // Update state context
        existingContext.UpdateState(command.StateCode, stateConfig.StateName, command.IsManualOverride);
        await _stateContextRepository.UpdateAsync(existingContext);

        _logger.LogInformation("State context updated successfully for session {SessionId}, new state {StateCode}",
            command.SessionId, command.StateCode);

        // Build response
        var stateContextDto = new StateContextDto
        {
            Id = existingContext.Id,
            SessionId = existingContext.SessionId,
            StateCode = existingContext.StateCode,
            StateName = existingContext.StateName,
            ZipCode = existingContext.ZipCode,
            IsManualOverride = existingContext.IsManualOverride,
            EffectiveDate = existingContext.EffectiveDate,
            CreatedAt = existingContext.CreatedAt,
            UpdatedAt = existingContext.UpdatedAt
        };

        var stateConfigDto = StateConfigurationDto.FromDomain(stateConfig);

        return new UpdateStateContextResult
        {
            StateContextResponse = new StateContextResponse
            {
                StateContext = stateContextDto,
                StateConfiguration = stateConfigDto
            }
        };
    }
}
