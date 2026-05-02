using LibraryApi.Application.DTOs;

namespace LibraryApi.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);

    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
}
