using System.Text.Json.Serialization;

namespace MAA.Application.Eligibility.DTOs;

public class EligibilityEvaluateRequestDto
{
    [JsonPropertyName("stateCode")]
    public required string StateCode { get; set; }

    [JsonPropertyName("effectiveDate")]
    public DateTime EffectiveDate { get; set; }

    [JsonPropertyName("answers")]
    public Dictionary<string, object?> Answers { get; set; } = new();
}
