namespace MAA.Domain.StateContext;

/// <summary>
/// Value object representing the result of a state resolution operation
/// </summary>
public class StateResolutionResult
{
    /// <summary>
    /// Whether the resolution was successful
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Resolved state code (null if failed)
    /// </summary>
    public string? StateCode { get; private init; }

    /// <summary>
    /// Error message (null if successful)
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Private constructor to enforce factory methods
    /// </summary>
    private StateResolutionResult() { }

    /// <summary>
    /// Creates a successful resolution result
    /// </summary>
    /// <param name="stateCode">The resolved state code</param>
    /// <returns>Success result</returns>
    public static StateResolutionResult Success(string stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ArgumentException("StateCode cannot be null or empty for success result", nameof(stateCode));

        return new StateResolutionResult
        {
            IsSuccess = true,
            StateCode = stateCode,
            ErrorMessage = null
        };
    }

    /// <summary>
    /// Creates a failed resolution result
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>Failure result</returns>
    public static StateResolutionResult Failure(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("ErrorMessage cannot be null or empty for failure result", nameof(errorMessage));

        return new StateResolutionResult
        {
            IsSuccess = false,
            StateCode = null,
            ErrorMessage = errorMessage
        };
    }
}
