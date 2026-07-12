using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Suggestions.Core.Contracts;
using Vuetrack.Api.Features.Suggestions.Core.Services;
using Vuetrack.Api.Infrastructure.Authentication;

namespace Vuetrack.Api.Features.Suggestions.Core;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/suggestions")]
[Authorize]
public class SuggestionsController(ISuggestionService suggestionService) : ControllerBase
{
    private ISuggestionService SuggestionService { get; } = suggestionService;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        return Ok(await SuggestionService.ListAsync(userId, from, to));
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateSuggestionsRequestContract request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        return Ok(await SuggestionService.GenerateAsync(userId, request, cancellationToken));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] SuggestionUpdateContract request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await SuggestionService.UpdateAsync(userId, id, request, cancellationToken);

        return result switch
        {
            SuggestionUpdated updated => Ok(updated.Suggestion),
            SuggestionNotFound => NotFound(),
            _ => StatusCode(500),
        };
    }

    [HttpPost("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(string id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await SuggestionService.DismissAsync(userId, id, cancellationToken);

        return result switch
        {
            SuggestionDismissed => NoContent(),
            SuggestionDismissNotFound => NotFound(),
            _ => StatusCode(500),
        };
    }
}
