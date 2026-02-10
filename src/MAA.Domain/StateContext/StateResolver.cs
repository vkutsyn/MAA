namespace MAA.Domain.StateContext;

/// <summary>
/// Resolves U.S. state from ZIP code using an in-memory lookup table
/// </summary>
public class StateResolver
{
    private readonly IReadOnlyDictionary<string, string> _zipToStateMapping;

    /// <summary>
    /// Constructor with dependency injection for ZIP-to-state mapping
    /// </summary>
    /// <param name="zipToStateMapping">Dictionary mapping ZIP codes to state codes</param>
    public StateResolver(IReadOnlyDictionary<string, string> zipToStateMapping)
    {
        _zipToStateMapping = zipToStateMapping ?? throw new ArgumentNullException(nameof(zipToStateMapping));
    }

    /// <summary>
    /// Resolves state code from a ZIP code
    /// </summary>
    /// <param name="zipCode">5-digit ZIP code</param>
    /// <returns>StateResolutionResult containing state code or error</returns>
    public StateResolutionResult Resolve(string zipCode)
    {
        // Validate ZIP code format first
        var (isValid, errorMessage) = ZipCodeValidator.ValidateWithMessage(zipCode);
        if (!isValid)
        {
            return StateResolutionResult.Failure(errorMessage!);
        }

        // Lookup state code
        if (_zipToStateMapping.TryGetValue(zipCode, out var stateCode))
        {
            return StateResolutionResult.Success(stateCode);
        }

        return StateResolutionResult.Failure($"ZIP code '{zipCode}' not found in our database");
    }

    /// <summary>
    /// Checks if a ZIP code exists in the mapping
    /// </summary>
    /// <param name="zipCode">5-digit ZIP code</param>
    /// <returns>True if ZIP exists, false otherwise</returns>
    public bool Exists(string zipCode)
    {
        if (!ZipCodeValidator.IsValid(zipCode))
            return false;

        return _zipToStateMapping.ContainsKey(zipCode);
    }
}
