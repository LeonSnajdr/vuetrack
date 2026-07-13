using AwesomeAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vuetrack.Api.Features.TimeEntry.Validation;
using Vuetrack.Api.Infrastructure.Validation;
using Vuetrack.Backends.Abstractions.Contracts;
using Xunit;

namespace Vuetrack.Api.Tests.Infrastructure.Validation;

public class ValidationActionFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_WhenArgumentInvalid_ShortCircuitsWithValidationProblemDetails()
    {
        var provider = BuildProvider();
        var filter = new ValidationActionFilter(provider);

        // Empty contract fails ProjectId/ActivityId NotEmpty and DateStarted < DateEnded.
        var invalidContract = new TimeEntryCreateContract();
        var executingContext = CreateContext(provider, argument: invalidContract);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(executingContext, () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateExecutedContext(executingContext));
        });

        nextCalled.Should().BeFalse();

        var objectResult = executingContext.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        objectResult.ContentTypes.Should().Contain("application/problem+json");

        var problem = objectResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Errors.Should().ContainKey(nameof(TimeEntryCreateContract.ProjectId));
        problem.Errors.Should().ContainKey(nameof(TimeEntryCreateContract.ActivityId));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenArgumentValid_CallsNext()
    {
        var provider = BuildProvider();
        var filter = new ValidationActionFilter(provider);

        var validContract = new TimeEntryCreateContract
        {
            ProjectId = "p1",
            ActivityId = "a1",
            DateStarted = DateTime.UnixEpoch,
            DateEnded = DateTime.UnixEpoch.AddHours(1),
        };
        var executingContext = CreateContext(provider, argument: validContract);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(executingContext, () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateExecutedContext(executingContext));
        });

        nextCalled.Should().BeTrue();
        executingContext.Result.Should().BeNull();
    }

    private static ServiceProvider BuildProvider() =>
        new ServiceCollection()
            .AddLogging()
            .AddMvcCore()
            .Services
            .AddProblemDetails()
            .AddScoped<IValidator<TimeEntryCreateContract>, TimeEntryCreateContractValidator>()
            .BuildServiceProvider();

    private static ActionExecutingContext CreateContext(IServiceProvider provider, object argument)
    {
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["contract"] = argument },
            controller: new object());
    }

    private static ActionExecutedContext CreateExecutedContext(ActionExecutingContext executingContext) =>
        new(executingContext, executingContext.Filters, executingContext.Controller);
}
