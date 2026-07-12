using FluentValidation;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Suggestions.Core.Contracts;

namespace Vuetrack.Api.Features.Suggestions.Core.Validation;

[InjectAs(typeof(IValidator<GenerateSuggestionsRequestContract>))]
public class GenerateSuggestionsRequestContractValidator : AbstractValidator<GenerateSuggestionsRequestContract>
{
    private static readonly TimeSpan MaxRange = TimeSpan.FromDays(31);

    private static readonly TimeSpan MaxFuture = TimeSpan.FromDays(1);

    public GenerateSuggestionsRequestContractValidator()
    {
        RuleFor(x => x.From)
            .LessThan(x => x.To)
            .WithMessage("From must be before To.");

        RuleFor(x => x)
            .Must(x => x.To - x.From <= MaxRange)
            .WithMessage($"Range must not exceed {MaxRange.TotalDays:0} days.");

        RuleFor(x => x.To)
            .Must(to => to <= DateTimeOffset.UtcNow + MaxFuture)
            .WithMessage("To must not be far in the future.");
    }
}
