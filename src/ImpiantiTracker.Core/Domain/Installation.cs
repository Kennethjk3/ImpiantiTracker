namespace ImpiantiTracker.Core.Domain;

/// <summary>A heating or cooling system installed at a customer's site (an "impianto").</summary>
public class Installation
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public InstallationType Type { get; set; }
    public required string Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }

    /// <summary>Nominal power in kW.</summary>
    public decimal PowerKw { get; set; }

    public DateOnly InstalledOn { get; set; }

    /// <summary>How often the system needs servicing, in months (agreed with the customer or set by the manufacturer).</summary>
    public int MaintenanceIntervalMonths { get; set; } = 12;

    /// <summary>Code from the regional heating-system registry (for example a CIT-CAL "targa impianto"), when the system has one.</summary>
    public string? RegistryCode { get; set; }

    public List<ServiceVisit> Visits { get; set; } = [];
}
