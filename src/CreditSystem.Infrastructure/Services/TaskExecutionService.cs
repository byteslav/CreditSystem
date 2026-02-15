namespace CreditSystem.Infrastructure.Services;

using CreditSystem.Application.DTOs.Tasks;
using CreditSystem.Application.Interfaces.Repositories;
using CreditSystem.Application.Interfaces.Services;
using CreditSystem.Domain.Enums;
using CreditSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class TaskExecutionService : ITaskExecutionService
{
    private const int MinTaskCost = 1;
    private const int MaxTaskCostExclusive = 15;
    private const int MinExecutionDelayMs = 10000;
    private const int MaxExecutionDelayMs = 40001;

    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly ApplicationDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TaskExecutionService> _logger;

    public TaskExecutionService(
        ITaskRepository taskRepository,
        IUserRepository userRepository,
        ApplicationDbContext context,
        IServiceScopeFactory scopeFactory,
        ILogger<TaskExecutionService> logger)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _context = context;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<ExecuteTaskResponse> ExecuteTaskAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting task execution for TaskId: {TaskId}, UserId: {UserId}", taskId, userId);

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var task = await _taskRepository.GetByIdAsyncTracked(taskId, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("Task not found: {TaskId}", taskId);
                throw new InvalidOperationException($"Task with id {taskId} not found.");
            }

            if (task.UserId != userId)
            {
                _logger.LogWarning("Unauthorized access attempt to TaskId: {TaskId} by UserId: {UserId}", taskId, userId);
                throw new InvalidOperationException("You are not authorized to execute this task.");
            }

            if (task.Status != TaskStatus.Created)
            {
                _logger.LogInformation("Task is not in Created status. Current status: {Status}. Returning early for idempotency.", task.Status);
                return BuildAlreadyProcessedResponse(task);
            }

            var user = await _userRepository.GetByIdAsyncTracked(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogError("User not found: {UserId}", userId);
                throw new InvalidOperationException("User not found.");
            }

            int cost = Random.Shared.Next(MinTaskCost, MaxTaskCostExclusive);
            _logger.LogInformation("Generated cost for TaskId: {TaskId}: {Cost} credits", taskId, cost);

            if (user.Credits < cost)
            {
                _logger.LogInformation("Insufficient credits for TaskId: {TaskId}. Required: {Cost}, Available: {Credits}",
                    taskId, cost, user.Credits);

                task.Status = TaskStatus.Rejected;
                task.Cost = cost;
                task.CompletedAt = DateTime.UtcNow;

                await _taskRepository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new ExecuteTaskResponse
                {
                    Id = task.Id,
                    Status = task.Status.ToString(),
                    Cost = cost,
                    StartedAt = DateTime.UtcNow,
                    Message = $"Insufficient credits. Required: {cost}, Available: {user.Credits}"
                };
            }

            user.Credits -= cost;
            task.Cost = cost;
            task.Status = TaskStatus.Running;
            task.StartedAt = DateTime.UtcNow;

            await _taskRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Transaction committed. TaskId: {TaskId} status set to Running. Credits deducted: {Cost}",
                taskId, cost);

            var response = new ExecuteTaskResponse
            {
                Id = task.Id,
                Status = task.Status.ToString(),
                Cost = cost,
                StartedAt = task.StartedAt.Value,
                Message = "Task execution started."
            };

            _ = Task.Run(() => ExecuteTaskInBackgroundAsync(taskId, userId));

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error executing task {TaskId}: {Message}", taskId, ex.Message);
            throw;
        }
    }

    private async Task ExecuteTaskInBackgroundAsync(Guid taskId, Guid userId)
    {
        try
        {
            int delayMs = Random.Shared.Next(MinExecutionDelayMs, MaxExecutionDelayMs);
            _logger.LogInformation("Starting background execution for TaskId: {TaskId}. Delay: {DelayMs}ms", taskId, delayMs);

            await Task.Delay(delayMs);

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await using var bgTransaction = await dbContext.Database.BeginTransactionAsync();
            var task = await dbContext.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
            {
                _logger.LogWarning("Task not found in background execution: {TaskId}", taskId);
                return;
            }

            if (task.UserId != userId)
            {
                _logger.LogWarning("Task ownership mismatch in background execution. TaskId: {TaskId}, UserId: {UserId}",
                    taskId, userId);
                return;
            }

            if (task.Status != TaskStatus.Running)
            {
                _logger.LogInformation("Skipping background completion for TaskId: {TaskId}. Current status: {Status}",
                    taskId, task.Status);
                return;
            }

            bool succeeded = Random.Shared.Next(0, 2) == 0; // simulating 50% success rate
            task.Status = succeeded ? TaskStatus.Succeeded : TaskStatus.Failed;
            task.CompletedAt = DateTime.UtcNow;

            if (!succeeded)
            {
                task.FailureReason = "Random failure during execution simulation.";
            }

            await dbContext.SaveChangesAsync();
            await bgTransaction.CommitAsync();

            _logger.LogInformation("Background execution completed for TaskId: {TaskId}. Status: {Status}",
                taskId, task.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in background execution for TaskId: {TaskId}: {Message}",
                taskId, ex.Message);
        }
    }

    private static ExecuteTaskResponse BuildAlreadyProcessedResponse(Domain.Entities.TaskItem task)
    {
        return new ExecuteTaskResponse
        {
            Id = task.Id,
            Status = task.Status.ToString(),
            Cost = task.Cost ?? 0,
            StartedAt = task.StartedAt ?? DateTime.UtcNow,
            Message = "Task has already been processed."
        };
    }
}
