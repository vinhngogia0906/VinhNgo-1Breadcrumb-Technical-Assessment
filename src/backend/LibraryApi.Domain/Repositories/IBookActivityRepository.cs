using LibraryApi.Domain.Common;
using LibraryApi.Domain.Entities;

namespace LibraryApi.Domain.Repositories;

public interface IBookActivityRepository
{
    Task AddAsync(BookActivity activity, CancellationToken ct = default);

    Task<PagedResult<BookActivity>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
