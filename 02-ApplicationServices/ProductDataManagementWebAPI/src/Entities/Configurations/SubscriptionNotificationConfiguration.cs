using Entities.Models.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class SubscriptionNotificationConfiguration : IEntityTypeConfiguration<SubscriptionNotification>
    {
        public void Configure(EntityTypeBuilder<SubscriptionNotification> builder)
        {
            builder.ToTable("SubscriptionNotifications");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(n => n.RecipientEmail)
                .HasColumnType("varchar(256)")
                .IsRequired();

            builder.Property(n => n.Subject)
                .HasColumnType("varchar(512)")
                .IsRequired();

            builder.Property(n => n.Body)
                .HasColumnType("text")
                .IsRequired();

            builder.HasIndex(n => new { n.TenantId, n.Type, n.SentAt });
        }
    }
}
