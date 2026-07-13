using System.Security.Claims;
using AwesomeAssertions;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Vuetrack.Api.Features.TimeEntry;
using Vuetrack.Api.Tests.Fakes;
using Vuetrack.Backends.Abstractions.Contracts;
using Xunit;

namespace Vuetrack.Api.Tests.Features.TimeEntry;

public class ErrorOrResultMappingTests
{
    [Fact]
    public async Task List_WhenServiceReturnsConflict_ReturnsProblemDetailsWithStatusAndDetail()
    {
        var error = Error.Conflict(code: "Backend.NotConnected", description: "The backend is not connected");
        var controller = CreateController(new StubTimeEntryService { ListResult = error });

        var result = await controller.List(DateTime.UnixEpoch, DateTime.UnixEpoch.AddHours(1), CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        objectResult.ContentTypes.Should().Contain("application/problem+json");

        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status409Conflict);
        problem.Detail.Should().Be("The backend is not connected");
    }

    [Fact]
    public async Task Create_WhenServiceReturnsValidationErrors_ReturnsValidationProblemDetails()
    {
        var errors = new List<Error>
        {
            Error.Validation(code: "ProjectId", description: "Project is required"),
            Error.Validation(code: "ActivityId", description: "Activity is required"),
        };
        var controller = CreateController(new StubTimeEntryService { CreateResult = errors });

        var result = await controller.Create(new TimeEntryCreateContract(), CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        objectResult.ContentTypes.Should().Contain("application/problem+json");

        var problem = objectResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("ProjectId");
        problem.Errors.Should().ContainKey("ActivityId");
        problem.Errors["ProjectId"].Should().Contain("Project is required");
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsNotFound_ReturnsProblemDetailsWith404()
    {
        var controller = CreateController(new StubTimeEntryService { DeleteResult = Error.NotFound(description: "Time entry not found") });

        var result = await controller.Delete("missing", CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        objectResult.ContentTypes.Should().Contain("application/problem+json");

        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Detail.Should().Be("Time entry not found");
    }

    private static TimeEntryController CreateController(StubTimeEntryService service)
    {
        var provider = new ServiceCollection()
            .AddLogging()
            .AddMvcCore()
            .AddApplicationPart(typeof(TimeEntryController).Assembly)
            .Services
            .AddProblemDetails()
            .BuildServiceProvider();

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "user-1")], "test"));
        var httpContext = new DefaultHttpContext { User = principal, RequestServices = provider };

        return new TimeEntryController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }
}
