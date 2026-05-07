using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models;
using Entities.Models.WorkSchedules;

namespace Entities.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
            builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.AzureAdB2CObjectId).IsRequired().HasMaxLength(200);
            builder.Property(u => u.IsActive).HasDefaultValue(false);
            builder.Property(p => p.SystemRole).HasConversion<string>();
            
            builder.HasIndex(u => u.AzureAdB2CObjectId).IsUnique();
        }
    }

    public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
    {
        public void Configure(EntityTypeBuilder<UserSession> builder)
        {
            builder.HasKey(us => us.Id);
            builder.Property(us => us.RefreshToken).IsRequired();
            builder.Property(us => us.ExpiresAt).IsRequired();
            builder.HasOne(us => us.User)
                   .WithMany(u => u.UserSessions)
                   .HasForeignKey(us => us.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfileBase>
    {
        public void Configure(EntityTypeBuilder<UserProfileBase> builder)
        {
            builder.HasKey(p => p.Id);
            builder.HasOne(p => p.User)
                   .WithMany(u => u.Profiles)
                   .HasForeignKey(p => p.UserId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);
            builder.HasDiscriminator<string>("ProfileType")
                   .HasValue<TenantPreferencesProfile>("TenantPreferences")
                   .HasValue<PermissionsVersionProfile>("PermissionsVersion");
        }
    }
}
