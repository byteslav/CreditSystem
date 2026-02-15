namespace CreditSystem.Domain.Entities;

using CreditSystem.Domain.Enums;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public TaskStatus Status { get; set; }
    public int? Cost { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}
