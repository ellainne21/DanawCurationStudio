using Danaw.Core.Entities;
using Danaw.Core.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Danaw.Tests;

public class EditAndResubmitTests
{
    private const int ResearcherId = 1;
    private const int ValidatorId = 4;

    private static ContentItemInput Input(int? id, string title, params SourceInput[] sources)
        => new(id, 1, title, "Arts", "A description.", sources);

    [Fact]
    public async Task Editing_a_draft_replaces_its_sources_and_submit_makes_it_pending()
    {
        using var db = new TestDb();
        int id;

        using (var ctx = db.NewContext())
        {
            var draft = await db.NewCurationService(ctx).SaveDraftAsync(ResearcherId,
                Input(null, "Okir Motifs", new SourceInput("Old source", null, null, "Old ref", null)));
            Assert.True(draft.Success, string.Join("; ", draft.Errors));
            id = draft.Value;
        }

        using (var ctx = db.NewContext())
        {
            var submit = await db.NewCurationService(ctx).SubmitAsync(ResearcherId,
                Input(id, "Okir Motifs",
                    new SourceInput("New source A", "Academic", null, "Ref A", 2020),
                    new SourceInput("New source B", "Digital", null, "Ref B", null)));
            Assert.True(submit.Success, string.Join("; ", submit.Errors));
        }

        using var check = db.NewContext();
        var item = await check.ContentItems
            .Include(c => c.Sources).ThenInclude(l => l.Source)
            .SingleAsync(c => c.ContentItemId == id);

        Assert.Equal(ContentStatus.Pending, item.Status);
        Assert.Equal(2, item.Sources.Count);
        Assert.DoesNotContain(item.Sources, l => l.Source!.Title == "Old source");
        Assert.Equal(2, await check.Sources.CountAsync());        // no orphaned Source rows
        Assert.Equal(1, await check.ContentItems.CountAsync());   // edited, not duplicated
    }

    // Interaction between the workflows (scenario 4): revision loop and a second decision
    [Fact]
    public async Task Item_sent_back_for_revision_can_be_edited_resubmitted_and_decided_again()
    {
        using var db = new TestDb();
        int id;

        using (var ctx = db.NewContext())
        {
            var first = await db.NewCurationService(ctx).SubmitAsync(ResearcherId,
                Input(null, "Kulintang Ensemble", new SourceInput("BCPCH profile", "Institutional", "BCPCH", "BCPCH 2023", 2023)));
            Assert.True(first.Success, string.Join("; ", first.Errors));
            id = first.Value;
        }

        // Validator sends it back.
        using (var ctx = db.NewContext())
        {
            var sourceId = await ctx.ContentItemSources
                .Where(l => l.ContentItemId == id).Select(l => l.SourceId).SingleAsync();

            var decision = await db.NewValidationService(ctx).RecordDecisionAsync(ValidatorId,
                new DecisionInput(id, ValidationDecision.RevisionRequested, "Add a second source.",
                    new[] { new SourceCheckInput(sourceId, true, "Confirmed.") }));
            Assert.True(decision.Success, string.Join("; ", decision.Errors));
        }

        // Researcher edits and resubmits with two sources.
        using (var ctx = db.NewContext())
        {
            var resubmit = await db.NewCurationService(ctx).SubmitAsync(ResearcherId,
                Input(id, "Kulintang Ensemble",
                    new SourceInput("BCPCH profile", "Institutional", "BCPCH", "BCPCH 2023", 2023),
                    new SourceInput("Academic paper", "Academic", null, "Journal 2019", 2019)));
            Assert.True(resubmit.Success, string.Join("; ", resubmit.Errors));
        }

        using (var check = db.NewContext())
        {
            var item = await check.ContentItems.Include(c => c.Sources).SingleAsync(c => c.ContentItemId == id);
            Assert.Equal(ContentStatus.Pending, item.Status);
            Assert.Equal(2, item.Sources.Count);
            Assert.All(item.Sources, l => Assert.False(l.IsVerified));   // must be verified again
            Assert.Equal(1, await check.ValidationRecords.CountAsync(r => r.ContentItemId == id)); // history kept
        }

        // Validator verifies the new sources and approves.
        using (var ctx = db.NewContext())
        {
            var ids = await ctx.ContentItemSources
                .Where(l => l.ContentItemId == id).Select(l => l.SourceId).ToArrayAsync();

            var approve = await db.NewValidationService(ctx).RecordDecisionAsync(ValidatorId,
                new DecisionInput(id, ValidationDecision.Approved, "Both sources verified.",
                    ids.Select(s => new SourceCheckInput(s, true, "Matches.")).ToArray()));
            Assert.True(approve.Success, string.Join("; ", approve.Errors));
        }

        using var end = db.NewContext();
        Assert.Equal(ContentStatus.Validated,
            (await end.ContentItems.SingleAsync(c => c.ContentItemId == id)).Status);
        Assert.Equal(2, await end.ValidationRecords.CountAsync(r => r.ContentItemId == id));
    }
}