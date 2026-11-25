using Danaw.Core.Entities;
using Danaw.Core.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Danaw.Tests;

public class CreatorOwnershipTests
{
    private const int CreatorId = 1;      // Balindong
    private const int OtherResearcherId = 2; // Balofinos
    private const int ValidatorId = 4;

    private static SourceInput Source(string title) => new(title, "Institutional", "BCPCH", "Ref " + title, 2023);

    private static ContentItemInput Input(int? id, string title, string description, params SourceInput[] sources)
        => new(id, 1, title, "Arts", description, sources);

    [Fact]
    public async Task Another_researcher_cannot_edit_someone_elses_draft_but_the_creator_still_can()
    {
        using var db = new TestDb();
        int id;

        using (var ctx = db.NewContext())
        {
            var draft = await db.NewCurationService(ctx).SaveDraftAsync(CreatorId,
                Input(null, "Okir Motifs", "Original description.", Source("Original source")));
            Assert.True(draft.Success, string.Join("; ", draft.Errors));
            id = draft.Value;
        }

        using (var ctx = db.NewContext())
        {
            var attempt = await db.NewCurationService(ctx).SaveDraftAsync(OtherResearcherId,
                Input(id, "Hijacked Title", "Changed by someone else.", Source("Other source")));

            Assert.False(attempt.Success);
            Assert.Contains(attempt.Errors, e => e.Contains("created this item", StringComparison.OrdinalIgnoreCase));
        }

        // Nothing was changed by the failed attempt.
        using (var check = db.NewContext())
        {
            var item = await check.ContentItems
                .Include(c => c.Sources).ThenInclude(l => l.Source)
                .SingleAsync(c => c.ContentItemId == id);

            Assert.Equal("Okir Motifs", item.Title);
            Assert.Equal("Original description.", item.Description);
            Assert.Single(item.Sources);
            Assert.Equal("Original source", item.Sources.Single().Source!.Title);
        }

        // The creator can still edit and submit it.
        using (var ctx = db.NewContext())
        {
            var own = await db.NewCurationService(ctx).SubmitAsync(CreatorId,
                Input(id, "Okir Motifs", "Updated by the creator.", Source("Original source")));
            Assert.True(own.Success, string.Join("; ", own.Errors));
        }

        using var end = db.NewContext();
        Assert.Equal(ContentStatus.Pending,
            (await end.ContentItems.SingleAsync(c => c.ContentItemId == id)).Status);
    }

    [Fact]
    public async Task Another_researcher_cannot_resubmit_an_item_that_was_sent_back_for_revision()
    {
        using var db = new TestDb();
        int id;

        using (var ctx = db.NewContext())
        {
            var submitted = await db.NewCurationService(ctx).SubmitAsync(CreatorId,
                Input(null, "Kulintang Ensemble", "Description.", Source("BCPCH profile")));
            Assert.True(submitted.Success, string.Join("; ", submitted.Errors));
            id = submitted.Value;
        }

        using (var ctx = db.NewContext())
        {
            var sourceId = await ctx.ContentItemSources
                .Where(l => l.ContentItemId == id).Select(l => l.SourceId).SingleAsync();

            var sentBack = await db.NewValidationService(ctx).RecordDecisionAsync(ValidatorId,
                new DecisionInput(id, ValidationDecision.RevisionRequested, "Add a second source.",
                    new[] { new SourceCheckInput(sourceId, true, "Confirmed.") }));
            Assert.True(sentBack.Success, string.Join("; ", sentBack.Errors));
        }

        using (var ctx = db.NewContext())
        {
            var attempt = await db.NewCurationService(ctx).SubmitAsync(OtherResearcherId,
                Input(id, "Kulintang Ensemble", "Description.", Source("BCPCH profile"), Source("Second source")));

            Assert.False(attempt.Success);
            Assert.Contains(attempt.Errors, e => e.Contains("created this item", StringComparison.OrdinalIgnoreCase));
        }

        using var check = db.NewContext();
        var item = await check.ContentItems.Include(c => c.Sources).SingleAsync(c => c.ContentItemId == id);
        Assert.Equal(ContentStatus.NeedsRevision, item.Status);
        Assert.Single(item.Sources);
    }
}