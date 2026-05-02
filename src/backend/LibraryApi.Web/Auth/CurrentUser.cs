using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LibraryApi.Web.Auth;

public class CurrentUser : ICurrentUser
{
    public CurrentUser(IHttpContextAccessor accessor)
    {
        var principal = accessor.HttpContext?.User;
        IsAuthenticated = principal?.Identity?.IsAuthenticated ?? false;

        var sub = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(sub, out var id))
            Id = id;
    }

    public Guid Id { get; }
    public bool IsAuthenticated { get; }
}
