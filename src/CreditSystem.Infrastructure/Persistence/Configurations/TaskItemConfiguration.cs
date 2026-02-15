namespace CreditSystem.Infrastructure.Persistence.Configurations;

using CreditSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired();

        builder.Property(t => t.Cost);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.StartedAt);

        builder.Property(t => t.CompletedAt);

        builder.Property(t => t.FailureReason)
            .HasMaxLength(512);

        builder.HasIndex(t => t.UserId);
    }
}
