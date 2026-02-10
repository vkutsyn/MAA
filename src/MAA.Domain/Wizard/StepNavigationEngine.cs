using JsonLogic.Net;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace MAA.Domain.Wizard;

/// <summary>
/// Evaluates step navigation rules using JsonLogic.
/// </summary>
public class StepNavigationEngine
{
    private readonly IStepDefinitionProvider _definitionProvider;
    private readonly JsonLogicEvaluator _evaluator;

    public StepNavigationEngine(IStepDefinitionProvider definitionProvider)
    {
        _definitionProvider = definitionProvider ?? throw new ArgumentNullException(nameof(definitionProvider));
        _evaluator = new JsonLogicEvaluator(new EvaluateOperators());
    }

    public StepDefinition? GetNextStep(string currentStepId, IReadOnlyDictionary<string, JsonElement> answers)
    {
        if (string.IsNullOrWhiteSpace(currentStepId))
            return null;

        var currentStep = _definitionProvider.GetById(currentStepId);
        if (currentStep == null || currentStep.NextStepRules.Count == 0)
            return null;

        var data = BuildEvaluationData(answers);

        foreach (var rule in currentStep.NextStepRules)
        {
            if (EvaluateRule(rule.Condition, data))
                return _definitionProvider.GetById(rule.ToStepId);
        }

        return null;
    }

    private bool EvaluateRule(JsonElement condition, JObject data)
    {
        if (condition.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return false;

        var conditionToken = JToken.Parse(condition.GetRawText());
        var result = _evaluator.Apply(conditionToken, data);
        return IsTruthy(result);
    }

    private static JObject BuildEvaluationData(IReadOnlyDictionary<string, JsonElement> answers)
    {
        var data = new JObject();

        foreach (var answer in answers)
        {
            data[answer.Key] = JToken.Parse(answer.Value.GetRawText());
        }

        return data;
    }

    private static bool IsTruthy(object? result)
    {
        if (result == null)
            return false;

        if (result is bool booleanResult)
            return booleanResult;

        if (result is JToken token)
        {
            return token.Type switch
            {
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Integer => token.Value<long>() != 0,
                JTokenType.Float => Math.Abs(token.Value<double>()) > double.Epsilon,
                JTokenType.String => !string.IsNullOrWhiteSpace(token.Value<string>()),
                JTokenType.Null => false,
                JTokenType.Undefined => false,
                _ => token.HasValues
            };
        }

        if (result is int intResult)
            return intResult != 0;

        if (result is double doubleResult)
            return Math.Abs(doubleResult) > double.Epsilon;

        if (result is string stringResult)
            return !string.IsNullOrWhiteSpace(stringResult);

        return true;
    }
}
