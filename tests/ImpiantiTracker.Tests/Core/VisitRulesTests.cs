using ImpiantiTracker.Core.Domain;
using ImpiantiTracker.Core.Visits;

namespace ImpiantiTracker.Tests.Core;

public class VisitRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 16);
    private static readonly DateOnly InstalledOn = new(2024, 5, 1);

    private static NewVisit Visit(DateOnly? date = null, string technician = "A. Procopio", decimal cost = 90m, string? notes = null) =>
        new(1, date ?? Today, VisitKind.Maintenance, technician, cost, notes);

    [Fact]
    public void A_correct_visit_has_no_errors() =>
        Assert.Empty(VisitRules.Validate(Visit(), InstalledOn, Today));

    [Fact]
    public void Rejects_a_future_date() =>
        Assert.Contains("future", Assert.Single(VisitRules.Validate(Visit(Today.AddDays(1)), InstalledOn, Today)));

    [Fact]
    public void Rejects_a_date_before_installation() =>
        Assert.Contains("before the installation", Assert.Single(VisitRules.Validate(Visit(InstalledOn.AddDays(-1)), InstalledOn, Today)));

    [Fact]
    public void Requires_a_technician() =>
        Assert.Single(VisitRules.Validate(Visit(technician: "   "), InstalledOn, Today));

    [Fact]
    public void Rejects_a_negative_cost() =>
        Assert.Single(VisitRules.Validate(Visit(cost: -1m), InstalledOn, Today));

    [Fact]
    public void Rejects_notes_that_are_too_long() =>
        Assert.Single(VisitRules.Validate(Visit(notes: new string('x', VisitRules.MaxNotesLength + 1)), InstalledOn, Today));

    [Fact]
    public void Reports_every_problem_at_once() =>
        Assert.Equal(3, VisitRules.Validate(Visit(Today.AddDays(3), technician: "", cost: -5m), InstalledOn, Today).Count);
}
