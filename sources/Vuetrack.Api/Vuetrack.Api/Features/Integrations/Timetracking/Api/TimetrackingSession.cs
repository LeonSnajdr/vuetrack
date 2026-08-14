using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Vuetrack.Api.Features.Integrations.Timetracking.Api;

public class TimetrackingSession(HttpClient httpClient, string accessToken, string? externalUserId) : ITimetrackingSession
{
    private HttpClient HttpClient { get; } = httpClient;

    private string AccessToken { get; } = accessToken;

    public string? ExternalUserId { get; } = externalUserId;

    public async Task<IReadOnlyList<TimetrackingTimeEntryResponse>> GetTimeEntriesAsync(string from, string to, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, $"timeEntry?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<TimetrackingTimeEntryResponse>>(cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<TimetrackingActivityResponse>> GetProjectsAsync(CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, "project");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<TimetrackingActivityResponse>>(cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<TimetrackingActivityResponse>> GetActivitiesAsync(string projectId, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, $"project/{Uri.EscapeDataString(projectId)}/activity");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<TimetrackingActivityResponse>>(cancellationToken) ?? [];
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

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var fieldErrors = ParseValidationErrors(body);
            throw new TimetrackingValidationException(fieldErrors);
        }

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

    private static IReadOnlyList<TimetrackingFieldError> ParseValidationErrors(string body)
    {
        var errors = new List<TimetrackingFieldError>();

        if (string.IsNullOrWhiteSpace(body))
        {
            return errors;
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        foreach (var property in root.EnumerateObject())
        {
            var error = new TimetrackingFieldError(property.Name);
            errors.Add(error);
        }

        return errors;
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
}

public interface ITimetrackingSession
{
    string? ExternalUserId { get; }

    Task<IReadOnlyList<TimetrackingTimeEntryResponse>> GetTimeEntriesAsync(string from, string to, CancellationToken cancellationToken);

    Task<IReadOnlyList<TimetrackingActivityResponse>> GetProjectsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<TimetrackingActivityResponse>> GetActivitiesAsync(string projectId, CancellationToken cancellationToken);

    Task<string?> FindProjectIdByTaskIdAsync(string taskId, CancellationToken cancellationToken);

    Task<TimetrackingProfileResponse> GetProfileAsync(CancellationToken cancellationToken);

    Task<TimetrackingTimeEntryResponse> UpsertTimeEntryAsync(IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken);

    Task DeleteTimeEntriesAsync(string idsToDelete, CancellationToken cancellationToken);
}
