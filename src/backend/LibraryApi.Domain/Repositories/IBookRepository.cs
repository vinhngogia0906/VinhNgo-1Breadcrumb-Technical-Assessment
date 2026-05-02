using LibraryApi.Domain.Common;
using LibraryApi.Domain.Entities;

namespace LibraryApi.Domain.Repositories;

public interface IBookRepository
{
    Task<PagedResult<Book>> GetPagedAsync(
        string? search,
        AvailabilityFilter availability,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Book book, CancellationToken ct = default);

    void Update(Book book);

    void Remove(Book book);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
