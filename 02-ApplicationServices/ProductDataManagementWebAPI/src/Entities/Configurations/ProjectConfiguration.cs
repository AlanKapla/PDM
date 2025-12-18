using Entities.Models;
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
        }
    }

    public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> builder)
        {
            builder.HasKey(pm => new { pm.TenantId, pm.ProjectId, pm.UserId});

            builder.Property(p => p.Role).HasConversion<string>();

            builder.HasOne(pm => pm.Project)
                   .WithMany(p => p.Members)
                   .HasForeignKey(pm => pm.ProjectId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pm => pm.TenantMember)
                   .WithMany(u => u.ProjectMembers)
                   .HasForeignKey(a => new { a.TenantId, a.UserId})
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ProjectGroupConfig : IEntityTypeConfiguration<ProjectGroup>
    {
        public void Configure(EntityTypeBuilder<ProjectGroup> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);

            builder.HasOne(x => x.Project)
                .WithMany(p => p.Groups)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ProjectGroupMemberConfig : IEntityTypeConfiguration<ProjectGroupMember>
    {
        public void Configure(EntityTypeBuilder<ProjectGroupMember> builder)
        {
            builder.HasKey(x => new { x.ProjectGroupId, x.ProjectId, x.TenantId, x.UserId });

            builder.HasOne(x => x.ProjectGroup)
                .WithMany(g => g.Members)
                .HasForeignKey(a => a.ProjectGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ProjectMember)
                .WithMany(m => m.ProjectGroupMembers)
                .HasForeignKey(x => new { x.ProjectId, x.TenantId, x.UserId })
                .HasPrincipalKey(m => new { m.ProjectId, m.TenantId, m.UserId })
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
