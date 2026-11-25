using Danaw.Core.Services;
using Danaw.Data;
using Danaw.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Danaw.Tests;

// A throwaway in-memory SQLite database with the real migrations and seed data applied.
// Each test creates its own, so tests never affect each other or any real data.
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); // the in-memory database lives only while this stays open

        using var ctx = NewContext();
        ctx.Database.Migrate();
    }

    // Optional interceptors let a test inject a fault into one context only.
    public DanawDbContext NewContext(params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<DanawDbContext>().UseSqlite(_connection);
        if (interceptors.Length > 0) builder.AddInterceptors(interceptors);
        return new DanawDbContext(builder.Options);
    }

    public ContentCurationService NewCurationService(DanawDbContext ctx)
        => new(new ContentItemRepository(ctx),
               new ReviewerRepository(ctx),
               new UnitOfWork(ctx),
               TimeProvider.System);

    public ValidationWorkflowService NewValidationService(DanawDbContext ctx)
        => new(new ContentItemRepository(ctx),
               new ReviewerRepository(ctx),
               new ValidationRecordRepository(ctx),
               new UnitOfWork(ctx),
               TimeProvider.System);

    public void Dispose() => _connection.Dispose();
}