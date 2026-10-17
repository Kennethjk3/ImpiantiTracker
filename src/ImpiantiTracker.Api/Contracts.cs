using ImpiantiTracker.Core.Domain;
using ImpiantiTracker.Core.Maintenance;
using ImpiantiTracker.Data;

namespace ImpiantiTracker.Api;

// What the API sends and receives. Entities never leave the server directly: they have
// navigation properties that loop (Customer -> Installations -> Customer) and fields the
// client doesn't need. Enums travel as strings ("HeatPump", "Overdue"), dates as "2026-10-17".

public sealed record MaintenancePlanResponse(DateOnly AsOf, int WarningDays, IReadOnlyList<DueItem> Items);

public sealed record CustomerResponse(int Id, string Name, string Address, string City, string? Phone, string? Email);

public sealed record VisitResponse(int Id, DateOnly Date, VisitKind Kind, string Technician, decimal Cost, string? Notes);

public sealed record InstallationDetailResponse(
    int Id,
    InstallationType Type,
    string Brand,
    string? Model,
    string? SerialNumber,
    decimal PowerKw,
    DateOnly InstalledOn,
    int MaintenanceIntervalMonths,
    string? RegistryCode,
    CustomerResponse Customer,
    DueItem Due,
    IReadOnlyList<VisitResponse> Visits)
{
    public static InstallationDetailResponse From(InstallationDetail detail)
    {
        var (i, due) = detail;
        var c = i.Customer!;
        return new InstallationDetailResponse(
            i.Id, i.Type, i.Brand, i.Model, i.SerialNumber, i.PowerKw, i.InstalledOn, i.MaintenanceIntervalMonths, i.RegistryCode,
            new CustomerResponse(c.Id, c.Name, c.Address, c.City, c.Phone, c.Email),
            due,
            i.Visits.Select(v => new VisitResponse(v.Id, v.Date, v.Kind, v.Technician, v.Cost, v.Notes)).ToList());
    }
}

/// <summary>
/// A new visit. Kind is a string so the API can give a clear message for a wrong value and accept
/// the Italian words too (manutenzione, rcee, riparazione). Date defaults to today, cost to 0.
/// </summary>
public sealed record VisitRequest(DateOnly? Date, string? Kind, string? Technician, decimal? Cost, string? Notes);

public sealed record VisitCreatedResponse(int VisitId, DueItem Due);
