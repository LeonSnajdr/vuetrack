using FluentValidation;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Suggestions.Contracts;

namespace Vuetrack.Api.Features.Suggestions.Validation;

[InjectAs(typeof(IValidator<SuggestionUpdateContract>))]
public class SuggestionUpdateContractValidator : AbstractValidator<SuggestionUpdateContract>
{
    public SuggestionUpdateContractValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Start).LessThan(x => x.End).WithMessage("Start must be before End.");
    }
}
