using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Backends;
using Vuetrack.Api.Features.Project.Services;
using Vuetrack.Api.Infrastructure.Authentication;
using Vuetrack.Api.Infrastructure.Validation;

namespace Vuetrack.Api.Features.Project;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/project")]
[Authorize]
public class ProjectController(IProjectService projectService) : ControllerBase
{
    private IProjectService ProjectService { get; } = projectService;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await ProjectService.ListAsync(userId, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("{projectId}/activity")]
    public async Task<IActionResult> Activities(string projectId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await ProjectService.ListActivitiesAsync(userId, projectId, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("findByTaskId")]
    public async Task<IActionResult> FindByTaskId([FromQuery] string taskId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await ProjectService.FindByTaskIdAsync(userId, taskId, cancellationToken);

        return this.ToActionResult(result);
    }
}
