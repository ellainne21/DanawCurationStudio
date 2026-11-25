using Danaw.Core.Abstractions;
using Danaw.Core.Common;
using Danaw.Core.Entities;
using Danaw.Core.Models;

namespace Danaw.Core.Services;

public class ContentCurationService : IContentCurationService
{
    private readonly IContentItemRepository _items;
    private readonly IReviewerRepository _reviewers;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public ContentCurationService(
        IContentItemRepository items,
        IReviewerRepository reviewers,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _items = items;
        _reviewers = reviewers;
        _uow = uow;
        _time = time;
    }

    public Task<OperationResult<int>> SaveDraftAsync(int reviewerId, ContentItemInput input)
        => SaveAsync(reviewerId, input, submit: false);

    public Task<OperationResult<int>> SubmitAsync(int reviewerId, ContentItemInput input)
        => SaveAsync(reviewerId, input, submit: true);

    private async Task<OperationResult<int>> SaveAsync(int reviewerId, ContentItemInput input, bool submit)
    {
        var errors = new List<string>();

        // Who is acting?
        var reviewer = await _reviewers.GetAsync(reviewerId);
        if (reviewer is null || !reviewer.IsActive)
            errors.Add("The selected user is not an active reviewer.");
        else if (reviewer.Role != ReviewerRole.Researcher)
            errors.Add("Only a researcher can create or submit content items.");

        // Field rules (limits come from the ERD).
        var title = (input.Title ?? string.Empty).Trim();
        var description = (input.Description ?? string.Empty).Trim();
        var domain = input.CulturalDomain?.Trim();

        if (title.Length == 0) errors.Add("Title is required.");
        else if (title.Length > 150) errors.Add("Title must be 150 characters or fewer.");

        if (domain is { Length: > 30 }) errors.Add("Cultural domain must be 30 characters or fewer.");

        var categoryOk = await _items.CategoryExistsAsync(input.CategoryId);
        if (!categoryOk) errors.Add("Select a valid category.");

        if (submit && description.Length == 0) errors.Add("Description is required to submit.");

        // Source rules: each source must be complete; submitting needs at least one.
        var sources = input.Sources ?? Array.Empty<SourceInput>();
        for (var i = 0; i < sources.Count; i++)
        {
            var s = sources[i];
            var n = i + 1;
            if (string.IsNullOrWhiteSpace(s.Title)) errors.Add($"Source {n}: title is required.");
            else if (s.Title.Trim().Length > 200) errors.Add($"Source {n}: title must be 200 characters or fewer.");

            if (string.IsNullOrWhiteSpace(s.Reference)) errors.Add($"Source {n}: reference is required.");
            else if (s.Reference.Trim().Length > 150) errors.Add($"Source {n}: reference must be 150 characters or fewer.");
        }

        if (submit && sources.Count == 0)
            errors.Add("At least one source citation is required before this item can be submitted.");

        // Duplicate title within the same category.
        if (title.Length > 0 && categoryOk &&
            await _items.TitleExistsAsync(input.CategoryId, title, input.ContentItemId))
            errors.Add("An item with this title already exists in the selected category.");

        // Editing an existing item: only its creator may change it, and only while
        // it is a Draft or Needs Revision.
        ContentItem? existing = null;
        if (input.ContentItemId is int id)
        {
            existing = await _items.GetWithSourcesAsync(id);
            if (existing is null)
                errors.Add("The content item could not be found.");
            else if (existing.CreatedByReviewerId != reviewerId)
                errors.Add("Only the researcher who created this item can edit or submit it.");
            else if (existing.Status != ContentStatus.Draft && existing.Status != ContentStatus.NeedsRevision)
                errors.Add("Only Draft or Needs Revision items can be edited or submitted.");
        }

        // Nothing has been written yet, so a failed check leaves the data untouched.
        if (errors.Count > 0)
            return OperationResult<int>.Fail(errors);

        var now = _time.GetUtcNow().UtcDateTime;
        ContentItem item;
        if (existing is null)
        {
            item = new ContentItem
            {
                CreatedByReviewerId = reviewerId,
                CreatedAt = now,
                Status = ContentStatus.Draft
            };
        }
        else
        {
            item = existing;
            _items.RemoveSources(item); // replaced below
        }

        item.CategoryId = input.CategoryId;
        item.Title = title;
        item.CulturalDomain = domain;
        item.Description = description;

        foreach (var s in sources)
        {
            item.Sources.Add(new ContentItemSource
            {
                IsVerified = false, // only Workflow 2 may verify
                Source = new Source
                {
                    Title = s.Title.Trim(),
                    SourceType = s.SourceType?.Trim(),
                    Publisher = s.Publisher?.Trim(),
                    Reference = s.Reference.Trim(),
                    YearPublished = s.YearPublished
                }
            });
        }

        if (submit)
        {
            item.Status = ContentStatus.Pending;
            item.SubmittedAt = now;
        }

        if (existing is null) await _items.AddAsync(item);
        await _uow.SaveChangesAsync();

        return OperationResult<int>.Ok(item.ContentItemId);
    }
}