using LibraryApi.Application.DTOs;
using LibraryApi.Domain.Common;

namespace LibraryApi.Application.Services;

public interface IBookService
{
    Task<PagedResult<BookDto>> SearchAsync(
        string? search,
        AvailabilityFilter availability,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<BookDto> GetAsync(Guid id, CancellationToken ct = default);

    Task<BookDto> CreateAsync(Guid ownerId, CreateBookDto dto, CancellationToken ct = default);

    Task<BookDto> UpdateAsync(Guid id, Guid currentUserId, UpdateBookDto dto, CancellationToken ct = default);

    Task DeleteAsync(Guid id, Guid currentUserId, CancellationToken ct = default);

    Task<BookDto> BorrowAsync(Guid id, Guid currentUserId, CancellationToken ct = default);

    Task<BookDto> ReturnAsync(Guid id, Guid currentUserId, CancellationToken ct = default);
}
