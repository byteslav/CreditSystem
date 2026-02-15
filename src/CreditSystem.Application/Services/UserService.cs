namespace CreditSystem.Application.Services;

using CreditSystem.Application.DTOs;
using CreditSystem.Application.Interfaces.Repositories;
using CreditSystem.Application.Interfaces.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<MeResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        
        if (user == null)
        {
            return null;
        }

        return new MeResponse
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            Credits = user.Credits,
            RegisteredAt = user.RegisteredAt
        };
    }
}
