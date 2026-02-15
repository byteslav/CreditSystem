namespace CreditSystem.Infrastructure.Extensions;

using CreditSystem.Application.Interfaces.Repositories;
using CreditSystem.Application.Interfaces.Security;
using CreditSystem.Application.Interfaces.Services;
using CreditSystem.Application.Services;
using CreditSystem.Infrastructure.Persistence;
using CreditSystem.Infrastructure.Repositories;
using CreditSystem.Infrastructure.Security;
using CreditSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddAuthenticationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? "SUPER_SECRET_DEVELOPMENT_KEY_123456789";
        var issuer = jwtSettings["Issuer"] ?? "CreditSystem";
        var audience = jwtSettings["Audience"] ?? "CreditSystemUsers";

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator>(sp =>
            new JwtTokenGenerator(secretKey, issuer, audience, expirationMinutes: 60));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskExecutionService, TaskExecutionService>();

        return services;
    }
}

