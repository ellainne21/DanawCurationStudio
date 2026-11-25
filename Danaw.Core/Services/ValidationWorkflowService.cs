using Danaw.Core.Abstractions;
using Danaw.Core.Common;
using Danaw.Core.Entities;
using Danaw.Core.Models;

namespace Danaw.Core.Services;

public class ValidationWorkflowService : IValidationWorkflowService
{
    private readonly IContentItemRepository _items;
    private readonly IReviewerRepository _reviewers;
    private readonly IValidationRecordRepository _records;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public ValidationWorkflowService(
        IContentItemRepository items,
        IReviewerRepository reviewers,
        IValidationRecordRepository records,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _items = items;
        _reviewers = reviewers;
        _records = records;
        _uow = uow;
        _time = time;
    }

    public async Task<OperationResult> RecordDecisionAsync(int reviewerId, DecisionInput input)
    {
        var errors = new List<string>();

        // Who is acting?
        var reviewer = await _reviewers.GetAsync(reviewerId);
        if (reviewer is null || !reviewer.IsActive)
            errors.Add("The selected user is not an active reviewer.");
        else if (reviewer.Role != ReviewerRole.Validator)
            errors.Add("Only a validator can record a decision.");

        // Revision requests and rejections must explain themselves.
        var comments = input.Comments?.Trim();
        if (input.Decision != ValidationDecision.Approved && string.IsNullOrEmpty(comments))
            errors.Add("A comment is required to request a revision or reject an item.");

        // The item must exist and still be waiting for a decision.
        var item = await _items.GetWithSourcesAsync(input.ContentItemId);
        if (item is null)
        {
            errors.Add("The content item could not be found.");
            return OperationResult.Fail(errors);
        }
        if (item.Status != ContentStatus.Pending)
            errors.Add("Only items with status Pending can receive a decision.");

        // Source checks must refer to this item's own sources, once each.
        var checks = input.SourceChecks ?? Array.Empty<SourceCheckInput>();
        var linkedIds = item.Sources.Select(l => l.SourceId).ToHashSet();
        foreach (var check in checks)
        {
            if (!linkedIds.Contains(check.SourceId))
                errors.Add($"Source {check.SourceId} does not belong to this item.");
        }
        if (checks.GroupBy(c => c.SourceId).Any(g => g.Count() > 1))
            errors.Add("Each source can be checked only once.");

        // Approval needs every source verified, counting the checks sent now.
        if (input.Decision == ValidationDecision.Approved)
        {
            if (item.Sources.Count == 0)
            {
                errors.Add("An item with no sources cannot be approved.");
            }
            else
            {
                var unverified = item.Sources
                    .Where(l => !(checks.FirstOrDefault(c => c.SourceId == l.SourceId)?.IsVerified ?? l.IsVerified))
                    .Select(l => l.Source?.Title ?? $"#{l.SourceId}")
                    .ToList();

                if (unverified.Count > 0)
                    errors.Add("Approval requires every source to be verified. Not verified: "
                               + string.Join(", ", unverified) + ".");
            }
        }

        // All checks happen BEFORE any change, so a rejected decision touches nothing.
        if (errors.Count > 0)
            return OperationResult.Fail(errors);

        var now = _time.GetUtcNow().UtcDateTime;

        foreach (var check in checks)
        {
            var link = item.Sources.First(l => l.SourceId == check.SourceId);
            link.IsVerified = check.IsVerified;
            link.CrossCheckNotes = check.CrossCheckNotes?.Trim();
            link.VerifiedAt = check.IsVerified ? now : null;
        }

        item.Status = input.Decision switch
        {
            ValidationDecision.Approved => ContentStatus.Validated,
            ValidationDecision.RevisionRequested => ContentStatus.NeedsRevision,
            ValidationDecision.Rejected => ContentStatus.Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(input), "Unknown decision.")
        };

        await _records.AddAsync(new ValidationRecord
        {
            ContentItemId = item.ContentItemId,
            ReviewerId = reviewerId,
            Decision = input.Decision,
            Comments = comments,
            DecidedAt = now
        });

        // ONE save: EF Core wraps the source checks, the status change and the
        // new record in a single transaction. All of it commits, or none of it.
        await _uow.SaveChangesAsync();

        return OperationResult.Ok();
    }
}