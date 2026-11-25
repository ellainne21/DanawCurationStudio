using Danaw.Core.Services;
using Danaw.Data;
using Danaw.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Danaw.Tests;

// A throwaway in-memory SQLite database with the real migrations and seed data applied.
// Each test creates its own, so tests never affect each other or any real data.
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<DanawDbContext> _options;

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); // the in-memory database lives only while this stays open

        _options = new DbContextOptionsBuilder<DanawDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = NewContext();
        ctx.Database.Migrate();
    }

    public DanawDbContext NewContext() => new(_options);

    public ContentCurationService NewCurationService(DanawDbContext ctx)
        => new(new ContentItemRepository(ctx),
               new ReviewerRepository(ctx),
               new UnitOfWork(ctx),
               TimeProvider.System);

    public void Dispose() => _connection.Dispose();
}
