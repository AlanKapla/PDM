using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class SharedProjectCostConfiguration : IEntityTypeConfiguration<SharedProjectCost>
    {
        public void Configure(EntityTypeBuilder<SharedProjectCost> builder)
        {
            builder.HasKey(spc => spc.Id);

            builder.Property(spc => spc.TenantId).IsRequired();
            builder.Property(spc => spc.ProjectId).IsRequired();
            builder.Property(spc => spc.ProjectCostId).IsRequired();
            builder.Property(spc => spc.SharedWithUserId).IsRequired();
            builder.Property(spc => spc.SharedByUserId).IsRequired();
            builder.Property(spc => spc.SharedAt).IsRequired();

            // Unique constraint: one user can have cost shared only once
            builder.HasIndex(spc => new { spc.ProjectCostId, spc.SharedWithUserId })
                .IsUnique();

            // Indexes for queries
            builder.HasIndex(spc => new { spc.TenantId, spc.ProjectId, spc.SharedWithUserId });
            builder.HasIndex(spc => spc.ProjectCostId);

            // Relationships
            builder.HasOne(spc => spc.ProjectCost)
                .WithMany(pc => pc.SharedWith)
                .HasForeignKey(spc => spc.ProjectCostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(spc => spc.SharedWithTenantMember)
                .WithMany()
                .HasForeignKey(spc => new { spc.TenantId, spc.SharedWithUserId })
                .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(spc => spc.SharedByTenantMember)
                .WithMany()
                .HasForeignKey(spc => new { spc.TenantId, spc.SharedByUserId })
                .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
