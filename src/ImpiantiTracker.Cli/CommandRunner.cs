using ImpiantiTracker.Core.Domain;
using ImpiantiTracker.Core.Maintenance;
using ImpiantiTracker.Core.Reporting;
using ImpiantiTracker.Core.Visits;
using ImpiantiTracker.Data;

namespace ImpiantiTracker.Cli;

/// <summary>Runs one command and returns the process exit code (0 = success).</summary>
public sealed class CommandRunner(AppDbContext db, AppSettings settings, DateOnly today, TextWriter output)
{
    public static readonly string[] Flags = ["all", "yes", "help"];

    private readonly TrackerService _service = new(db);

    public Task<int> RunAsync(ParsedArgs args, CancellationToken ct = default) => args.Command switch
    {
        "init" => InitAsync(reset: false, ct),
        "reset" => args.HasFlag("yes")
            ? InitAsync(reset: true, ct)
            : throw new UsageException("reset deletes every record. Run 'reset --yes' to confirm."),
        "list" => ListAsync(args, ct),
        "show" => ShowAsync(args, ct),
        "due" => DueAsync(args, ct),
        "add-visit" => AddVisitAsync(args, ct),
        "report" => ReportAsync(args, ct),
        "export" => ExportAsync(args, ct),
        _ => throw new UsageException($"Unknown command '{args.Command}'."),
    };

    private async Task<int> InitAsync(bool reset, CancellationToken ct)
    {
        var seeded = await DatabaseInitializer.InitializeAsync(db, settings.Provider, today, reset, ct);
        output.WriteLine(seeded
            ? $"Database ready ({settings.Provider}), sample data loaded. Try: due"
            : $"Database ready ({settings.Provider}). It already had data, so nothing was added.");
        return 0;
    }

    private async Task<int> ListAsync(ParsedArgs args, CancellationToken ct)
    {
        var rows = await _service.ListInstallationsAsync(args.Get("city"), ct);
        if (rows.Count == 0)
        {
            output.WriteLine("No installations found.");
            return 0;
        }

        TablePrinter.Print(output,
            ["ID", "Customer", "City", "Type", "Brand / model", "kW", "Installed", "Last visit"],
            rows.Select(r => TablePrinter.Row(
                r.Id.ToString(), r.Customer, r.City, Labels.Of(r.Type),
                r.Model is null ? r.Brand : $"{r.Brand} {r.Model}",
                Formats.Kw(r.PowerKw), Formats.Date(r.InstalledOn), Formats.Date(r.LastVisit))).ToList());

        output.WriteLine();
        output.WriteLine($"{rows.Count} installation(s).");
        return 0;
    }

    private async Task<int> ShowAsync(ParsedArgs args, CancellationToken ct)
    {
        if (args.Positionals.Count == 0 || !int.TryParse(args.Positionals[0], out var id))
            throw new UsageException("Usage: show <installation id>");

        var installation = await _service.GetInstallationAsync(id, ct);
        if (installation is null)
        {
            output.WriteLine($"Installation {id} does not exist.");
            return 1;
        }

        var customer = installation.Customer!;
        output.WriteLine($"#{installation.Id}  {Labels.Of(installation.Type)}  {installation.Brand} {installation.Model}".TrimEnd());
        output.WriteLine($"Customer:    {customer.Name}, {customer.Address}, {customer.City}");
        if (customer.Phone is not null) output.WriteLine($"Phone:       {customer.Phone}");
        output.WriteLine($"Serial:      {installation.SerialNumber ?? "-"}    Power: {Formats.Kw(installation.PowerKw)} kW");
        output.WriteLine($"Installed:   {Formats.Date(installation.InstalledOn)}    Service every {installation.MaintenanceIntervalMonths} months");
        if (installation.RegistryCode is not null) output.WriteLine($"Registry:    {installation.RegistryCode}");

        // The same rule the maintenance plan uses, applied to the history in memory.
        var lastService = installation.Visits
            .Where(v => MaintenanceSchedule.CountsAsService(v.Kind))
            .Select(v => (DateOnly?)v.Date)
            .Max();
        var plan = MaintenanceSchedule.Evaluate(
            new InstallationSnapshot(installation.Id, customer.Name, customer.City, customer.Phone,
                installation.Type, installation.Brand, installation.InstalledOn,
                installation.MaintenanceIntervalMonths, lastService),
            today, settings.WarningDays);
        output.WriteLine($"Next due:    {Formats.Date(plan.NextDue)} ({StatusLabel(plan.Status)}, {Formats.DaysFromToday(plan.DaysUntilDue)})");
        output.WriteLine();

        if (installation.Visits.Count == 0)
        {
            output.WriteLine("No visits recorded yet.");
            return 0;
        }

        TablePrinter.Print(output,
            ["Date", "Type", "Technician", "Cost", "Notes"],
            installation.Visits.Select(v => TablePrinter.Row(
                Formats.Date(v.Date), Labels.Of(v.Kind), v.Technician, Formats.Money(v.Cost), v.Notes ?? "")).ToList());
        return 0;
    }

    private async Task<int> DueAsync(ParsedArgs args, CancellationToken ct)
    {
        var days = WarningDays(args);
        var plan = await _service.GetMaintenancePlanAsync(today, days, args.HasFlag("all"), ct);

        if (plan.Count == 0)
        {
            output.WriteLine($"Nothing overdue or due in the next {days} days.");
            return 0;
        }

        TablePrinter.Print(output,
            ["ID", "Status", "Due", "When", "Customer", "City", "Phone", "Type", "Last service"],
            plan.Select(d => TablePrinter.Row(
                d.InstallationId.ToString(), StatusLabel(d.Status), Formats.Date(d.NextDue),
                Formats.DaysFromToday(d.DaysUntilDue), d.Customer, d.City, d.Phone ?? "",
                Labels.Of(d.Type), Formats.Date(d.LastService))).ToList(),
            rowColor: i => plan[i].Status switch
            {
                MaintenanceStatus.Overdue => ConsoleColor.Red,
                MaintenanceStatus.DueSoon => ConsoleColor.Yellow,
                _ => null,
            });

        var overdue = plan.Count(d => d.Status == MaintenanceStatus.Overdue);
        var dueSoon = plan.Count(d => d.Status == MaintenanceStatus.DueSoon);
        output.WriteLine();
        output.WriteLine($"{overdue} overdue, {dueSoon} due within {days} days (as of {Formats.Date(today)}).");
        return 0;
    }

    private async Task<int> AddVisitAsync(ParsedArgs args, CancellationToken ct)
    {
        var kindText = args.Require("kind");
        if (!Labels.TryParseVisitKind(kindText, out var kind))
            throw new UsageException($"Unknown visit kind '{kindText}'. Use maintenance, rcee or repair.");

        var visit = new NewVisit(
            InstallationId: args.GetInt("installation", 0) is > 0 and var id
                ? id
                : throw new UsageException("Missing or invalid --installation <id>."),
            Date: args.GetDate("date", today),
            Kind: kind,
            Technician: args.Require("tech"),
            Cost: args.GetDecimal("cost", 0m),
            Notes: args.Get("notes"));

        var result = await _service.AddVisitAsync(visit, today, ct);
        if (!result.Success)
        {
            output.WriteLine("The visit was not saved:");
            foreach (var error in result.Errors) output.WriteLine($"  - {error}");
            return 1;
        }

        output.WriteLine($"Visit #{result.VisitId} saved for installation {visit.InstallationId}.");

        var plan = await _service.GetMaintenancePlanAsync(today, settings.WarningDays, includeOk: true, ct);
        if (plan.FirstOrDefault(d => d.InstallationId == visit.InstallationId) is { } item)
            output.WriteLine($"Next service: {Formats.Date(item.NextDue)} ({StatusLabel(item.Status)}).");
        return 0;
    }

    private async Task<int> ReportAsync(ParsedArgs args, CancellationToken ct)
    {
        var year = args.GetInt("year", today.Year);
        if (year is < 2000 or > 2100)
            throw new UsageException("--year must be between 2000 and 2100.");

        var report = await _service.GetYearReportAsync(year, ct);
        output.WriteLine($"Year {report.Year}: {report.TotalVisits} visit(s), {Formats.Money(report.TotalRevenue)}");
        if (report.TotalVisits == 0) return 0;

        output.WriteLine();
        TablePrinter.Print(output, ["Month", "Visits", "Revenue"],
            report.ByMonth.Select(m => TablePrinter.Row(Formats.Month(m.Month), m.Visits.ToString(), Formats.Money(m.Revenue))).ToList());

        output.WriteLine();
        TablePrinter.Print(output, ["Technician", "Visits", "Revenue"],
            report.ByTechnician.Select(t => TablePrinter.Row(t.Technician, t.Visits.ToString(), Formats.Money(t.Revenue))).ToList());

        output.WriteLine();
        TablePrinter.Print(output, ["Visit type", "Visits"],
            report.ByKind.Select(k => TablePrinter.Row(Labels.Of(k.Kind), k.Visits.ToString())).ToList());
        return 0;
    }

    private async Task<int> ExportAsync(ParsedArgs args, CancellationToken ct)
    {
        if (args.Positionals.Count == 0)
            throw new UsageException("Usage: export <file.csv> [--days N] [--all]");

        var path = Path.GetFullPath(args.Positionals[0]);
        var plan = await _service.GetMaintenancePlanAsync(today, WarningDays(args), args.HasFlag("all"), ct);

        await using (var file = new StreamWriter(path, append: false, Csv.ExcelEncoding))
        {
            await file.WriteLineAsync(Csv.Row(
                ["ID", "Status", "Due", "DaysUntilDue", "Customer", "City", "Phone", "Type", "Brand", "LastService"]));
            foreach (var d in plan)
            {
                await file.WriteLineAsync(Csv.Row(
                [
                    d.InstallationId.ToString(), StatusLabel(d.Status), Formats.Date(d.NextDue), d.DaysUntilDue.ToString(),
                    d.Customer, d.City, d.Phone, Labels.Of(d.Type), d.Brand,
                    d.LastService is { } last ? Formats.Date(last) : "",
                ]));
            }
        }

        output.WriteLine($"Wrote {plan.Count} row(s) to {path}");
        return 0;
    }

    private int WarningDays(ParsedArgs args)
    {
        var days = args.GetInt("days", settings.WarningDays);
        return days >= 0 ? days : throw new UsageException("--days cannot be negative.");
    }

    private static string StatusLabel(MaintenanceStatus status) => status switch
    {
        MaintenanceStatus.Overdue => "OVERDUE",
        MaintenanceStatus.DueSoon => "Due soon",
        _ => "OK",
    };
}
