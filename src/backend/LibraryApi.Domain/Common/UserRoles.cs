namespace LibraryApi.Domain.Common;

/// <summary>
/// Role string constants. Stored on User.Role and used as the claim value for
/// ASP.NET Core role-based authorization.
/// </summary>
public static class UserRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
}
