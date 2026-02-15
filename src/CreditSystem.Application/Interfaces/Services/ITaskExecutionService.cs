namespace CreditSystem.Application.Interfaces.Services;

using CreditSystem.Application.DTOs.Tasks;

public interface ITaskExecutionService
{
    Task<ExecuteTaskResponse> ExecuteTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);
}
