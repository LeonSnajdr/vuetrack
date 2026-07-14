using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Vuetrack.Backends.Timetracking.Connection;

namespace Vuetrack.Backends.Timetracking.Api;

public class TimetrackingApiClient(HttpClient httpClient, ITimetrackingConnectionAccessor accessor) : ITimetrackingApiClient
{
    private HttpClient HttpClient { get; } = httpClient;

    private ITimetrackingConnectionAccessor Accessor { get; } = accessor;

    public async Task<IReadOnlyList<TimetrackingTimeEntryResponse>> GetTimeEntriesAsync(string from, string to, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, $"timeEntry?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<TimetrackingTimeEntryResponse>>(cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<TimetrackingRefResponse>> GetProjectsAsync(CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, "project");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<TimetrackingRefResponse>>(cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<TimetrackingRefResponse>> GetActivitiesAsync(string projectId, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, $"project/{Uri.EscapeDataString(projectId)}/activity");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<TimetrackingRefResponse>>(cancellationToken) ?? [];
    }

    public async Task<string?> FindProjectIdByTaskIdAsync(string taskId, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, $"project/findByTaskId?taskId={Uri.EscapeDataString(taskId)}");
        using var response = await HttpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(body) ? null : body.Trim();
    }

    public async Task<TimetrackingProfileResponse> GetProfileAsync(CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, "profile");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TimetrackingProfileResponse>(cancellationToken) ?? new TimetrackingProfileResponse();
    }

    public async Task<TimetrackingTimeEntryResponse> UpsertTimeEntryAsync(IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, "timeEntry/upsert");
        request.Content = new FormUrlEncodedContent(form);
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TimetrackingTimeEntryResponse>(cancellationToken) ?? throw new InvalidOperationException("Timetracking upsert returned an empty response.");
    }

    public async Task DeleteTimeEntriesAsync(string idsToDelete, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Delete, "timeEntry");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["idsToDelete"] = idsToDelete });
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string path)
    {
        var connection = Accessor.Current ?? throw new InvalidOperationException("No active timetracking connection for this request.");

        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
}

public interface ITimetrackingApiClient
{
    Task<IReadOnlyList<TimetrackingTimeEntryResponse>> GetTimeEntriesAsync(string from, string to, CancellationToken cancellationToken);

    Task<IReadOnlyList<TimetrackingRefResponse>> GetProjectsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<TimetrackingRefResponse>> GetActivitiesAsync(string projectId, CancellationToken cancellationToken);

    Task<string?> FindProjectIdByTaskIdAsync(string taskId, CancellationToken cancellationToken);

    Task<TimetrackingProfileResponse> GetProfileAsync(CancellationToken cancellationToken);

    Task<TimetrackingTimeEntryResponse> UpsertTimeEntryAsync(IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken);

    Task DeleteTimeEntriesAsync(string idsToDelete, CancellationToken cancellationToken);
}
