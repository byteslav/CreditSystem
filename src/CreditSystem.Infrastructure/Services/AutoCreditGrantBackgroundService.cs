namespace CreditSystem.Infrastructure.Services;

using System.Data;
using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Infrastructure.Options;
using CreditSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class AutoCreditGrantBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoCreditGrantBackgroundService> _logger;
    private readonly AutoGrantOptions _options;
    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;

    public AutoCreditGrantBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<AutoGrantOptions> options,
        ILogger<AutoCreditGrantBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting auto credit grant service. GrantAmount={GrantAmount}, GrantFrequencyDays={GrantFrequencyDays}, CheckIntervalMinutes={CheckIntervalMinutes}",
            _options.GrantAmount,
            _options.GrantFrequencyDays,
            GetCheckInterval().TotalMinutes);

        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = RunAsync(_stoppingCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping auto credit grant service.");

        if (_executingTask == null || _stoppingCts == null)
        {
            return;
        }

        _stoppingCts.Cancel();
        await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        var checkInterval = GetCheckInterval();

        await ProcessDueGrantsSafeAsync(stoppingToken);

        using var timer = new PeriodicTimer(checkInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessDueGrantsSafeAsync(stoppingToken);
        }
    }

    private async Task ProcessDueGrantsSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ProcessDueGrantsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Auto credit grant service is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while processing auto credit grants.");
        }
    }

    private async Task ProcessDueGrantsAsync(CancellationToken cancellationToken)
    {
        var grantAmount = GetGrantAmount();
        var grantFrequencyDays = GetGrantFrequencyDays();

        var now = DateTime.UtcNow;
        var dueBefore = now.AddDays(-grantFrequencyDays);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            var eligibleUsers = await dbContext.Users
                .Where(u => (u.LastCreditGrantAt ?? u.RegisteredAt) <= dueBefore)
                .OrderBy(u => u.Id)
                .ToListAsync(cancellationToken);

            if (eligibleUsers.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var grantedAt = DateTime.UtcNow;

            foreach (var user in eligibleUsers)
            {
                var baseline = user.LastCreditGrantAt ?? user.RegisteredAt;
                if (baseline > dueBefore)
                {
                    continue;
                }

                user.Credits += grantAmount;
                user.LastCreditGrantAt = grantedAt;

                dbContext.CreditTransactions.Add(new CreditTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TaskItemId = null,
                    Amount = grantAmount,
                    Type = CreditTransactionType.AutoGrant,
                    CreatedAt = grantedAt
                });

                _logger.LogInformation(
                    "Auto-granted {GrantAmount} credits to UserId={UserId} at {GrantedAt}",
                    grantAmount,
                    user.Id,
                    grantedAt);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private int GetGrantAmount() => Math.Max(_options.GrantAmount, 1);

    private int GetGrantFrequencyDays() => Math.Max(_options.GrantFrequencyDays, 1);

    private TimeSpan GetCheckInterval()
    {
        var minutes = Math.Max(_options.CheckIntervalMinutes, 1);
        return TimeSpan.FromMinutes(minutes);
    }

    public void Dispose()
    {
        _stoppingCts?.Dispose();
    }
}
