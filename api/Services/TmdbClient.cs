using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MoviesApp.Api.Models;

namespace MoviesApp.Api.Services;

/// <summary>
/// Thin TMDB v3 client using the Bearer Read Access Token. Fetches everything needed for a movie in
/// one round-trip via append_to_response, scoped to the US region for certification and providers.
/// The browser never calls TMDB directly — only this server-side client does.
/// </summary>
public sealed class TmdbClient
{
    private readonly HttpClient _http;
    private readonly string _region;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public TmdbClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        var baseUrl = config["Tmdb:BaseUrl"] ?? "https://api.themoviedb.org/3";
        var token = config["Tmdb:ReadAccessToken"]
            ?? throw new InvalidOperationException("Tmdb__ReadAccessToken is not configured.");
        _region = config["Tmdb:Region"] ?? "US";

        _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>Search movies by title and optional year.</summary>
    public async Task<JsonElement> SearchAsync(string query, int? year, CancellationToken ct)
    {
        var url = $"search/movie?query={Uri.EscapeDataString(query)}&include_adult=false&region={_region}";
        if (year is > 0)
        {
            url += $"&year={year}";
        }

        return await GetJsonAsync(url, ct);
    }

    /// <summary>Fetch the full movie payload (details + credits + keywords + providers + release dates).</summary>
    public async Task<JsonElement> GetMovieRawAsync(int tmdbId, CancellationToken ct)
    {
        var url = $"movie/{tmdbId}?append_to_response=credits,keywords,watch/providers,release_dates,videos";
        return await GetJsonAsync(url, ct);
    }

    private async Task<JsonElement> GetJsonAsync(string url, CancellationToken ct)
    {
        // Polite retry on 429 with Retry-After.
        for (var attempt = 0; ; attempt++)
        {
            using var response = await _http.GetAsync(url, ct);
            if ((int)response.StatusCode == 429 && attempt < 3)
            {
                var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
                await Task.Delay(delay, ct);
                continue;
            }

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return doc.RootElement.Clone();
        }
    }
}
