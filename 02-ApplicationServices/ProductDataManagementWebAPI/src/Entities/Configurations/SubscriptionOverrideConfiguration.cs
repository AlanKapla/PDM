using Entities.Models.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class SubscriptionOverrideConfiguration : IEntityTypeConfiguration<SubscriptionOverride>
    {
        public void Configure(EntityTypeBuilder<SubscriptionOverride> builder)
        {
            builder.ToTable("SubscriptionOverrides");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.Key)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(o => o.Value)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(o => o.Reason)
                .IsRequired()
                .HasMaxLength(1024);

            builder.Property(o => o.IsActive)
                .HasDefaultValue(true);

            builder.HasIndex(o => new { o.TenantSubscriptionId, o.Key, o.IsActive });
        }
    }
}
