using MAA.Application.Sessions.Commands;

namespace MAA.Application.Sessions.Validators;

/// <summary>
/// Validator for SaveAnswerCommand.
/// Validates field key format, answer value constraints, and field type consistency.
/// Constitution II: Testing Standards - input validation defensive programming.
/// </summary>
public class SaveAnswerCommandValidator
{
    private static readonly string[] ValidFieldTypes = { "currency", "integer", "string", "boolean", "date", "text" };

    /// <summary>
    /// Validates the save answer command.
    /// </summary>
    /// <param name="command">Command to validate</param>
    /// <returns>Validation result with errors if any</returns>
    public ValidationResult Validate(SaveAnswerCommand command)
    {
        var result = new ValidationResult();

        if (command == null)
        {
            result.AddError("Command cannot be null");
            return result;
        }

        // Validate SessionId
        if (command.SessionId == Guid.Empty)
        {
            result.AddError("SessionId is required");
        }

        // Validate FieldKey
        if (string.IsNullOrWhiteSpace(command.FieldKey))
        {
            result.AddError("FieldKey is required");
        }
        else if (command.FieldKey.Length > 200)
        {
            result.AddError("FieldKey cannot exceed 200 characters");
        }

        // Validate FieldType
        if (string.IsNullOrWhiteSpace(command.FieldType))
        {
            result.AddError("FieldType is required");
        }
        else if (!ValidFieldTypes.Contains(command.FieldType.ToLower()))
        {
            result.AddError($"FieldType must be one of: {string.Join(", ", ValidFieldTypes)}");
        }

        // Validate AnswerValue
        if (string.IsNullOrWhiteSpace(command.AnswerValue))
        {
            result.AddError("AnswerValue is required");
        }
        else if (command.AnswerValue.Length > 10000)
        {
            result.AddError("AnswerValue cannot exceed 10000 characters");
        }

        // Type-specific validation
        if (!string.IsNullOrWhiteSpace(command.FieldType) && !string.IsNullOrWhiteSpace(command.AnswerValue))
        {
            switch (command.FieldType.ToLower())
            {
                case "integer":
                    if (!int.TryParse(command.AnswerValue, out _))
                    {
                        result.AddError("AnswerValue must be a valid integer for FieldType 'integer'");
                    }
                    break;

                case "currency":
                    if (!decimal.TryParse(command.AnswerValue, out var currencyValue))
                    {
                        result.AddError("AnswerValue must be a valid number for FieldType 'currency'");
                    }
                    else if (currencyValue < 0)
                    {
                        result.AddError("AnswerValue cannot be negative for FieldType 'currency'");
                    }
                    break;

                case "boolean":
                    if (!bool.TryParse(command.AnswerValue, out _))
                    {
                        result.AddError("AnswerValue must be 'true' or 'false' for FieldType 'boolean'");
                    }
                    break;

                case "date":
                    if (!DateTime.TryParse(command.AnswerValue, out _))
                    {
                        result.AddError("AnswerValue must be a valid date for FieldType 'date'");
                    }
                    break;
            }
        }

        // PII field validation (SSN specific)
        if (command.IsPii && command.FieldKey == "ssn")
        {
            // Basic SSN format validation (without dashes: 9 digits)
            var ssnDigits = command.AnswerValue.Replace("-", "").Replace(" ", "");
            if (ssnDigits.Length != 9 || !ssnDigits.All(char.IsDigit))
            {
                result.AddError("SSN must be 9 digits (format: 123-45-6789 or 123456789)");
            }
        }

        return result;
    }
}

/// <summary>
/// Validation result container.
/// </summary>
public class ValidationResult
{
    private readonly List<string> _errors = new();

    public bool IsValid => _errors.Count == 0;
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

    public void AddError(string error)
    {
        _errors.Add(error);
    }

    public string GetErrorMessage() => string.Join("; ", _errors);
}
