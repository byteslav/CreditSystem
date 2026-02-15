namespace CreditSystem.Infrastructure.Repositories;

using CreditSystem.Application.Interfaces.Repositories;
using CreditSystem.Domain.Entities;
using CreditSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _context.TaskItems.AddAsync(task, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TaskItem?> GetByIdAsyncTracked(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
