using System.Text.Json;
using MoviesApp.Api.Models;

namespace MoviesApp.Api.Services;

/// <summary>Maps a raw TMDB movie payload (details + appended endpoints) into our greedy MovieDetail.</summary>
public static class TmdbMapper
{
    public static MovieDetail Map(JsonElement root)
    {
        var detail = new MovieDetail
        {
            TmdbId = root.GetIntOrDefault("id"),
            ImdbId = root.GetStringOrNull("imdb_id"),
            Title = root.GetStringOrNull("title") ?? string.Empty,
            OriginalTitle = root.GetStringOrNull("original_title"),
            OriginalLanguage = root.GetStringOrNull("original_language"),
            Overview = root.GetStringOrNull("overview"),
            Tagline = root.GetStringOrNull("tagline"),
            Status = root.GetStringOrNull("status"),
            Homepage = root.GetStringOrNull("homepage"),
            Adult = root.GetBoolOrDefault("adult"),
            Video = root.GetBoolOrDefault("video"),
            ReleaseDate = root.GetStringOrNull("release_date"),
            Runtime = root.GetNullableInt("runtime"),
            PosterPath = root.GetStringOrNull("poster_path"),
            BackdropPath = root.GetStringOrNull("backdrop_path"),
            VoteAverage = root.GetDoubleOrDefault("vote_average"),
            VoteCount = root.GetIntOrDefault("vote_count"),
            Popularity = root.GetDoubleOrDefault("popularity"),
            Budget = root.GetNullableLong("budget"),
            Revenue = root.GetNullableLong("revenue"),
            LastFetchedUtc = DateTimeOffset.UtcNow,
            Raw = root.Clone(),
        };

        var rd = detail.ReleaseDate;
        if (!string.IsNullOrEmpty(rd) && rd.Length >= 4 && int.TryParse(rd[..4], out var year))
        {
            detail.Year = year;
        }

        // Genres
        if (root.TryGetProperty("genres", out var genres) && genres.ValueKind == JsonValueKind.Array)
        {
            foreach (var g in genres.EnumerateArray())
            {
                var name = g.GetStringOrNull("name");
                if (name is null) continue;
                detail.GenreRefs.Add(new NamedRef { Id = g.GetIntOrDefault("id"), Name = name });
                detail.Genres.Add(name);
            }
        }

        // Spoken languages
        if (root.TryGetProperty("spoken_languages", out var langs) && langs.ValueKind == JsonValueKind.Array)
        {
            foreach (var l in langs.EnumerateArray())
            {
                detail.SpokenLanguages.Add(new SpokenLanguage
                {
                    Iso639_1 = l.GetStringOrNull("iso_639_1"),
                    Name = l.GetStringOrNull("name") ?? string.Empty,
                    EnglishName = l.GetStringOrNull("english_name"),
                });
            }
        }

        // Production companies / countries
        if (root.TryGetProperty("production_companies", out var pc) && pc.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in pc.EnumerateArray())
            {
                detail.ProductionCompanies.Add(new ProductionCompany
                {
                    Id = c.GetIntOrDefault("id"),
                    Name = c.GetStringOrNull("name") ?? string.Empty,
                    LogoPath = c.GetStringOrNull("logo_path"),
                    OriginCountry = c.GetStringOrNull("origin_country"),
                });
            }
        }

        if (root.TryGetProperty("production_countries", out var pcountry) && pcountry.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in pcountry.EnumerateArray())
            {
                detail.ProductionCountries.Add(new NamedRef
                {
                    Name = c.GetStringOrNull("name") ?? string.Empty,
                });
            }
        }

        // Collection
        if (root.TryGetProperty("belongs_to_collection", out var coll) && coll.ValueKind == JsonValueKind.Object)
        {
            detail.BelongsToCollection = new CollectionRef
            {
                Id = coll.GetIntOrDefault("id"),
                Name = coll.GetStringOrNull("name") ?? string.Empty,
                PosterPath = coll.GetStringOrNull("poster_path"),
                BackdropPath = coll.GetStringOrNull("backdrop_path"),
            };
        }

        // Credits
        if (root.TryGetProperty("credits", out var credits) && credits.ValueKind == JsonValueKind.Object)
        {
            if (credits.TryGetProperty("cast", out var cast) && cast.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in cast.EnumerateArray())
                {
                    detail.Cast.Add(new CastMember
                    {
                        Id = c.GetIntOrDefault("id"),
                        Name = c.GetStringOrNull("name") ?? string.Empty,
                        OriginalName = c.GetStringOrNull("original_name"),
                        Character = c.GetStringOrNull("character"),
                        Order = c.GetIntOrDefault("order"),
                        ProfilePath = c.GetStringOrNull("profile_path"),
                        Gender = c.GetNullableInt("gender"),
                        Popularity = c.GetDoubleOrDefault("popularity"),
                        KnownForDepartment = c.GetStringOrNull("known_for_department"),
                        CastId = c.GetNullableInt("cast_id"),
                        CreditId = c.GetStringOrNull("credit_id"),
                    });
                }
            }

            if (credits.TryGetProperty("crew", out var crew) && crew.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in crew.EnumerateArray())
                {
                    detail.Crew.Add(new CrewMember
                    {
                        Id = c.GetIntOrDefault("id"),
                        Name = c.GetStringOrNull("name") ?? string.Empty,
                        Job = c.GetStringOrNull("job") ?? string.Empty,
                        Department = c.GetStringOrNull("department"),
                        ProfilePath = c.GetStringOrNull("profile_path"),
                        Gender = c.GetNullableInt("gender"),
                        CreditId = c.GetStringOrNull("credit_id"),
                    });
                }
            }
        }

        // Keywords
        if (root.TryGetProperty("keywords", out var kw) && kw.ValueKind == JsonValueKind.Object &&
            kw.TryGetProperty("keywords", out var kwArr) && kwArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var k in kwArr.EnumerateArray())
            {
                var name = k.GetStringOrNull("name");
                if (name is null) continue;
                detail.KeywordRefs.Add(new NamedRef { Id = k.GetIntOrDefault("id"), Name = name });
                detail.Keywords.Add(name);
            }
        }

        // US certification + release dates
        if (root.TryGetProperty("release_dates", out var relDates) &&
            relDates.TryGetProperty("results", out var relResults) && relResults.ValueKind == JsonValueKind.Array)
        {
            foreach (var country in relResults.EnumerateArray())
            {
                if (country.GetStringOrNull("iso_3166_1") != "US") continue;
                if (!country.TryGetProperty("release_dates", out var dates) || dates.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var d in dates.EnumerateArray())
                {
                    var info = new ReleaseDateInfo
                    {
                        Certification = d.GetStringOrNull("certification"),
                        ReleaseDate = d.GetStringOrNull("release_date"),
                        Type = d.GetIntOrDefault("type"),
                        Note = d.GetStringOrNull("note"),
                    };
                    detail.UsReleaseDates.Add(info);
                }
            }

            // Prefer theatrical (type 3) certification, else first non-empty.
            detail.Certification = detail.UsReleaseDates
                .Where(x => !string.IsNullOrWhiteSpace(x.Certification))
                .OrderByDescending(x => x.Type == 3)
                .Select(x => x.Certification)
                .FirstOrDefault();
        }

        // US watch providers
        if (root.TryGetProperty("watch/providers", out var wp) &&
            wp.TryGetProperty("results", out var wpResults) &&
            wpResults.TryGetProperty("US", out var us))
        {
            detail.ProvidersLink = us.GetStringOrNull("link");
            foreach (var type in new[] { "flatrate", "free", "ads", "rent", "buy" })
            {
                if (!us.TryGetProperty(type, out var arr) || arr.ValueKind != JsonValueKind.Array) continue;
                foreach (var p in arr.EnumerateArray())
                {
                    detail.Providers.Add(new WatchProvider
                    {
                        ProviderId = p.GetIntOrDefault("provider_id"),
                        Name = p.GetStringOrNull("provider_name") ?? string.Empty,
                        LogoPath = p.GetStringOrNull("logo_path"),
                        DisplayPriority = p.GetNullableInt("display_priority"),
                        Type = type,
                    });
                }
            }
        }

        return detail;
    }
}

internal static class JsonElementExtensions
{
    public static string? GetStringOrNull(this JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    public static int GetIntOrDefault(this JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : 0;

    public static int? GetNullableInt(this JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : null;

    public static long? GetNullableLong(this JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var l) ? l : null;

    public static double GetDoubleOrDefault(this JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) ? d : 0;

    public static bool GetBoolOrDefault(this JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) && v.GetBoolean();
}
