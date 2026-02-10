using MAA.Application.StateContext.DTOs;
using MAA.Application.StateContext.Queries;
using Microsoft.Extensions.Logging;

namespace MAA.Application.StateContext.Queries;

/// <summary>
/// Handler for GetStateContextQuery
/// </summary>
public class GetStateContextHandler
{
    private readonly IStateContextRepository _stateContextRepository;
    private readonly ILogger<GetStateContextHandler> _logger;

    public GetStateContextHandler(
        IStateContextRepository stateContextRepository,
        ILogger<GetStateContextHandler> logger)
    {
        _stateContextRepository = stateContextRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the retrieval of state context
    /// </summary>
    public async Task<StateContextResponse?> HandleAsync(GetStateContextQuery query)
    {
        _logger.LogInformation("Getting state context for session {SessionId}", query.SessionId);

        var stateContext = await _stateContextRepository.GetBySessionIdAsync(query.SessionId);
        if (stateContext == null)
        {
            _logger.LogWarning("State context not found for session {SessionId}", query.SessionId);
            return null;
        }

        // Build DTO response
        var stateContextDto = new StateContextDto
        {
            Id = stateContext.Id,
            SessionId = stateContext.SessionId,
            StateCode = stateContext.StateCode,
            StateName = stateContext.StateName,
            ZipCode = stateContext.ZipCode,
            IsManualOverride = stateContext.IsManualOverride,
            EffectiveDate = stateContext.EffectiveDate,
            CreatedAt = stateContext.CreatedAt,
            UpdatedAt = stateContext.UpdatedAt
        };

        var stateConfigDto = StateConfigurationDto.FromDomain(stateContext.StateConfiguration);

        return new StateContextResponse
        {
            StateContext = stateContextDto,
            StateConfiguration = stateConfigDto
        };
    }
}
