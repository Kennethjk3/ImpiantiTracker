using System.Text;
using ImpiantiTracker.Core.Domain;
using ImpiantiTracker.Core.Reporting;
using ImpiantiTracker.Core.Visits;
using ImpiantiTracker.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ImpiantiTracker.Api;

public static class Endpoints
{
    private const int MaxWindowDays = 365;

    public static IEndpointRouteBuilder MapTrackerApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/installations", ListInstallations)
            .WithName("ListInstallations")
            .WithSummary("Every installation with its customer and last visit, optionally for one city.");

        api.MapGet("/installations/{id:int}", GetInstallation)
            .WithName("GetInstallation")
            .WithSummary("One installation with its customer, visit history (newest first) and maintenance status.");

        api.MapPost("/installations/{id:int}/visits", AddVisit)
            .WithName("AddVisit")
            .WithSummary("Record a visit. Maintenance and efficiency checks move the next due date; repairs don't.");

        api.MapGet("/maintenance", GetMaintenancePlan)
            .WithName("GetMaintenancePlan")
            .WithSummary("Installations overdue or due within 'days' (default from settings); all=true includes the rest.");

        api.MapGet("/maintenance/export", ExportMaintenancePlan)
            .WithName("ExportMaintenancePlan")
            .WithSummary("The maintenance plan as a CSV file for Excel (';' separated, UTF-8 with BOM).");

        api.MapGet("/reports/{year:int}", GetYearReport)
            .WithName("GetYearReport")
            .WithSummary("Visits and revenue for one year, by month, technician and visit type.");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<InstallationRow>>> ListInstallations(
        TrackerService service, string? city, CancellationToken ct) =>
        TypedResults.Ok(await service.ListInstallationsAsync(city, ct));

    private static async Task<Results<Ok<InstallationDetailResponse>, ProblemHttpResult>> GetInstallation(
        int id, TrackerService service, TimeProvider clock, ApiSettings settings, CancellationToken ct)
    {
        var detail = await service.GetInstallationDetailAsync(id, clock.Today(), settings.WarningDays, ct);
        return detail is null
            ? InstallationNotFound(id)
            : TypedResults.Ok(InstallationDetailResponse.From(detail));
    }

    private static async Task<Results<Created<VisitCreatedResponse>, ValidationProblem, ProblemHttpResult>> AddVisit(
        int id, VisitRequest request, TrackerService service, TimeProvider clock, ApiSettings settings, CancellationToken ct)
    {
        if (!Labels.TryParseVisitKind(request.Kind, out var kind))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["kind"] = [$"Unknown visit kind '{request.Kind}'. Use Maintenance, EfficiencyCheck or Repair."],
            });
        }

        var today = clock.Today();
        var visit = new NewVisit(id, request.Date ?? today, kind, request.Technician ?? "", request.Cost ?? 0m, request.Notes);

        var result = await service.AddVisitAsync(visit, today, ct);
        if (result.NotFound)
            return InstallationNotFound(id);
        if (!result.Success)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["visit"] = [.. result.Errors] });

        var detail = await service.GetInstallationDetailAsync(id, today, settings.WarningDays, ct);
        return TypedResults.Created($"/api/installations/{id}", new VisitCreatedResponse(result.VisitId!.Value, detail!.Due));
    }

    private static async Task<Results<Ok<MaintenancePlanResponse>, ValidationProblem>> GetMaintenancePlan(
        TrackerService service, TimeProvider clock, ApiSettings settings, int? days, bool? all, CancellationToken ct)
    {
        var window = days ?? settings.WarningDays;
        if (window is < 0 or > MaxWindowDays)
            return DaysOutOfRange();

        var today = clock.Today();
        var plan = await service.GetMaintenancePlanAsync(today, window, all ?? false, ct);
        return TypedResults.Ok(new MaintenancePlanResponse(today, window, plan));
    }

    private static async Task<Results<FileContentHttpResult, ValidationProblem>> ExportMaintenancePlan(
        TrackerService service, TimeProvider clock, ApiSettings settings, int? days, bool? all, CancellationToken ct)
    {
        var window = days ?? settings.WarningDays;
        if (window is < 0 or > MaxWindowDays)
            return DaysOutOfRange();

        var today = clock.Today();
        var plan = await service.GetMaintenancePlanAsync(today, window, all ?? false, ct);

        var text = string.Concat(MaintenancePlanCsv.Lines(plan).Select(line => line + "\r\n"));
        byte[] bytes = [.. Csv.ExcelEncoding.GetPreamble(), .. Csv.ExcelEncoding.GetBytes(text)];
        return TypedResults.File(bytes, "text/csv; charset=utf-8", $"maintenance-plan-{today:yyyy-MM-dd}.csv");
    }

    private static async Task<Results<Ok<YearReport>, ValidationProblem>> GetYearReport(
        int year, TrackerService service, CancellationToken ct)
    {
        if (year is < 2000 or > 2100)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["year"] = ["The year must be between 2000 and 2100."],
            });
        }
        return TypedResults.Ok(await service.GetYearReportAsync(year, ct));
    }

    private static ProblemHttpResult InstallationNotFound(int id) =>
        TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Installation not found",
            detail: $"Installation {id} does not exist.");

    private static ValidationProblem DaysOutOfRange() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["days"] = [$"days must be between 0 and {MaxWindowDays}."],
        });
}
