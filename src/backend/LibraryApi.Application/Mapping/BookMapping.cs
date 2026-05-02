using LibraryApi.Application.DTOs;
using LibraryApi.Domain.Common;
using LibraryApi.Domain.Entities;

namespace LibraryApi.Application.Mapping;

internal static class BookMapping
{
    public static BookDto ToDto(this Book book) => new(
        book.Id,
        book.Title,
        book.OwnerId,
        book.Owner?.DisplayName ?? string.Empty,
        book.BorrowerId,
        book.Borrower?.DisplayName,
        book.IsAvailable,
        book.CreatedAt,
        book.UpdatedAt);

    public static PagedResult<BookDto> ToDto(this PagedResult<Book> page) => new(
        page.Items.Select(b => b.ToDto()).ToList(),
        page.Page,
        page.PageSize,
        page.TotalCount);
}
