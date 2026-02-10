using MAA.Application.StateContext.Commands;
using MAA.Application.StateContext.DTOs;
using MAA.Domain.Exceptions;
using MAA.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MAA.Application.StateContext.Commands;

/// <summary>
/// Handler for InitializeStateContextCommand
/// </summary>
public class InitializeStateContextHandler
{
    private readonly IStateContextRepository _stateContextRepository;
    private readonly IStateConfigurationRepository _stateConfigRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly Domain.StateContext.StateResolver _stateResolver;
    private readonly ILogger<InitializeStateContextHandler> _logger;

    public InitializeStateContextHandler(
        IStateContextRepository stateContextRepository,
        IStateConfigurationRepository stateConfigRepository,
        ISessionRepository sessionRepository,
        Domain.StateContext.StateResolver stateResolver,
        ILogger<InitializeStateContextHandler> logger)
    {
        _stateContextRepository = stateContextRepository;
        _stateConfigRepository = stateConfigRepository;
        _sessionRepository = sessionRepository;
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

        // Validate that the session exists
        var session = await _sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found", command.SessionId);
            throw new ValidationException($"Session {command.SessionId} does not exist. Please create a session first.");
        }

        // Check if state context already exists
        var contextExists = await _stateContextRepository.ExistsBySessionIdAsync(command.SessionId);
        if (contextExists)
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
            throw new ValidationException($"State configuration not found for state: {stateCode}. Please ensure state configurations are properly seeded.");
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
        try
        {
            await _stateContextRepository.AddAsync(stateContext);
            _logger.LogInformation("State context initialized successfully for session {SessionId}, state {StateCode}",
                command.SessionId, stateCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save state context for session {SessionId}", command.SessionId);
            throw new ValidationException($"Failed to save state context: {ex.Message}");
        }

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
