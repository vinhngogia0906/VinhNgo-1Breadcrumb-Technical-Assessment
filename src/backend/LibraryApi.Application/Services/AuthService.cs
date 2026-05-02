using LibraryApi.Application.Auth;
using LibraryApi.Application.DTOs;
using LibraryApi.Domain.Entities;
using LibraryApi.Domain.Exceptions;
using LibraryApi.Domain.Repositories;

namespace LibraryApi.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _tokens;

    public AuthService(IUserRepository users, IPasswordHasher hasher, IJwtTokenGenerator tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var existing = await _users.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new ConflictException("An account with that email already exists.");

        var user = new User
        {
            Email = email,
            DisplayName = dto.DisplayName.Trim(),
            PasswordHash = _hasher.Hash(dto.Password)
        };

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null || !_hasher.Verify(dto.Password, user.PasswordHash))
            throw new DomainException("Invalid email or password.");

        return BuildResponse(user);
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var token = _tokens.Generate(user);
        return new AuthResponseDto(
            token.Token,
            token.ExpiresAt,
            new AuthUserDto(user.Id, user.Email, user.DisplayName));
    }
}
