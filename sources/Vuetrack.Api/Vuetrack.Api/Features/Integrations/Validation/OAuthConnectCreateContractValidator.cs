using FluentValidation;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Contracts;

namespace Vuetrack.Api.Features.Integrations.Validation;

[InjectAs(typeof(IValidator<OAuthConnectCreateContract>))]
public class OAuthConnectCreateContractValidator : AbstractValidator<OAuthConnectCreateContract>
{
    public OAuthConnectCreateContractValidator()
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
