using FluentAssertions;
using LibraryApi.Application.DTOs;
using LibraryApi.Application.Services;
using LibraryApi.Domain.Common;
using LibraryApi.Domain.Entities;
using LibraryApi.Domain.Exceptions;
using LibraryApi.Domain.Repositories;
using Moq;

namespace LibraryApi.Tests.Services;

public class BookServiceTests
{
    private readonly Mock<IBookRepository> _bookRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IBookActivityRepository> _activityRepo = new();
    private readonly List<BookActivity> _recordedActivity = new();
    private readonly BookService _sut;

    private readonly User _owner = new() { Id = Guid.NewGuid(), Email = "owner@test", DisplayName = "Owner" };
    private readonly User _other = new() { Id = Guid.NewGuid(), Email = "other@test", DisplayName = "Other" };

    public BookServiceTests()
    {
        _activityRepo
            .Setup(r => r.AddAsync(It.IsAny<BookActivity>(), It.IsAny<CancellationToken>()))
            .Callback<BookActivity, CancellationToken>((a, _) => _recordedActivity.Add(a))
            .Returns(Task.CompletedTask);

        _sut = new BookService(_bookRepo.Object, _userRepo.Object, _activityRepo.Object);
    }

    [Fact]
    public async Task CreateAsync_creates_book_with_owner()
    {
        _userRepo.Setup(r => r.GetByIdAsync(_owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_owner);

        Book? saved = null;
        _bookRepo.Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Callback<Book, CancellationToken>((b, _) => saved = b)
            .Returns(Task.CompletedTask);

        var dto = await _sut.CreateAsync(_owner.Id, new CreateBookDto { Title = "  Atomic Habits  " });

        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Atomic Habits");
        saved.OwnerId.Should().Be(_owner.Id);
        dto.IsAvailable.Should().BeTrue();
        _bookRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_throws_forbidden_when_caller_is_not_owner()
    {
        var book = MakeBook(_owner);
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        var act = () => _sut.UpdateAsync(book.Id, _other.Id, new UpdateBookDto { Title = "Hack" });

        await act.Should().ThrowAsync<ForbiddenException>();
        _bookRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BorrowAsync_marks_book_borrowed_by_caller()
    {
        var book = MakeBook(_owner);
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _userRepo.Setup(r => r.GetByIdAsync(_other.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_other);

        var dto = await _sut.BorrowAsync(book.Id, _other.Id);

        book.BorrowerId.Should().Be(_other.Id);
        dto.IsAvailable.Should().BeFalse();
        dto.BorrowerName.Should().Be(_other.DisplayName);
    }

    [Fact]
    public async Task BorrowAsync_rejects_when_already_borrowed()
    {
        var book = MakeBook(_owner);
        book.BorrowerId = _other.Id;
        book.Borrower = _other;
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        var act = () => _sut.BorrowAsync(book.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task BorrowAsync_rejects_owner_borrowing_own_book()
    {
        var book = MakeBook(_owner);
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        var act = () => _sut.BorrowAsync(book.Id, _owner.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ReturnAsync_clears_borrower_when_caller_is_borrower()
    {
        var book = MakeBook(_owner);
        book.BorrowerId = _other.Id;
        book.Borrower = _other;
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _userRepo.Setup(r => r.GetByIdAsync(_other.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_other);

        var dto = await _sut.ReturnAsync(book.Id, _other.Id);

        book.BorrowerId.Should().BeNull();
        dto.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task ReturnAsync_rejects_third_party()
    {
        var book = MakeBook(_owner);
        book.BorrowerId = _other.Id;
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        var act = () => _sut.ReturnAsync(book.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DeleteAsync_throws_when_caller_is_not_owner()
    {
        var book = MakeBook(_owner);
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        var act = () => _sut.DeleteAsync(book.Id, _other.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
        _bookRepo.Verify(r => r.Remove(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_passes_through_to_repository_and_maps()
    {
        var book = MakeBook(_owner);
        _bookRepo.Setup(r => r.GetPagedAsync("foo", AvailabilityFilter.Available, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Book>(new[] { book }, 2, 5, 7));

        var result = await _sut.SearchAsync("foo", AvailabilityFilter.Available, 2, 5);

        result.TotalCount.Should().Be(7);
        result.Items.Should().ContainSingle().Which.OwnerName.Should().Be(_owner.DisplayName);
    }

    [Fact]
    public async Task CreateAsync_records_Created_activity()
    {
        _userRepo.Setup(r => r.GetByIdAsync(_owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_owner);

        await _sut.CreateAsync(_owner.Id, new CreateBookDto { Title = "Refactoring" });

        _recordedActivity.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                BookTitle = "Refactoring",
                ActorId = _owner.Id,
                ActorName = _owner.DisplayName,
                Action = BookAction.Created
            }, opts => opts.ExcludingMissingMembers());
    }

    [Fact]
    public async Task BorrowAsync_records_Borrowed_activity_with_borrower_as_actor()
    {
        var book = MakeBook(_owner);
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _userRepo.Setup(r => r.GetByIdAsync(_other.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_other);

        await _sut.BorrowAsync(book.Id, _other.Id);

        _recordedActivity.Should().ContainSingle()
            .Which.Action.Should().Be(BookAction.Borrowed);
        _recordedActivity[0].ActorId.Should().Be(_other.Id);
        _recordedActivity[0].BookTitle.Should().Be(book.Title);
    }

    [Fact]
    public async Task UpdateAsync_records_Updated_activity_with_title_diff()
    {
        var book = MakeBook(_owner);
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _userRepo.Setup(r => r.GetByIdAsync(_owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_owner);

        await _sut.UpdateAsync(book.Id, _owner.Id, new UpdateBookDto { Title = "New Title" });

        var entry = _recordedActivity.Should().ContainSingle().Subject;
        entry.Action.Should().Be(BookAction.Updated);
        entry.Details.Should().Contain("Title").And.Contain("New Title");
    }

    [Fact]
    public async Task DeleteAsync_records_Deleted_activity_before_remove()
    {
        var book = MakeBook(_owner);
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _userRepo.Setup(r => r.GetByIdAsync(_owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_owner);

        await _sut.DeleteAsync(book.Id, _owner.Id);

        _recordedActivity.Should().ContainSingle()
            .Which.Action.Should().Be(BookAction.Deleted);
        _bookRepo.Verify(r => r.Remove(It.IsAny<Book>()), Times.Once);
    }

    private static Book MakeBook(User owner) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Title",
        OwnerId = owner.Id,
        Owner = owner
    };
}
