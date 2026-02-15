namespace CreditSystem.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public int Credits { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastCreditGrantAt { get; set; }
}
