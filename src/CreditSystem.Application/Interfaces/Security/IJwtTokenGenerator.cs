namespace CreditSystem.Application.Interfaces.Security;

using CreditSystem.Domain.Entities;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
