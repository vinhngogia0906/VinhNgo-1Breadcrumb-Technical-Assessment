using LibraryApi.Domain.Common;

namespace LibraryApi.Application.DTOs;

public record BookActivityDto(
    Guid Id,
    Guid BookId,
    string BookTitle,
    Guid ActorId,
    string ActorName,
    BookAction Action,
    string? Details,
    DateTime OccurredAt);
