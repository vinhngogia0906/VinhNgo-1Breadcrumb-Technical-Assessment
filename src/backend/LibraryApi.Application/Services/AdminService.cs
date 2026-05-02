using LibraryApi.Application.DTOs;
using LibraryApi.Domain.Common;
using LibraryApi.Domain.Repositories;

namespace LibraryApi.Application.Services;

public class AdminService : IAdminService
{
    private readonly IBookActivityRepository _activity;

    public AdminService(IBookActivityRepository activity) => _activity = activity;

    public async Task<PagedResult<BookActivityDto>> GetActivityAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var result = await _activity.GetPagedAsync(page, pageSize, ct);
        var items = result.Items
            .Select(a => new BookActivityDto(
                a.Id,
                a.BookId,
                a.BookTitle,
                a.ActorId,
                a.ActorName,
                a.Action,
                a.Details,
                a.OccurredAt))
            .ToList();
        return new PagedResult<BookActivityDto>(items, result.Page, result.PageSize, result.TotalCount);
    }
}
