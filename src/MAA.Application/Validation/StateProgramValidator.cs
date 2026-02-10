using System.Text.RegularExpressions;
using MAA.Application.Eligibility.Repositories;
using MAA.Application.StateContext;
using MAA.Domain.Exceptions;

namespace MAA.Application.Validation;

/// <summary>
/// Validates state and program codes against format and reference data.
/// </summary>
public class StateProgramValidator
{
    private static readonly Regex StateCodeRegex = new("^[A-Za-z]{2}$", RegexOptions.Compiled);
    private static readonly Regex ProgramCodeRegex = new("^[A-Za-z0-9_-]{3,50}$", RegexOptions.Compiled);

    private readonly IStateConfigurationRepository _stateConfigurationRepository;
    private readonly IMedicaidProgramRepository _medicaidProgramRepository;

    public StateProgramValidator(
        IStateConfigurationRepository stateConfigurationRepository,
        IMedicaidProgramRepository medicaidProgramRepository)
    {
        _stateConfigurationRepository = stateConfigurationRepository ?? throw new ArgumentNullException(nameof(stateConfigurationRepository));
        _medicaidProgramRepository = medicaidProgramRepository ?? throw new ArgumentNullException(nameof(medicaidProgramRepository));
    }

    public async Task<(string StateCode, string ProgramCode)> ValidateAsync(
        string stateCode,
        string programCode,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(stateCode))
            errors["stateCode"] = new[] { "State code is required." };
        else if (!StateCodeRegex.IsMatch(stateCode))
            errors["stateCode"] = new[] { "State code must be a 2-letter abbreviation." };

        if (string.IsNullOrWhiteSpace(programCode))
            errors["programCode"] = new[] { "Program code is required." };
        else if (!ProgramCodeRegex.IsMatch(programCode))
            errors["programCode"] = new[] { "Program code must be 3-50 characters (A-Z, 0-9, underscore, dash)." };

        if (errors.Count > 0)
            throw new ValidationException(errors);

        var normalizedState = stateCode.ToUpperInvariant();
        var normalizedProgram = programCode.ToUpperInvariant();

        var stateExists = await _stateConfigurationRepository.ExistsAsync(normalizedState);
        if (!stateExists)
            errors["stateCode"] = new[] { $"State code '{stateCode}' is not supported." };

        var programs = await _medicaidProgramRepository.GetByStateAsync(normalizedState);
        var programExists = programs.Any(p =>
            string.Equals(p.ProgramCode, normalizedProgram, StringComparison.OrdinalIgnoreCase));

        if (!programExists)
            errors["programCode"] = new[] { $"Program code '{programCode}' is not supported for state '{normalizedState}'." };

        if (errors.Count > 0)
            throw new ValidationException(errors);

        return (normalizedState, normalizedProgram);
    }
}
