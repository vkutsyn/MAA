using System.Text.RegularExpressions;

namespace MAA.Domain.Eligibility;

/// <summary>
/// Validates explanation text for readability and presence of jargon,
/// ensuring plain-language communication standards.
/// </summary>
public class ExplanationReadability
{
    private static readonly HashSet<string> JargonTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "algorithm",
        "regex",
        "JSONLogic",
        "MAGI",
        "FPIG",
        "AMI",
        "FPL",
        "XML",
        "JSON",
        "API",
        "payload",
        "schema",
        "normalized",
        "serialized",
        "deterministic",
        "cardinality",
        "relational",
        "denormalized"
    };

    /// <summary>
    /// Checks if an explanation item uses accessible language.
    /// </summary>
    /// <param name="item">The explanation item to validate.</param>
    /// <returns>Validation result indicating readability issues, if any.</returns>
    public ReadabilityValidation Validate(ExplanationItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        var result = new ReadabilityValidation { IsReadable = true };

        // Check for jargon
        var jargonTermsFound = FindJargon(item.Message);
        if (jargonTermsFound.Count > 0)
        {
            result.IsReadable = false;
            result.JargonTermsFound = jargonTermsFound;
            result.IssueType = ReadabilityIssueType.JargonDetected;
        }

        // Check readability metrics
        var metrics = CalculateReadabilityMetrics(item.Message);
        result.AvergaeWordLength = metrics.AverageWordLength;
        result.AverageSentenceLength = metrics.AverageSentenceLength;

        if (metrics.AverageSentenceLength > 20)
        {
            result.IsReadable = false;
            result.IssueType = ReadabilityIssueType.ComplexSentenceStructure;
        }

        if (metrics.AverageWordLength > 6)
        {
            result.IsReadable = false;
            result.IssueType = ReadabilityIssueType.ComplexVocabulary;
        }

        return result;
    }

    /// <summary>
    /// Checks if any explanation items contain accessibility issues.
    /// </summary>
    public List<ReadabilityValidation> ValidateAll(IEnumerable<ExplanationItem> items)
    {
        return items
            .Select(Validate)
            .Where(v => !v.IsReadable)
            .ToList();
    }

    /// <summary>
    /// Finds jargon terms in the given text.
    /// </summary>
    private List<string> FindJargon(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var words = text.Split([' ', '.', ',', '!', '?', ';', ':', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        return words
            .Where(w => JargonTerms.Contains(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Calculates readability metrics for the given text.
    /// </summary>
    private ReadabilityMetrics CalculateReadabilityMetrics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ReadabilityMetrics();

        var words = text.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        var sentences = Regex.Split(text, @"[.!?]+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        var totalWordLength = words.Sum(w => w.Length);
        var averageWordLength = words.Length > 0 ? (double)totalWordLength / words.Length : 0;
        var averageSentenceLength = sentences.Count > 0 ? (double)words.Length / sentences.Count : 0;

        return new ReadabilityMetrics
        {
            AverageWordLength = averageWordLength,
            AverageSentenceLength = averageSentenceLength,
            WordCount = words.Length,
            SentenceCount = sentences.Count
        };
    }
}

/// <summary>
/// Represents readability validation results for an explanation item.
/// </summary>
public class ReadabilityValidation
{
    /// <summary>
    /// Indicates if the explanation meets readability standards.
    /// </summary>
    public bool IsReadable { get; set; } = true;

    /// <summary>
    /// Type of readability issue found, if any.
    /// </summary>
    public ReadabilityIssueType IssueType { get; set; }

    /// <summary>
    /// Jargon terms found in the explanation.
    /// </summary>
    public List<string> JargonTermsFound { get; set; } = new();

    /// <summary>
    /// Average word length in the explanation.
    /// </summary>
    public double AvergaeWordLength { get; set; }

    /// <summary>
    /// Average sentence length in the explanation.
    /// </summary>
    public double AverageSentenceLength { get; set; }
}

/// <summary>
/// Types of readability issues that may be detected.
/// </summary>
public enum ReadabilityIssueType
{
    /// <summary>
    /// No readability issues detected.
    /// </summary>
    None,

    /// <summary>
    /// Technical jargon was detected in the explanation.
    /// </summary>
    JargonDetected,

    /// <summary>
    /// Sentences are too complex or lengthy.
    /// </summary>
    ComplexSentenceStructure,

    /// <summary>
    /// Vocabulary is too advanced or technical.
    /// </summary>
    ComplexVocabulary
}

/// <summary>
/// Metrics for readability analysis.
/// </summary>
public class ReadabilityMetrics
{
    public double AverageWordLength { get; set; }
    public double AverageSentenceLength { get; set; }
    public int WordCount { get; set; }
    public int SentenceCount { get; set; }
}
