namespace MoviesApp.Api.Models;

/// <summary>A curated watchlist (e.g. "Summer 2026").</summary>
public sealed class MovieList
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public List<ListMovie> Movies { get; set; } = new();
}

/// <summary>Association of a TMDB movie with a list, plus curator metadata.</summary>
public sealed class ListMovie
{
    public int TmdbId { get; set; }
    public int Order { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset AddedUtc { get; set; }

    /// <summary>Card-level TMDB fields hydrated from the Blob cache.</summary>
    public MovieSummary? Summary { get; set; }
}
