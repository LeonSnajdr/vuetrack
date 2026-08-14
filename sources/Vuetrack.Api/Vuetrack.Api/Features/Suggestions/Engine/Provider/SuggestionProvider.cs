using System.ClientModel;
using System.Text.Json;
using ErrorOr;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;

namespace Vuetrack.Api.Features.Suggestions.Engine.Provider;

[InjectAs(typeof(ISuggestionProvider))]
public sealed class SuggestionProvider : ISuggestionProvider
{
    private const string CandidatesSchema =
        """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["candidates"],
          "properties": {
            "candidates": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["taskId", "dateStarted", "dateEnded", "confidence", "sourceExternalIds"],
                "properties": {
                  "taskId": { "type": ["string", "null"] },
                  "dateStarted": { "type": "string" },
                  "dateEnded": { "type": "string" },
                  "confidence": { "type": "number" },
                  "sourceExternalIds": {
                    "type": "array",
                    "items": { "type": "string" }
                  }
                }
              }
            }
          }
        }
        """;

    private static readonly JsonSerializerOptions JsonOptions = BuildJsonOptions();

    private static readonly string SystemPrompt = LoadSystemPrompt();

    public SuggestionProvider(IOptions<ProviderOpenAiOptions> options, ILogger<SuggestionProvider> logger)
    {
        Options = options.Value;
        Logger = logger;
        ChatClient = BuildChatClient(Options);
    }

    private ProviderOpenAiOptions Options { get; }

    private ILogger<SuggestionProvider> Logger { get; }

    private ChatClient ChatClient { get; }

    public async Task<ErrorOr<IReadOnlyList<SuggestionProviderCandidate>>> ProvideAsync(SuggestionProviderContext context, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(context, JsonOptions);

        var systemMessage = ChatMessage.CreateSystemMessage(SystemPrompt);
        var userMessage = ChatMessage.CreateUserMessage(payload);
        List<ChatMessage> messages = [systemMessage, userMessage];

        var completionOptions = BuildCompletionOptions(Options);

        ChatCompletion completion;
        try
        {
            completion = await ChatClient.CompleteChatAsync(messages, completionOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "OpenAI suggestion provider failed for {SignalCount} signals in [{From}, {To}]", context.Signals.Count, context.From, context.To);
            return Error.Failure();
        }

        var text = ExtractText(completion);
        if (!string.IsNullOrWhiteSpace(text))
        {
            return ParseCandidates(text, Logger);
        }

        Logger.LogWarning("OpenAI suggestion provider returned empty content for {SignalCount} signals", context.Signals.Count);

        return [];
    }

    private static ChatCompletionOptions BuildCompletionOptions(ProviderOpenAiOptions options)
    {
        var schema = BinaryData.FromString(CandidatesSchema);
        var format = ChatResponseFormat.CreateJsonSchemaFormat("suggestion_candidates", schema, jsonSchemaIsStrict: true);
        return new ChatCompletionOptions { ResponseFormat = format, MaxOutputTokenCount = options.MaxOutputTokens };
    }

    private static string LoadSystemPrompt()
    {
        var assembly = typeof(SuggestionProvider).Assembly;
        const string resourceName = "Vuetrack.Api.Features.Suggestions.Engine.Provider.SystemPrompt.md";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource {resourceName} was not found.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    private static ChatClient BuildChatClient(ProviderOpenAiOptions options)
    {
        var credential = new ApiKeyCredential(options.ApiKey);
        var endpoint = new Uri(options.Endpoint);
        var clientOptions = new OpenAIClientOptions { Endpoint = endpoint, NetworkTimeout = TimeSpan.FromSeconds(60) };
        return new ChatClient(options.Model, credential, clientOptions);
    }

    private static string? ExtractText(ChatCompletion completion)
    {
        return completion.Content.Count == 0 ? null : completion.Content[0].Text;
    }

    private static ErrorOr<IReadOnlyList<SuggestionProviderCandidate>> ParseCandidates(string text, ILogger logger)
    {
        CandidatesResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<CandidatesResponse>(text, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "OpenAI suggestion provider returned unparseable content");
            return [];
        }

        var result = response?.Candidates ?? [];
        return result.ToErrorOr();
    }

    private static JsonSerializerOptions BuildJsonOptions()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new ActivitySignalDetailJsonConverter());
        return jsonOptions;
    }

    private sealed record CandidatesResponse
    {
        public IReadOnlyList<SuggestionProviderCandidate> Candidates { get; init; } = [];
    }
}
