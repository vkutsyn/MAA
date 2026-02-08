namespace MAA.Domain.Exceptions;

/// <summary>
/// Exception thrown when domain validation fails.
/// Used for business rule violations and entity validation errors.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Gets the validation errors dictionary.
    /// Key: property name; Value: list of error messages.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Creates a new validation exception with a single error message.
    /// </summary>
    /// <param name="message">Error message</param>
    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]> { { "General", new[] { message } } };
    }

    /// <summary>
    /// Creates a new validation exception with multiple errors.
    /// </summary>
    /// <param name="errors">Dictionary of property names to error message arrays</param>
    public ValidationException(IReadOnlyDictionary<string, string[]> errors) : base(BuildErrorMessage(errors))
    {
        Errors = errors;
    }

    private static string BuildErrorMessage(IReadOnlyDictionary<string, string[]> errors)
    {
        var message = "Validation failed: ";
        var allErrors = string.Join("; ", errors.SelectMany(x => x.Value));
        return message + allErrors;
    }
}
