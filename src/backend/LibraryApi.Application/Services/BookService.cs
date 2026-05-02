using LibraryApi.Application.DTOs;
using LibraryApi.Application.Mapping;
using LibraryApi.Domain.Common;
using LibraryApi.Domain.Entities;
using LibraryApi.Domain.Exceptions;
using LibraryApi.Domain.Repositories;

namespace LibraryApi.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _books;
    private readonly IUserRepository _users;

    public BookService(IBookRepository books, IUserRepository users)
    {
        _books = books;
        _users = users;
    }

    public async Task<PagedResult<BookDto>> SearchAsync(
        string? search,
        AvailabilityFilter availability,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var result = await _books.GetPagedAsync(search, availability, page, pageSize, ct);
        return result.ToDto();
    }

    public async Task<BookDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var book = await _books.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Book), id);
        return book.ToDto();
    }

    public async Task<BookDto> CreateAsync(Guid ownerId, CreateBookDto dto, CancellationToken ct = default)
    {
        var owner = await _users.GetByIdAsync(ownerId, ct)
            ?? throw new NotFoundException(nameof(User), ownerId);

        var book = new Book
        {
            Title = dto.Title.Trim(),
            OwnerId = owner.Id,
            Owner = owner
        };

        await _books.AddAsync(book, ct);
        await _books.SaveChangesAsync(ct);

        return book.ToDto();
    }

    public async Task<BookDto> UpdateAsync(Guid id, Guid currentUserId, UpdateBookDto dto, CancellationToken ct = default)
    {
        var book = await _books.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Book), id);

        if (book.OwnerId != currentUserId)
            throw new ForbiddenException("Only the owner can update this book.");

        book.Title = dto.Title.Trim();
        book.UpdatedAt = DateTime.UtcNow;

        _books.Update(book);
        await _books.SaveChangesAsync(ct);

        return book.ToDto();
    }

    public async Task DeleteAsync(Guid id, Guid currentUserId, CancellationToken ct = default)
    {
        var book = await _books.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Book), id);

        if (book.OwnerId != currentUserId)
            throw new ForbiddenException("Only the owner can delete this book.");

        _books.Remove(book);
        await _books.SaveChangesAsync(ct);
    }

    public async Task<BookDto> BorrowAsync(Guid id, Guid currentUserId, CancellationToken ct = default)
    {
        var book = await _books.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Book), id);

        if (book.BorrowerId is not null)
            throw new ConflictException("Book is already borrowed.");

        if (book.OwnerId == currentUserId)
            throw new ConflictException("You cannot borrow your own book.");

        var borrower = await _users.GetByIdAsync(currentUserId, ct)
            ?? throw new NotFoundException(nameof(User), currentUserId);

        book.BorrowerId = borrower.Id;
        book.Borrower = borrower;
        book.UpdatedAt = DateTime.UtcNow;

        _books.Update(book);
        await _books.SaveChangesAsync(ct);

        return book.ToDto();
    }

    public async Task<BookDto> ReturnAsync(Guid id, Guid currentUserId, CancellationToken ct = default)
    {
        var book = await _books.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Book), id);

        if (book.BorrowerId is null)
            throw new ConflictException("Book is not currently borrowed.");

        // Allow either the borrower or the owner to return.
        if (book.BorrowerId != currentUserId && book.OwnerId != currentUserId)
            throw new ForbiddenException("Only the borrower or the owner can return this book.");

        book.BorrowerId = null;
        book.Borrower = null;
        book.UpdatedAt = DateTime.UtcNow;

        _books.Update(book);
        await _books.SaveChangesAsync(ct);

        return book.ToDto();
    }
}
