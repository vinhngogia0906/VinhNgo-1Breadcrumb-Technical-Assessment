using System.ComponentModel.DataAnnotations;

namespace LibraryApi.Application.DTOs;

public record BookDto(
    Guid Id,
    string Title,
    Guid OwnerId,
    string OwnerName,
    Guid? BorrowerId,
    string? BorrowerName,
    bool IsAvailable,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class CreateBookDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
}

public class UpdateBookDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
}
