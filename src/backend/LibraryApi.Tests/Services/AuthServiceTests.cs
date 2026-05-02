using FluentAssertions;
using LibraryApi.Application.Auth;
using LibraryApi.Application.DTOs;
using LibraryApi.Application.Services;
using LibraryApi.Domain.Entities;
using LibraryApi.Domain.Exceptions;
using LibraryApi.Domain.Repositories;
using Moq;

namespace LibraryApi.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokens = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users.Object, _hasher.Object, _tokens.Object);
        _tokens.Setup(t => t.Generate(It.IsAny<User>()))
            .Returns(new GeneratedToken("token", DateTime.UtcNow.AddMinutes(60)));
    }

    [Fact]
    public async Task RegisterAsync_creates_user_and_returns_token()
    {
        _users.Setup(r => r.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _hasher.Setup(h => h.Hash("password123")).Returns("hash");

        var response = await _sut.RegisterAsync(new RegisterDto
        {
            Email = "A@B.com",
            DisplayName = " Alice ",
            Password = "password123"
        });

        response.Token.Should().Be("token");
        response.User.Email.Should().Be("a@b.com");
        response.User.DisplayName.Should().Be("Alice");
        _users.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == "a@b.com" && u.DisplayName == "Alice" && u.PasswordHash == "hash"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_throws_conflict_when_email_in_use()
    {
        _users.Setup(r => r.GetByEmailAsync("dup@x.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = "dup@x.com" });

        var act = () => _sut.RegisterAsync(new RegisterDto
        {
            Email = "dup@x.com",
            DisplayName = "x",
            Password = "password123"
        });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task LoginAsync_returns_token_for_valid_credentials()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "a@b.com", PasswordHash = "h" };
        _users.Setup(r => r.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("pw", "h")).Returns(true);

        var response = await _sut.LoginAsync(new LoginDto { Email = "a@b.com", Password = "pw" });

        response.Token.Should().Be("token");
        response.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_throws_for_unknown_email()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => _sut.LoginAsync(new LoginDto { Email = "x@y.com", Password = "pw" });

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task LoginAsync_throws_for_wrong_password()
    {
        var user = new User { Email = "a@b.com", PasswordHash = "h" };
        _users.Setup(r => r.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("pw", "h")).Returns(false);

        var act = () => _sut.LoginAsync(new LoginDto { Email = "a@b.com", Password = "pw" });

        await act.Should().ThrowAsync<DomainException>();
    }
}
