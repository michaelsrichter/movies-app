using MoviesApp.Api.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddSingleton<StorageClientFactory>()
    .AddSingleton<ListRepository>()
    .AddSingleton<ListMovieRepository>()
    .AddSingleton<DiscussionRepository>()
    .AddSingleton<BlobCacheService>()
    .AddSingleton<MovieCacheService>();

builder.Services.AddHttpClient<TmdbClient>();
builder.Services.AddHttpClient<DiscussionGenerationService>();

builder.Build().Run();
