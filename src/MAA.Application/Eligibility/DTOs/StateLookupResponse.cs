namespace MAA.Application.Eligibility.DTOs;

/// <summary>
/// DTO representing the result of a state lookup by ZIP code.
/// Used in GET /api/states/lookup?zip={zip} endpoint.
/// </summary>
public class StateLookupResponse
{
    /// <summary>
    /// Two-letter state code (e.g., "CA").
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Full state name (e.g., "California").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Lookup source (always "zip" for this endpoint).
    /// </summary>
    public required string Source { get; set; }
}
