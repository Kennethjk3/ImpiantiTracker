using ImpiantiTracker.Core.Domain;
using ImpiantiTracker.Core.Maintenance;
using ImpiantiTracker.Core.Reporting;
using ImpiantiTracker.Core.Visits;
using Microsoft.EntityFrameworkCore;

namespace ImpiantiTracker.Data;

public sealed record InstallationRow(
    int Id,
    string Customer,
    string City,
    InstallationType Type,
    string Brand,
    string? Model,
    decimal PowerKw,
    DateOnly InstalledOn,
    DateOnly? LastVisit);

public sealed record AddVisitResult(bool Success, int? VisitId, IReadOnlyList<string> Errors)
{
    public static AddVisitResult Saved(int visitId) => new(true, visitId, []);
    public static AddVisitResult Failed(IReadOnlyList<string> errors) => new(false, null, errors);
    public static AddVisitResult Failed(string error) => new(false, null, [error]);
}

/// <summary>Everything the application does with the database. Reads use AsNoTracking and project only the columns they need.</summary>
public sealed class TrackerService(AppDbContext db)
{
    public async Task<IReadOnlyList<InstallationRow>> ListInstallationsAsync(string? city = null, CancellationToken ct = default)
    {
        var query = db.Installations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(city))
        {
            // Case-insensitive on every provider (SQLite compares text case-sensitively by default).
            var wanted = city.Trim().ToUpperInvariant();
            query = query.Where(i => i.Customer!.City.ToUpper() == wanted);
        }

        return await query
            .OrderBy(i => i.Customer!.Name)
            .ThenBy(i => i.Id)
            .Select(i => new InstallationRow(
                i.Id, i.Customer!.Name, i.Customer.City, i.Type, i.Brand, i.Model, i.PowerKw, i.InstalledOn,
                i.Visits.OrderByDescending(v => v.Date).Select(v => (DateOnly?)v.Date).FirstOrDefault()))
            .ToListAsync(ct);
    }

    /// <summary>An installation with its customer and full visit history, or null if it doesn't exist.</summary>
    public Task<Installation?> GetInstallationAsync(int id, CancellationToken ct = default) =>
        db.Installations.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Visits.OrderByDescending(v => v.Date))
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    /// <summary>
    /// The maintenance plan. One SQL query fetches each installation with the date of its last
    /// routine service (a correlated subquery); the due-date rules then run in Core.
    /// </summary>
    public async Task<IReadOnlyList<DueItem>> GetMaintenancePlanAsync(
        DateOnly today, int warningDays, bool includeOk = false, CancellationToken ct = default)
    {
        var snapshots = await db.Installations.AsNoTracking()
            .Select(i => new InstallationSnapshot(
                i.Id, i.Customer!.Name, i.Customer.City, i.Customer.Phone, i.Type, i.Brand,
                i.InstalledOn, i.MaintenanceIntervalMonths,
                // Same rule as MaintenanceSchedule.CountsAsService: repairs don't count.
                i.Visits
                    .Where(v => v.Kind == VisitKind.Maintenance || v.Kind == VisitKind.EfficiencyCheck)
                    .OrderByDescending(v => v.Date)
                    .Select(v => (DateOnly?)v.Date)
                    .FirstOrDefault()))
            .ToListAsync(ct);

        return MaintenanceSchedule.Plan(snapshots, today, warningDays, includeOk);
    }

    public async Task<AddVisitResult> AddVisitAsync(NewVisit input, DateOnly today, CancellationToken ct = default)
    {
        var installedOn = await db.Installations
            .Where(i => i.Id == input.InstallationId)
            .Select(i => (DateOnly?)i.InstalledOn)
            .FirstOrDefaultAsync(ct);

        if (installedOn is null)
            return AddVisitResult.Failed($"Installation {input.InstallationId} does not exist.");

        var errors = VisitRules.Validate(input, installedOn.Value, today);
        if (errors.Count > 0)
            return AddVisitResult.Failed(errors);

        var visit = new ServiceVisit
        {
            InstallationId = input.InstallationId,
            Date = input.Date,
            Kind = input.Kind,
            Technician = input.Technician.Trim(),
            Cost = input.Cost,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
        };

        db.ServiceVisits.Add(visit);
        await db.SaveChangesAsync(ct);
        return AddVisitResult.Saved(visit.Id);
    }

    public async Task<YearReport> GetYearReportAsync(int year, CancellationToken ct = default)
    {
        var from = new DateOnly(year, 1, 1);
        var to = from.AddYears(1);

        var lines = await db.ServiceVisits.AsNoTracking()
            .Where(v => v.Date >= from && v.Date < to)
            .Select(v => new VisitLine(v.Date, v.Kind, v.Technician, v.Cost))
            .ToListAsync(ct);

        return VisitReport.Build(year, lines);
    }
}
