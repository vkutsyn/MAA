using System.Text.Json;

namespace MAA.Domain.Wizard;

/// <summary>
/// Provides wizard step definitions loaded from configuration.
/// </summary>
public interface IStepDefinitionProvider
{
    IReadOnlyList<StepDefinition> GetAll();
    StepDefinition? GetById(string stepId);
}

/// <summary>
/// In-memory step definition provider backed by JSON configuration.
/// </summary>
public class StepDefinitionProvider : IStepDefinitionProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Lazy<IReadOnlyList<StepDefinition>> _definitions;

    public StepDefinitionProvider()
    {
        _definitions = new Lazy<IReadOnlyList<StepDefinition>>(LoadDefinitions);
    }

    public IReadOnlyList<StepDefinition> GetAll() => _definitions.Value;

    public StepDefinition? GetById(string stepId)
    {
        if (string.IsNullOrWhiteSpace(stepId))
            return null;

        return _definitions.Value.FirstOrDefault(definition =>
            string.Equals(definition.StepId, stepId, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<StepDefinition> LoadDefinitions()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Wizard", "Definitions", "eligibility-steps.json");
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Wizard step definitions file not found.", filePath);

        var json = File.ReadAllText(filePath);
        var definitions = JsonSerializer.Deserialize<List<StepDefinition>>(json, SerializerOptions);
        return definitions ?? Array.Empty<StepDefinition>();
    }
}
