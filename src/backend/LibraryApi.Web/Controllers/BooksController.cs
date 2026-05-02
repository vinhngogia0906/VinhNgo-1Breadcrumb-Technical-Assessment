using LibraryApi.Application.DTOs;
using LibraryApi.Application.Services;
using LibraryApi.Domain.Common;
using LibraryApi.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApi.Web.Controllers;

// Path conventions (`/books` for the collection, `/book` and `/book/{id}` for
// individual operations) mirror the contract published at /openapi/v1.yaml,
// which itself follows the structure of https://libapi.1breadcrumb.com/.
[ApiController]
[Authorize]
[Route("api")]
[Produces("application/json")]
public class BooksController : ControllerBase
{
    private readonly IBookService _books;
    private readonly ICurrentUser _currentUser;

    public BooksController(IBookService books, ICurrentUser currentUser)
    {
        _books = books;
        _currentUser = currentUser;
    }

    [HttpGet("books")]
    public async Task<ActionResult<PagedResult<BookDto>>> GetBooks(
        [FromQuery] string? search,
        [FromQuery] AvailabilityFilter availability = AvailabilityFilter.All,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
        => Ok(await _books.SearchAsync(search, availability, page, pageSize, ct));

    [HttpGet("book/{id:guid}", Name = nameof(GetBook))]
    public async Task<ActionResult<BookDto>> GetBook(Guid id, CancellationToken ct)
        => Ok(await _books.GetAsync(id, ct));

    [HttpPost("book")]
    public async Task<ActionResult<BookDto>> PostBook([FromBody] CreateBookDto dto, CancellationToken ct)
    {
        var created = await _books.CreateAsync(_currentUser.Id, dto, ct);
        return CreatedAtRoute(nameof(GetBook), new { id = created.Id }, created);
    }

    [HttpPut("book/{id:guid}")]
    public async Task<ActionResult<BookDto>> PutBook(Guid id, [FromBody] UpdateBookDto dto, CancellationToken ct)
        => Ok(await _books.UpdateAsync(id, _currentUser.Id, dto, ct));

    [HttpDelete("book/{id:guid}")]
    public async Task<IActionResult> DeleteBook(Guid id, CancellationToken ct)
    {
        await _books.DeleteAsync(id, _currentUser.Id, ct);
        return NoContent();
    }

    [HttpPost("book/{id:guid}/borrow")]
    public async Task<ActionResult<BookDto>> BorrowBook(Guid id, CancellationToken ct)
        => Ok(await _books.BorrowAsync(id, _currentUser.Id, ct));

    [HttpPost("book/{id:guid}/return")]
    public async Task<ActionResult<BookDto>> ReturnBook(Guid id, CancellationToken ct)
        => Ok(await _books.ReturnAsync(id, _currentUser.Id, ct));
}
