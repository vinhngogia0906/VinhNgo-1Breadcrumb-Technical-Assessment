namespace LibraryApi.Web.Auth;

public interface ICurrentUser
{
    Guid Id { get; }
    bool IsAuthenticated { get; }
}
