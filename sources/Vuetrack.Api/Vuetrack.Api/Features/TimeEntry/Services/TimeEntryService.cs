using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Backends;
using Vuetrack.Backends.Abstractions;
using Vuetrack.Backends.Abstractions.Contracts;

namespace Vuetrack.Api.Features.TimeEntry.Services;

[Inject]
public class TimeEntryService(IBackendResolver resolver) : ITimeEntryService
{
    private const BackendKey Backend = BackendKey.Timetracking;

    private IBackendResolver Resolver { get; } = resolver;

    public async Task<ErrorOr<IReadOnlyList<TimeEntryContract>>> ListAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var backend = await Resolver.ResolveConnectedAsync(Backend, userId, cancellationToken);
        if (backend.IsError)
        {
            return backend.Errors;
        }

        return await backend.Value.GetTimeEntriesAsync(new DateRange { From = from, To = to }, cancellationToken);
    }

    public async Task<ErrorOr<TimeEntryContract>> CreateAsync(string userId, TimeEntryCreateContract contract, CancellationToken cancellationToken)
    {
        var backend = await Resolver.ResolveConnectedAsync(Backend, userId, cancellationToken);
        if (backend.IsError)
        {
            return backend.Errors;
        }

        return await backend.Value.CreateTimeEntryAsync(contract, cancellationToken);
    }

    public async Task<ErrorOr<TimeEntryContract>> UpdateAsync(string userId, string id, TimeEntryUpdateContract contract, CancellationToken cancellationToken)
    {
        var backend = await Resolver.ResolveConnectedAsync(Backend, userId, cancellationToken);
        if (backend.IsError)
        {
            return backend.Errors;
        }

        return await backend.Value.UpdateTimeEntryAsync(id, contract, cancellationToken);
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var backend = await Resolver.ResolveConnectedAsync(Backend, userId, cancellationToken);
        if (backend.IsError)
        {
            return backend.Errors;
        }

        return await backend.Value.DeleteTimeEntryAsync(id, cancellationToken);
    }
}

public interface ITimeEntryService
{
    Task<ErrorOr<IReadOnlyList<TimeEntryContract>>> ListAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<ErrorOr<TimeEntryContract>> CreateAsync(string userId, TimeEntryCreateContract contract, CancellationToken cancellationToken);

    Task<ErrorOr<TimeEntryContract>> UpdateAsync(string userId, string id, TimeEntryUpdateContract contract, CancellationToken cancellationToken);

    Task<ErrorOr<Deleted>> DeleteAsync(string userId, string id, CancellationToken cancellationToken);
}
