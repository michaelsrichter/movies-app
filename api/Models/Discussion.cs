namespace MoviesApp.Api.Models;

/// <summary>Admin-approved family discussion topics for a movie.</summary>
public sealed class Discussion
{
    public int TmdbId { get; set; }
    public List<DiscussionTopic> Topics { get; set; } = new();

    /// <summary>"ai" or "manual".</summary>
    public string Source { get; set; } = "ai";

    /// <summary>"draft" or "published". Public site renders "published" only.</summary>
    public string Status { get; set; } = "draft";

    public string? Model { get; set; }
    public DateTimeOffset? GeneratedUtc { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedUtc { get; set; }
}

public sealed class DiscussionTopic
{
    public string Heading { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    /// <summary>themes | ethics | history | character | conversation-starter.</summary>
    public string Category { get; set; } = "conversation-starter";
}
