namespace LibraryApi.Domain.Entities;

public class Book
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public Guid? BorrowerId { get; set; }
    public User? Borrower { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsAvailable => BorrowerId is null;
}
