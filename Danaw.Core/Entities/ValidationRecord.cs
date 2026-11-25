namespace Danaw.Core.Entities;

public class ValidationRecord
{
    public int ValidationRecordId { get; set; }

    public int ContentItemId { get; set; }
    public ContentItem? ContentItem { get; set; }

    public int ReviewerId { get; set; }
    public Reviewer? Reviewer { get; set; }

    public ValidationDecision Decision { get; set; }
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; }
}
