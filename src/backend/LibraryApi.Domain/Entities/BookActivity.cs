using LibraryApi.Domain.Common;

namespace LibraryApi.Domain.Entities;

/// <summary>
/// Audit log row capturing one action against a book. Title and actor name are
/// snapshotted at write time so the history survives book deletion or user
/// renaming. <see cref="BookId"/> is intentionally not a foreign key — we want
/// the row to outlive the book.
/// </summary>
public class BookActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;

    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;

    public BookAction Action { get; set; }
    public string? Details { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
