using LibraryApi.Domain.Entities;

namespace LibraryApi.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
