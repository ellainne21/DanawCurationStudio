using Danaw.Core.Entities;
using Danaw.Core.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Danaw.Tests;

public class ContentCurationServiceTests
{
    // Seeded ids from the migration.
    private const int ResearcherId = 1;
    private const int ValidatorId = 4;
    private const int ArtsCraftsCategoryId = 1;

    private static SourceInput Source(string title, string reference)
        => new(title, "Report", "BCPCH", reference, 2023);

    private static ContentItemInput Input(
        string title = "Okir Motif Origins",
        string description = "Origins and naming of okir motifs.",
        params SourceInput[] sources)
        => new(null, ArtsCraftsCategoryId, title, "Arts", description, sources);

    // Scenario 1: Workflow 1 success
    [Fact]
    public async Task Submit_with_valid_data_and_sources_saves_a_pending_item()
    {
        using var db = new TestDb();
        int newId;

        using (var ctx = db.NewContext())
        {
            var service = db.NewCurationService(ctx);
            var result = await service.SubmitAsync(ResearcherId, Input(
                sources: new[]
                {
                    Source("BCPCH Meranaw heritage profile, 2023", "BCPCH 2023"),
                    Source("NCCA registry entry", "TALAPAMANA #4471")
                }));

            Assert.True(result.Success, string.Join("; ", result.Errors));
            newId = result.Value;
        }

        // Fresh context: proves the data was really saved, not just held in memory.
        using var check = db.NewContext();
        var item = await check.ContentItems
            .Include(c => c.Sources)
            .SingleAsync(c => c.ContentItemId == newId);

        Assert.Equal(ContentStatus.Pending, item.Status);
        Assert.NotNull(item.SubmittedAt);
        Assert.Equal(2, item.Sources.Count);
        Assert.All(item.Sources, link => Assert.False(link.IsVerified));
        Assert.Equal(2, await check.Sources.CountAsync());
    }

    // Scenario 2: Workflow 1 rejection, missing source
    [Fact]
    public async Task Submit_without_any_source_is_rejected_and_nothing_is_saved()
    {
        using var db = new TestDb();

        using (var ctx = db.NewContext())
        {
            var service = db.NewCurationService(ctx);
            var result = await service.SubmitAsync(ResearcherId, Input());

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Contains("source", StringComparison.OrdinalIgnoreCase));
        }

        using var check = db.NewContext();
        Assert.Equal(0, await check.ContentItems.CountAsync());
        Assert.Equal(0, await check.Sources.CountAsync());
        Assert.Equal(0, await check.ContentItemSources.CountAsync());
    }

    // Scenario 2 variant: blank title
    [Fact]
    public async Task Submit_with_blank_title_is_rejected()
    {
        using var db = new TestDb();
        using var ctx = db.NewContext();
        var service = db.NewCurationService(ctx);

        var result = await service.SubmitAsync(ResearcherId, Input(
            title: "   ",
            sources: new[] { Source("BCPCH profile", "BCPCH 2023") }));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Title", StringComparison.OrdinalIgnoreCase));

        using var check = db.NewContext();
        Assert.Equal(0, await check.ContentItems.CountAsync());
    }

    // Business rule: duplicate title in the same category
    [Fact]
    public async Task Submit_with_duplicate_title_in_same_category_is_rejected_ignoring_case()
    {
        using var db = new TestDb();
        var src = new[] { Source("BCPCH profile", "BCPCH 2023") };

        using (var ctx = db.NewContext())
        {
            var first = await db.NewCurationService(ctx).SubmitAsync(ResearcherId, Input(sources: src));
            Assert.True(first.Success);
        }

        using (var ctx = db.NewContext())
        {
            var second = await db.NewCurationService(ctx)
                .SubmitAsync(ResearcherId, Input(title: "OKIR motif ORIGINS", sources: src));

            Assert.False(second.Success);
            Assert.Contains(second.Errors, e => e.Contains("already exists", StringComparison.OrdinalIgnoreCase));
        }

        using var check = db.NewContext();
        Assert.Equal(1, await check.ContentItems.CountAsync());
    }

    // Business rule: only researchers may submit
    [Fact]
    public async Task Validator_cannot_submit_a_content_item()
    {
        using var db = new TestDb();
        using var ctx = db.NewContext();

        var result = await db.NewCurationService(ctx).SubmitAsync(ValidatorId, Input(
            sources: new[] { Source("BCPCH profile", "BCPCH 2023") }));

        Assert.False(result.Success);

        using var check = db.NewContext();
        Assert.Equal(0, await check.ContentItems.CountAsync());
    }
}