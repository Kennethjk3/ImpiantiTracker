using ImpiantiTracker.Core.Domain;

namespace ImpiantiTracker.Core.Maintenance;

public enum MaintenanceStatus
{
    Ok,
    DueSoon,
    Overdue,
}

/// <summary>What the scheduler needs to know about one installation. Built by the data layer in a single query.</summary>
public sealed record InstallationSnapshot(
    int InstallationId,
    string Customer,
    string City,
    string? Phone,
    InstallationType Type,
    string Brand,
    DateOnly InstalledOn,
    int MaintenanceIntervalMonths,
    DateOnly? LastService);

/// <summary>One line of the maintenance plan: when the installation is due and how urgent it is.</summary>
public sealed record DueItem(
    int InstallationId,
    string Customer,
    string City,
    string? Phone,
    InstallationType Type,
    string Brand,
    DateOnly? LastService,
    DateOnly NextDue,
    MaintenanceStatus Status,
    int DaysUntilDue);

/// <summary>The maintenance rules. Pure functions: no database, no clock (today is always passed in).</summary>
public static class MaintenanceSchedule
{
    /// <summary>
    /// Routine maintenance and efficiency checks reset the maintenance clock; repairs do not.
    /// The data layer filters with the same rule in SQL; a test keeps the two in step.
    /// </summary>
    public static bool CountsAsService(VisitKind kind) =>
        kind is VisitKind.Maintenance or VisitKind.EfficiencyCheck;

    /// <summary>
    /// Next due date = last service (or the installation date, if never serviced) + the interval.
    /// AddMonths clamps to the end of the month: 31 Jan + 1 month = 28/29 Feb, like DATEADD in SQL Server.
    /// </summary>
    public static DateOnly NextDue(DateOnly installedOn, int intervalMonths, DateOnly? lastService)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(intervalMonths);
        var from = lastService is { } last && last > installedOn ? last : installedOn;
        return from.AddMonths(intervalMonths);
    }

    public static MaintenanceStatus Classify(DateOnly nextDue, DateOnly today, int warningDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(warningDays);
        if (nextDue < today) return MaintenanceStatus.Overdue;
        if (nextDue <= today.AddDays(warningDays)) return MaintenanceStatus.DueSoon;
        return MaintenanceStatus.Ok;
    }

    public static DueItem Evaluate(InstallationSnapshot s, DateOnly today, int warningDays)
    {
        var nextDue = NextDue(s.InstalledOn, s.MaintenanceIntervalMonths, s.LastService);
        return new DueItem(
            s.InstallationId, s.Customer, s.City, s.Phone, s.Type, s.Brand,
            s.LastService, nextDue,
            Classify(nextDue, today, warningDays),
            nextDue.DayNumber - today.DayNumber);
    }

    /// <summary>The maintenance plan: most urgent first. By default only installations that need attention.</summary>
    public static IReadOnlyList<DueItem> Plan(
        IEnumerable<InstallationSnapshot> installations, DateOnly today, int warningDays, bool includeOk = false) =>
        installations
            .Select(s => Evaluate(s, today, warningDays))
            .Where(d => includeOk || d.Status != MaintenanceStatus.Ok)
            .OrderBy(d => d.NextDue)
            .ThenBy(d => d.Customer, StringComparer.Ordinal)
            .ToList();
}
