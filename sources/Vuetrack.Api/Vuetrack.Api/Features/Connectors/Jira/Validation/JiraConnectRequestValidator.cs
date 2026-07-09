using FluentValidation;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Connectors.Jira.Contracts;

namespace Vuetrack.Api.Features.Connectors.Jira.Validation;

[InjectAs(typeof(IValidator<JiraConnectRequest>))]
public class JiraConnectRequestValidator : AbstractValidator<JiraConnectRequest>
{
    public JiraConnectRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.State).NotEmpty();

        RuleFor(x => x.RedirectUri)
            .NotEmpty()
            .Must(BeAnAbsoluteUri)
            .WithMessage("RedirectUri must be a valid absolute URI.");
    }

    private static bool BeAnAbsoluteUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out _);
}
