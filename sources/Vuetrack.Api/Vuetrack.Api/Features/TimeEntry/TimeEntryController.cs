using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Backends;
using Vuetrack.Api.Features.TimeEntry.Services;
using Vuetrack.Api.Infrastructure.Authentication;
using Vuetrack.Api.Infrastructure.Validation;
using Vuetrack.Backends.Abstractions.Contracts;

namespace Vuetrack.Api.Features.TimeEntry;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/timeEntry")]
[Authorize]
public class TimeEntryController(ITimeEntryService timeEntryService) : ControllerBase
{
    private ITimeEntryService TimeEntryService { get; } = timeEntryService;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await TimeEntryService.ListAsync(userId, from, to, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TimeEntryCreateContract contract, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await TimeEntryService.CreateAsync(userId, contract, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] TimeEntryUpdateContract contract, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await TimeEntryService.UpdateAsync(userId, id, contract, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await TimeEntryService.DeleteAsync(userId, id, cancellationToken);

        return this.ToActionResult(result);
    }
}
