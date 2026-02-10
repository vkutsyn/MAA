using FluentValidation;
using MAA.Application.StateContext.DTOs;
using MAA.Domain.StateContext;

namespace MAA.Application.StateContext.Validators;

/// <summary>
/// Validator for UpdateStateContextRequest
/// </summary>
public class UpdateStateContextRequestValidator : AbstractValidator<UpdateStateContextRequest>
{
    public UpdateStateContextRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("SessionId is required");

        RuleFor(x => x.StateCode)
            .NotEmpty()
            .WithMessage("State code is required")
            .Length(2)
            .WithMessage("State code must be 2 characters")
            .Matches("^[A-Z]{2}$")
            .WithMessage("State code must be 2 uppercase letters");

        RuleFor(x => x.ZipCode)
            .NotEmpty()
            .WithMessage("ZIP code is required")
            .Must(ZipCodeValidator.IsValid)
            .WithMessage("ZIP code must be exactly 5 digits");
    }
}
