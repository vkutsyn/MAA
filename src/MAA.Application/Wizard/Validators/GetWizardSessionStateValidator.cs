using FluentValidation;
using MAA.Application.Wizard.Queries;

namespace MAA.Application.Wizard.Validators;

/// <summary>
/// Validator for GetWizardSessionStateQuery.
/// </summary>
public class GetWizardSessionStateValidator : AbstractValidator<GetWizardSessionStateQuery>
{
    public GetWizardSessionStateValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("SessionId is required");
    }
}
