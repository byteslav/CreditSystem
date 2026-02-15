namespace CreditSystem.Application.Services;

using CreditSystem.Application.DTOs.Tasks;
using CreditSystem.Application.Interfaces.Repositories;
using CreditSystem.Application.Interfaces.Services;
using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<CreateTaskResponse> CreateTaskAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var taskItem = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = TaskStatus.Created,
            Cost = null,
            CreatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(taskItem, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return new CreateTaskResponse
        {
            Id = taskItem.Id,
            Status = taskItem.Status.ToString(),
            CreatedAt = taskItem.CreatedAt
        };
    }

    public async Task<IReadOnlyList<TaskListItemResponse>> GetUserTasksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetByUserIdAsync(userId, cancellationToken);

        return tasks
            .Select(t => new TaskListItemResponse
            {
                Id = t.Id,
                Status = t.Status.ToString(),
                Cost = t.Cost,
                CreatedAt = t.CreatedAt,
                StartedAt = t.StartedAt,
                CompletedAt = t.CompletedAt
            })
            .ToList()
            .AsReadOnly();
    }
}
