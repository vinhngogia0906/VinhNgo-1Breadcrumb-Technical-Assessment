using LibraryApi.Application.DTOs;
using LibraryApi.Domain.Common;

namespace LibraryApi.Application.Services;

public interface IAdminService
{
    Task<PagedResult<BookActivityDto>> GetActivityAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);
}
