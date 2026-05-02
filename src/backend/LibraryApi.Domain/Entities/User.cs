namespace LibraryApi.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Book> OwnedBooks { get; set; } = new List<Book>();
    public ICollection<Book> BorrowedBooks { get; set; } = new List<Book>();
}
