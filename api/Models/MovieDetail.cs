using System.Text.Json;

namespace MoviesApp.Api.Models;

/// <summary>Lightweight movie fields needed to render a list card.</summary>
public sealed class MovieSummary
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int? Runtime { get; set; }
    public string? Certification { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? PosterBlurDataUrl { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<WatchProvider> Providers { get; set; } = new();
}

/// <summary>
/// Full normalized TMDB payload cached in Blob storage. We capture greedily — many fields are stored
/// even if the app does not surface them yet — so re-fetches are rare and future features need no
/// schema migration. The complete untouched TMDB response is also retained in <see cref="Raw"/>.
/// </summary>
public sealed class MovieDetail
{
    // Identity
    public int TmdbId { get; set; }
    public string? ImdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? OriginalLanguage { get; set; }

    // Descriptive
    public string? Overview { get; set; }
    public string? Tagline { get; set; }
    public string? Status { get; set; }
    public string? Homepage { get; set; }
    public bool Adult { get; set; }
    public bool Video { get; set; }

    // Release
    public string? ReleaseDate { get; set; }
    public int? Year { get; set; }
    public int? Runtime { get; set; }
    public string? Certification { get; set; }
    /// <summary>All US release dates/types (premiere, theatrical, digital, physical, TV).</summary>
    public List<ReleaseDateInfo> UsReleaseDates { get; set; } = new();

    // Imagery
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? PosterBlurDataUrl { get; set; }

    // Ratings / popularity
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public double Popularity { get; set; }

    // Financials
    public long? Budget { get; set; }
    public long? Revenue { get; set; }

    // Taxonomy
    public List<NamedRef> GenreRefs { get; set; } = new();
    public List<string> Genres { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public List<NamedRef> KeywordRefs { get; set; } = new();
    public List<SpokenLanguage> SpokenLanguages { get; set; } = new();
    public List<ProductionCompany> ProductionCompanies { get; set; } = new();
    public List<NamedRef> ProductionCountries { get; set; } = new();
    public CollectionRef? BelongsToCollection { get; set; }

    // People
    public List<CastMember> Cast { get; set; } = new();
    public List<CrewMember> Crew { get; set; } = new();

    // Streaming (US)
    public List<WatchProvider> Providers { get; set; } = new();
    public string? ProvidersLink { get; set; }

    // Cache audit
    public string? ETag { get; set; }
    public DateTimeOffset LastFetchedUtc { get; set; }

    /// <summary>The complete raw TMDB response (details + appended endpoints), retained verbatim.</summary>
    public JsonElement? Raw { get; set; }
}

public sealed class NamedRef
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class CollectionRef
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
}

public sealed class SpokenLanguage
{
    public string? Iso639_1 { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EnglishName { get; set; }
}

public sealed class ProductionCompany
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? OriginCountry { get; set; }
}

public sealed class ReleaseDateInfo
{
    public string? Certification { get; set; }
    public string? ReleaseDate { get; set; }
    /// <summary>1=Premiere,2=Theatrical(limited),3=Theatrical,4=Digital,5=Physical,6=TV.</summary>
    public int Type { get; set; }
    public string? Note { get; set; }
}

public sealed class CastMember
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OriginalName { get; set; }
    public string? Character { get; set; }
    public int Order { get; set; }
    public string? ProfilePath { get; set; }
    public int? Gender { get; set; }
    public double Popularity { get; set; }
    public string? KnownForDepartment { get; set; }
    public int? CastId { get; set; }
    public string? CreditId { get; set; }
}

public sealed class CrewMember
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? ProfilePath { get; set; }
    public int? Gender { get; set; }
    public string? CreditId { get; set; }
}

public sealed class WatchProvider
{
    public int ProviderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public int? DisplayPriority { get; set; }
    /// <summary>One of: flatrate, rent, buy, free, ads.</summary>
    public string Type { get; set; } = "flatrate";
}
