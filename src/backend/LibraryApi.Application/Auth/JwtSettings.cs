namespace LibraryApi.Application.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "LibraryApi";
    public string Audience { get; set; } = "LibraryApiClient";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60 * 8;
}
