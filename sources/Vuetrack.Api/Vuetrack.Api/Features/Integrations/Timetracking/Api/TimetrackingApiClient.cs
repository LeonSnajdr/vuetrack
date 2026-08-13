using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Vuetrack.Api.Features.Integrations.Timetracking.Connection;

namespace Vuetrack.Api.Features.Integrations.Timetracking.Api;

public class TimetrackingApiClient(HttpClient httpClient) : ITimetrackingApiClient
{
    private HttpClient HttpClient { get; } = httpClient;

    public async Task<IReadOnlyList<TimetrackingTimeEntryResponse>> GetTimeEntriesAsync(TimetrackingConnectionContext context, string from, string to, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(context.AccessToken, HttpMethod.Get, $"timeEntry?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<TimetrackingTimeEntryResponse>>(cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<TimetrackingActivityResponse>> GetProjectsAsync(TimetrackingConnectionContext context, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(context.AccessToken, HttpMethod.Get, "project");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<TimetrackingActivityResponse>>(cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<TimetrackingActivityResponse>> GetActivitiesAsync(TimetrackingConnectionContext context, string projectId, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(context.AccessToken, HttpMethod.Get, $"project/{Uri.EscapeDataString(projectId)}/activity");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<TimetrackingActivityResponse>>(cancellationToken) ?? [];
    }

    public async Task<string?> FindProjectIdByTaskIdAsync(TimetrackingConnectionContext context, string taskId, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(context.AccessToken, HttpMethod.Get, $"project/findByTaskId?taskId={Uri.EscapeDataString(taskId)}");
        using var response = await HttpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(body) ? null : body.Trim();
    }

    public async Task<TimetrackingProfileResponse> GetProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(accessToken, HttpMethod.Get, "profile");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TimetrackingProfileResponse>(cancellationToken) ?? new TimetrackingProfileResponse();
    }

    public async Task<TimetrackingTimeEntryResponse> UpsertTimeEntryAsync(TimetrackingConnectionContext context, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(context.AccessToken, HttpMethod.Post, "timeEntry/upsert");
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

    public async Task DeleteTimeEntriesAsync(TimetrackingConnectionContext context, string idsToDelete, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(context.AccessToken, HttpMethod.Delete, "timeEntry");
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

    private static HttpRequestMessage BuildRequest(string accessToken, HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
}

public interface ITimetrackingApiClient
{
    Task<IReadOnlyList<TimetrackingTimeEntryResponse>> GetTimeEntriesAsync(TimetrackingConnectionContext context, string from, string to, CancellationToken cancellationToken);

    Task<IReadOnlyList<TimetrackingActivityResponse>> GetProjectsAsync(TimetrackingConnectionContext context, CancellationToken cancellationToken);

    Task<IReadOnlyList<TimetrackingActivityResponse>> GetActivitiesAsync(TimetrackingConnectionContext context, string projectId, CancellationToken cancellationToken);

    Task<string?> FindProjectIdByTaskIdAsync(TimetrackingConnectionContext context, string taskId, CancellationToken cancellationToken);

    Task<TimetrackingProfileResponse> GetProfileAsync(string accessToken, CancellationToken cancellationToken);

    Task<TimetrackingTimeEntryResponse> UpsertTimeEntryAsync(TimetrackingConnectionContext context, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken);

    Task DeleteTimeEntriesAsync(TimetrackingConnectionContext context, string idsToDelete, CancellationToken cancellationToken);
}
