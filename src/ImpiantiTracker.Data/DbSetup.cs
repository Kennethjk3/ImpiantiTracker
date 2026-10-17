using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ImpiantiTracker.Data;

public enum DatabaseProvider
{
    Sqlite,
    SqlServer,
}

public static class DbSetup
{
    public static DatabaseProvider ParseProvider(string text) =>
        Enum.TryParse<DatabaseProvider>(text.Trim(), ignoreCase: true, out var provider) && Enum.IsDefined(provider)
            ? provider
            : throw new ArgumentException($"Unknown database provider '{text}'. Use SqlServer or Sqlite.");

    public static DbContextOptions<AppDbContext> BuildOptions(DatabaseProvider provider, string connectionString)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        Configure(builder, provider, connectionString);
        return builder.Options;
    }

    /// <summary>Points EF Core at the chosen database. Used directly by the CLI and through AddDbContext by the API.</summary>
    public static void Configure(DbContextOptionsBuilder builder, DatabaseProvider provider, string connectionString)
    {
        switch (provider)
        {
            case DatabaseProvider.SqlServer:
                // Retries transient failures, e.g. while the SQL Server container is still starting.
                builder.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5));
                break;
            case DatabaseProvider.Sqlite:
                builder.UseSqlite(connectionString);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
    }
}

/// <summary>
/// Used only by the "dotnet ef" tools to build migrations. Migrations target SQL Server;
/// generating them doesn't open a connection, so the connection string is a placeholder.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(DbSetup.BuildOptions(DatabaseProvider.SqlServer, "Server=localhost,1433;Database=Impianti;TrustServerCertificate=True"));
}
