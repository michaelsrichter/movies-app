using System.ClientModel;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using MoviesApp.Api.Models;
using OpenAI.Chat;

namespace MoviesApp.Api.Services;

/// <summary>
/// Generates DRAFT family discussion topics via Azure AI Foundry (richtercloud-0138-resource).
/// Called only at admin time; the admin edits/approves and the published result is stored and served
/// statically. There is never a live LLM call on a public request.
/// </summary>
public sealed class DiscussionGenerationService
{
    private readonly ChatClient? _chat;
    private readonly string _deployment;

    public DiscussionGenerationService(IConfiguration config)
    {
        var endpoint = config["AzureAI:Endpoint"];
        var apiKey = config["AzureAI:ApiKey"];
        _deployment = config["AzureAI:Deployment"] ?? "gpt-4o-mini";

        if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey))
        {
            var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
            _chat = client.GetChatClient(_deployment);
        }
    }

    public async Task<Discussion> GenerateDraftAsync(MovieDetail movie, CancellationToken ct)
    {
        if (_chat is null)
        {
            throw new InvalidOperationException("Azure AI Foundry is not configured (AzureAI__Endpoint/ApiKey).");
        }

        var system =
            "You craft family movie discussion guides for parents of teenagers. Produce thoughtful, " +
            "age-appropriate topics covering themes, ethical dilemmas, historical context, character " +
            "motivations, and conversation starters. Return STRICT JSON: an array of objects with " +
            "\"heading\", \"prompt\", and \"category\" (one of themes|ethics|history|character|conversation-starter).";

        var user = JsonSerializer.Serialize(new
        {
            movie.Title,
            movie.Year,
            movie.Overview,
            movie.Tagline,
            movie.Genres,
            movie.Keywords,
            Director = movie.Crew.FirstOrDefault(c => c.Job == "Director")?.Name,
        });

        var completion = await _chat.CompleteChatAsync(
            new ChatMessage[]
            {
                new SystemChatMessage(system),
                new UserChatMessage(user),
            },
            new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() },
            ct);

        var raw = completion.Value.Content.Count > 0 ? completion.Value.Content[0].Text : "[]";
        var topics = ParseTopics(raw);

        return new Discussion
        {
            TmdbId = movie.TmdbId,
            Topics = topics,
            Source = "ai",
            Status = "draft",
            Model = _deployment,
            GeneratedUtc = DateTimeOffset.UtcNow,
        };
    }

    private static List<DiscussionTopic> ParseTopics(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var array = root.ValueKind == JsonValueKind.Array
                ? root
                : root.TryGetProperty("topics", out var t) ? t : default;

            if (array.ValueKind != JsonValueKind.Array)
            {
                return new();
            }

            return array.EnumerateArray()
                .Select(e => new DiscussionTopic
                {
                    Heading = e.TryGetProperty("heading", out var h) ? h.GetString() ?? "" : "",
                    Prompt = e.TryGetProperty("prompt", out var p) ? p.GetString() ?? "" : "",
                    Category = e.TryGetProperty("category", out var c) ? c.GetString() ?? "conversation-starter" : "conversation-starter",
                })
                .Where(t => !string.IsNullOrWhiteSpace(t.Heading))
                .ToList();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}
