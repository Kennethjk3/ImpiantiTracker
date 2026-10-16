using ImpiantiTracker.Core.Domain;

namespace ImpiantiTracker.Core.Visits;

/// <summary>A visit as entered by the user, before it is validated and saved.</summary>
public sealed record NewVisit(
    int InstallationId,
    DateOnly Date,
    VisitKind Kind,
    string Technician,
    decimal Cost,
    string? Notes);

public static class VisitRules
{
    public const int MaxNotesLength = 500;

    /// <summary>Returns every problem with the visit, so the user can fix them all at once. Empty means valid.</summary>
    public static IReadOnlyList<string> Validate(NewVisit visit, DateOnly installedOn, DateOnly today)
    {
        var errors = new List<string>();

        if (visit.Date > today)
            errors.Add($"The visit date {visit.Date:yyyy-MM-dd} is in the future.");
        if (visit.Date < installedOn)
            errors.Add($"The visit date {visit.Date:yyyy-MM-dd} is before the installation date {installedOn:yyyy-MM-dd}.");
        if (string.IsNullOrWhiteSpace(visit.Technician))
            errors.Add("The technician's name is required.");
        if (visit.Cost < 0)
            errors.Add("The cost cannot be negative.");
        if (visit.Notes?.Length > MaxNotesLength)
            errors.Add($"Notes can be at most {MaxNotesLength} characters.");

        return errors;
    }
}
