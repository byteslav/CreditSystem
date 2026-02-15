namespace CreditSystem.Application.DTOs.Tasks;

public class CreateTaskResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
