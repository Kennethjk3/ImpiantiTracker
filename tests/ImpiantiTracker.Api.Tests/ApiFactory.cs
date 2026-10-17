using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace ImpiantiTracker.Api.Tests;

/// <summary>
/// Runs the real API in memory (no network port) against its own SQLite database,
/// on a fixed date so the sample data always produces the same maintenance plan.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateOnly Today = new(2026, 10, 17);

    // A named in-memory database lives as long as one connection to it stays open.
    // This "keeper" connection holds it open; the app opens its own connections to the same name.
    private readonly string _connectionString = $"Data Source=api-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keeper;

    public ApiFactory()
    {
        _keeper = new SqliteConnection(_connectionString);
        _keeper.Open();
    }

    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:ConnectionStrings:Sqlite", _connectionString);
        builder.UseSetting("Database:InitializeOnStartup", "true");
        builder.UseSetting("Maintenance:WarningDays", "30");

        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(new FixedClock(Today)));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _keeper.Dispose();
    }

    private sealed class FixedClock(DateOnly today) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(today.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
