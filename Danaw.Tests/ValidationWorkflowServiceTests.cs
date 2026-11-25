using Danaw.Core.Entities;
using Danaw.Core.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Danaw.Tests;

public class ValidationWorkflowServiceTests
{
    private const int ResearcherId = 1;
    private const int ValidatorId = 4;

    // Arrange helper: a Pending item with two unverified sources, made through Workflow 1.
    private static async Task<(int ItemId, int[] SourceIds)> CreatePendingItemAsync(TestDb db)
    {
        using var ctx = db.NewContext();
        var result = await db.NewCurationService(ctx).SubmitAsync(ResearcherId, new ContentItemInput(
            null, 1, "Okir Motif Origins", "Arts", "Origins and naming of okir motifs.",
            new[]
            {
                new SourceInput("BCPCH Meranaw heritage profile, 2023", "Report", "BCPCH", "BCPCH 2023", 2023),
                new SourceInput("NCCA registry entry", "Registry", "NCCA", "TALAPAMANA #4471", null)
            }));
        Assert.True(result.Success, string.Join("; ", result.Errors));

        var ids = await ctx.ContentItemSources
            .Where(l => l.ContentItemId == result.Value)
            .Select(l => l.SourceId)
            .ToArrayAsync();
        return (result.Value, ids);
    }

    private static SourceCheckInput[] Checks(int[] ids, params bool[] verified)
        => ids.Select((id, i) => new SourceCheckInput(
               id, verified[i], verified[i] ? "Matches the cited record." : "Could not confirm."))
           .ToArray();

    // Scenario 3: Workflow 2 success and persistence
    [Fact]
    public async Task Approve_with_all_sources_verified_commits_status_record_and_checks()
    {
        using var db = new TestDb();
        var (itemId, ids) = await CreatePendingItemAsync(db);

        using (var ctx = db.NewContext())
        {
            var result = await db.NewValidationService(ctx).RecordDecisionAsync(ValidatorId,
                new DecisionInput(itemId, ValidationDecision.Approved, "Sources check out.",
                                  Checks(ids, true, true)));
            Assert.True(result.Success, string.Join("; ", result.Errors));
        }

        using var check = db.NewContext();
        var item = await check.ContentItems.Include(c => c.Sources).SingleAsync(c => c.ContentItemId == itemId);
        var records = await check.ValidationRecords.Where(r => r.ContentItemId == itemId).ToListAsync();

        Assert.Equal(ContentStatus.Validated, item.Status);
        Assert.Single(records);
        Assert.Equal(ValidationDecision.Approved, records[0].Decision);
        Assert.Equal(ValidatorId, records[0].ReviewerId);
        Assert.Equal("Sources check out.", records[0].Comments);
        Assert.All(item.Sources, l =>
        {
            Assert.True(l.IsVerified);
            Assert.NotNull(l.VerifiedAt);
            Assert.Equal("Matches the cited record.", l.CrossCheckNotes);
        });
    }

    // Scenario 5: approve with an unverified source is rejected, nothing changes
    [Fact]
    public async Task Approve_with_an_unverified_source_is_rejected_and_nothing_changes()
    {
        using var db = new TestDb();
        var (itemId, ids) = await CreatePendingItemAsync(db);

        using (var ctx = db.NewContext())
        {
            var result = await db.NewValidationService(ctx).RecordDecisionAsync(ValidatorId,
                new DecisionInput(itemId, ValidationDecision.Approved, "Looks fine.",
                                  Checks(ids, true, false)));

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Contains("verified", StringComparison.OrdinalIgnoreCase));
        }

        using var check = db.NewContext();
        var item = await check.ContentItems.Include(c => c.Sources).SingleAsync(c => c.ContentItemId == itemId);

        Assert.Equal(ContentStatus.Pending, item.Status);
        Assert.Equal(0, await check.ValidationRecords.CountAsync());
        Assert.All(item.Sources, l =>
        {
            Assert.False(l.IsVerified);
            Assert.Null(l.VerifiedAt);
            Assert.Null(l.CrossCheckNotes);
        });
    }

    // Scenario 7: a second decision on an already-decided item is refused
    [Fact]
    public async Task Second_decision_on_an_already_decided_item_is_rejected()
    {
        using var db = new TestDb();
        var (itemId, ids) = await CreatePendingItemAsync(db);

        using (var ctx = db.NewContext())
        {
            var first = await db.NewValidationService(ctx).RecordDecisionAsync(ValidatorId,
                new DecisionInput(itemId, ValidationDecision.Approved, "OK.", Checks(ids, true, true)));
            Assert.True(first.Success);
        }

        using (var ctx = db.NewContext())
        {
            var second = await db.NewValidationService(ctx).RecordDecisionAsync(ValidatorId,
                new DecisionInput(itemId, ValidationDecision.Rejected, "Changed my mind.", Array.Empty<SourceCheckInput>()));

            Assert.False(second.Success);
            Assert.Contains(second.Errors, e => e.Contains("Pending", StringComparison.OrdinalIgnoreCase));
        }

        using var check = db.NewContext();
        Assert.Equal(1, await check.ValidationRecords.CountAsync());
        Assert.Equal(ContentStatus.Validated,
            (await check.ContentItems.SingleAsync(c => c.ContentItemId == itemId)).Status);
    }

    // Scenario 7 variant: revision request needs a comment
    [Fact]
    public async Task Request_revision_without_comment_is_rejected()
    {
        using var db = new TestDb();
        var (itemId, ids) = await CreatePendingItemAsync(db);

        using (var ctx = db.NewContext())
        {
            var result = await db.NewValidationService(ctx).RecordDecisionAsync(ValidatorId,
                new DecisionInput(itemId, ValidationDecision.RevisionRequested, "   ", Checks(ids, true, false)));

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Contains("comment", StringComparison.OrdinalIgnoreCase));
        }

        using var check = db.NewContext();
        Assert.Equal(ContentStatus.Pending,
            (await check.ContentItems.SingleAsync(c => c.ContentItemId == itemId)).Status);
        Assert.Equal(0, await check.ValidationRecords.CountAsync());
    }

    // Request revision with a comment succeeds and keeps the validator's source notes
    [Fact]
    public async Task Request_revision_with_comment_sets_needs_revision_and_keeps_notes()
    {
        using var db = new TestDb();
        var (itemId, ids) = await CreatePendingItemAsync(db);

        using (var ctx = db.NewContext())
        {
            var result = await db.NewValidationService(ctx).RecordDecisionAsync(ValidatorId,
                new DecisionInput(itemId, ValidationDecision.RevisionRequested,
                                  "List alternate local terms.", Checks(ids, true, false)));
            Assert.True(result.Success, string.Join("; ", result.Errors));
        }

        using var check = db.NewContext();
        var item = await check.ContentItems.Include(c => c.Sources).SingleAsync(c => c.ContentItemId == itemId);

        Assert.Equal(ContentStatus.NeedsRevision, item.Status);
        Assert.Equal(1, await check.ValidationRecords.CountAsync());
        Assert.Equal(1, item.Sources.Count(l => l.IsVerified));
        Assert.Contains(item.Sources, l => l.CrossCheckNotes == "Could not confirm.");
    }

    // Role rule: only validators decide
    [Fact]
    public async Task Researcher_cannot_record_a_decision()
    {
        using var db = new TestDb();
        var (itemId, ids) = await CreatePendingItemAsync(db);

        using (var ctx = db.NewContext())
        {
            var result = await db.NewValidationService(ctx).RecordDecisionAsync(ResearcherId,
                new DecisionInput(itemId, ValidationDecision.Approved, "Self-approval.", Checks(ids, true, true)));
            Assert.False(result.Success);
        }

        using var check = db.NewContext();
        Assert.Equal(ContentStatus.Pending,
            (await check.ContentItems.SingleAsync(c => c.ContentItemId == itemId)).Status);
        Assert.Equal(0, await check.ValidationRecords.CountAsync());
    }
}