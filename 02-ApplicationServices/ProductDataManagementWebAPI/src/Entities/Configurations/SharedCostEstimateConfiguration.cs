using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.CostEstimates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class SharedCostEstimateConfiguration : IEntityTypeConfiguration<SharedCostEstimate>
    {
        public void Configure(EntityTypeBuilder<SharedCostEstimate> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.TenantId).IsRequired();
            builder.Property(s => s.ProjectId).IsRequired();
            builder.Property(s => s.CostEstimateId).IsRequired();
            builder.Property(s => s.SharedByUserId).IsRequired();
            builder.Property(s => s.SharedWithUserId).IsRequired();
            builder.Property(s => s.SharedAt).IsRequired();

            builder.HasOne(s => s.CostEstimate)
                .WithMany()
                .HasForeignKey(s => s.CostEstimateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.SharedByUser)
                .WithMany()
                .HasForeignKey(s => s.SharedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.SharedWithUser)
                .WithMany()
                .HasForeignKey(s => s.SharedWithUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.SharedByTenantMember)
                .WithMany()
                .HasForeignKey(s => new { s.TenantId, s.SharedByUserId })
                .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.SharedWithTenantMember)
                .WithMany()
                .HasForeignKey(s => new { s.TenantId, s.SharedWithUserId })
                .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.SharedByProjectMember)
                .WithMany()
                .HasForeignKey(s => new { s.TenantId, s.ProjectId, s.SharedByUserId })
                .HasPrincipalKey(pm => new { pm.TenantId, pm.ProjectId, pm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.SharedWithProjectMember)
                .WithMany()
                .HasForeignKey(s => new { s.TenantId, s.ProjectId, s.SharedWithUserId })
                .HasPrincipalKey(pm => new { pm.TenantId, pm.ProjectId, pm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => new { s.CostEstimateId, s.SharedWithUserId })
                .IsUnique();

            builder.HasIndex(s => new { s.SharedWithUserId, s.ProjectId });
            builder.HasIndex(s => s.CostEstimateId);
        }
    }
}
