using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserApp.Domain.Notifications;

namespace UserApp.Infrastructure.Notifications.Configurations;

public sealed class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        // Primary Key
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.ActionUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Metadata)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.RecipientId);

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.CreatedAt);

        builder.HasIndex(x => x.ReadAt);

        builder.HasIndex(x => new
        {
            x.RecipientId,
            x.ReadAt
        });

        builder.HasIndex(x => new
        {
            x.RecipientId,
            x.CreatedAt
        });
    }
}