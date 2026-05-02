using LibraryApi.Application.Auth;
using LibraryApi.Domain.Common;
using LibraryApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApi.Infrastructure.Data;

/// <summary>
/// Inserts the seed accounts (admin + test user) and a few demo books so a
/// fresh `docker compose up` is immediately usable for testers.
/// Idempotent: each user is keyed on email and skipped if it exists.
/// Hashes are produced through <see cref="IPasswordHasher"/> so seeded
/// passwords match the same login flow real users go through.
/// </summary>
public class DataSeeder : IDataSeeder
{
    public const string AdminEmail = "admin@bread.com";
    public const string AdminPassword = "AdminPass123";
    public const string AdminDisplayName = "Library Admin";

    public const string TestEmail = "test@bread.com";
    public const string TestPassword = "TestPass123";
    public const string TestDisplayName = "Test Crumb";

    private static readonly string[] DemoBookTitles =
    {
        "The Pragmatic Programmer",
        "Clean Code",
        "Designing Data-Intensive Applications"
    };

    private readonly LibraryDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(LibraryDbContext db, IPasswordHasher hasher, ILogger<DataSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var admin = await EnsureUserAsync(AdminEmail, AdminDisplayName, AdminPassword, UserRoles.Admin, ct);
        var tester = await EnsureUserAsync(TestEmail, TestDisplayName, TestPassword, UserRoles.User, ct);

        var anyBooks = await _db.Books.AnyAsync(ct);
        if (!anyBooks)
        {
            foreach (var title in DemoBookTitles)
            {
                _db.Books.Add(new Book
                {
                    Title = title,
                    OwnerId = tester.Id
                });
            }
            _logger.LogInformation("Seeded {Count} demo books owned by {Email}.", DemoBookTitles.Length, tester.Email);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<User> EnsureUserAsync(string email, string displayName, string password, string role, CancellationToken ct)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existing is not null) return existing;

        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = _hasher.Hash(password),
            Role = role
        };
        _db.Users.Add(user);
        _logger.LogInformation("Seeded {Role} account: {Email}", role, email);
        return user;
    }
}
