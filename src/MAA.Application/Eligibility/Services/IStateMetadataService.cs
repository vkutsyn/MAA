using MAA.Application.Eligibility.DTOs;

namespace MAA.Application.Eligibility.Services;

/// <summary>
/// Service for state metadata operations for the eligibility wizard.
/// Provides state listings and ZIP code lookups.
/// </summary>
public interface IStateMetadataService
{
    /// <summary>
    /// Gets all available states for the eligibility wizard.
    /// Returns pilot states with full question taxonomy.
    /// </summary>
    /// <returns>List of state information DTOs</returns>
    Task<List<StateInfoDto>> GetAllStatesAsync();

    /// <summary>
    /// Looks up a state by 5-digit ZIP code.
    /// Returns state information if ZIP match found.
    /// </summary>
    /// <param name="zip">5-digit ZIP code</param>
    /// <returns>State lookup response or null if not found</returns>
    Task<StateLookupResponse?> LookupStateByZipAsync(string zip);
}
