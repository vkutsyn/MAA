using JsonLogic.Net;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace MAA.Domain.Eligibility;

public class EligibilityEvaluator
{
    private readonly ConfidenceScoringPolicy _scoringPolicy;
    private readonly JsonLogicEvaluator _evaluator;

    public EligibilityEvaluator(ConfidenceScoringPolicy scoringPolicy)
    {
        _scoringPolicy = scoringPolicy ?? throw new ArgumentNullException(nameof(scoringPolicy));
        _evaluator = new JsonLogicEvaluator(new EvaluateOperators());
    }

    public EligibilityResult Evaluate(
        EligibilityRequest request,
        RuleSetVersion ruleSet,
        IReadOnlyList<EligibilityRule> rules)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (ruleSet == null)
            throw new ArgumentNullException(nameof(ruleSet));
        if (rules == null)
            throw new ArgumentNullException(nameof(rules));

        var data = BuildEvaluationData(request.Answers);
        var matches = new List<ProgramMatch>();

        foreach (var rule in rules)
        {
            var isMatched = EvaluateRule(rule.RuleLogic, data);
            if (!isMatched)
                continue;

            var programName = rule.Program?.ProgramName ?? rule.ProgramCode;
            var confidence = _scoringPolicy.CalculateScore(request.Answers, true);

            matches.Add(new ProgramMatch
            {
                ProgramCode = rule.ProgramCode,
                ProgramName = programName,
                ConfidenceScore = confidence,
                Explanation = $"Rule matched for {programName}."
            });
        }

        var overallConfidence = matches.Count > 0
            ? matches.Max(match => match.ConfidenceScore)
            : _scoringPolicy.CalculateScore(request.Answers, false);

        var status = _scoringPolicy.GetStatus(overallConfidence);

        return new EligibilityResult
        {
            Status = status,
            MatchedPrograms = matches,
            ConfidenceScore = overallConfidence,
            Explanation = matches.Count > 0
                ? $"Matched {matches.Count} program(s)."
                : "No matching programs were found.",
            RuleVersionUsed = ruleSet.Version,
            EvaluatedAt = DateTime.UtcNow
        };
    }

    private bool EvaluateRule(string ruleLogic, JObject data)
    {
        if (string.IsNullOrWhiteSpace(ruleLogic))
            throw new ArgumentException("Rule logic is required.", nameof(ruleLogic));

        var ruleToken = JToken.Parse(ruleLogic);
        var result = _evaluator.Apply(ruleToken, data);
        return IsTruthy(result);
    }

    private static JObject BuildEvaluationData(IReadOnlyDictionary<string, object?> answers)
    {
        var data = new JObject();

        if (answers == null)
            return data;

        foreach (var answer in answers)
        {
            data[answer.Key] = ConvertToToken(answer.Value);
        }

        return data;
    }

    private static JToken ConvertToToken(object? value)
    {
        if (value == null)
            return JValue.CreateNull();

        if (value is JsonElement element)
            return JToken.Parse(element.GetRawText());

        return JToken.FromObject(value);
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
