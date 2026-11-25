using Danaw.Core.Abstractions;
using Danaw.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Danaw.Data.Repositories;

public class ContentItemRepository : IContentItemRepository
{
    private readonly DanawDbContext _db;

    public ContentItemRepository(DanawDbContext db) => _db = db;

    public Task<ContentItem?> GetWithSourcesAsync(int contentItemId)
        => _db.ContentItems
            .Include(c => c.Category)
            .Include(c => c.CreatedBy)
            .Include(c => c.Sources).ThenInclude(l => l.Source)
            .FirstOrDefaultAsync(c => c.ContentItemId == contentItemId);

    public Task<bool> CategoryExistsAsync(int categoryId)
        => _db.Categories.AnyAsync(c => c.CategoryId == categoryId);

    public async Task<IReadOnlyList<Category>> ListCategoriesAsync()
        => await _db.Categories.OrderBy(c => c.Name).ToListAsync();

    // The Title column uses NOCASE collation, so this comparison ignores case.
    public Task<bool> TitleExistsAsync(int categoryId, string title, int? excludeContentItemId)
        => _db.ContentItems.AnyAsync(c =>
            c.CategoryId == categoryId &&
            c.Title == title &&
            (excludeContentItemId == null || c.ContentItemId != excludeContentItemId));

    public async Task AddAsync(ContentItem item)
        => await _db.ContentItems.AddAsync(item);

    public async Task<IReadOnlyList<ContentItem>> ListAsync(ContentStatus? status, string? titleSearch)
    {
        var query = _db.ContentItems
            .Include(c => c.Category)
            .Include(c => c.CreatedBy)
            .AsQueryable();

        if (status is not null)
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(titleSearch))
        {
            var term = titleSearch.Trim();
            query = query.Where(c => c.Title.Contains(term));
        }

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    // Used when a draft is edited: the old source rows are replaced, not left behind.
    public void RemoveSources(ContentItem item)
    {
        var links = item.Sources.ToList();
        foreach (var link in links)
        {
            if (link.Source is not null)
                _db.Sources.Remove(link.Source);
        }
        _db.ContentItemSources.RemoveRange(links);
        item.Sources.Clear();
    }
}