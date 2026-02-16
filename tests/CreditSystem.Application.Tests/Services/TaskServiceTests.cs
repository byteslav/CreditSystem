namespace CreditSystem.Application.Tests.Services;

using Moq;
using Xunit;
using CreditSystem.Application.DTOs.Tasks;
using CreditSystem.Application.Interfaces.Repositories;
using CreditSystem.Application.Services;
using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _taskService = new TaskService(_mockTaskRepository.Object);
    }

    #region CreateTaskAsync Tests

    [Fact]
    public async Task CreateTaskAsync_WithValidUserId_ReturnsCreateTaskResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _mockTaskRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskItem>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync(cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _taskService.CreateTaskAsync(userId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<CreateTaskResponse>(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Created", result.Status);
        Assert.True(result.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task CreateTaskAsync_CallsRepositoryAddAsyncWithCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _mockTaskRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskItem>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync(cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _taskService.CreateTaskAsync(userId, cancellationToken);

        // Assert
        _mockTaskRepository.Verify(
            x => x.AddAsync(It.Is<TaskItem>(t =>
                t.UserId == userId &&
                t.Status == TaskStatus.Created &&
                t.Cost == null &&
                t.CreatedAt > DateTime.MinValue),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_CallsSaveChangesAsyncAfterAdd()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        var callOrder = new List<string>();

        _mockTaskRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskItem>(), cancellationToken))
            .Callback(() => callOrder.Add("Add"))
            .Returns(Task.CompletedTask);

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync(cancellationToken))
            .Callback(() => callOrder.Add("SaveChanges"))
            .Returns(Task.CompletedTask);

        // Act
        await _taskService.CreateTaskAsync(userId, cancellationToken);

        // Assert
        Assert.Equal(new[] { "Add", "SaveChanges" }, callOrder);
    }

    [Fact]
    public async Task CreateTaskAsync_CreatedTaskHasUniqueId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _mockTaskRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskItem>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync(cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await _taskService.CreateTaskAsync(userId, cancellationToken);
        var result2 = await _taskService.CreateTaskAsync(userId, cancellationToken);

        // Assert
        Assert.NotEqual(result1.Id, result2.Id);
    }

    [Fact]
    public async Task CreateTaskAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();

        _mockTaskRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskItem>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync(cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _taskService.CreateTaskAsync(userId, cancellationToken);

        // Assert
        _mockTaskRepository.Verify(
            x => x.AddAsync(It.IsAny<TaskItem>(), cancellationToken),
            Times.Once);

        _mockTaskRepository.Verify(
            x => x.SaveChangesAsync(cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_ThrowsExceptionFromRepository_PropagatesException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedError = new InvalidOperationException("Database error");

        _mockTaskRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedError);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _taskService.CreateTaskAsync(userId));

        Assert.Equal(expectedError.Message, exception.Message);
    }

    #endregion

    #region GetUserTasksAsync Tests

    [Fact]
    public async Task GetUserTasksAsync_WithValidUserId_ReturnsTaskList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        var mockTasks = new List<TaskItem>
        {
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = TaskStatus.Created,
                Cost = null,
                CreatedAt = DateTime.UtcNow,
                StartedAt = null,
                CompletedAt = null
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = TaskStatus.Succeeded,
                Cost = 100,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                CompletedAt = DateTime.UtcNow
            }
        };

        _mockTaskRepository
            .Setup(x => x.GetByUserIdAsync(userId, cancellationToken))
            .ReturnsAsync(mockTasks);

        // Act
        var result = await _taskService.GetUserTasksAsync(userId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<List<TaskListItemResponse>>(result.ToList());
    }

    [Fact]
    public async Task GetUserTasksAsync_ReturnsReadOnlyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var mockTasks = new List<TaskItem>
        {
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = TaskStatus.Created,
                Cost = null,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockTaskRepository
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTasks);

        // Act
        var result = await _taskService.GetUserTasksAsync(userId);

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<TaskListItemResponse>>(result);
    }

    [Fact]
    public async Task GetUserTasksAsync_MapsTaskItemsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddHours(-2);
        var startedAt = DateTime.UtcNow.AddHours(-1);
        var completedAt = DateTime.UtcNow;

        var mockTask = new TaskItem
        {
            Id = taskId,
            UserId = userId,
            Status = TaskStatus.Succeeded,
            Cost = 50,
            CreatedAt = createdAt,
            StartedAt = startedAt,
            CompletedAt = completedAt
        };

        _mockTaskRepository
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskItem> { mockTask });

        // Act
        var result = await _taskService.GetUserTasksAsync(userId);

        // Assert
        var task = result.Single();
        Assert.Equal(taskId, task.Id);
        Assert.Equal("Succeeded", task.Status);
        Assert.Equal(50, task.Cost);
        Assert.Equal(createdAt, task.CreatedAt);
        Assert.Equal(startedAt, task.StartedAt);
        Assert.Equal(completedAt, task.CompletedAt);
    }

    [Fact]
    public async Task GetUserTasksAsync_WithNullCost_MapsNullCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = TaskStatus.Created,
            Cost = null,
            CreatedAt = DateTime.UtcNow
        };

        _mockTaskRepository
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskItem> { mockTask });

        // Act
        var result = await _taskService.GetUserTasksAsync(userId);

        // Assert
        var task = result.Single();
        Assert.Null(task.Cost);
    }

    [Fact]
    public async Task GetUserTasksAsync_WithEmptyList_ReturnsEmptyReadOnlyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockTaskRepository
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskItem>());

        // Act
        var result = await _taskService.GetUserTasksAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserTasksAsync_CallsRepositoryWithCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _mockTaskRepository
            .Setup(x => x.GetByUserIdAsync(userId, cancellationToken))
            .ReturnsAsync(new List<TaskItem>());

        // Act
        await _taskService.GetUserTasksAsync(userId, cancellationToken);

        // Assert
        _mockTaskRepository.Verify(
            x => x.GetByUserIdAsync(userId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetUserTasksAsync_WithMultipleStatuses_MapsAllStatusesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var mockTasks = new List<TaskItem>
        {
            new TaskItem { Id = Guid.NewGuid(), UserId = userId, Status = TaskStatus.Created, Cost = null, CreatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), UserId = userId, Status = TaskStatus.Running, Cost = null, CreatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), UserId = userId, Status = TaskStatus.Succeeded, Cost = 100, CreatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), UserId = userId, Status = TaskStatus.Failed, Cost = null, CreatedAt = DateTime.UtcNow }
        };

        _mockTaskRepository
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTasks);

        // Act
        var result = await _taskService.GetUserTasksAsync(userId);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Contains(result, t => t.Status == "Created");
        Assert.Contains(result, t => t.Status == "Running");
        Assert.Contains(result, t => t.Status == "Succeeded");
        Assert.Contains(result, t => t.Status == "Failed");
    }

    [Fact]
    public async Task GetUserTasksAsync_ThrowsExceptionFromRepository_PropagatesException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedError = new InvalidOperationException("Database error");

        _mockTaskRepository
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedError);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _taskService.GetUserTasksAsync(userId));

        Assert.Equal(expectedError.Message, exception.Message);
    }

    #endregion
}
