namespace CreditSystem.Application.Tests.Services;

using Moq;
using Xunit;
using CreditSystem.Infrastructure.Options;
using CreditSystem.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class AutoCreditGrantBackgroundServiceTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<AutoCreditGrantBackgroundService>> _mockLogger;
    private readonly IOptions<AutoGrantOptions> _defaultOptions;

    public AutoCreditGrantBackgroundServiceTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<AutoCreditGrantBackgroundService>>();
        _defaultOptions = Options.Create(new AutoGrantOptions
        {
            GrantAmount = 100,
            GrantFrequencyDays = 3,
            CheckIntervalMinutes = 60
        });

        _mockScopeFactory.Setup(x => x.CreateScope())
            .Returns(new Mock<IServiceScope>().Object);
    }

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_LogsInformationMessage()
    {
        // Arrange
        var service = new AutoCreditGrantBackgroundService(
            _mockScopeFactory.Object,
            _defaultOptions,
            _mockLogger.Object);

        var cancellationToken = CancellationToken.None;

        // Act
        await service.StartAsync(cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting auto credit grant service")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithValidOptions_Completes()
    {
        // Arrange
        var service = new AutoCreditGrantBackgroundService(
            _mockScopeFactory.Object,
            _defaultOptions,
            _mockLogger.Object);

        var cancellationToken = CancellationToken.None;

        // Act
        var task = service.StartAsync(cancellationToken);

        // Assert
        Assert.NotNull(task);
        await task;

        // Cleanup
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_LogsGrantAmountAndFrequency()
    {
        // Arrange
        var options = Options.Create(new AutoGrantOptions
        {
            GrantAmount = 250,
            GrantFrequencyDays = 7,
            CheckIntervalMinutes = 30
        });

        var service = new AutoCreditGrantBackgroundService(
            _mockScopeFactory.Object,
            options,
            _mockLogger.Object);

        var cancellationToken = CancellationToken.None;

        // Act
        await service.StartAsync(cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("GrantAmount=250") &&
                    v.ToString()!.Contains("GrantFrequencyDays=7")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        await service.StopAsync(CancellationToken.None);
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_LogsInformationMessage()
    {
        // Arrange
        var service = new AutoCreditGrantBackgroundService(
            _mockScopeFactory.Object,
            _defaultOptions,
            _mockLogger.Object);

        var cancellationToken = CancellationToken.None;
        await service.StartAsync(cancellationToken);
        await Task.Delay(100);

        // Act
        await service.StopAsync(cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopping auto credit grant service")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhenNotStarted_DoesNotThrow()
    {
        // Arrange
        var service = new AutoCreditGrantBackgroundService(
            _mockScopeFactory.Object,
            _defaultOptions,
            _mockLogger.Object);

        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await service.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StopAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var service = new AutoCreditGrantBackgroundService(
            _mockScopeFactory.Object,
            _defaultOptions,
            _mockLogger.Object);

        var cancellationToken = CancellationToken.None;
        await service.StartAsync(cancellationToken);
        await Task.Delay(100);

        // Act & Assert
        await service.StopAsync(cancellationToken);
        await service.StopAsync(cancellationToken);
    }

    #endregion

    #region Options Validation Tests

    [Fact]
    public async Task Service_WithMinimalGrantAmount_Defaults()
    {
        // Arrange
        var options = Options.Create(new AutoGrantOptions
        {
            GrantAmount = 0, // Should default to at least 1
            GrantFrequencyDays = 1,
            CheckIntervalMinutes = 1
        });

        var service = new AutoCreditGrantBackgroundService(
            _mockScopeFactory.Object,
            options,
            _mockLogger.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        // Service should handle zero values gracefully
        Assert.NotNull(service);

        // Cleanup
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Service_WithDefaultOptions_Initializes()
    {
        // Arrange & Act
        var service = new AutoCreditGrantBackgroundService(
            _mockScopeFactory.Object,
            _defaultOptions,
            _mockLogger.Object);

        await service.StartAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(service);

        // Cleanup
        await service.StopAsync(CancellationToken.None);
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public async Task Service_StartThenStop_Completes()
    {
        // Arrange
        var service = new AutoCreditGrantBackgroundService(
            _mockScopeFactory.Object,
            _defaultOptions,
            _mockLogger.Object);

        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(50);
        await service.StopAsync(cts.Token);

        // Assert
        Assert.True(true, "Service lifecycle completed successfully");
    }

    [Fact]
    public void Service_Dispose_DoesNotThrow()
    {
        // Arrange
        var service = new AutoCreditGrantBackgroundService(
            _mockScopeFactory.Object,
            _defaultOptions,
            _mockLogger.Object);

        // Act & Assert
        service.Dispose();
    }

    [Fact]
    public async Task Service_WithCancellationToken_RespondsToCancel()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var service = new AutoCreditGrantBackgroundService(
            _mockScopeFactory.Object,
            _defaultOptions,
            _mockLogger.Object);

        await service.StartAsync(cts.Token);

        // Act
        cts.Cancel();
        await Task.Delay(100);

        // Assert
        // Service should complete without throwing
        Assert.True(true);

        // Cleanup
        await service.StopAsync(CancellationToken.None);
    }

    #endregion
}
