namespace CreditSystem.Application.Interfaces.Services;

using CreditSystem.Application.DTOs;

public interface IUserService
{
    Task<MeResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
