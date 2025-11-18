using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
            builder.Property(u => u.PasswordHash).IsRequired();
            builder.Property(u => u.FirstName).HasMaxLength(100);
            builder.Property(u => u.LastName).HasMaxLength(100);

            builder.Property(p => p.SystemRole).HasConversion<string>();
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
}