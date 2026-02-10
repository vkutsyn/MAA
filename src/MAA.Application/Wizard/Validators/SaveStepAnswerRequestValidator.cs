using FluentValidation;
using MAA.Application.Wizard.DTOs;

namespace MAA.Application.Wizard.Validators;

/// <summary>
/// Validator for SaveStepAnswerRequest.
/// </summary>
public class SaveStepAnswerRequestValidator : AbstractValidator<SaveStepAnswerRequest>
{
    public SaveStepAnswerRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("SessionId is required");

        RuleFor(x => x.StepId)
            .NotEmpty()
            .WithMessage("StepId is required");

        RuleFor(x => x.SchemaVersion)
            .NotEmpty()
            .WithMessage("SchemaVersion is required");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required")
            .Must(status => status == "draft" || status == "submitted")
            .WithMessage("Status must be 'draft' or 'submitted'");

        RuleFor(x => x.AnswerData)
            .NotNull()
            .WithMessage("AnswerData is required");
    }
}
