using ImpiantiTracker.Cli;
using ImpiantiTracker.Data;
using Microsoft.Extensions.Configuration;

try
{
    var parsed = ArgParser.Parse(args, CommandRunner.Flags);
    if (parsed.Command is null or "help" || parsed.HasFlag("help"))
    {
        Console.WriteLine(Help.Text);
        return 0;
    }

    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables(prefix: "IMPIANTI_")
        .Build();

    var settings = AppSettings.From(config);
    var today = DateOnly.FromDateTime(DateTime.Now);

    await using var db = new AppDbContext(DbSetup.BuildOptions(settings.Provider, settings.ConnectionString));
    return await new CommandRunner(db, settings, today, Console.Out).RunAsync(parsed);
}
catch (UsageException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine("Run 'help' to see the commands.");
    return 2;
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Console.Error.WriteLine($"Error: {ex.GetBaseException().Message}");
    Console.Error.WriteLine("If the database hasn't been created yet, run: init");
    return 1;
}
