using Entities.Models.Costs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ProjectCostConfiguration : IEntityTypeConfiguration<ProjectCost>
    {
        public void Configure(EntityTypeBuilder<ProjectCost> builder)
        {
            builder.Property(pc => pc.UserId).IsRequired();

            builder.Property(pc => pc.ApprovalStatus)
                .IsRequired()
                .HasDefaultValue(CostApprovalStatus.Draft)
                .HasConversion<string>();

            builder.Property(pc => pc.ApprovedByUserId);
            builder.Property(pc => pc.ApprovedAt);

            builder.HasOne(pc => pc.ProjectMember)
                .WithMany()
                .HasForeignKey(pc => new { pc.TenantId, pc.ProjectId, pc.UserId })
                .HasPrincipalKey(pm => new { pm.TenantId, pm.ProjectId, pm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(pc => new { pc.TenantId, pc.ProjectId, pc.ApprovalStatus });
            builder.HasIndex(pc => pc.Date);
        }
    }
}
