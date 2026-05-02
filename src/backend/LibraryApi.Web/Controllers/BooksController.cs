using LibraryApi.Application.DTOs;
using LibraryApi.Application.Services;
using LibraryApi.Domain.Common;
using LibraryApi.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApi.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _books;
    private readonly ICurrentUser _currentUser;

    public BooksController(IBookService books, ICurrentUser currentUser)
    {
        _books = books;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BookDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] AvailabilityFilter availability = AvailabilityFilter.All,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
        => Ok(await _books.SearchAsync(search, availability, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _books.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<BookDto>> Create([FromBody] CreateBookDto dto, CancellationToken ct)
    {
        var created = await _books.CreateAsync(_currentUser.Id, dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BookDto>> Update(Guid id, [FromBody] UpdateBookDto dto, CancellationToken ct)
        => Ok(await _books.UpdateAsync(id, _currentUser.Id, dto, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _books.DeleteAsync(id, _currentUser.Id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/borrow")]
    public async Task<ActionResult<BookDto>> Borrow(Guid id, CancellationToken ct)
        => Ok(await _books.BorrowAsync(id, _currentUser.Id, ct));

    [HttpPost("{id:guid}/return")]
    public async Task<ActionResult<BookDto>> Return(Guid id, CancellationToken ct)
        => Ok(await _books.ReturnAsync(id, _currentUser.Id, ct));
}
