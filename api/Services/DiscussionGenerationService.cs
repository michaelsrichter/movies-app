using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MoviesApp.Api.Models;

namespace MoviesApp.Api.Services;

/// <summary>
/// Generates DRAFT family discussion topics via Azure AI Foundry (richtercloud-0138-resource) using a
/// direct chat-completions HTTP call (avoids SDK api-version coupling for newer models like gpt-5.4).
/// Called only at admin time; the admin edits/approves and the published result is stored and served
/// statically. There is never a live LLM call on a public request.
/// </summary>
public sealed class DiscussionGenerationService
{
    private readonly HttpClient _http;
    private readonly string? _endpoint;
    private readonly string? _apiKey;
    private readonly string _deployment;
    private readonly string _apiVersion;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DiscussionGenerationService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _endpoint = config["AzureAI:Endpoint"]?.TrimEnd('/');
        _apiKey = config["AzureAI:ApiKey"];
        _deployment = config["AzureAI:Deployment"] ?? "gpt-5.4";
        _apiVersion = config["AzureAI:ApiVersion"] ?? "2024-12-01-preview";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_endpoint) && !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<Discussion> GenerateDraftAsync(MovieDetail movie, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Azure AI Foundry is not configured (AzureAI__Endpoint/ApiKey).");
        }

        var system =
            "You craft family movie discussion guides for parents of teenagers. Produce 6-8 thoughtful, " +
            "age-appropriate discussion topics covering themes, ethical dilemmas, historical context, " +
            "character motivations, and conversation starters. Respond ONLY with a JSON object of the form " +
            "{\"topics\":[{\"heading\":\"short title\",\"prompt\":\"a question or prompt for the family\"," +
            "\"category\":\"one of: themes|ethics|history|character|conversation-starter\"}]}.";

        var context = JsonSerializer.Serialize(new
        {
            movie.Title,
            movie.Year,
            movie.Overview,
            movie.Tagline,
            movie.Genres,
            Keywords = movie.Keywords.Take(15),
            Certification = movie.Certification,
            Director = movie.Crew.FirstOrDefault(c => c.Job == "Director")?.Name,
        }, JsonOptions);

        var requestBody = new
        {
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = $"Create discussion topics for this film:\n{context}" },
            },
            response_format = new { type = "json_object" },
            max_completion_tokens = 4000,
        };

        var url = $"{_endpoint}/openai/deployments/{_deployment}/chat/completions?api-version={_apiVersion}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(requestBody),
        };
        request.Headers.TryAddWithoutValidation("api-key", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        return new Discussion
        {
            TmdbId = movie.TmdbId,
            Topics = ParseTopics(content),
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

            JsonElement array = default;
            if (root.ValueKind == JsonValueKind.Array)
            {
                array = root;
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Prefer a "topics" property; otherwise take the first array-valued property.
                if (!root.TryGetProperty("topics", out array) || array.ValueKind != JsonValueKind.Array)
                {
                    array = root.EnumerateObject()
                        .Select(p => p.Value)
                        .FirstOrDefault(v => v.ValueKind == JsonValueKind.Array);
                }
            }

            if (array.ValueKind != JsonValueKind.Array)
            {
                return new();
            }

            return array.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Object)
                .Select(e => new DiscussionTopic
                {
                    Heading = e.TryGetProperty("heading", out var h) ? h.GetString() ?? "" : "",
                    Prompt = e.TryGetProperty("prompt", out var p) ? p.GetString() ?? "" : "",
                    Category = e.TryGetProperty("category", out var c) ? c.GetString() ?? "conversation-starter" : "conversation-starter",
                })
                .Where(t => !string.IsNullOrWhiteSpace(t.Heading) && !string.IsNullOrWhiteSpace(t.Prompt))
                .ToList();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}
