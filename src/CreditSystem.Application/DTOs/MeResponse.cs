namespace CreditSystem.Application.DTOs;

public class MeResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public int Credits { get; set; }
    public DateTime RegisteredAt { get; set; }
}
