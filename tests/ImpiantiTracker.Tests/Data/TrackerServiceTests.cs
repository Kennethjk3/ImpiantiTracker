using ImpiantiTracker.Core.Domain;
using ImpiantiTracker.Core.Maintenance;
using ImpiantiTracker.Core.Visits;
using ImpiantiTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace ImpiantiTracker.Tests.Data;

public class TrackerServiceTests
{
    private static readonly DateOnly Today = TestDatabase.Today;

    [Fact]
    public async Task The_plan_lists_overdue_installations_first_then_those_due_soon()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.NewContext();

        var plan = await new TrackerService(db).GetMaintenancePlanAsync(Today, warningDays: 30);

        Assert.Equal(
            ["Studio Medico Greco", "Condominio Le Palme", "Hotel Belvedere", "Ferraro Anna", "Mancuso Luca"],
            plan.Select(p => p.Customer));
        Assert.Equal(3, plan.Count(p => p.Status == MaintenanceStatus.Overdue));
        Assert.Equal(2, plan.Count(p => p.Status == MaintenanceStatus.DueSoon));
    }

    [Fact]
    public async Task A_wider_window_brings_in_more_installations()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.NewContext();

        var plan = await new TrackerService(db).GetMaintenancePlanAsync(Today, warningDays: 60);

        Assert.Contains(plan, p => p.Customer == "Bar Centrale" && p.Status == MaintenanceStatus.DueSoon);
    }

    [Fact]
    public async Task Repairs_do_not_count_as_servicing()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.NewContext();

        // Le Palme had a repair two months ago, but its last routine service was 13 months ago.
        var lePalme = (await new TrackerService(db).GetMaintenancePlanAsync(Today, 30))
            .Single(p => p.Customer == "Condominio Le Palme");

        Assert.Equal(Today.AddMonths(-13), lePalme.LastService);
        Assert.Equal(MaintenanceStatus.Overdue, lePalme.Status);
    }

    [Fact]
    public async Task Recording_a_service_moves_the_next_due_date()
    {
        await using var database = await TestDatabase.CreateAsync();
        var installationId = await InstallationOf(database, "Condominio Le Palme");

        await using (var db = database.NewContext())
        {
            var result = await new TrackerService(db).AddVisitAsync(
                new NewVisit(installationId, Today, VisitKind.Maintenance, "K. Kier", 140m, "Annual service"), Today);
            Assert.True(result.Success);
        }

        await using (var db = database.NewContext())
        {
            var item = (await new TrackerService(db).GetMaintenancePlanAsync(Today, 30, includeOk: true))
                .Single(p => p.InstallationId == installationId);

            Assert.Equal(Today.AddMonths(12), item.NextDue);
            Assert.Equal(MaintenanceStatus.Ok, item.Status);
        }
    }

    [Fact]
    public async Task An_invalid_visit_is_not_saved()
    {
        await using var database = await TestDatabase.CreateAsync();
        var installationId = await InstallationOf(database, "Ferraro Anna");

        await using var db = database.NewContext();
        var before = await db.ServiceVisits.CountAsync();

        var result = await new TrackerService(db).AddVisitAsync(
            new NewVisit(installationId, Today.AddDays(1), VisitKind.Repair, "K. Kier", 50m, null), Today);

        Assert.False(result.Success);
        Assert.Contains("future", Assert.Single(result.Errors));
        Assert.Equal(before, await db.ServiceVisits.CountAsync());
    }

    [Fact]
    public async Task A_visit_for_an_unknown_installation_is_rejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.NewContext();

        var result = await new TrackerService(db).AddVisitAsync(
            new NewVisit(999, Today, VisitKind.Maintenance, "K. Kier", 0m, null), Today);

        Assert.False(result.Success);
        Assert.Contains("999", Assert.Single(result.Errors));
    }

    [Fact]
    public async Task The_city_filter_ignores_case()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.NewContext();

        var rows = await new TrackerService(db).ListInstallationsAsync("catanzaro");

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal("Catanzaro", r.City));
    }

    [Fact]
    public async Task Show_returns_the_history_newest_first()
    {
        await using var database = await TestDatabase.CreateAsync();
        var installationId = await InstallationOf(database, "Condominio Le Palme");
        await using var db = database.NewContext();

        var installation = await new TrackerService(db).GetInstallationAsync(installationId);

        Assert.NotNull(installation);
        Assert.Equal("Condominio Le Palme", installation.Customer!.Name);
        Assert.Equal(3, installation.Visits.Count);
        Assert.Equal(installation.Visits.Select(v => v.Date).OrderByDescending(d => d), installation.Visits.Select(v => v.Date));
    }

    [Fact]
    public async Task The_year_report_only_counts_that_year()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.NewContext();

        var report = await new TrackerService(db).GetYearReportAsync(2026);

        Assert.Equal(8, report.TotalVisits);
        Assert.Equal(940m, report.TotalRevenue);
        Assert.Equal(new[] { "A. Procopio", "G. Rotella" }, report.ByTechnician.Select(t => t.Technician));
    }

    [Fact]
    public async Task Initializing_again_does_not_duplicate_the_sample_data()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.NewContext();

        var seededAgain = await DatabaseInitializer.InitializeAsync(db, DatabaseProvider.Sqlite, Today);

        Assert.False(seededAgain);
        Assert.Equal(7, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task The_database_refuses_a_duplicate_serial_number_for_the_same_brand()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.NewContext();
        var customerId = await db.Customers.Select(c => c.Id).FirstAsync();

        db.Installations.Add(new Installation
        {
            CustomerId = customerId, Type = InstallationType.HeatPump, Brand = "Daikin", SerialNumber = "DK-48213",
            PowerKw = 8m, InstalledOn = Today,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static async Task<int> InstallationOf(TestDatabase database, string customer)
    {
        await using var db = database.NewContext();
        return await db.Installations.Where(i => i.Customer!.Name == customer).Select(i => i.Id).FirstAsync();
    }
}
