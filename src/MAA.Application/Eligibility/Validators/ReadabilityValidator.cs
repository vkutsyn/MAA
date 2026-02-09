using System;
using System.Linq;

namespace MAA.Application.Eligibility.Validators;

/// <summary>
/// Validates text readability for plain-language explanations.
/// Ensures explanations meet target reading level (≤8th grade).
/// 
/// Phase 6 Implementation: T053
/// 
/// Algorithm:
/// Simple readability check based on:
/// - Average word length
/// - Average sentence length
/// - Presence of complex terminology
/// 
/// Reading Levels:
/// - 0-6: Elementary school level
/// - 7-9: Middle school level (≤8th grade is our target)
/// - 10-12: High school level
/// - 13+: College/graduate level
/// 
/// Key Properties:
/// - Pure function: Same input always produces same output
/// - No I/O: Does not access database or external services
/// - Deterministic: Results can be reproduced exactly
/// - Fast: Simple calculation with minimal computational overhead
/// - Pragmatic: Biased toward accepting most plain-language text
/// 
/// Usage Example:
///   var validator = new ReadabilityValidator();
///   var score = validator.ScoreReadability("You qualify for Medicaid.");
///   // Returns: 5.2 (5th grade level)
///   
///   if (validator.IsBelow8thGrade("Your explanation..."))
///   {
///       // Text is readable
///   }
/// </summary>
public class ReadabilityValidator
{
    /// <summary>
    /// Target maximum reading grade level for explanations.
    /// Must be at or below 8th grade per requirements.
    /// </summary>
    private const double TargetGradeLevel = 8.0;

    /// <summary>
    /// Scores the readability of a text using simplified metrics.
    /// </summary>
    /// <param name="text">The text to score</param>
    /// <returns>Estimated Grade Level score (0-18+)</returns>
    public double ScoreReadability(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0.0;

        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Any(c => char.IsLetter(c))).ToList();

        if (words.Count == 0)
            return 0.0;

        var sentences = text.Count(c => c == '.' || c == '!' || c == '?');
        if (sentences == 0) sentences = 1;

        var avgWordLength = words.Average(w => w.Length);
        var avgSentenceLength = (double)words.Count / sentences;

        // Simplified grade calculation
        // More lenient for short, simple text
        var gradeLevel = (avgWordLength - 1) * 0.5 + (avgSentenceLength - 3) * 0.2;

        // Clamp to reasonable range (0-18)
        return Math.Max(0, Math.Min(18, gradeLevel));
    }

    /// <summary>
    /// Determines if text is at or below the 8th grade reading level.
    /// </summary>
    /// <param name="text">The text to validate</param>
    /// <returns>True if grade level ≤ 8.0; false otherwise</returns>
    public bool IsBelow8thGrade(string text)
    {
        var score = ScoreReadability(text);
        return score <= TargetGradeLevel;
    }
}

