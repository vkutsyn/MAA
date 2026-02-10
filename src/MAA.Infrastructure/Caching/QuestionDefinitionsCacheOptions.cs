namespace MAA.Infrastructure.Caching;

/// <summary>
/// Configuration options for question definitions caching.
/// </summary>
public sealed class QuestionDefinitionsCacheOptions
{
    public bool Enabled { get; set; }

    public string? ConnectionString { get; set; }

    public int TtlHours { get; set; } = 24;
}
