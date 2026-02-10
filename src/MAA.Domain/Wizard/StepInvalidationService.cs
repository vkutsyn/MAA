using System.Text.Json;

namespace MAA.Domain.Wizard;

/// <summary>
/// Determines downstream steps that should be invalidated when answers change.
/// </summary>
public class StepInvalidationService
{
    private readonly IStepDefinitionProvider _definitionProvider;

    public StepInvalidationService(IStepDefinitionProvider definitionProvider)
    {
        _definitionProvider = definitionProvider ?? throw new ArgumentNullException(nameof(definitionProvider));
    }

    public IReadOnlyList<string> GetDownstreamStepIds(string currentStepId)
    {
        if (string.IsNullOrWhiteSpace(currentStepId))
            return Array.Empty<string>();

        var definitions = _definitionProvider.GetAll();
        var current = definitions.FirstOrDefault(definition =>
            string.Equals(definition.StepId, currentStepId, StringComparison.OrdinalIgnoreCase));

        if (current == null)
            return Array.Empty<string>();

        var currentSequence = GetSequence(current);

        var downstream = definitions
            .Where(definition => !string.Equals(definition.StepId, currentStepId, StringComparison.OrdinalIgnoreCase))
            .Where(definition => currentSequence == null || GetSequence(definition) > currentSequence)
            .Select(definition => definition.StepId)
            .ToList();

        return downstream;
    }

    private static int? GetSequence(StepDefinition definition)
    {
        if (definition.Sequence.HasValue)
            return definition.Sequence.Value;

        if (definition.DisplayMeta != null
            && definition.DisplayMeta.TryGetValue("sequence", out var sequenceElement)
            && sequenceElement.ValueKind == JsonValueKind.Number
            && sequenceElement.TryGetInt32(out var sequence))
        {
            return sequence;
        }

        return null;
    }
}
