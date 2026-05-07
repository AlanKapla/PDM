using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ProjectCostConfiguration : IEntityTypeConfiguration<ProjectCost>
    {
        public void Configure(EntityTypeBuilder<ProjectCost> builder)
        {
            builder.Property(pc => pc.UserId).IsRequired();

            builder.Property(pc => pc.Place)
                .HasMaxLength(500);

            builder.Property(pc => pc.IsAccepted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(pc => pc.AcceptedByUserId);
            builder.Property(pc => pc.AcceptedAt);

            builder.HasOne(pc => pc.ProjectMember)
                .WithMany()
                .HasForeignKey(pc => new { pc.TenantId, pc.ProjectId, pc.UserId })
                .HasPrincipalKey(pm => new { pm.TenantId, pm.ProjectId, pm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(pc => new { pc.TenantId, pc.ProjectId, pc.IsAccepted });
            builder.HasIndex(pc => pc.Date);
        }
    }
}
