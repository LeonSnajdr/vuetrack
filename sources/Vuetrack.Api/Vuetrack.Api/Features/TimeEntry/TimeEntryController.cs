using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Contracts;
using Vuetrack.Api.Features.TimeEntry.Contracts;
using Vuetrack.Api.Infrastructure.Authentication;
using Vuetrack.Api.Infrastructure.Validation;

namespace Vuetrack.Api.Features.TimeEntry;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/timeEntry")]
[Authorize(Roles = "User")]
public class TimeEntryController(IBackend store) : ControllerBase
{
    private IBackend Store { get; } = store;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var range = new DateRange { From = from, To = to };
        var result = await Store.GetTimeEntriesAsync(userId, range, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TimeEntryCreateContract contract, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await Store.CreateTimeEntryAsync(userId, contract, cancellationToken);

        return this.ToCreatedResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] TimeEntryUpdateContract contract, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await Store.UpdateTimeEntryAsync(userId, id, contract, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await Store.DeleteTimeEntryAsync(userId, id, cancellationToken);

        return this.ToActionResult(result);
    }
}
