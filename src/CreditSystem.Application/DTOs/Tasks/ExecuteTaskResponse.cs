namespace CreditSystem.Application.DTOs.Tasks;

public class ExecuteTaskResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = null!;
    public int Cost { get; set; }
    public DateTime StartedAt { get; set; }
    public string? Message { get; set; }
}
