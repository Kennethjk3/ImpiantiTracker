using ImpiantiTracker.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ImpiantiTracker.Tests.Data;

/// <summary>
/// A real SQLite database living in memory: real SQL, real constraints, no files, gone after the test.
/// The connection stays open for the test's lifetime, because closing it deletes the database.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    /// <summary>Every test runs on the same fixed date, so the sample data always gives the same plan.</summary>
    public static readonly DateOnly Today = new(2026, 10, 16);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    private TestDatabase(SqliteConnection connection)
    {
        _connection = connection;
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
    }

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var database = new TestDatabase(connection);
        await using var db = database.NewContext();
        await DatabaseInitializer.InitializeAsync(db, DatabaseProvider.Sqlite, Today);
        return database;
    }

    /// <summary>A fresh context each time, like each command run of the real app.</summary>
    public AppDbContext NewContext() => new(_options);

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
