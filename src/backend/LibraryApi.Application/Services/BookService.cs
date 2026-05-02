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
    private readonly IBookActivityRepository _activity;

    public BookService(IBookRepository books, IUserRepository users, IBookActivityRepository activity)
    {
        _books = books;
        _users = users;
        _activity = activity;
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
        await RecordAsync(book, owner, BookAction.Created, details: null, ct);
        await _books.SaveChangesAsync(ct);

        return book.ToDto();
    }

    public async Task<BookDto> UpdateAsync(Guid id, Guid currentUserId, UpdateBookDto dto, CancellationToken ct = default)
    {
        var book = await _books.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Book), id);

        if (book.OwnerId != currentUserId)
            throw new ForbiddenException("Only the owner can update this book.");

        var actor = await RequireUserAsync(currentUserId, ct);
        var previousTitle = book.Title;
        var newTitle = dto.Title.Trim();

        book.Title = newTitle;
        book.UpdatedAt = DateTime.UtcNow;

        _books.Update(book);
        await RecordAsync(
            book,
            actor,
            BookAction.Updated,
            details: previousTitle == newTitle ? null : $"Title: \"{previousTitle}\" → \"{newTitle}\"",
            ct);
        await _books.SaveChangesAsync(ct);

        return book.ToDto();
    }

    public async Task DeleteAsync(Guid id, Guid currentUserId, CancellationToken ct = default)
    {
        var book = await _books.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Book), id);

        if (book.OwnerId != currentUserId)
            throw new ForbiddenException("Only the owner can delete this book.");

        var actor = await RequireUserAsync(currentUserId, ct);

        // Record before remove so we still snapshot the title.
        await RecordAsync(book, actor, BookAction.Deleted, details: null, ct);

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

        var borrower = await RequireUserAsync(currentUserId, ct);

        book.BorrowerId = borrower.Id;
        book.Borrower = borrower;
        book.UpdatedAt = DateTime.UtcNow;

        _books.Update(book);
        await RecordAsync(book, borrower, BookAction.Borrowed, details: null, ct);
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

        var actor = await RequireUserAsync(currentUserId, ct);
        var previousBorrowerName = book.Borrower?.DisplayName;

        book.BorrowerId = null;
        book.Borrower = null;
        book.UpdatedAt = DateTime.UtcNow;

        _books.Update(book);
        await RecordAsync(
            book,
            actor,
            BookAction.Returned,
            details: previousBorrowerName is null ? null : $"Returned from {previousBorrowerName}",
            ct);
        await _books.SaveChangesAsync(ct);

        return book.ToDto();
    }

    private async Task<User> RequireUserAsync(Guid id, CancellationToken ct) =>
        await _users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);

    private Task RecordAsync(Book book, User actor, BookAction action, string? details, CancellationToken ct) =>
        _activity.AddAsync(new BookActivity
        {
            BookId = book.Id,
            BookTitle = book.Title,
            ActorId = actor.Id,
            ActorName = actor.DisplayName,
            Action = action,
            Details = details,
            OccurredAt = DateTime.UtcNow
        }, ct);
}
