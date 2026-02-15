namespace CreditSystem.Application.Interfaces.Services;

using CreditSystem.Application.DTOs.Tasks;

public interface ITaskService
{
    Task<CreateTaskResponse> CreateTaskAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskListItemResponse>> GetUserTasksAsync(Guid userId, CancellationToken cancellationToken = default);
}
