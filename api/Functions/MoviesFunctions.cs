using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using MoviesApp.Api.Services;

namespace MoviesApp.Api.Functions;

/// <summary>Public, anonymous endpoint for a movie detail page.</summary>
public sealed class MoviesFunctions
{
    private readonly BlobCacheService _cache;
    private readonly DiscussionRepository _discussions;

    public MoviesFunctions(BlobCacheService cache, DiscussionRepository discussions)
    {
        _cache = cache;
        _discussions = discussions;
    }

    [Function("GetMovie")]
    public async Task<IActionResult> GetMovie(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "movies/{tmdbId:int}")] HttpRequest req,
        int tmdbId,
        CancellationToken ct)
    {
        var detail = await _cache.GetMovieAsync(tmdbId, ct);
        if (detail is null)
        {
            return new NotFoundResult();
        }

        var discussion = await _discussions.GetAsync(tmdbId, ct);
        // Only published discussion topics are exposed publicly.
        var publishedTopics = discussion is { Status: "published" } ? discussion.Topics : new();

        req.HttpContext.Response.Headers.CacheControl = "public, max-age=60, stale-while-revalidate=300";
        return new OkObjectResult(new
        {
            movie = detail,
            discussionTopics = publishedTopics,
        });
    }
}
