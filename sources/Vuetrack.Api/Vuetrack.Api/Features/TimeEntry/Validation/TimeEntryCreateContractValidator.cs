using FluentValidation;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations.Contracts;
using Vuetrack.Api.Features.TimeEntry.Contracts;
using Vuetrack.Api.Infrastructure.Validation;

namespace Vuetrack.Api.Features.TimeEntry.Validation;

[InjectAs(typeof(IValidator<TimeEntryCreateContract>))]
public class TimeEntryCreateContractValidator : AbstractValidator<TimeEntryCreateContract>
{
    public TimeEntryCreateContractValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ActivityId).NotEmpty();
        RuleFor(x => x.DateStarted).LessThan(x => x.DateEnded).WithErrorCode(nameof(ValidationError.DateOrder));
    }
}
