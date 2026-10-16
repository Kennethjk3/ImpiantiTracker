using ImpiantiTracker.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ImpiantiTracker.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Installation> Installations => Set<Installation>();
    public DbSet<ServiceVisit> ServiceVisits => Set<ServiceVisit>();

    // The domain classes in Core have no database attributes: all mapping lives here.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.Property(c => c.Name).HasMaxLength(120);
            e.Property(c => c.Phone).HasMaxLength(30);
            e.Property(c => c.Email).HasMaxLength(120);
            e.Property(c => c.Address).HasMaxLength(200);
            e.Property(c => c.City).HasMaxLength(80);
            e.HasIndex(c => c.Name);
        });

        modelBuilder.Entity<Installation>(e =>
        {
            e.ToTable("Installations", t =>
                t.HasCheckConstraint("CK_Installations_Interval", "[MaintenanceIntervalMonths] BETWEEN 1 AND 60"));

            // Enums are stored as text ('HeatPump', not 1) so plain SQL queries stay readable.
            e.Property(i => i.Type).HasConversion<string>().HasMaxLength(32);
            e.Property(i => i.Brand).HasMaxLength(60);
            e.Property(i => i.Model).HasMaxLength(80);
            e.Property(i => i.SerialNumber).HasMaxLength(60);
            e.Property(i => i.PowerKw).HasPrecision(6, 2);
            e.Property(i => i.RegistryCode).HasMaxLength(40);

            // A manufacturer never reuses a serial number. On SQL Server EF adds
            // "WHERE [SerialNumber] IS NOT NULL" so installations without one don't clash.
            e.HasIndex(i => new { i.Brand, i.SerialNumber }).IsUnique();

            e.HasOne(i => i.Customer)
                .WithMany(c => c.Installations)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceVisit>(e =>
        {
            e.ToTable("ServiceVisits", t =>
                t.HasCheckConstraint("CK_ServiceVisits_Cost", "[Cost] >= 0"));

            e.Property(v => v.Kind).HasConversion<string>().HasMaxLength(32);
            e.Property(v => v.Technician).HasMaxLength(80);
            e.Property(v => v.Notes).HasMaxLength(500);
            e.Property(v => v.Cost).HasPrecision(10, 2);

            // "Last visit of this installation" is the most common lookup.
            e.HasIndex(v => new { v.InstallationId, v.Date });

            e.HasOne(v => v.Installation)
                .WithMany(i => i.Visits)
                .HasForeignKey(v => v.InstallationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
