using LibraryApi.Domain.Common;
using LibraryApi.Domain.Entities;
using LibraryApi.Domain.Repositories;
using LibraryApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Infrastructure.Repositories;

public class BookActivityRepository : IBookActivityRepository
{
    private readonly LibraryDbContext _db;

    public BookActivityRepository(LibraryDbContext db) => _db = db;

    public async Task AddAsync(BookActivity activity, CancellationToken ct = default) =>
        await _db.BookActivities.AddAsync(activity, ct);

    public async Task<PagedResult<BookActivity>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.BookActivities.AsNoTracking();

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<BookActivity>(items, page, pageSize, total);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
