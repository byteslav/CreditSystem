namespace CreditSystem.Application.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = null!;
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public int Credits { get; set; }
}
