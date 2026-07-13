using FluentValidation;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Backends.Timetracking.Contracts;

namespace Vuetrack.Api.Features.Backends.Timetracking.Validation;

[InjectAs(typeof(IValidator<TimetrackingConnectCreateContract>))]
public class TimetrackingConnectCreateContractValidator : AbstractValidator<TimetrackingConnectCreateContract>
{
    public TimetrackingConnectCreateContractValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.RedirectUri)
            .NotEmpty()
            .Must(BeAnAbsoluteUri)
            .WithMessage("RedirectUri must be a valid absolute URI.");
    }

    private static bool BeAnAbsoluteUri(string value) => Uri.TryCreate(value, UriKind.Absolute, out _);
}
