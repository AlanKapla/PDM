using Microsoft.EntityFrameworkCore; // required for DbContext, DbSet
using Entities.Models; // domenowe encje

namespace Entities.Context
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<UserSession> UserSessions => Set<UserSession>();
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<TenantMember> TenantMembers=> Set<TenantMember>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<UserPasswordReset> UserPasswordResets => Set<UserPasswordReset>();
        public DbSet<UserActivation> UserActivations => Set<UserActivation>();
        public DbSet<UserProfileBase> UserProfiles => Set<UserProfileBase>();
        public DbSet<TenantPreferencesProfile> TenantPreferencesProfiles => Set<TenantPreferencesProfile>();
        public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            modelBuilder.Entity<UserProfileBase>(b =>
            {
                b.HasKey(p => p.Id);
                b.HasOne(p => p.User)
                 .WithMany()
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasDiscriminator<string>("ProfileType")
                 .HasValue<TenantPreferencesProfile>("TenantPreferences");
            });
        }
    }
}
