using FluentValidation;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Suggestions.Core.Contracts;

namespace Vuetrack.Api.Features.Suggestions.Core.Validation;

[InjectAs(typeof(IValidator<SuggestionUpdateContract>))]
public class SuggestionUpdateContractValidator : AbstractValidator<SuggestionUpdateContract>
{
    public SuggestionUpdateContractValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Comment).MaximumLength(2000);
        RuleFor(x => x.DateStarted).LessThan(x => x.DateEnded).WithMessage("Start must be before End.");
    }
}
