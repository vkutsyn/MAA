namespace MAA.Application.Eligibility.DTOs;

/// <summary>
/// DTO representing state metadata for the eligibility wizard.
/// Used in GET /api/states endpoint.
/// </summary>
public class StateInfoDto
{
    /// <summary>
    /// Two-letter state code (e.g., "CA", "TX").
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Full state name (e.g., "California", "Texas").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Whether this state is a pilot state with full question taxonomy.
    /// </summary>
    public bool IsPilot { get; set; }
}
