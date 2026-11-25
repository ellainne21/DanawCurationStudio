namespace Danaw.Core.Models;

public record SourceInput(
    string Title,
    string? SourceType,
    string? Publisher,
    string Reference,
    int? YearPublished);

public record ContentItemInput(
    int? ContentItemId,
    int CategoryId,
    string Title,
    string? CulturalDomain,
    string Description,
    IReadOnlyList<SourceInput> Sources);