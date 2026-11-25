using Danaw.Core.Abstractions;

namespace Danaw.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DanawDbContext _db;

    public UnitOfWork(DanawDbContext db) => _db = db;

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}