namespace CreditSystem.Infrastructure.Persistence.Configurations;

using CreditSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CreditTransactionConfiguration : IEntityTypeConfiguration<CreditTransaction>
{
    public void Configure(EntityTypeBuilder<CreditTransaction> builder)
    {
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.UserId)
            .IsRequired();

        builder.Property(ct => ct.TaskItemId);

        builder.Property(ct => ct.Amount)
            .IsRequired();

        builder.Property(ct => ct.Type)
            .IsRequired();

        builder.Property(ct => ct.CreatedAt)
            .IsRequired();
    }
}
