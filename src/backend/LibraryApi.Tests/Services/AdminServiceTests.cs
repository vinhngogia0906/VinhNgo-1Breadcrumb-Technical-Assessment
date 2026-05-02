using FluentAssertions;
using LibraryApi.Application.Services;
using LibraryApi.Domain.Common;
using LibraryApi.Domain.Entities;
using LibraryApi.Domain.Repositories;
using Moq;

namespace LibraryApi.Tests.Services;

public class AdminServiceTests
{
    private readonly Mock<IBookActivityRepository> _activity = new();
    private readonly AdminService _sut;

    public AdminServiceTests() => _sut = new AdminService(_activity.Object);

    [Fact]
    public async Task GetActivityAsync_maps_repository_results_into_dtos()
    {
        var entry = new BookActivity
        {
            BookId = Guid.NewGuid(),
            BookTitle = "DDIA",
            ActorId = Guid.NewGuid(),
            ActorName = "Alice",
            Action = BookAction.Borrowed,
            Details = null,
            OccurredAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        };
        _activity.Setup(r => r.GetPagedAsync(2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<BookActivity>(new[] { entry }, 2, 5, 11));

        var result = await _sut.GetActivityAsync(2, 5);

        result.TotalCount.Should().Be(11);
        result.Page.Should().Be(2);
        result.Items.Should().ContainSingle();
        var dto = result.Items[0];
        dto.BookTitle.Should().Be("DDIA");
        dto.ActorName.Should().Be("Alice");
        dto.Action.Should().Be(BookAction.Borrowed);
        dto.OccurredAt.Should().Be(entry.OccurredAt);
    }
}
