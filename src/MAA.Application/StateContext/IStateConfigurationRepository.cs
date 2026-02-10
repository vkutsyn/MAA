namespace MAA.Application.StateContext;

/// <summary>
/// Repository interface for StateConfiguration entity
/// </summary>
public interface IStateConfigurationRepository
{
    /// <summary>
    /// Gets an active state configuration by state code
    /// Uses caching to minimize database queries
    /// </summary>
    Task<Domain.StateContext.StateConfiguration?> GetActiveByStateCodeAsync(string stateCode);

    /// <summary>
    /// Gets all active state configurations (for dropdowns, etc.)
    /// Uses caching to minimize database queries
    /// </summary>
    Task<List<Domain.StateContext.StateConfiguration>> GetAllActiveAsync();

    /// <summary>
    /// Checks if a state configuration exists for a state code
    /// </summary>
    Task<bool> ExistsAsync(string stateCode);
}
