namespace Danaw.Core.Entities;

public class Source
{
    public int SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public string? Publisher { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int? YearPublished { get; set; }

    public ICollection<ContentItemSource> ContentItems { get; set; } = new List<ContentItemSource>();
}
