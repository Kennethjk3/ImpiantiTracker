using ImpiantiTracker.Core.Domain;
using ImpiantiTracker.Core.Maintenance;

namespace ImpiantiTracker.Tests.Core;

public class MaintenanceScheduleTests
{
    private static readonly DateOnly Today = new(2026, 10, 16);

    [Fact]
    public void Never_serviced_counts_from_the_installation_date() =>
        Assert.Equal(new DateOnly(2026, 3, 10), MaintenanceSchedule.NextDue(new DateOnly(2025, 3, 10), 12, lastService: null));

    [Fact]
    public void Counts_from_the_last_service() =>
        Assert.Equal(new DateOnly(2027, 2, 15), MaintenanceSchedule.NextDue(new DateOnly(2020, 1, 1), 12, new DateOnly(2026, 2, 15)));

    [Fact]
    public void A_service_dated_before_installation_is_ignored() =>
        Assert.Equal(new DateOnly(2026, 7, 1), MaintenanceSchedule.NextDue(new DateOnly(2026, 1, 1), 6, new DateOnly(2025, 12, 1)));

    [Theory]
    [InlineData(2026, 2026, 2, 28)] // 31 Jan + 1 month: no 31 Feb, so the last day of February
    [InlineData(2028, 2028, 2, 29)] // leap year
    public void End_of_month_is_clamped(int installedYear, int year, int month, int day) =>
        Assert.Equal(new DateOnly(year, month, day), MaintenanceSchedule.NextDue(new DateOnly(installedYear, 1, 31), 1, null));

    [Theory]
    [InlineData(0)]
    [InlineData(-6)]
    public void Interval_must_be_positive(int months) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MaintenanceSchedule.NextDue(Today, months, null));

    [Theory]
    [InlineData("2026-10-15", MaintenanceStatus.Overdue)]  // yesterday
    [InlineData("2026-10-16", MaintenanceStatus.DueSoon)]  // today: due, not yet late
    [InlineData("2026-11-15", MaintenanceStatus.DueSoon)]  // last day of the 30-day window
    [InlineData("2026-11-16", MaintenanceStatus.Ok)]       // outside the window
    public void Classify_uses_the_warning_window(string nextDue, MaintenanceStatus expected) =>
        Assert.Equal(expected, MaintenanceSchedule.Classify(
            DateOnly.ParseExact(nextDue, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), Today, warningDays: 30));

    [Theory]
    [InlineData(VisitKind.Maintenance, true)]
    [InlineData(VisitKind.EfficiencyCheck, true)]
    [InlineData(VisitKind.Repair, false)]
    public void Only_routine_visits_reset_the_clock(VisitKind kind, bool expected) =>
        Assert.Equal(expected, MaintenanceSchedule.CountsAsService(kind));

    [Fact]
    public void Plan_puts_the_most_urgent_first_and_hides_ok_by_default()
    {
        var plan = MaintenanceSchedule.Plan(
        [
            Snapshot(1, "Fine", lastService: Today.AddMonths(-2)),
            Snapshot(2, "Soon", lastService: Today.AddMonths(-12).AddDays(10)),
            Snapshot(3, "Late", lastService: Today.AddMonths(-14)),
        ], Today, warningDays: 30);

        Assert.Equal(["Late", "Soon"], plan.Select(p => p.Customer));
        Assert.Equal(MaintenanceStatus.Overdue, plan[0].Status);
        Assert.True(plan[0].DaysUntilDue < 0);
        Assert.Equal(10, plan[1].DaysUntilDue);
    }

    [Fact]
    public void Plan_can_include_everything()
    {
        var plan = MaintenanceSchedule.Plan([Snapshot(1, "Fine", Today.AddMonths(-2))], Today, 30, includeOk: true);

        var item = Assert.Single(plan);
        Assert.Equal(MaintenanceStatus.Ok, item.Status);
        Assert.Equal(Today.AddMonths(10), item.NextDue);
    }

    private static InstallationSnapshot Snapshot(int id, string customer, DateOnly? lastService) =>
        new(id, customer, "Catanzaro", null, InstallationType.Boiler, "Vaillant",
            InstalledOn: new DateOnly(2020, 1, 1), MaintenanceIntervalMonths: 12, lastService);
}
