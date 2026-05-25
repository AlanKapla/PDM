using Entities.Models.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
        {
            builder.ToTable("SubscriptionPayments");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Plan)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(p => p.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(p => p.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(p => p.Currency)
                .HasColumnType("varchar(8)")
                .IsRequired();

            builder.Property(p => p.ExternalTransactionId)
                .HasColumnType("varchar(256)");

            builder.Property(p => p.FailureReason)
                .HasColumnType("varchar(1024)");

            builder.HasIndex(p => new { p.TenantSubscriptionId, p.Status });
            builder.HasIndex(p => p.TenantId);

            builder.HasOne(p => p.TenantSubscription)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.TenantSubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
