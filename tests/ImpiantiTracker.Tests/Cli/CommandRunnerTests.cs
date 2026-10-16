using ImpiantiTracker.Cli;
using ImpiantiTracker.Data;
using ImpiantiTracker.Tests.Data;

namespace ImpiantiTracker.Tests.Cli;

/// <summary>End to end: parse the arguments, run the command against the test database, check what it prints.</summary>
public class CommandRunnerTests
{
    private static readonly AppSettings Settings = new(DatabaseProvider.Sqlite, "unused in tests", WarningDays: 30);

    private static async Task<(int ExitCode, string Output)> RunAsync(TestDatabase database, params string[] args)
    {
        await using var db = database.NewContext();
        var output = new StringWriter();
        var exitCode = await new CommandRunner(db, Settings, TestDatabase.Today, output)
            .RunAsync(ArgParser.Parse(args, CommandRunner.Flags));
        return (exitCode, output.ToString());
    }

    [Fact]
    public async Task Due_prints_the_plan_and_a_summary()
    {
        await using var database = await TestDatabase.CreateAsync();

        var (exitCode, output) = await RunAsync(database, "due");

        Assert.Equal(0, exitCode);
        Assert.Contains("Studio Medico Greco", output);
        Assert.Contains("3 overdue, 2 due within 30 days (as of 16/10/2026).", output);
    }

    [Fact]
    public async Task Add_visit_reports_the_new_due_date()
    {
        await using var database = await TestDatabase.CreateAsync();

        var (exitCode, output) = await RunAsync(database,
            "add-visit", "--installation", "1", "--kind", "manutenzione", "--tech", "K. Kier", "--cost", "140,50");

        Assert.Equal(0, exitCode);
        Assert.Contains("Next service: 16/10/2027 (OK).", output);
    }

    [Fact]
    public async Task Export_writes_a_csv_that_excel_can_open()
    {
        await using var database = await TestDatabase.CreateAsync();
        var path = Path.Combine(Path.GetTempPath(), $"impianti-{Guid.NewGuid():N}.csv");

        try
        {
            var (exitCode, _) = await RunAsync(database, "export", path);
            var bytes = await File.ReadAllBytesAsync(path);
            var lines = await File.ReadAllLinesAsync(path);

            Assert.Equal(0, exitCode);
            Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
            Assert.StartsWith("ID;Status;Due;", lines[0]);
            Assert.Equal(1 + 5, lines.Length); // header + the 5 installations needing attention
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Reset_needs_confirmation()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<UsageException>(() => RunAsync(database, "reset"));
    }

    [Fact]
    public async Task An_unknown_command_is_a_usage_error()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<UsageException>(() => RunAsync(database, "frobnicate"));
    }
}
