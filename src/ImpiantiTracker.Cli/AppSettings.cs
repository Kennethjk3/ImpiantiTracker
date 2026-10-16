using ImpiantiTracker.Data;
using Microsoft.Extensions.Configuration;

namespace ImpiantiTracker.Cli;

public sealed record AppSettings(DatabaseProvider Provider, string ConnectionString, int WarningDays)
{
    public static AppSettings From(IConfiguration config)
    {
        var provider = DbSetup.ParseProvider(config["Database:Provider"] ?? nameof(DatabaseProvider.Sqlite));

        var connectionString = config[$"Database:ConnectionStrings:{provider}"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new UsageException($"No connection string configured for {provider} (Database:ConnectionStrings:{provider}).");

        var warningDays = int.TryParse(config["Maintenance:WarningDays"], out var days) && days >= 0 ? days : 30;

        return new AppSettings(provider, connectionString, warningDays);
    }
}
