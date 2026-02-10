using System.Text.Json;
using System.Text.Json.Serialization;

namespace MAA.Domain.Sessions;

/// <summary>
/// Defines and validates the JSONB structure for Session.Data field.
/// This schema provides flexible storage for session metadata while maintaining structure.
/// </summary>
public static class SessionDataSchema
{
    /// <summary>
    /// Validates a session data JSON string against the expected schema.
    /// </summary>
    /// <param name="json">Raw JSON string from Session.Data</param>
    /// <returns>Validation result with any errors</returns>
    public static ValidationResult Validate(string json)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(json))
        {
            result.AddError("Session data cannot be null or empty");
            return result;
        }

        try
        {
            var data = JsonSerializer.Deserialize<SessionData>(json);

            if (data == null)
            {
                result.AddError("Failed to parse session data as valid JSON");
                return result;
            }

            // Validate metadata if present
            if (data.Metadata != null)
            {
                ValidateMetadata(data.Metadata, result);
            }

            // Additional validation rules can be added here
        }
        catch (JsonException ex)
        {
            result.AddError($"Invalid JSON format: {ex.Message}");
        }

        return result;
    }

    private static void ValidateMetadata(SessionMetadata metadata, ValidationResult result)
    {
        // Validate timeout values if present
        if (metadata.TimeoutMinutes.HasValue && metadata.TimeoutMinutes.Value <= 0)
        {
            result.AddError("TimeoutMinutes must be greater than 0");
        }

        if (metadata.MaxInactivityMinutes.HasValue && metadata.MaxInactivityMinutes.Value <= 0)
        {
            result.AddError("MaxInactivityMinutes must be greater than 0");
        }
    }

    /// <summary>
    /// Creates an empty session data structure with default values.
    /// </summary>
    public static string CreateEmpty()
    {
        var data = new SessionData
        {
            Metadata = new SessionMetadata
            {
                CreatedBy = "system",
                Version = 1
            }
        };

        return JsonSerializer.Serialize(data, JsonOptions);
    }

    /// <summary>
    /// Parses session data JSON into a structured object.
    /// </summary>
    public static SessionData? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<SessionData>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Serializes session data object to JSON string.
    /// </summary>
    public static string Serialize(SessionData data)
    {
        return JsonSerializer.Serialize(data, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}

/// <summary>
/// Root structure for Session.Data JSONB field.
/// Stores session-level metadata and configuration.
/// </summary>
public class SessionData
{
    /// <summary>
    /// Session metadata (timeouts, versioning, audit info).
    /// </summary>
    [JsonPropertyName("metadata")]
    public SessionMetadata? Metadata { get; set; }

    /// <summary>
    /// Custom session-level properties (extensible).
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; set; }
}

/// <summary>
/// Session metadata structure.
/// </summary>
public class SessionMetadata
{
    /// <summary>
    /// Schema version for forward compatibility.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Who/what created this session (e.g., "wizard", "api", "admin").
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Session timeout in minutes (overrides default if present).
    /// </summary>
    [JsonPropertyName("timeoutMinutes")]
    public int? TimeoutMinutes { get; set; }

    /// <summary>
    /// Maximum inactivity period in minutes.
    /// </summary>
    [JsonPropertyName("maxInactivityMinutes")]
    public int? MaxInactivityMinutes { get; set; }

    /// <summary>
    /// Device fingerprint or identifier.
    /// </summary>
    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Session tags for categorization/analytics.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Additional notes or audit information.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

/// <summary>
/// Validation result for session data schema.
/// </summary>
public class ValidationResult
{
    private readonly List<string> _errors = new();

    /// <summary>
    /// Whether validation passed (no errors).
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// List of validation error messages.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

    internal void AddError(string error)
    {
        _errors.Add(error);
    }

    /// <summary>
    /// Returns a formatted error message string.
    /// </summary>
    public string GetErrorMessage() => string.Join("; ", _errors);
}
