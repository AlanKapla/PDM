using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities.Models; 

namespace Entities.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
            builder.Property(u => u.PasswordHash).IsRequired(false); // opcjonalne dla użytkowników Google
            builder.Property(u => u.FirstName).HasMaxLength(100);
            builder.Property(u => u.LastName).HasMaxLength(100);
            builder.Property(u => u.IsActive).HasDefaultValue(false);
            builder.Property(p => p.SystemRole).HasConversion<string>();
            
            // Pola dla zewnętrznych providerów
            builder.Property(u => u.AuthProvider).HasConversion<string>().HasDefaultValue(AuthProvider.Local);
            builder.Property(u => u.ExternalId).HasMaxLength(200);
            
            // Indeks dla kombinacji provider + external ID
            builder.HasIndex(u => new { u.AuthProvider, u.ExternalId });
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

    public class UserPasswordResetConfiguration : IEntityTypeConfiguration<UserPasswordReset>
    {
        public void Configure(EntityTypeBuilder<UserPasswordReset> builder)
        {
            builder.HasKey(pr => pr.Id);
            builder.Property(pr => pr.Token).IsRequired().HasMaxLength(200);
            builder.HasIndex(pr => pr.Token).IsUnique();
            builder.Property(pr => pr.ExpiresAt).IsRequired();
            builder.Property(pr => pr.CreatedAt).IsRequired();
            builder.HasOne(pr => pr.User)
                   .WithMany(u => u.PasswordResets)
                   .HasForeignKey(pr => pr.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class UserActivationConfiguration : IEntityTypeConfiguration<UserActivation>
    {
        public void Configure(EntityTypeBuilder<UserActivation> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Token).IsRequired().HasMaxLength(200);
            builder.HasIndex(a => a.Token).IsUnique();
            builder.Property(a => a.ExpiresAt).IsRequired();
            builder.Property(a => a.CreatedAt).IsRequired();
            builder.HasOne(a => a.User)
                   .WithMany(u => u.Activations)
                   .HasForeignKey(a => a.UserId)
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
                   .OnDelete(DeleteBehavior.Cascade);
            builder.HasDiscriminator<string>("ProfileType")
                   .HasValue<TenantPreferencesProfile>("TenantPreferences");
        }
    }
}