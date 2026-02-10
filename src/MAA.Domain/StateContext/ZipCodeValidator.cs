using MAA.Domain.Exceptions;

namespace MAA.Domain.StateContext;

/// <summary>
/// Static validator for U.S. ZIP codes (5-digit format)
/// </summary>
public static class ZipCodeValidator
{
    private const string ZipCodePattern = @"^\d{5}$";
    private static readonly System.Text.RegularExpressions.Regex ZipCodeRegex =
        new(ZipCodePattern, System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Validates if a ZIP code string is in valid 5-digit format
    /// </summary>
    /// <param name="zipCode">The ZIP code to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(string? zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
            return false;

        return ZipCodeRegex.IsMatch(zipCode);
    }

    /// <summary>
    /// Validates a ZIP code and throws an exception if invalid
    /// </summary>
    /// <param name="zipCode">The ZIP code to validate</param>
    /// <exception cref="ValidationException">Thrown when ZIP code is invalid</exception>
    public static void Validate(string? zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ValidationException("ZIP code is required");

        if (!ZipCodeRegex.IsMatch(zipCode))
            throw new ValidationException("ZIP code must be exactly 5 digits");
    }

    /// <summary>
    /// Validates a ZIP code and returns a validation result with detailed error message
    /// </summary>
    /// <param name="zipCode">The ZIP code to validate</param>
    /// <returns>Validation result with success status and error message</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateWithMessage(string? zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
            return (false, "ZIP code is required");

        if (!ZipCodeRegex.IsMatch(zipCode))
            return (false, "ZIP code must be exactly 5 digits");

        return (true, null);
    }
}
