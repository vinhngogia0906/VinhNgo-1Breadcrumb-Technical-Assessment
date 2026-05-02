using System.ComponentModel.DataAnnotations;

namespace LibraryApi.Application.DTOs;

public class RegisterDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public record AuthUserDto(Guid Id, string Email, string DisplayName, string Role);

public record AuthResponseDto(string Token, DateTime ExpiresAt, AuthUserDto User);
