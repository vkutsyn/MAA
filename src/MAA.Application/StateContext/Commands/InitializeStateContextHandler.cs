using MAA.Application.StateContext.Commands;
using MAA.Application.StateContext.DTOs;
using MAA.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace MAA.Application.StateContext.Commands;

/// <summary>
/// Handler for InitializeStateContextCommand
/// </summary>
public class InitializeStateContextHandler
{
    private readonly IStateContextRepository _stateContextRepository;
    private readonly IStateConfigurationRepository _stateConfigRepository;
    private readonly Domain.StateContext.StateResolver _stateResolver;
    private readonly ILogger<InitializeStateContextHandler> _logger;

    public InitializeStateContextHandler(
        IStateContextRepository stateContextRepository,
        IStateConfigurationRepository stateConfigRepository,
        Domain.StateContext.StateResolver stateResolver,
        ILogger<InitializeStateContextHandler> logger)
    {
        _stateContextRepository = stateContextRepository;
        _stateConfigRepository = stateConfigRepository;
        _stateResolver = stateResolver;
        _logger = logger;
    }

    /// <summary>
    /// Handles the initialization of state context
    /// </summary>
    public async Task<InitializeStateContextResult> HandleAsync(InitializeStateContextCommand command)
    {
        _logger.LogInformation("Initializing state context for session {SessionId}, ZIP {ZipCode}",
            command.SessionId, command.ZipCode);

        // Check if state context already exists
        var existingContext = await _stateContextRepository.GetBySessionIdAsync(command.SessionId);
        if (existingContext != null)
        {
            throw new ValidationException("State context already exists for this session");
        }

        // Resolve state from ZIP code or use override
        string stateCode;
        bool isManualOverride;

        if (!string.IsNullOrEmpty(command.StateCodeOverride))
        {
            // Manual override provided
            stateCode = command.StateCodeOverride;
            isManualOverride = true;
            _logger.LogInformation("Using manual state override: {StateCode}", stateCode);
        }
        else
        {
            // Auto-detect state from ZIP code
            var resolutionResult = _stateResolver.Resolve(command.ZipCode);
            if (!resolutionResult.IsSuccess)
            {
                _logger.LogWarning("ZIP code resolution failed: {ErrorMessage}", resolutionResult.ErrorMessage);
                throw new ValidationException(resolutionResult.ErrorMessage!);
            }

            stateCode = resolutionResult.StateCode!;
            isManualOverride = false;
            _logger.LogInformation("Auto-detected state from ZIP: {StateCode}", stateCode);
        }

        // Get state configuration
        var stateConfig = await _stateConfigRepository.GetActiveByStateCodeAsync(stateCode);
        if (stateConfig == null)
        {
            _logger.LogError("State configuration not found for state code: {StateCode}", stateCode);
            throw new ValidationException($"State configuration not found for state: {stateCode}");
        }

        // Create state context
        var stateContext = Domain.StateContext.StateContext.Create(
            command.SessionId,
            stateCode,
            stateConfig.StateName,
            command.ZipCode,
            isManualOverride
        );

        // Save state context
        await _stateContextRepository.AddAsync(stateContext);

        _logger.LogInformation("State context initialized successfully for session {SessionId}, state {StateCode}",
            command.SessionId, stateCode);

        // Build response
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

        var stateConfigDto = StateConfigurationDto.FromDomain(stateConfig);

        return new InitializeStateContextResult
        {
            StateContextResponse = new StateContextResponse
            {
                StateContext = stateContextDto,
                StateConfiguration = stateConfigDto
            }
        };
    }
}
