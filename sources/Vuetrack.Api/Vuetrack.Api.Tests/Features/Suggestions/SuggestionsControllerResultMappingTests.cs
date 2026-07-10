using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Suggestions;
using Vuetrack.Api.Features.Suggestions.Contracts;
using Vuetrack.Api.Tests.Fakes;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Suggestions;

public class SuggestionsControllerResultMappingTests
{
    [Fact]
    public async Task Update_WhenServiceReturnsUpdated_ReturnsOkWithSuggestion()
    {
        var contract = new SuggestionContract("id-1", "Title", null, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMinutes(30), "Edited", [], 0.6, null, null);
        var controller = CreateController(new StubSuggestionService { OnUpdate = (_, _, _, _) => Task.FromResult<SuggestionUpdateResult>(new SuggestionUpdated(contract)) });

        var result = await controller.Update("id-1", UpdateContract(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(contract);
    }

    [Fact]
    public async Task Update_WhenServiceReturnsNotFound_ReturnsNotFound()
    {
        var controller = CreateController(new StubSuggestionService { OnUpdate = (_, _, _, _) => Task.FromResult<SuggestionUpdateResult>(new SuggestionNotFound()) });

        var result = await controller.Update("missing", UpdateContract(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Dismiss_WhenServiceReturnsDismissed_ReturnsNoContent()
    {
        var controller = CreateController(new StubSuggestionService { OnDismiss = (_, _, _) => Task.FromResult<SuggestionDismissResult>(new SuggestionDismissed()) });

        var result = await controller.Dismiss("id-1", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Dismiss_WhenServiceReturnsNotFound_ReturnsNotFound()
    {
        var controller = CreateController(new StubSuggestionService { OnDismiss = (_, _, _) => Task.FromResult<SuggestionDismissResult>(new SuggestionDismissNotFound()) });

        var result = await controller.Dismiss("missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    private static SuggestionUpdateContract UpdateContract() => new()
    {
        Title = "Title",
        Start = DateTimeOffset.UnixEpoch,
        End = DateTimeOffset.UnixEpoch.AddMinutes(30),
    };

    private static SuggestionsController CreateController(StubSuggestionService service)
    {
        var controller = new SuggestionsController(service);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")]));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };
        return controller;
    }
}
