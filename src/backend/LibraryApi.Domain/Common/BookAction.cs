namespace LibraryApi.Domain.Common;

/// <summary>
/// The kind of action recorded against a book. Stored as a string in the
/// activity log so admin queries are human-readable.
/// </summary>
public enum BookAction
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    Borrowed = 3,
    Returned = 4
}
