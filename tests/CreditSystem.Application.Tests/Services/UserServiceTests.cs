namespace CreditSystem.Application.Tests.Services;

using Moq;
using Xunit;
using CreditSystem.Application.DTOs;
using CreditSystem.Application.Interfaces.Repositories;
using CreditSystem.Application.Services;
using CreditSystem.Domain.Entities;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _userService = new UserService(_mockUserRepository.Object);
    }

    #region GetCurrentUserAsync Tests

    [Fact]
    public async Task GetCurrentUserAsync_WithValidUserId_ReturnsMeResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            Username = "testuser",
            PasswordHash = "hash123",
            Credits = 100,
            RegisteredAt = DateTime.UtcNow,
            LastCreditGrantAt = DateTime.UtcNow
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetCurrentUserAsync(userId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MeResponse>(result);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithNonExistentUserId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetCurrentUserAsync(userId, cancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentUserAsync_MapsUserPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "john.doe@example.com";
        var username = "johndoe";
        var credits = 250;
        var registeredAt = DateTime.UtcNow.AddDays(-30);

        var user = new User
        {
            Id = userId,
            Email = email,
            Username = username,
            PasswordHash = "hashed_password_123",
            Credits = credits,
            RegisteredAt = registeredAt,
            LastCreditGrantAt = DateTime.UtcNow
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetCurrentUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(email, result.Email);
        Assert.Equal(username, result.Username);
        Assert.Equal(credits, result.Credits);
        Assert.Equal(registeredAt, result.RegisteredAt);
    }

    [Fact]
    public async Task GetCurrentUserAsync_DoesNotIncludePasswordHash()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = "secret_hashed_password";

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = passwordHash,
            Credits = 100,
            RegisteredAt = DateTime.UtcNow
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetCurrentUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        // Verify PasswordHash is not exposed in MeResponse
        var responseProperties = result.GetType().GetProperties();
        var passwordHashProperty = responseProperties.FirstOrDefault(p => p.Name == "PasswordHash");
        Assert.Null(passwordHashProperty);
    }

    [Fact]
    public async Task GetCurrentUserAsync_CallsRepositoryWithCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync((User?)null);

        // Act
        await _userService.GetCurrentUserAsync(userId, cancellationToken);

        // Assert
        _mockUserRepository.Verify(
            x => x.GetByIdAsync(userId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync((User?)null);

        // Act
        await _userService.GetCurrentUserAsync(userId, cancellationToken);

        // Assert
        _mockUserRepository.Verify(
            x => x.GetByIdAsync(userId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithDefaultCancellationToken_CallsRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hash",
            Credits = 50,
            RegisteredAt = DateTime.UtcNow
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetCurrentUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        _mockUserRepository.Verify(
            x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithZeroCredits_MapsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "poor@example.com",
            Username = "nocredits",
            PasswordHash = "hash",
            Credits = 0,
            RegisteredAt = DateTime.UtcNow
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetCurrentUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Credits);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithHighCredits_MapsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var highCredits = int.MaxValue;

        var user = new User
        {
            Id = userId,
            Email = "rich@example.com",
            Username = "richuser",
            PasswordHash = "hash",
            Credits = highCredits,
            RegisteredAt = DateTime.UtcNow
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetCurrentUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(highCredits, result.Credits);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithLastCreditGrantAtNull_StillReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hash",
            Credits = 100,
            RegisteredAt = DateTime.UtcNow,
            LastCreditGrantAt = null
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetCurrentUserAsync(userId);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ThrowsExceptionFromRepository_PropagatesException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedError = new InvalidOperationException("Database error");

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedError);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.GetCurrentUserAsync(userId));

        Assert.Equal(expectedError.Message, exception.Message);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithRepositoryTimeout_PropagatesException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedError = new OperationCanceledException("Request timed out");

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedError);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => _userService.GetCurrentUserAsync(userId));

        Assert.Equal(expectedError.Message, exception.Message);
    }

    [Fact]
    public async Task GetCurrentUserAsync_MultipleCallsWithDifferentUsers_ReturnCorrectData()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var user1 = new User
        {
            Id = userId1,
            Email = "user1@example.com",
            Username = "user1",
            PasswordHash = "hash1",
            Credits = 100,
            RegisteredAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = userId2,
            Email = "user2@example.com",
            Username = "user2",
            PasswordHash = "hash2",
            Credits = 200,
            RegisteredAt = DateTime.UtcNow
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user1);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user2);

        // Act
        var result1 = await _userService.GetCurrentUserAsync(userId1);
        var result2 = await _userService.GetCurrentUserAsync(userId2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(userId1, result1.Id);
        Assert.Equal(userId2, result2.Id);
        Assert.Equal("user1@example.com", result1.Email);
        Assert.Equal("user2@example.com", result2.Email);
        Assert.Equal(100, result1.Credits);
        Assert.Equal(200, result2.Credits);
    }

    #endregion
}
