using ImpiantiTracker.Core.Maintenance;

namespace ImpiantiTracker.Core.Domain;

public enum InstallationType
{
    Boiler,          // caldaia
    HeatPump,        // pompa di calore
    AirConditioner,  // climatizzatore
    PelletStove,     // stufa a pellet
    SolarThermal,    // solare termico
}

public enum VisitKind
{
    /// <summary>Routine servicing (manutenzione ordinaria).</summary>
    Maintenance,

    /// <summary>Energy-efficiency check with its report (controllo di efficienza energetica, RCEE).</summary>
    EfficiencyCheck,

    /// <summary>A repair after a fault (riparazione). Does not count as routine servicing.</summary>
    Repair,
}

public static class Labels
{
    public static string Of(InstallationType type) => type switch
    {
        InstallationType.Boiler => "Boiler",
        InstallationType.HeatPump => "Heat pump",
        InstallationType.AirConditioner => "Air conditioner",
        InstallationType.PelletStove => "Pellet stove",
        InstallationType.SolarThermal => "Solar thermal",
        _ => type.ToString(),
    };

    public static string Of(VisitKind kind) => kind switch
    {
        VisitKind.Maintenance => "Maintenance",
        VisitKind.EfficiencyCheck => "Efficiency check (RCEE)",
        VisitKind.Repair => "Repair",
        _ => kind.ToString(),
    };

    public static string Of(MaintenanceStatus status) => status switch
    {
        MaintenanceStatus.Overdue => "OVERDUE",
        MaintenanceStatus.DueSoon => "Due soon",
        _ => "OK",
    };

    /// <summary>
    /// Parses a visit kind typed by a user. Accepts the English names and the Italian
    /// words technicians actually use (manutenzione, rcee, riparazione), ignoring case.
    /// </summary>
    public static bool TryParseVisitKind(string? text, out VisitKind kind)
    {
        switch (text?.Trim().ToLowerInvariant())
        {
            case "maintenance" or "manutenzione":
                kind = VisitKind.Maintenance;
                return true;
            case "efficiencycheck" or "efficiency" or "rcee" or "controllo":
                kind = VisitKind.EfficiencyCheck;
                return true;
            case "repair" or "riparazione":
                kind = VisitKind.Repair;
                return true;
            default:
                kind = default;
                return false;
        }
    }
}
