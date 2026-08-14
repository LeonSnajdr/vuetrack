using AwesomeAssertions;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Vuetrack.Api.Infrastructure.Validation;
using Xunit;

namespace Vuetrack.Api.Tests.Infrastructure.Validation;

public class ValidationErrorOrTests
{
    [Fact]
    public void ToActionResult_WhenValue_ReturnsOkWithValue()
    {
        var controller = CreateController();

        var result = controller.ToActionResult<string>("payload");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().Be("payload");
    }

    [Fact]
    public void ToActionResult_WhenSuccess_ReturnsNoContent()
    {
        var controller = CreateController();

        var result = controller.ToActionResult(Result.Success);

        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public void ToCreatedResult_WhenValue_Returns201WithBody()
    {
        var controller = CreateController();

        var result = controller.ToCreatedResult<string>("created-id");

        var objectResult = result.Should().BeOfType<ObjectResult>().Which;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        objectResult.Value.Should().Be("created-id");
    }

    [Fact]
    public void ToCreatedResult_WhenError_ReturnsProblem()
    {
        var controller = CreateController();

        var result = controller.ToCreatedResult<string>(Error.NotFound());

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ToActionResult_WhenDeleted_ReturnsNoContent()
    {
        var controller = CreateController();

        var result = controller.ToActionResult(Result.Deleted);

        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public void ToActionResult_WhenError_ReturnsProblemDetails()
    {
        var controller = CreateController();
        ErrorOr<Created> error = Error.Conflict(description: "Already exists");

        var result = controller.ToActionResult(error);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        objectResult.ContentTypes.Should().Contain("application/problem+json");

        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status409Conflict);
        problem.Detail.Should().Be("Already exists");
    }

    [Fact]
    public void ToActionResult_WhenValidationErrors_ReturnsFieldKeyedValidationProblem()
    {
        var controller = CreateController();
        List<Error> errors = [Error.Validation("projectId", "Required"), Error.Validation("activityId", "Required")];
        ErrorOr<Created> result = errors;

        var actionResult = controller.ToActionResult(result);

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        objectResult.ContentTypes.Should().Contain("application/problem+json");

        var problem = objectResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("projectId");
        problem.Errors["projectId"].Should().Contain("Required");
        problem.Errors.Should().ContainKey("activityId");
    }

    private static ControllerBase CreateController()
    {
        var provider = new ServiceCollection()
            .AddLogging()
            .AddMvcCore()
            .Services
            .AddProblemDetails()
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = provider };

        return new TestController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }

    private sealed class TestController : ControllerBase;
}
