namespace ImpiantiTracker.Core.Domain;

/// <summary>A technician's visit to an installation (an "intervento").</summary>
public class ServiceVisit
{
    public int Id { get; set; }

    public int InstallationId { get; set; }
    public Installation? Installation { get; set; }

    public DateOnly Date { get; set; }
    public VisitKind Kind { get; set; }
    public required string Technician { get; set; }
    public string? Notes { get; set; }

    /// <summary>Amount charged, in euro.</summary>
    public decimal Cost { get; set; }
}
