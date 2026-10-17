using System.Text.Json.Serialization;
using ImpiantiTracker.Api;
using ImpiantiTracker.Data;

var builder = WebApplication.CreateBuilder(args);

// Same override convention as the CLI: IMPIANTI_Database__Provider=SqlServer, etc.
builder.Configuration.AddEnvironmentVariables(prefix: "IMPIANTI_");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => ApiSettings.From(sp.GetRequiredService<IConfiguration>()));

// The settings are read when the first DbContext is created, after tests have had a chance to override them.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var settings = sp.GetRequiredService<ApiSettings>();
    DbSetup.Configure(options, settings.Provider, settings.ConnectionString);
});
builder.Services.AddScoped<TrackerService>();

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // Malformed input (a date the JSON reader can't parse, ?days=abc) is the client's mistake:
    // say what was wrong instead of a bare "An error occurred".
    if (context.Exception is BadHttpRequestException bad)
    {
        context.ProblemDetails.Title = "The request could not be read.";
        context.ProblemDetails.Detail = bad.InnerException is System.Text.Json.JsonException json
            ? $"Invalid JSON: {json.Message}"
            : bad.Message;
    }
});
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    // Binding failures are 400s; anything else is our fault (500).
    StatusCodeSelector = ex => ex is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
    app.MapOpenApi(); // the API description at /openapi/v1.json

app.MapHealthChecks("/health");
app.MapTrackerApi();

// Create or migrate the database and load the sample data the first time.
// Convenient for a demo; a production deployment would run migrations as a separate step.
var startupSettings = app.Services.GetRequiredService<ApiSettings>();
if (startupSettings.InitializeOnStartup)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var today = app.Services.GetRequiredService<TimeProvider>().Today();
    await DatabaseInitializer.InitializeAsync(db, startupSettings.Provider, today);
}

await app.RunAsync();

/// <summary>Visible to the integration tests (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;
