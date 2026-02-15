namespace CreditSystem.Domain.Entities;

using CreditSystem.Domain.Enums;

public class CreditTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TaskItemId { get; set; }
    public int Amount { get; set; }
    public CreditTransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}
