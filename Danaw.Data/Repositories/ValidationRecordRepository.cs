using Danaw.Core.Abstractions;
using Danaw.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Danaw.Data.Repositories;

public class ValidationRecordRepository : IValidationRecordRepository
{
    private readonly DanawDbContext _db;

    public ValidationRecordRepository(DanawDbContext db) => _db = db;

    // Only stages the record; nothing is written until the unit of work saves.
    public async Task AddAsync(ValidationRecord record)
        => await _db.ValidationRecords.AddAsync(record);

    public async Task<IReadOnlyList<ValidationRecord>> ListForItemAsync(int contentItemId)
        => await _db.ValidationRecords
            .Include(r => r.Reviewer)
            .Where(r => r.ContentItemId == contentItemId)
            .OrderByDescending(r => r.DecidedAt)
            .ToListAsync();
}