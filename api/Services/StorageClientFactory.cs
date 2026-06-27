using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace MoviesApp.Api.Services;

/// <summary>
/// Creates Table/Blob clients. In Azure (Fork A) a connection string is used; locally, when no
/// connection string is present, DefaultAzureCredential resolves the developer's `az login`
/// (AzureCliCredential) against the account endpoint — so account keys never sit on the dev box.
/// </summary>
public sealed class StorageClientFactory
{
    private readonly string? _connectionString;
    private readonly string? _accountName;

    public StorageClientFactory(IConfiguration config)
    {
        _connectionString = config["Storage:ConnectionString"];
        _accountName = config["Storage:AccountName"];

        if (string.IsNullOrWhiteSpace(_connectionString) && string.IsNullOrWhiteSpace(_accountName))
        {
            throw new InvalidOperationException(
                "Configure either Storage__ConnectionString or Storage__AccountName.");
        }
    }

    private bool UseConnectionString => !string.IsNullOrWhiteSpace(_connectionString);

    public TableClient GetTableClient(string tableName)
    {
        if (UseConnectionString)
        {
            return new TableClient(_connectionString, tableName);
        }

        var uri = new Uri($"https://{_accountName}.table.core.windows.net");
        return new TableClient(uri, tableName, new DefaultAzureCredential());
    }

    public BlobContainerClient GetBlobContainerClient(string containerName)
    {
        if (UseConnectionString)
        {
            return new BlobContainerClient(_connectionString, containerName);
        }

        var uri = new Uri($"https://{_accountName}.blob.core.windows.net/{containerName}");
        return new BlobContainerClient(uri, new DefaultAzureCredential());
    }
}
