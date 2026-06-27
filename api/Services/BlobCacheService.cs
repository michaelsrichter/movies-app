using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MoviesApp.Api.Models;

namespace MoviesApp.Api.Services;

/// <summary>
/// Stores normalized TMDB payloads in the private `tmdb-cache` blob container. The cache is
/// indefinite; refresh is a manual admin action. Each payload carries etag + lastFetchedUtc.
/// </summary>
public sealed class BlobCacheService
{
    private const string Container = "tmdb-cache";
    private readonly StorageClientFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public BlobCacheService(StorageClientFactory factory) => _factory = factory;

    private BlobContainerClient Client()
    {
        var client = _factory.GetBlobContainerClient(Container);
        client.CreateIfNotExists(PublicAccessType.None);
        return client;
    }

    private static string MovieBlobName(int tmdbId) => $"movies/{tmdbId}.json";

    public async Task<MovieDetail?> GetMovieAsync(int tmdbId, CancellationToken ct)
    {
        var blob = Client().GetBlobClient(MovieBlobName(tmdbId));
        try
        {
            var response = await blob.DownloadContentAsync(ct);
            return response.Value.Content.ToObjectFromJson<MovieDetail>(JsonOptions);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task SaveMovieAsync(MovieDetail movie, CancellationToken ct)
    {
        var blob = Client().GetBlobClient(MovieBlobName(movie.TmdbId));
        var json = BinaryData.FromObjectAsJson(movie, JsonOptions);
        await blob.UploadAsync(json, overwrite: true, ct);
    }
}
