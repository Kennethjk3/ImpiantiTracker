using ImpiantiTracker.Core.Domain;

namespace ImpiantiTracker.Core.Reporting;

public sealed record VisitLine(DateOnly Date, VisitKind Kind, string Technician, decimal Cost);

public sealed record MonthTotal(int Month, int Visits, decimal Revenue);
public sealed record TechnicianTotal(string Technician, int Visits, decimal Revenue);
public sealed record KindTotal(VisitKind Kind, int Visits);

public sealed record YearReport(
    int Year,
    int TotalVisits,
    decimal TotalRevenue,
    IReadOnlyList<MonthTotal> ByMonth,
    IReadOnlyList<TechnicianTotal> ByTechnician,
    IReadOnlyList<KindTotal> ByKind);

public static class VisitReport
{
    /// <summary>
    /// Builds the yearly summary from the visits of that year. Grouping happens in memory:
    /// a small company has a few thousand visits a year, and this keeps the same code working
    /// on SQL Server and SQLite. For large volumes, the same grouping is in sql/02-yearly-report.sql.
    /// </summary>
    public static YearReport Build(int year, IEnumerable<VisitLine> visits)
    {
        var lines = visits.Where(v => v.Date.Year == year).ToList();

        var byMonth = lines
            .GroupBy(v => v.Date.Month)
            .Select(g => new MonthTotal(g.Key, g.Count(), g.Sum(v => v.Cost)))
            .OrderBy(m => m.Month)
            .ToList();

        var byTechnician = lines
            .GroupBy(v => v.Technician)
            .Select(g => new TechnicianTotal(g.Key, g.Count(), g.Sum(v => v.Cost)))
            .OrderByDescending(t => t.Visits)
            .ThenBy(t => t.Technician, StringComparer.Ordinal)
            .ToList();

        var byKind = lines
            .GroupBy(v => v.Kind)
            .Select(g => new KindTotal(g.Key, g.Count()))
            .OrderBy(k => k.Kind)
            .ToList();

        return new YearReport(year, lines.Count, lines.Sum(v => v.Cost), byMonth, byTechnician, byKind);
    }
}
