using MAA.Domain;
using MAA.Domain.Exceptions;
using MAA.Domain.Rules;

namespace MAA.Application.Validation;

/// <summary>
/// Validates conditional rule expressions and prevents circular dependencies.
/// </summary>
public class ConditionalRuleValidator
{
    public void Validate(
        IReadOnlyCollection<Question> questions,
        IReadOnlyCollection<ConditionalRule> rules)
    {
        if (questions.Count == 0)
            return;

        var errors = new Dictionary<string, List<string>>();
        var ruleMap = rules.ToDictionary(r => r.ConditionalRuleId, r => r);
        var questionIds = new HashSet<Guid>(questions.Select(q => q.QuestionId));
        var dependencyMap = new Dictionary<Guid, HashSet<Guid>>();

        foreach (var question in questions)
        {
            if (!question.ConditionalRuleId.HasValue)
                continue;

            var ruleId = question.ConditionalRuleId.Value;
            if (!ruleMap.TryGetValue(ruleId, out var rule))
            {
                AddError(errors, "conditionalRules", $"Missing conditional rule '{ruleId}'.");
                continue;
            }

            IReadOnlySet<Guid> referencedIds;
            try
            {
                referencedIds = ConditionalRuleEvaluator.GetReferencedQuestionIds(rule.RuleExpression);
            }
            catch (ArgumentException ex)
            {
                AddError(errors, "conditionalRules", $"Invalid rule '{ruleId}': {ex.Message}");
                continue;
            }

            foreach (var referencedId in referencedIds)
            {
                if (!questionIds.Contains(referencedId))
                {
                    AddError(errors, "conditionalRules",
                        $"Rule '{ruleId}' references unknown question '{referencedId}'.");
                }
            }

            dependencyMap[question.QuestionId] = referencedIds
                .Where(questionIds.Contains)
                .ToHashSet();
        }

        if (errors.Count > 0)
            throw new ValidationException(errors.ToDictionary(k => k.Key, v => v.Value.ToArray()));

        if (HasCycle(questionIds, dependencyMap))
            throw new ValidationException("Circular conditional rules detected.");
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var list))
        {
            list = new List<string>();
            errors[key] = list;
        }

        list.Add(message);
    }

    private static bool HasCycle(HashSet<Guid> nodes, Dictionary<Guid, HashSet<Guid>> edges)
    {
        var inDegree = nodes.ToDictionary(node => node, _ => 0);

        foreach (var dependencies in edges.Values)
        {
            foreach (var dependency in dependencies)
            {
                if (inDegree.ContainsKey(dependency))
                    inDegree[dependency]++;
            }
        }

        var queue = new Queue<Guid>(inDegree.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var visited = 0;

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            visited++;

            if (!edges.TryGetValue(node, out var neighbors))
                continue;

            foreach (var neighbor in neighbors)
            {
                if (!inDegree.ContainsKey(neighbor))
                    continue;

                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        return visited != nodes.Count;
    }
}
