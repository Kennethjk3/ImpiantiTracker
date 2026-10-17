using System.Net;
using System.Net.Http.Json;
using System.Text;
using ImpiantiTracker.Core.Domain;
using ImpiantiTracker.Core.Maintenance;
using ImpiantiTracker.Core.Reporting;
using ImpiantiTracker.Data;
using Microsoft.AspNetCore.Mvc;

namespace ImpiantiTracker.Api.Tests;

/// <summary>
/// Each test starts the API on a fresh database (new ApiFactory), so tests that add visits
/// can't affect one another. Requests go through the full pipeline: routing, binding, JSON, EF Core, SQL.
/// </summary>
public class ApiTests
{
    private static readonly DateOnly Today = ApiFactory.Today;

    [Fact]
    public async Task Health_check_answers()
    {
        await using var api = new ApiFactory();
        var response = await api.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_plan_lists_what_needs_attention_most_urgent_first()
    {
        await using var api = new ApiFactory();

        var plan = await api.CreateClient().GetFromJsonAsync<MaintenancePlanResponse>("/api/maintenance", ApiFactory.Json);

        Assert.NotNull(plan);
        Assert.Equal(Today, plan.AsOf);
        Assert.Equal(30, plan.WarningDays);
        Assert.Equal(
            ["Studio Medico Greco", "Condominio Le Palme", "Hotel Belvedere", "Ferraro Anna", "Mancuso Luca"],
            plan.Items.Select(i => i.Customer));
        Assert.Equal(MaintenanceStatus.Overdue, plan.Items[0].Status);
    }

    [Fact]
    public async Task The_plan_can_include_every_installation()
    {
        await using var api = new ApiFactory();

        var plan = await api.CreateClient().GetFromJsonAsync<MaintenancePlanResponse>("/api/maintenance?all=true", ApiFactory.Json);

        Assert.Equal(9, plan!.Items.Count);
        Assert.Contains(plan.Items, i => i.Status == MaintenanceStatus.Ok);
    }

    [Fact]
    public async Task Enums_and_dates_travel_as_readable_strings()
    {
        await using var api = new ApiFactory();

        var json = await api.CreateClient().GetStringAsync("/api/maintenance");

        Assert.Contains("\"status\":\"Overdue\"", json);
        Assert.Contains("\"type\":\"AirConditioner\"", json);
        Assert.Contains("\"asOf\":\"2026-10-17\"", json);
    }

    [Theory]
    [InlineData("/api/maintenance?days=-1")]
    [InlineData("/api/maintenance?days=366")]
    [InlineData("/api/maintenance?days=abc")]
    [InlineData("/api/maintenance/export?days=-5")]
    [InlineData("/api/reports/1990")]
    public async Task Bad_query_values_are_400s(string url)
    {
        await using var api = new ApiFactory();

        var response = await api.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Installations_can_be_filtered_by_city()
    {
        await using var api = new ApiFactory();

        var rows = await api.CreateClient().GetFromJsonAsync<List<InstallationRow>>("/api/installations?city=catanzaro", ApiFactory.Json);

        Assert.Equal(2, rows!.Count);
        Assert.All(rows, r => Assert.Equal("Catanzaro", r.City));
    }

    [Fact]
    public async Task An_installation_comes_with_its_customer_history_and_status()
    {
        await using var api = new ApiFactory();

        var detail = await api.CreateClient().GetFromJsonAsync<InstallationDetailResponse>("/api/installations/1", ApiFactory.Json);

        Assert.NotNull(detail);
        Assert.Equal("Condominio Le Palme", detail.Customer.Name);
        Assert.Equal(3, detail.Visits.Count);
        Assert.Equal(VisitKind.Repair, detail.Visits[0].Kind); // newest first
        Assert.Equal(MaintenanceStatus.Overdue, detail.Due.Status);
        Assert.Equal(Today.AddMonths(-13), detail.Due.LastService); // the repair doesn't count
    }

    [Fact]
    public async Task An_unknown_installation_is_a_404_problem()
    {
        await using var api = new ApiFactory();

        var response = await api.CreateClient().GetAsync("/api/installations/999");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Installation 999 does not exist.", problem!.Detail);
    }

    [Fact]
    public async Task Recording_a_service_returns_201_and_the_new_due_date()
    {
        await using var api = new ApiFactory();
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/installations/1/visits",
            new { kind = "Maintenance", technician = "K. Kier", cost = 140.50m, notes = "Annual service" });
        var created = await response.Content.ReadFromJsonAsync<VisitCreatedResponse>(ApiFactory.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/installations/1", response.Headers.Location?.OriginalString);
        Assert.Equal(Today.AddMonths(12), created!.Due.NextDue);
        Assert.Equal(MaintenanceStatus.Ok, created.Due.Status);

        // And it's really saved: the plan no longer lists installation 1.
        var plan = await client.GetFromJsonAsync<MaintenancePlanResponse>("/api/maintenance", ApiFactory.Json);
        Assert.DoesNotContain(plan!.Items, i => i.InstallationId == 1);
    }

    [Fact]
    public async Task Italian_visit_kinds_are_accepted()
    {
        await using var api = new ApiFactory();

        var response = await api.CreateClient().PostAsJsonAsync("/api/installations/2/visits",
            new { kind = "riparazione", technician = "G. Rotella" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task An_invalid_visit_returns_every_problem_and_saves_nothing()
    {
        await using var api = new ApiFactory();
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/installations/1/visits",
            new { date = "2030-01-01", kind = "Repair", technician = "", cost = -5 });
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(3, problem!.Errors["visit"].Length);

        var detail = await client.GetFromJsonAsync<InstallationDetailResponse>("/api/installations/1", ApiFactory.Json);
        Assert.Equal(3, detail!.Visits.Count);
    }

    [Fact]
    public async Task An_unknown_visit_kind_is_named_in_the_error()
    {
        await using var api = new ApiFactory();

        var response = await api.CreateClient().PostAsJsonAsync("/api/installations/1/visits",
            new { kind = "service", technician = "X" });
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("service", Assert.Single(problem!.Errors["kind"]));
    }

    [Fact]
    public async Task Malformed_json_is_a_400_not_a_500()
    {
        await using var api = new ApiFactory();

        var response = await api.CreateClient().PostAsync("/api/installations/1/visits",
            new StringContent("""{ "date": "17/10/2026", "kind": "Repair", "technician": "X" }""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_visit_for_an_unknown_installation_is_a_404()
    {
        await using var api = new ApiFactory();

        var response = await api.CreateClient().PostAsJsonAsync("/api/installations/999/visits",
            new { kind = "Repair", technician = "X" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_year_report_has_totals_and_breakdowns()
    {
        await using var api = new ApiFactory();

        var report = await api.CreateClient().GetFromJsonAsync<YearReport>("/api/reports/2026", ApiFactory.Json);

        Assert.Equal(8, report!.TotalVisits);
        Assert.Equal(940m, report.TotalRevenue);
        Assert.Equal(["A. Procopio", "G. Rotella"], report.ByTechnician.Select(t => t.Technician));
    }

    [Fact]
    public async Task The_csv_export_is_a_file_excel_can_open()
    {
        await using var api = new ApiFactory();

        var response = await api.CreateClient().GetAsync("/api/maintenance/export");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var lines = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("maintenance-plan-2026-10-17.csv", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        Assert.Equal(string.Join(';', MaintenancePlanCsv.Header), lines[0]);
        Assert.Equal(1 + 5, lines.Length);
    }

    [Fact]
    public async Task The_openapi_description_lists_every_endpoint()
    {
        await using var api = new ApiFactory();

        var json = await api.CreateClient().GetStringAsync("/openapi/v1.json");

        foreach (var path in new[] { "/api/installations", "/api/installations/{id}", "/api/installations/{id}/visits",
                     "/api/maintenance", "/api/maintenance/export", "/api/reports/{year}" })
        {
            Assert.Contains($"\"{path}\"", json);
        }
    }
}
