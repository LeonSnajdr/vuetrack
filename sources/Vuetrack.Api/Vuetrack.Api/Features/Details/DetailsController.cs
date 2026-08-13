using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Details.Contracts;
using Vuetrack.Api.Features.Details.Services;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Infrastructure.Authentication;
using Vuetrack.Api.Infrastructure.Validation;

namespace Vuetrack.Api.Features.Details;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/details")]
[Authorize(Roles = "User")]
public class DetailsController(IDetailService detailService) : ControllerBase
{
    private IDetailService DetailService { get; } = detailService;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? taskId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var query = new DetailQuery { TaskId = taskId };
        var result = await DetailService.GetAsync(query, userId, cancellationToken);

        return this.ToActionResult(result);
    }
}
