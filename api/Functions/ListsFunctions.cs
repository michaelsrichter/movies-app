using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using MoviesApp.Api.Models;
using MoviesApp.Api.Services;

namespace MoviesApp.Api.Functions;

/// <summary>Public, anonymous endpoints for browsing the current watchlist.</summary>
public sealed class ListsFunctions
{
    private readonly ListRepository _lists;
    private readonly ListMovieRepository _listMovies;
    private readonly BlobCacheService _cache;

    public ListsFunctions(ListRepository lists, ListMovieRepository listMovies, BlobCacheService cache)
    {
        _lists = lists;
        _listMovies = listMovies;
        _cache = cache;
    }

    [Function("GetCurrentList")]
    public async Task<IActionResult> GetCurrent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lists/current")] HttpRequest req,
        CancellationToken ct)
    {
        var list = await _lists.GetActiveAsync(ct);
        if (list is null)
        {
            return new NotFoundResult();
        }

        var movies = await _listMovies.GetForListAsync(list.Id, ct);
        foreach (var m in movies)
        {
            var detail = await _cache.GetMovieAsync(m.TmdbId, ct);
            if (detail is not null)
            {
                m.Summary = ToSummary(detail);
            }
        }

        list.Movies = movies.ToList();

        // Weak ETag from the newest movie fetch + list id so clients/CDN can revalidate cheaply.
        req.HttpContext.Response.Headers.CacheControl = "public, max-age=60, stale-while-revalidate=300";
        return new OkObjectResult(list);
    }

    private static MovieSummary ToSummary(MovieDetail d) => new()
    {
        TmdbId = d.TmdbId,
        Title = d.Title,
        Year = d.Year,
        Runtime = d.Runtime,
        Certification = d.Certification,
        VoteAverage = d.VoteAverage,
        PosterPath = d.PosterPath,
        PosterBlurDataUrl = d.PosterBlurDataUrl,
        Genres = d.Genres,
        Providers = d.Providers,
    };
}
