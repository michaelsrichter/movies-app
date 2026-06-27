using Azure;
using Azure.Data.Tables;
using MoviesApp.Api.Models;

namespace MoviesApp.Api.Services;

/// <summary>Reads/writes movie-to-list associations in the `ListMovies` table.</summary>
public sealed class ListMovieRepository
{
    private const string TableName = "ListMovies";
    private readonly StorageClientFactory _factory;

    public ListMovieRepository(StorageClientFactory factory) => _factory = factory;

    private TableClient Table()
    {
        var table = _factory.GetTableClient(TableName);
        table.CreateIfNotExists();
        return table;
    }

    public async Task<IReadOnlyList<ListMovie>> GetForListAsync(string listId, CancellationToken ct)
    {
        var table = Table();
        var result = new List<ListMovie>();
        await foreach (var e in table.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{listId}'", cancellationToken: ct))
        {
            result.Add(new ListMovie
            {
                TmdbId = int.TryParse(e.RowKey, out var id) ? id : 0,
                Order = e.GetInt32("Order") ?? 0,
                Notes = e.GetString("Notes"),
                AddedUtc = e.GetDateTimeOffset("AddedUtc") ?? DateTimeOffset.UtcNow,
            });
        }

        return result.OrderBy(m => m.Order).ToList();
    }

    public async Task UpsertAsync(string listId, ListMovie movie, CancellationToken ct)
    {
        var table = Table();
        var entity = new TableEntity(listId, movie.TmdbId.ToString())
        {
            ["Order"] = movie.Order,
            ["Notes"] = movie.Notes,
            ["AddedUtc"] = movie.AddedUtc == default ? DateTimeOffset.UtcNow : movie.AddedUtc,
        };
        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
    }

    public async Task DeleteAsync(string listId, int tmdbId, CancellationToken ct)
    {
        var table = Table();
        await table.DeleteEntityAsync(listId, tmdbId.ToString(), ETag.All, ct);
    }
}
