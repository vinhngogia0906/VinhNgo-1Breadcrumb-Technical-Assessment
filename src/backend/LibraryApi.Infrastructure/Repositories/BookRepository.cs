using LibraryApi.Domain.Common;
using LibraryApi.Domain.Entities;
using LibraryApi.Domain.Repositories;
using LibraryApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Infrastructure.Repositories;

public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _db;

    public BookRepository(LibraryDbContext db) => _db = db;

    public async Task<PagedResult<Book>> GetPagedAsync(
        string? search,
        AvailabilityFilter availability,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var query = _db.Books
            .AsNoTracking()
            .Include(b => b.Owner)
            .Include(b => b.Borrower)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(b => EF.Functions.ILike(b.Title, pattern));
        }

        query = availability switch
        {
            AvailabilityFilter.Available => query.Where(b => b.BorrowerId == null),
            AvailabilityFilter.Unavailable => query.Where(b => b.BorrowerId != null),
            _ => query
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(b => b.Title)
            .ThenBy(b => b.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Book>(items, page, pageSize, total);
    }

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Books
            .Include(b => b.Owner)
            .Include(b => b.Borrower)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task AddAsync(Book book, CancellationToken ct = default) =>
        await _db.Books.AddAsync(book, ct);

    public void Update(Book book) => _db.Books.Update(book);

    public void Remove(Book book) => _db.Books.Remove(book);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
