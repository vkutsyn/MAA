using FluentValidation;
using MAA.Application.StateContext.DTOs;
using MAA.Domain.StateContext;

namespace MAA.Application.StateContext.Validators;

/// <summary>
/// Validator for InitializeStateContextRequest
/// </summary>
public class InitializeStateContextRequestValidator : AbstractValidator<InitializeStateContextRequest>
{
    public InitializeStateContextRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("SessionId is required");

        RuleFor(x => x.ZipCode)
            .NotEmpty()
            .WithMessage("ZIP code is required")
            .Must(ZipCodeValidator.IsValid)
            .WithMessage("ZIP code must be exactly 5 digits");

        RuleFor(x => x.StateCodeOverride)
            .Length(2)
            .When(x => !string.IsNullOrEmpty(x.StateCodeOverride))
            .WithMessage("State code must be 2 characters")
            .Matches("^[A-Z]{2}$")
            .When(x => !string.IsNullOrEmpty(x.StateCodeOverride))
            .WithMessage("State code must be 2 uppercase letters");
    }
}
