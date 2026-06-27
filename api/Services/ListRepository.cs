using Azure;
using Azure.Data.Tables;
using MoviesApp.Api.Models;

namespace MoviesApp.Api.Services;

/// <summary>Reads/writes list metadata in the `Lists` table.</summary>
public sealed class ListRepository
{
    private const string TableName = "Lists";
    private const string Partition = "list";
    private readonly StorageClientFactory _factory;

    public ListRepository(StorageClientFactory factory) => _factory = factory;

    private TableClient Table()
    {
        var table = _factory.GetTableClient(TableName);
        table.CreateIfNotExists();
        return table;
    }

    public async Task<MovieList?> GetActiveAsync(CancellationToken ct)
    {
        var table = Table();
        await foreach (var e in table.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{Partition}' and IsActive eq true",
            cancellationToken: ct))
        {
            return Map(e);
        }

        return null;
    }

    public async Task<IReadOnlyList<MovieList>> GetAllAsync(CancellationToken ct)
    {
        var table = Table();
        var result = new List<MovieList>();
        await foreach (var e in table.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{Partition}'", cancellationToken: ct))
        {
            result.Add(Map(e));
        }

        return result.OrderBy(l => l.SortOrder).ToList();
    }

    public async Task UpsertAsync(MovieList list, CancellationToken ct)
    {
        var table = Table();
        var entity = new TableEntity(Partition, list.Id)
        {
            ["Title"] = list.Title,
            ["Slug"] = list.Slug,
            ["Period"] = list.Period,
            ["IsActive"] = list.IsActive,
            ["SortOrder"] = list.SortOrder,
            ["CreatedUtc"] = list.CreatedUtc,
        };
        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
    }

    public async Task DeleteAsync(string listId, CancellationToken ct)
    {
        var table = Table();
        await table.DeleteEntityAsync(Partition, listId, ETag.All, ct);
    }

    private static MovieList Map(TableEntity e) => new()
    {
        Id = e.RowKey,
        Title = e.GetString("Title") ?? string.Empty,
        Slug = e.GetString("Slug") ?? string.Empty,
        Period = e.GetString("Period") ?? string.Empty,
        IsActive = e.GetBoolean("IsActive") ?? false,
        SortOrder = e.GetInt32("SortOrder") ?? 0,
        CreatedUtc = e.GetDateTimeOffset("CreatedUtc") ?? DateTimeOffset.UtcNow,
    };
}
