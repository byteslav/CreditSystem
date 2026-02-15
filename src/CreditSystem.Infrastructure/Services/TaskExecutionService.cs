namespace CreditSystem.Infrastructure.Services;

using CreditSystem.Application.DTOs.Tasks;
using CreditSystem.Application.Interfaces.Repositories;
using CreditSystem.Application.Interfaces.Services;
using CreditSystem.Domain.Enums;
using CreditSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class TaskExecutionService : ITaskExecutionService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TaskExecutionService> _logger;
    private static readonly Random _random = new();

    public TaskExecutionService(
        ITaskRepository taskRepository,
        IUserRepository userRepository,
        ApplicationDbContext context,
        ILogger<TaskExecutionService> logger)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _context = context;
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
                return new ExecuteTaskResponse
                {
                    Id = task.Id,
                    Status = task.Status.ToString(),
                    Cost = task.Cost ?? 0,
                    StartedAt = task.StartedAt ?? DateTime.UtcNow,
                    Message = "Task has already been processed."
                };
            }

            var user = await _userRepository.GetByIdAsyncTracked(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogError("User not found: {UserId}", userId);
                throw new InvalidOperationException("User not found.");
            }

            int cost = _random.Next(1, 16);
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

            _ = ExecuteTaskInBackgroundAsync(taskId, userId, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error executing task {TaskId}: {Message}", taskId, ex.Message);
            throw;
        }
    }

    private async Task ExecuteTaskInBackgroundAsync(Guid taskId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            int delayMs = _random.Next(10000, 40001);
            _logger.LogInformation("Starting background execution for TaskId: {TaskId}. Delay: {DelayMs}ms", taskId, delayMs);

            await Task.Delay(delayMs, cancellationToken);

            using var bgTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var task = await _taskRepository.GetByIdAsyncTracked(taskId, cancellationToken);
                if (task == null)
                {
                    _logger.LogWarning("Task not found in background execution: {TaskId}", taskId);
                    return;
                }

                bool succeeded = _random.Next(0, 2) == 0;
                task.Status = succeeded ? TaskStatus.Succeeded : TaskStatus.Failed;
                task.CompletedAt = DateTime.UtcNow;

                if (!succeeded)
                {
                    task.FailureReason = "Random failure during execution simulation.";
                }

                await _taskRepository.SaveChangesAsync(cancellationToken);
                await bgTransaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Background execution completed for TaskId: {TaskId}. Status: {Status}",
                    taskId, task.Status);
            }
            catch (Exception ex)
            {
                await bgTransaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error in background execution for TaskId: {TaskId}: {Message}",
                    taskId, ex.Message);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background execution cancelled for TaskId: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in background execution for TaskId: {TaskId}: {Message}",
                taskId, ex.Message);
        }
    }
}
