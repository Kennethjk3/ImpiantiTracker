namespace ImpiantiTracker.Core.Domain;

/// <summary>A customer of the company: a household, a shop, a condominium.</summary>
public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public required string Address { get; set; }
    public required string City { get; set; }

    public List<Installation> Installations { get; set; } = [];
}
