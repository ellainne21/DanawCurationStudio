namespace Danaw.Core.Entities;

public class ContentItem
{
    public int ContentItemId { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int CreatedByReviewerId { get; set; }
    public Reviewer? CreatedBy { get; set; }

    public int? AssignedReviewerId { get; set; }
    public Reviewer? AssignedReviewer { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? CulturalDomain { get; set; }
    public string Description { get; set; } = string.Empty;

    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    public ICollection<ContentItemSource> Sources { get; set; } = new List<ContentItemSource>();
    public ICollection<ValidationRecord> ValidationRecords { get; set; } = new List<ValidationRecord>();
}
