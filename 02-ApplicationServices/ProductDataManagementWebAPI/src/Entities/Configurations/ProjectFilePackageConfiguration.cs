using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ProjectFilePackageConfiguration : IEntityTypeConfiguration<ProjectFilePackage>
    {
        public void Configure(EntityTypeBuilder<ProjectFilePackage> builder)
        {
            builder.HasKey(pfp => pfp.Id);
            
            builder.Property(pfp => pfp.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            builder.Property(pfp => pfp.CreatedAt)
                .IsRequired();
            
            builder.Property(pfp => pfp.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasQueryFilter(pfp => !pfp.IsDeleted);

            // Relation with Project
            builder.HasOne(pfp => pfp.Project)
                .WithMany()
                .HasForeignKey(pfp => pfp.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relation with Owner
            builder.HasOne(pfp => pfp.Owner)
                .WithMany()
                .HasForeignKey(pfp => pfp.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relation with User (CreatedBy)
            builder.HasOne(pfp => pfp.CreatedByUser)
                .WithMany()
                .HasForeignKey(pfp => pfp.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relation with TenantMember (OwnerTenantMember)
            builder.HasOne(pfp => pfp.OwnerTenantMember)
                .WithMany()
                .HasForeignKey(pfp => new { pfp.TenantId, pfp.OwnerId })
                .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            // Relation with TenantMember (CreatedByTenantMember)
            builder.HasOne(pfp => pfp.CreatedByTenantMember)
                .WithMany()
                .HasForeignKey(pfp => new { pfp.TenantId, pfp.CreatedByUserId })
                .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: package name must be unique per tenant + project + owner
            builder.HasIndex(pfp => new { pfp.TenantId, pfp.ProjectId, pfp.OwnerId, pfp.Name })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // Index for fast lookups
            builder.HasIndex(pfp => new { pfp.ProjectId, pfp.TenantId });
            
            // Index for owner's packages
            builder.HasIndex(pfp => new { pfp.OwnerId, pfp.ProjectId });
            
            // Index for non-deleted packages
            builder.HasIndex(pfp => new { pfp.ProjectId, pfp.IsDeleted });
        }
    }
}
