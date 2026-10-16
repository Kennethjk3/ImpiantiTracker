using Microsoft.EntityFrameworkCore;

namespace ImpiantiTracker.Data;

public static class DatabaseInitializer
{
    /// <summary>
    /// Creates or updates the schema, then loads the sample data if the database is empty.
    /// Returns true when sample data was added.
    /// SQL Server uses the migrations in /Migrations (the production path).
    /// SQLite, used for quick local runs and tests, creates the schema straight from the model.
    /// </summary>
    public static async Task<bool> InitializeAsync(
        AppDbContext db, DatabaseProvider provider, DateOnly today, bool reset = false, CancellationToken ct = default)
    {
        if (reset)
            await db.Database.EnsureDeletedAsync(ct);

        if (provider == DatabaseProvider.SqlServer)
            await db.Database.MigrateAsync(ct);
        else
            await db.Database.EnsureCreatedAsync(ct);

        if (await db.Customers.AnyAsync(ct))
            return false;

        db.Customers.AddRange(SeedData.Create(today));
        await db.SaveChangesAsync(ct);
        return true;
    }
}
