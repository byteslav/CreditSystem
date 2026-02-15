namespace CreditSystem.Application.Services;

using CreditSystem.Application.DTOs;
using CreditSystem.Application.Interfaces.Repositories;
using CreditSystem.Application.Interfaces.Security;
using CreditSystem.Application.Interfaces.Services;
using CreditSystem.Domain.Entities;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEmail != null)
        {
            throw new InvalidOperationException("Email is already registered");
        }

        var existingUsername = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUsername != null)
        {
            throw new InvalidOperationException("Username is already taken");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Username = request.Username,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Credits = 500,
            RegisteredAt = DateTime.UtcNow,
            LastCreditGrantAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var token = _tokenGenerator.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Credits = user.Credits
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var token = _tokenGenerator.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Credits = user.Credits
        };
    }
}
