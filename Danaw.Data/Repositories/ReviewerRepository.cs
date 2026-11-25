using Danaw.Core.Abstractions;
using Danaw.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Danaw.Data.Repositories;

public class ReviewerRepository : IReviewerRepository
{
    private readonly DanawDbContext _db;

    public ReviewerRepository(DanawDbContext db) => _db = db;

    public Task<Reviewer?> GetAsync(int reviewerId)
        => _db.Reviewers.FirstOrDefaultAsync(r => r.ReviewerId == reviewerId);

    public async Task<IReadOnlyList<Reviewer>> ListActiveAsync()
        => await _db.Reviewers
            .Where(r => r.IsActive)
            .OrderBy(r => r.ReviewerId)
            .ToListAsync();
}