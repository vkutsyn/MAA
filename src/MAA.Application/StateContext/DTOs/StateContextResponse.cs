namespace MAA.Application.StateContext.DTOs;

/// <summary>
/// Response containing state context and configuration
/// </summary>
public record StateContextResponse
{
    /// <summary>
    /// The state context that was created or retrieved
    /// </summary>
    public required StateContextDto StateContext { get; init; }

    /// <summary>
    /// The state-specific Medicaid configuration
    /// </summary>
    public required StateConfigurationDto StateConfiguration { get; init; }
}
