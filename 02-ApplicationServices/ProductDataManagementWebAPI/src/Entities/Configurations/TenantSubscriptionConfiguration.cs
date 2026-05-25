using Entities.Models.Subscriptions;
using Entities.Models.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
    {
        public void Configure(EntityTypeBuilder<TenantSubscription> builder)
        {
            builder.ToTable("TenantSubscriptions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Plan)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(s => s.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(s => s.MaxProjects)
                .IsRequired();

            builder.Property(s => s.MaxUsers)
                .IsRequired();

            builder.Property(s => s.IsFullAccess)
                .HasDefaultValue(false);

            // ── Billing ──────────────────────────────────────────────────────────

            builder.Property(s => s.NextPaymentDue);

            builder.Property(s => s.LastPaidAt);

            builder.Property(s => s.LastPaidAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(s => s.GracePeriodDays)
                .HasDefaultValue(7);

            builder.Property(s => s.GracePeriodEndsAt);

            builder.HasIndex(s => s.TenantId)
                .IsUnique();

            builder.HasOne(s => s.Tenant)
                .WithOne()
                .HasForeignKey<TenantSubscription>(s => s.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Overrides)
                .WithOne(o => o.TenantSubscription)
                .HasForeignKey(o => o.TenantSubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
