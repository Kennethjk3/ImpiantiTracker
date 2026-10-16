using ImpiantiTracker.Core.Domain;
using ImpiantiTracker.Core.Reporting;

namespace ImpiantiTracker.Tests.Core;

public class CsvTests
{
    [Theory]
    [InlineData("Ferraro Anna", "Ferraro Anna")]
    [InlineData("80,50", "80,50")]                       // commas are fine: ';' is the separator
    [InlineData("Bar; Centrale", "\"Bar; Centrale\"")]   // the separator forces quotes
    [InlineData("Detto \"Il Gabbiano\"", "\"Detto \"\"Il Gabbiano\"\"\"")] // inner quotes are doubled
    [InlineData("line 1\nline 2", "\"line 1\nline 2\"")]
    [InlineData(null, "")]
    public void Escapes_only_when_needed(string? value, string expected) =>
        Assert.Equal(expected, Csv.Escape(value));

    [Fact]
    public void Row_joins_with_semicolons() =>
        Assert.Equal("1;OVERDUE;;Hotel Belvedere", Csv.Row(["1", "OVERDUE", null, "Hotel Belvedere"]));

    [Fact]
    public void Excel_encoding_writes_a_byte_order_mark() =>
        Assert.Equal([0xEF, 0xBB, 0xBF], Csv.ExcelEncoding.GetPreamble());
}

public class VisitReportTests
{
    [Fact]
    public void Groups_by_month_technician_and_kind_for_one_year_only()
    {
        var report = VisitReport.Build(2026,
        [
            new VisitLine(new DateOnly(2026, 3, 2), VisitKind.Maintenance, "A. Procopio", 100m),
            new VisitLine(new DateOnly(2026, 3, 20), VisitKind.Repair, "G. Rotella", 50.50m),
            new VisitLine(new DateOnly(2026, 7, 1), VisitKind.Maintenance, "A. Procopio", 80m),
            new VisitLine(new DateOnly(2025, 12, 31), VisitKind.Maintenance, "A. Procopio", 999m), // other year
        ]);

        Assert.Equal(3, report.TotalVisits);
        Assert.Equal(230.50m, report.TotalRevenue);
        Assert.Equal([new MonthTotal(3, 2, 150.50m), new MonthTotal(7, 1, 80m)], report.ByMonth);
        Assert.Equal("A. Procopio", report.ByTechnician[0].Technician);
        Assert.Equal(2, report.ByTechnician[0].Visits);
        Assert.Equal([new KindTotal(VisitKind.Maintenance, 2), new KindTotal(VisitKind.Repair, 1)], report.ByKind);
    }
}

public class LabelsTests
{
    [Theory]
    [InlineData("maintenance", VisitKind.Maintenance)]
    [InlineData("Manutenzione", VisitKind.Maintenance)]
    [InlineData("RCEE", VisitKind.EfficiencyCheck)]
    [InlineData(" repair ", VisitKind.Repair)]
    [InlineData("riparazione", VisitKind.Repair)]
    public void Parses_english_and_italian_visit_kinds(string text, VisitKind expected)
    {
        Assert.True(Labels.TryParseVisitKind(text, out var kind));
        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData("5")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("service")]
    public void Rejects_anything_else(string? text) =>
        Assert.False(Labels.TryParseVisitKind(text, out _));
}
