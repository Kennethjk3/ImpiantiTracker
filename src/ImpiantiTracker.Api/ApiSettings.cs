using ImpiantiTracker.Data;

namespace ImpiantiTracker.Api;

/// <summary>The "Database" and "Maintenance" sections of appsettings.json, read and checked once.</summary>
public sealed record ApiSettings(DatabaseProvider Provider, string ConnectionString, bool InitializeOnStartup, int WarningDays)
{
    public static ApiSettings From(IConfiguration config)
    {
        var provider = DbSetup.ParseProvider(config["Database:Provider"] ?? nameof(DatabaseProvider.Sqlite));

        var connectionString = config[$"Database:ConnectionStrings:{provider}"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"No connection string configured for {provider} (Database:ConnectionStrings:{provider}).");

        var initialize = !bool.TryParse(config["Database:InitializeOnStartup"], out var flag) || flag;
        var warningDays = int.TryParse(config["Maintenance:WarningDays"], out var days) && days >= 0 ? days : 30;

        return new ApiSettings(provider, connectionString, initialize, warningDays);
    }
}

public static class TimeProviderExtensions
{
    /// <summary>Today's date on the server's clock. Tests replace the TimeProvider to fix the date.</summary>
    public static DateOnly Today(this TimeProvider clock) => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
}
