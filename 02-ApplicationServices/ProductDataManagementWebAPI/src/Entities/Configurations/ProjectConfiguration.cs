using Entities.Models.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);

            builder.HasOne(p => p.Tenant)
                   .WithMany(t => t.Projects)
                   .HasForeignKey(p => p.TenantId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CreatedByUserId })
                .HasPrincipalKey(t => new { t.TenantId, t.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(p => p.BudgetNet).HasPrecision(18, 4);
            builder.Property(p => p.BudgetGross).HasPrecision(18, 4);
        }
    }

    public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> builder)
        {
            builder.HasKey(pm => new { pm.TenantId, pm.ProjectId, pm.UserId });

            builder.Property(pm => pm.IsActive)
                .HasDefaultValue(true);

            builder.HasOne(pm => pm.Project)
                   .WithMany(p => p.Members)
                   .HasForeignKey(pm => pm.ProjectId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pm => pm.TenantMember)
                   .WithMany(u => u.ProjectMembers)
                   .HasForeignKey(a => new { a.TenantId, a.UserId })
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ProjectMemberModulePermissionConfiguration : IEntityTypeConfiguration<ProjectMemberModulePermission>
    {
        public void Configure(EntityTypeBuilder<ProjectMemberModulePermission> builder)
        {
            builder.HasKey(p => new { p.TenantId, p.ProjectId, p.UserId, p.Module });

            builder.Property(p => p.Module).HasConversion<int>();

            builder.HasOne(p => p.ProjectMember)
                   .WithMany(pm => pm.ModulePermissions)
                   .HasForeignKey(p => new { p.TenantId, p.ProjectId, p.UserId })
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
