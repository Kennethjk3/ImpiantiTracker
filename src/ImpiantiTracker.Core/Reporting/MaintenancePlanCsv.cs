using System.Globalization;
using ImpiantiTracker.Core.Domain;
using ImpiantiTracker.Core.Maintenance;

namespace ImpiantiTracker.Core.Reporting;

/// <summary>The maintenance plan as CSV. Shared by the CLI's export command and the API's download endpoint.</summary>
public static class MaintenancePlanCsv
{
    public static readonly string[] Header =
        ["ID", "Status", "Due", "DaysUntilDue", "Customer", "City", "Phone", "Type", "Brand", "LastService"];

    public static IEnumerable<string> Lines(IEnumerable<DueItem> plan)
    {
        yield return Csv.Row(Header);
        foreach (var d in plan)
        {
            yield return Csv.Row(
            [
                d.InstallationId.ToString(CultureInfo.InvariantCulture),
                Labels.Of(d.Status),
                ItalianDate(d.NextDue),
                d.DaysUntilDue.ToString(CultureInfo.InvariantCulture),
                d.Customer, d.City, d.Phone, Labels.Of(d.Type), d.Brand,
                d.LastService is { } last ? ItalianDate(last) : "",
            ]);
        }
    }

    public static async Task WriteAsync(TextWriter writer, IEnumerable<DueItem> plan, CancellationToken ct = default)
    {
        foreach (var line in Lines(plan))
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(line);
        }
    }

    private static string ItalianDate(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
