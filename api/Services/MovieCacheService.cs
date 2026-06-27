using MoviesApp.Api.Models;

namespace MoviesApp.Api.Services;

/// <summary>
/// Returns movie detail from the Blob cache, fetching+normalizing from TMDB on a miss or when a
/// refresh is forced. The cache is indefinite; refresh is a manual admin action.
/// </summary>
public sealed class MovieCacheService
{
    private readonly BlobCacheService _cache;
    private readonly TmdbClient _tmdb;

    public MovieCacheService(BlobCacheService cache, TmdbClient tmdb)
    {
        _cache = cache;
        _tmdb = tmdb;
    }

    public async Task<MovieDetail?> GetAsync(int tmdbId, CancellationToken ct) =>
        await _cache.GetMovieAsync(tmdbId, ct);

    public async Task<MovieDetail> GetOrFetchAsync(int tmdbId, bool forceRefresh, CancellationToken ct)
    {
        if (!forceRefresh)
        {
            var cached = await _cache.GetMovieAsync(tmdbId, ct);
            if (cached is not null)
            {
                return cached;
            }
        }

        return await RefreshAsync(tmdbId, ct);
    }

    public async Task<MovieDetail> RefreshAsync(int tmdbId, CancellationToken ct)
    {
        var raw = await _tmdb.GetMovieRawAsync(tmdbId, ct);
        var detail = TmdbMapper.Map(raw);
        await _cache.SaveMovieAsync(detail, ct);
        return detail;
    }
}
