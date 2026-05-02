namespace LibraryApi.Infrastructure.Data;

public interface IDataSeeder
{
    /// <summary>
    /// Idempotently insert the demo accounts (admin + test user) and a couple
    /// of sample books on first boot. Safe to call on every startup; no-op
    /// when the data already exists.
    /// </summary>
    Task SeedAsync(CancellationToken ct = default);
}
