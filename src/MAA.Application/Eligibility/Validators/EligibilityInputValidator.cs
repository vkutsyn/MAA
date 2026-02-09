using FluentValidation;
using FluentValidation.Results;
using MAA.Application.Eligibility.DTOs;

namespace MAA.Application.Eligibility.Validators;

/// <summary>
/// FluentValidation validator for User Eligibility Input
/// 
/// Phase 3 Implementation: T022
/// 
/// Responsibilities:
/// - Validate input DTO before passing to handler
/// - Enforce business rules (state codes, household size ranges, etc.)
/// - Provide clear error messages for UI/API responses
/// - Early validation prevents invalid data from reaching domain logic
/// 
/// Validation Rules:
/// - StateCode: Required, one of [IL, CA, NY, TX, FL]
/// - HouseholdSize: Required, integer 1-50
/// - MonthlyIncomeCents: Required, non-negative
/// - Age: Optional, if provided must be 0-125
/// - IsCitizen: Required, boolean
/// - All other fields: Optional, types validated by model binding
/// 
/// Integration:
/// - Called by ASP.NET pipeline before handler receives request
/// - Configured in Startup.cs via AddValidatorsFromAssembly
/// - Returns 400 Bad Request with validation errors if fails
/// 
/// Design:
/// - Inherits from AbstractValidator<T> provided by FluentValidation
/// - Rules configured in constructor
/// - Reusable across controllers and tests
/// </summary>
public class EligibilityInputValidator : AbstractValidator<UserEligibilityInputDto>
{
    private static readonly string[] ValidStateCodes = { "IL", "CA", "NY", "TX", "FL" };

    public EligibilityInputValidator()
    {
        RuleFor(x => x.StateCode)
            .NotEmpty()
            .WithMessage("State code is required")
            .Must(code => ValidStateCodes.Contains(code.ToUpperInvariant()))
            .WithMessage($"State code must be one of: {string.Join(", ", ValidStateCodes)}")
            .Length(2, 2)
            .WithMessage("State code must be exactly 2 characters");

        RuleFor(x => x.HouseholdSize)
            .NotEmpty()
            .WithMessage("Household size is required")
            .InclusiveBetween(1, 50)
            .WithMessage("Household size must be between 1 and 50 persons");

        RuleFor(x => x.MonthlyIncomeCents)
            .NotEmpty()
            .WithMessage("Monthly income is required")
            .GreaterThanOrEqualTo(0)
            .WithMessage("Monthly income cannot be negative");

        RuleFor(x => x.IsCitizen)
            .NotEmpty()
            .WithMessage("Citizenship status is required");

        RuleFor(x => x.AssetsCents)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AssetsCents.HasValue)
            .WithMessage("Assets cannot be negative if provided");
    }

    /// <summary>
    /// Validates a complete eligibility input
    /// Called before handler processes request
    /// </summary>
    public ValidationResult ValidateEligibilityInput(UserEligibilityInputDto input)
    {
        return Validate(input);
    }
}

/// <summary>
/// Async validation extension methods
/// </summary>
public static class EligibilityInputValidatorExtensions
{
    /// <summary>
    /// Validates input asynchronously
    /// </summary>
    public static async Task<ValidationResult> ValidateEligibilityInputAsync(
        this AbstractValidator<UserEligibilityInputDto> validator,
        UserEligibilityInputDto input)
    {
        return await validator.ValidateAsync(input);
    }

    /// <summary>
    /// Validates input and throws if invalid
    /// </summary>
    public static void ValidateEligibilityInputAndThrow(
        this AbstractValidator<UserEligibilityInputDto> validator,
        UserEligibilityInputDto input)
    {
        var result = validator.Validate(input);
        if (!result.IsValid)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException(errors);
        }
    }

    /// <summary>
    /// Validates input asynchronously and throws if invalid
    /// </summary>
    public static async Task ValidateEligibilityInputAndThrowAsync(
        this AbstractValidator<UserEligibilityInputDto> validator,
        UserEligibilityInputDto input)
    {
        var result = await validator.ValidateAsync(input);
        if (!result.IsValid)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException(errors);
        }
    }
}
