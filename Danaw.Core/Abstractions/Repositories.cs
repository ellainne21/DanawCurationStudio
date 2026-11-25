using Danaw.Core.Entities;

namespace Danaw.Core.Abstractions;

public interface IContentItemRepository
{
    Task<ContentItem?> GetWithSourcesAsync(int contentItemId);
    Task<bool> CategoryExistsAsync(int categoryId);
    Task<IReadOnlyList<Category>> ListCategoriesAsync();
    Task<bool> TitleExistsAsync(int categoryId, string title, int? excludeContentItemId);
    Task AddAsync(ContentItem item);
    Task<IReadOnlyList<ContentItem>> ListAsync(ContentStatus? status, string? titleSearch);
    void RemoveSources(ContentItem item);
}

public interface IReviewerRepository
{
    Task<Reviewer?> GetAsync(int reviewerId);
    Task<Reviewer?> GetByUsernameAsync(string username);
    Task<IReadOnlyList<Reviewer>> ListActiveAsync();
}

public interface IValidationRecordRepository
{
    Task AddAsync(ValidationRecord record);
    Task<IReadOnlyList<ValidationRecord>> ListForItemAsync(int contentItemId);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}