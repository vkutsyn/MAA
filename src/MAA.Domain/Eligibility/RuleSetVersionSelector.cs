namespace MAA.Domain.Eligibility;

/// <summary>
/// Selects the most recent rule version effective on or before a given date.
/// Ensures deterministic rule application based on effective dates.
/// </summary>
public class RuleSetVersionSelector
{
    /// <summary>
    /// Selects the most recent rule version that is effective on or before the request date.
    /// </summary>
    /// <param name="versions">Available rule versions, ordered by effective date.</param>
    /// <param name="requestDate">The effective date for which to select the rule version.</param>
    /// <returns>The selected RuleSetVersion, or null if no version is effective for the date.</returns>
    /// <exception cref="ArgumentNullException">Thrown when versions is null.</exception>
    public RuleSetVersion? SelectRuleSetVersion(
        IEnumerable<RuleSetVersion> versions,
        DateTime requestDate)
    {
        if (versions == null)
            throw new ArgumentNullException(nameof(versions));

        return versions
            .Where(v => v.EffectiveDate <= requestDate &&
                       (v.EndDate == null || v.EndDate >= requestDate))
            .OrderByDescending(v => v.EffectiveDate)
            .FirstOrDefault();
    }

    /// <summary>
    /// Validates that a rule version is active and effective for the given date.
    /// </summary>
    /// <param name="version">The rule version to validate.</param>
    /// <param name="requestDate">The effective date to check.</param>
    /// <returns>True if the version is active and effective; false otherwise.</returns>
    public bool IsEffectiveForDate(RuleSetVersion version, DateTime requestDate)
    {
        if (version == null)
            return false;

        return version.EffectiveDate <= requestDate &&
               (version.EndDate == null || version.EndDate >= requestDate) &&
               version.Status == RuleSetStatus.Active;
    }
}
