using System.Security.Claims;
using AwesomeAssertions;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Vuetrack.Api.Features.Suggestions.Core;
using Vuetrack.Api.Features.Suggestions.Core.Contracts;
using Vuetrack.Api.Tests.Fakes;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Suggestions.Core;

public class SuggestionsControllerResultMappingTests
{
    [Fact]
    public async Task Update_WhenServiceReturnsValue_ReturnsOkWithSuggestion()
    {
        var contract = new SuggestionContract("id-1", "Title", null, null, null, DateTime.UnixEpoch, DateTime.UnixEpoch.AddMinutes(30), null, "Edited", [], 0.6);
        var controller = CreateController(new StubSuggestionService { OnUpdate = (_, _, _, _) => Task.FromResult<ErrorOr<SuggestionContract>>(contract) });

        var result = await controller.Update("id-1", UpdateContract(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(contract);
    }

    [Fact]
    public async Task Update_WhenServiceReturnsNotFound_ReturnsProblemDetailsWith404()
    {
        var controller = CreateController(new StubSuggestionService { OnUpdate = (_, _, _, _) => Task.FromResult<ErrorOr<SuggestionContract>>(Error.NotFound()) });

        var result = await controller.Update("missing", UpdateContract(), CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        objectResult.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task Dismiss_WhenServiceReturnsDeleted_ReturnsNoContent()
    {
        var controller = CreateController(new StubSuggestionService { OnDismiss = (_, _, _) => Task.FromResult<ErrorOr<Deleted>>(Result.Deleted) });

        var result = await controller.Dismiss("id-1", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Dismiss_WhenServiceReturnsNotFound_ReturnsProblemDetailsWith404()
    {
        var controller = CreateController(new StubSuggestionService { OnDismiss = (_, _, _) => Task.FromResult<ErrorOr<Deleted>>(Error.NotFound()) });

        var result = await controller.Dismiss("missing", CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        objectResult.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task Accept_WhenServiceReturnsSuccess_ReturnsNoContent()
    {
        var controller = CreateController(new StubSuggestionService { OnAccept = (_, _, _) => Task.FromResult<ErrorOr<Success>>(Result.Success) });

        var result = await controller.Accept("id-1", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Reload_ReturnsGenerationResult()
    {
        var expected = new GenerateSuggestionsResultContract(1, []);
        var controller = CreateController(new StubSuggestionService { OnReload = (_, _, _) => Task.FromResult(expected) });

        var result = await controller.Reload(new GenerateSuggestionsRequestContract { From = DateTime.UnixEpoch, To = DateTime.UnixEpoch.AddDays(1) }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expected);
    }

    private static SuggestionUpdateContract UpdateContract() => new()
    {
        Title = "Title",
        DateStarted = DateTime.UnixEpoch,
        DateEnded = DateTime.UnixEpoch.AddMinutes(30),
    };

    private static SuggestionsController CreateController(StubSuggestionService service)
    {
        var provider = new ServiceCollection()
            .AddLogging()
            .AddMvcCore()
            .AddApplicationPart(typeof(SuggestionsController).Assembly)
            .Services
            .AddProblemDetails()
            .BuildServiceProvider();

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "user-1")], "test"));
        var httpContext = new DefaultHttpContext { User = principal, RequestServices = provider };

        return new SuggestionsController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }
}
