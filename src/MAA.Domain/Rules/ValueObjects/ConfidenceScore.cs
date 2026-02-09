namespace MAA.Domain.Rules.ValueObjects;

/// <summary>
/// Value Object: Confidence Score
/// Represents a confidence score (0-100) indicating how certain we are about an eligibility result
/// 
/// Phase 2 Implementation: T016
/// 
/// Usage:
/// - 95: High confidence (all factors verify, exact match)
/// - 75: Medium confidence (some factors pending verification)
/// - 50: Low confidence (borderline eligibility)
/// - 25: Very low confidence (possible but unlikely)
/// 
/// Properties:
/// - Immutable: Once created, cannot be changed
/// - Validated: Constructor enforces 0-100 range
/// - Comparable: Supports comparisons for sorting results
/// </summary>
public class ConfidenceScore : IComparable<ConfidenceScore>, IEquatable<ConfidenceScore>
{
    /// <summary>
    /// The numeric score value (0-100)
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Creates a new ConfidenceScore with validation
    /// </summary>
    /// <param name="value">Score value (must be 0-100)</param>
    /// <exception cref="ArgumentException">If value is outside 0-100 range</exception>
    public ConfidenceScore(int value)
    {
        if (value < 0 || value > 100)
        {
            throw new ArgumentException(
                $"Confidence score must be between 0 and 100, but received {value}.",
                nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets a human-readable category name for the score
    /// </summary>
    public string Category => Value switch
    {
        >= 90 => "Very High",
        >= 75 => "High",
        >= 50 => "Medium",
        >= 25 => "Low",
        _ => "Very Low"
    };

    /// <summary>
    /// Compares this score to another ConfidenceScore
    /// Returns: -1 if less, 0 if equal, 1 if greater
    /// </summary>
    public int CompareTo(ConfidenceScore? other)
    {
        if (other == null) return 1;
        return Value.CompareTo(other.Value);
    }

    /// <summary>
    /// Checks equality with another ConfidenceScore
    /// </summary>
    public bool Equals(ConfidenceScore? other)
    {
        return other != null && Value == other.Value;
    }

    /// <summary>
    /// Checks equality with any object
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is ConfidenceScore score && Equals(score);
    }

    /// <summary>
    /// Returns hash code for use in collections
    /// </summary>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// String representation (e.g., "85" or "85 (High)")
    /// </summary>
    public override string ToString()
    {
        return $"{Value} ({Category})";
    }

    /// <summary>
    /// Implicit conversion from int to ConfidenceScore
    /// Allows: ConfidenceScore score = 95;
    /// </summary>
    public static implicit operator ConfidenceScore(int value)
    {
        return new ConfidenceScore(value);
    }

    /// <summary>
    /// Implicit conversion from ConfidenceScore to int
    /// Allows: int score = confidenceScore;
    /// </summary>
    public static implicit operator int(ConfidenceScore score)
    {
        return score.Value;
    }

    /// <summary>
    /// Comparison operators for natural sorting
    /// </summary>
    public static bool operator <(ConfidenceScore? left, ConfidenceScore? right)
    {
        if (left == null || right == null) return false;
        return left.Value < right.Value;
    }

    public static bool operator >(ConfidenceScore? left, ConfidenceScore? right)
    {
        if (left == null || right == null) return false;
        return left.Value > right.Value;
    }

    public static bool operator <=(ConfidenceScore? left, ConfidenceScore? right)
    {
        if (left == null || right == null) return false;
        return left.Value <= right.Value;
    }

    public static bool operator >=(ConfidenceScore? left, ConfidenceScore? right)
    {
        if (left == null || right == null) return false;
        return left.Value >= right.Value;
    }

    public static bool operator ==(ConfidenceScore? left, ConfidenceScore? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ConfidenceScore? left, ConfidenceScore? right)
    {
        return !(left == right);
    }
}
