using FluentValidation;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Backends.Abstractions.Contracts;

namespace Vuetrack.Api.Features.TimeEntry.Validation;

[InjectAs(typeof(IValidator<TimeEntryUpdateContract>))]
public class TimeEntryUpdateContractValidator : AbstractValidator<TimeEntryUpdateContract>
{
    public TimeEntryUpdateContractValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ActivityId).NotEmpty();
        RuleFor(x => x.DateStarted).LessThan(x => x.DateEnded);
    }
}
