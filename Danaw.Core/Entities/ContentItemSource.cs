namespace Danaw.Core.Entities;

// Join entity for the many-to-many link between ContentItem and Source.
public class ContentItemSource
{
    public int ContentItemId { get; set; }
    public ContentItem? ContentItem { get; set; }

    public int SourceId { get; set; }
    public Source? Source { get; set; }

    public bool IsVerified { get; set; }
    public string? CrossCheckNotes { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
