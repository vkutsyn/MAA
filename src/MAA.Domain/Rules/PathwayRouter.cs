namespace MAA.Domain.Rules;

/// <summary>
/// Pure function to route applicable programs based on user's eligible pathways.
/// Takes a list of pathways and returns programs matching those pathways for a state.
/// No I/O, no database access - stateless logic.
/// 
/// Used by evaluation handlers to filter programs before evaluating eligibility rules.
/// Example: If user qualifies for Aged pathway, only show Aged Medicaid programs.
/// Example: If user qualifies for [MAGI, Pregnancy-Related], show all MAGI + Pregnancy programs.
/// </summary>
public class PathwayRouter
{
    /// <summary>
    /// Filters programs based on user's applicable pathways.
    /// Returns only programs where eligibility_pathway matches at least one user pathway.
    /// </summary>
    /// <param name="applicablePathways">List of pathways user qualifies for (from PathwayIdentifier)</param>
    /// <param name="allPrograms">Available programs for the state (from database query)</param>
    /// <returns>Programs matching at least one applicable pathway; empty if none match</returns>
    /// <exception cref="ArgumentNullException">If applicablePathways or allPrograms is null</exception>
    public List<MedicaidProgram> RouteToProgramsForPathways(
        List<EligibilityPathway> applicablePathways,
        List<MedicaidProgram> allPrograms)
    {
        if (applicablePathways == null)
            throw new ArgumentNullException(nameof(applicablePathways), "Applicable pathways cannot be null");

        if (allPrograms == null)
            throw new ArgumentNullException(nameof(allPrograms), "All programs cannot be null");

        // Empty pathways list → no programs apply
        if (!applicablePathways.Any())
            return new List<MedicaidProgram>();

        // Return programs where eligibility_pathway is in applicablePathways
        var routedPrograms = allPrograms
            .Where(program => applicablePathways.Contains(program.EligibilityPathway))
            .OrderBy(program => program.ProgramName)  // Deterministic sort
            .ToList();

        return routedPrograms;
    }

    /// <summary>
    /// Convenience method: Route to programs for a single pathway.
    /// </summary>
    public List<MedicaidProgram> RouteToProgramsForPathway(
        EligibilityPathway pathway,
        List<MedicaidProgram> allPrograms)
    {
        var singlePathway = new List<EligibilityPathway> { pathway };
        return RouteToProgramsForPathways(singlePathway, allPrograms);
    }

    /// <summary>
    /// Counts how many programs are available for given pathways.
    /// Useful for validation and logging.
    /// </summary>
    public int CountAvailableProgramsForPathways(
        List<EligibilityPathway> applicablePathways,
        List<MedicaidProgram> allPrograms)
    {
        if (applicablePathways == null || allPrograms == null)
            return 0;

        return allPrograms
            .Count(program => applicablePathways.Contains(program.EligibilityPathway));
    }

    /// <summary>
    /// Checks if user has any programs available in their pathways.
    /// Returns false if pathways or programs don't intersect.
    /// </summary>
    public bool HasAvailableProgramsForPathways(
        List<EligibilityPathway> applicablePathways,
        List<MedicaidProgram> allPrograms)
    {
        return CountAvailableProgramsForPathways(applicablePathways, allPrograms) > 0;
    }
}
