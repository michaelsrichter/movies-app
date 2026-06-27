using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using MoviesApp.Api.Models;

namespace MoviesApp.Api.Services;

/// <summary>Reads/writes admin-approved discussion topics in the `Discussions` table.</summary>
public sealed class DiscussionRepository
{
    private const string TableName = "Discussions";
    private const string RowKey = "current";
    private readonly StorageClientFactory _factory;

    public DiscussionRepository(StorageClientFactory factory) => _factory = factory;

    private TableClient Table()
    {
        var table = _factory.GetTableClient(TableName);
        table.CreateIfNotExists();
        return table;
    }

    public async Task<Discussion?> GetAsync(int tmdbId, CancellationToken ct)
    {
        var table = Table();
        try
        {
            var e = await table.GetEntityAsync<TableEntity>(tmdbId.ToString(), RowKey, cancellationToken: ct);
            return Map(e.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task UpsertAsync(Discussion discussion, CancellationToken ct)
    {
        var table = Table();
        var entity = new TableEntity(discussion.TmdbId.ToString(), RowKey)
        {
            ["TopicsJson"] = JsonSerializer.Serialize(discussion.Topics),
            ["Source"] = discussion.Source,
            ["Status"] = discussion.Status,
            ["Model"] = discussion.Model,
            ["GeneratedUtc"] = discussion.GeneratedUtc,
            ["ApprovedBy"] = discussion.ApprovedBy,
            ["ApprovedUtc"] = discussion.ApprovedUtc,
        };
        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
    }

    private static Discussion Map(TableEntity e) => new()
    {
        TmdbId = int.TryParse(e.PartitionKey, out var id) ? id : 0,
        Topics = JsonSerializer.Deserialize<List<DiscussionTopic>>(e.GetString("TopicsJson") ?? "[]") ?? new(),
        Source = e.GetString("Source") ?? "ai",
        Status = e.GetString("Status") ?? "draft",
        Model = e.GetString("Model"),
        GeneratedUtc = e.GetDateTimeOffset("GeneratedUtc"),
        ApprovedBy = e.GetString("ApprovedBy"),
        ApprovedUtc = e.GetDateTimeOffset("ApprovedUtc"),
    };
}
