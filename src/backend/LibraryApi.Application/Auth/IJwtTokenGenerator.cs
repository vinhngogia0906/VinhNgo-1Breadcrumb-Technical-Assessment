using LibraryApi.Domain.Entities;

namespace LibraryApi.Application.Auth;

public record GeneratedToken(string Token, DateTime ExpiresAt);

public interface IJwtTokenGenerator
{
    GeneratedToken Generate(User user);
}
