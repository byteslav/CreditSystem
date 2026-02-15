namespace CreditSystem.Application.DTOs.Tasks;

public class TaskListItemResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = null!;
    public int? Cost { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
