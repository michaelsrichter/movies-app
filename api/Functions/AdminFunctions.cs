using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using MoviesApp.Api.Models;
using MoviesApp.Api.Services;

namespace MoviesApp.Api.Functions;

/// <summary>
/// Admin endpoints (role `admin`, enforced by SWA via staticwebapp.config.json on /api/manage/*).
/// Routes use the `manage/` prefix because the Azure Functions host reserves routes beginning with
/// `admin`. Manage lists, add/remove movies (caching TMDB on add), refresh TMDB, and search TMDB.
/// </summary>
public sealed class AdminFunctions
{
    private readonly ListRepository _lists;
    private readonly ListMovieRepository _listMovies;
    private readonly MovieCacheService _movies;
    private readonly TmdbClient _tmdb;
    private readonly DiscussionRepository _discussions;
    private readonly DiscussionGenerationService _generator;

    public AdminFunctions(
        ListRepository lists,
        ListMovieRepository listMovies,
        MovieCacheService movies,
        TmdbClient tmdb,
        DiscussionRepository discussions,
        DiscussionGenerationService generator)
    {
        _lists = lists;
        _listMovies = listMovies;
        _movies = movies;
        _tmdb = tmdb;
        _discussions = discussions;
        _generator = generator;
    }

    public sealed record CreateListRequest(string Title, string? Slug, string? Period, bool IsActive);
    public sealed record AddMovieRequest(int TmdbId, int? Order, string? Notes);
    public sealed record PublishDiscussionRequest(List<DiscussionTopic>? Topics, string? Status, string? ApprovedBy);

    [Function("AdminGetLists")]
    public async Task<IActionResult> GetLists(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/lists")] HttpRequest req,
        CancellationToken ct)
    {
        var lists = await _lists.GetAllAsync(ct);
        return new OkObjectResult(lists);
    }

    [Function("AdminCreateList")]
    public async Task<IActionResult> CreateList(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/lists")] HttpRequest req,
        CancellationToken ct)
    {
        var body = await req.ReadFromJsonAsync<CreateListRequest>(ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Title))
        {
            return new BadRequestObjectResult("Title is required.");
        }

        var slug = string.IsNullOrWhiteSpace(body.Slug) ? Slugify(body.Title) : body.Slug!;
        var list = new MovieList
        {
            Id = slug,
            Title = body.Title,
            Slug = slug,
            Period = body.Period ?? body.Title,
            IsActive = body.IsActive,
            SortOrder = 0,
            CreatedUtc = DateTimeOffset.UtcNow,
        };

        if (body.IsActive)
        {
            await DeactivateOthersAsync(list.Id, ct);
        }

        await _lists.UpsertAsync(list, ct);
        return new OkObjectResult(list);
    }

    [Function("AdminDeleteList")]
    public async Task<IActionResult> DeleteList(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "manage/lists/{id}")] HttpRequest req,
        string id,
        CancellationToken ct)
    {
        await _lists.DeleteAsync(id, ct);
        return new NoContentResult();
    }

    [Function("AdminAddMovie")]
    public async Task<IActionResult> AddMovie(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/lists/{id}/movies")] HttpRequest req,
        string id,
        CancellationToken ct)
    {
        var body = await req.ReadFromJsonAsync<AddMovieRequest>(ct);
        if (body is null || body.TmdbId <= 0)
        {
            return new BadRequestObjectResult("TmdbId is required.");
        }

        // Cache the TMDB payload on add so the public list renders without a live TMDB call.
        var detail = await _movies.GetOrFetchAsync(body.TmdbId, forceRefresh: false, ct);

        var existing = await _listMovies.GetForListAsync(id, ct);
        var order = body.Order ?? existing.Count;

        await _listMovies.UpsertAsync(id, new ListMovie
        {
            TmdbId = body.TmdbId,
            Order = order,
            Notes = body.Notes,
            AddedUtc = DateTimeOffset.UtcNow,
        }, ct);

        return new OkObjectResult(new { tmdbId = body.TmdbId, title = detail.Title, order });
    }

    [Function("AdminRemoveMovie")]
    public async Task<IActionResult> RemoveMovie(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "manage/lists/{id}/movies/{tmdbId:int}")] HttpRequest req,
        string id,
        int tmdbId,
        CancellationToken ct)
    {
        await _listMovies.DeleteAsync(id, tmdbId, ct);
        return new NoContentResult();
    }

    [Function("AdminRefreshMovie")]
    public async Task<IActionResult> RefreshMovie(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/movies/{tmdbId:int}/refresh")] HttpRequest req,
        int tmdbId,
        CancellationToken ct)
    {
        var detail = await _movies.RefreshAsync(tmdbId, ct);
        return new OkObjectResult(new { tmdbId, title = detail.Title, lastFetchedUtc = detail.LastFetchedUtc });
    }

    [Function("AdminTmdbSearch")]
    public async Task<IActionResult> Search(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/tmdb/search")] HttpRequest req,
        CancellationToken ct)
    {
        var q = req.Query["q"].ToString();
        if (string.IsNullOrWhiteSpace(q))
        {
            return new BadRequestObjectResult("Query 'q' is required.");
        }

        int? year = int.TryParse(req.Query["year"].ToString(), out var y) ? y : null;
        var results = await _tmdb.SearchAsync(q, year, ct);
        return new OkObjectResult(results);
    }

    [Function("AdminGenerateDiscussion")]
    public async Task<IActionResult> GenerateDiscussion(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/movies/{tmdbId:int}/discussion/generate")] HttpRequest req,
        int tmdbId,
        CancellationToken ct)
    {
        var movie = await _movies.GetOrFetchAsync(tmdbId, forceRefresh: false, ct);

        Discussion draft;
        try
        {
            draft = await _generator.GenerateDraftAsync(movie, ct);
        }
        catch (InvalidOperationException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = 503 };
        }

        await _discussions.UpsertAsync(draft, ct);
        return new OkObjectResult(draft);
    }

    [Function("AdminUpdateDiscussion")]
    public async Task<IActionResult> UpdateDiscussion(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "manage/movies/{tmdbId:int}/discussion")] HttpRequest req,
        int tmdbId,
        CancellationToken ct)
    {
        var body = await req.ReadFromJsonAsync<PublishDiscussionRequest>(ct);
        var existing = await _discussions.GetAsync(tmdbId, ct) ?? new Discussion { TmdbId = tmdbId };

        if (body?.Topics is { Count: > 0 })
        {
            existing.Topics = body.Topics;
        }

        if (!string.IsNullOrWhiteSpace(body?.Status))
        {
            existing.Status = body!.Status!;
            if (existing.Status == "published")
            {
                existing.ApprovedBy = body.ApprovedBy ?? existing.ApprovedBy;
                existing.ApprovedUtc = DateTimeOffset.UtcNow;
            }
        }

        await _discussions.UpsertAsync(existing, ct);
        return new OkObjectResult(existing);
    }

    private async Task DeactivateOthersAsync(string keepId, CancellationToken ct)
    {
        foreach (var l in await _lists.GetAllAsync(ct))
        {
            if (l.Id != keepId && l.IsActive)
            {
                l.IsActive = false;
                await _lists.UpsertAsync(l, ct);
            }
        }
    }

    private static string Slugify(string value)
    {
        var chars = value.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
