using Entities.Models.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class TenantInvitationConfiguration : IEntityTypeConfiguration<TenantInvitation>
    {
        public void Configure(EntityTypeBuilder<TenantInvitation> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.Token)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.HasIndex(x => x.Token)
                .IsUnique();

            builder.HasIndex(x => new { x.TenantId, x.Email });

            builder.HasIndex(x => new { x.TenantId, x.Email, x.ProjectId });

            builder.HasIndex(x => new { x.TenantId, x.Status });

            builder.HasIndex(x => x.ExpiresAt);

            builder.HasOne(x => x.InvitedByUser)
                .WithMany()
                .HasForeignKey(x => x.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class TenantInvitationModulePermissionConfiguration : IEntityTypeConfiguration<TenantInvitationModulePermission>
    {
        public void Configure(EntityTypeBuilder<TenantInvitationModulePermission> builder)
        {
            builder.HasKey(p => new { p.InvitationId, p.Module });

            builder.Property(p => p.Module).HasConversion<int>();

            builder.HasOne(p => p.Invitation)
                .WithMany(i => i.ModulePermissions)
                .HasForeignKey(p => p.InvitationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
