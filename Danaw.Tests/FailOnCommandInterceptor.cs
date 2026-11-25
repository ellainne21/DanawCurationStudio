using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Danaw.Tests;

// Test-only helper: watches every SQL command EF Core sends and throws when it sees
// the one we want to fail. Everything sent before it is recorded, so a test can prove
// the failure happened PARTWAY through the save.
public sealed class FailOnCommandInterceptor : DbCommandInterceptor
{
    private readonly string _failWhenSqlContains;

    public FailOnCommandInterceptor(string failWhenSqlContains)
        => _failWhenSqlContains = failWhenSqlContains;

    public bool Triggered { get; private set; }
    public List<string> ExecutedBeforeFailure { get; } = new();

    private void Check(DbCommand command)
    {
        if (command.CommandText.Contains(_failWhenSqlContains, StringComparison.OrdinalIgnoreCase))
        {
            Triggered = true;
            throw new InvalidOperationException("Simulated database failure.");
        }
        ExecutedBeforeFailure.Add(command.CommandText);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Check(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Check(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Check(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Check(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Check(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Check(command);
        return ValueTask.FromResult(result);
    }
}