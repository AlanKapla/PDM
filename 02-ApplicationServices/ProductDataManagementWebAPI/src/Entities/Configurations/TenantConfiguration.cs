using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        }
    }

    public class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMember>
    {
        public void Configure(EntityTypeBuilder<TenantMember> builder)
        {
            builder.HasKey(tm => new { tm.TenantId, tm.UserId });

            builder.HasOne(tm => tm.Tenant)
                   .WithMany(t => t.Members)
                   .HasForeignKey(tm => tm.TenantId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(tm => tm.User)
                   .WithMany(u => u.TenantMemberships)
                   .HasForeignKey(tm => tm.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(p => p.Role)
                .HasConversion<string>();
        }
    }
}