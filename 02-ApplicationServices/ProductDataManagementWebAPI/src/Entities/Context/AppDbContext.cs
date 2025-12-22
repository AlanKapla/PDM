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
        public DbSet<ProjectFilePackage> ProjectFilePackages => Set<ProjectFilePackage>();
        public DbSet<ProjectFile> ProjectFiles => Set<ProjectFile>();
        public DbSet<ProjectFileVersion> ProjectFileVersions => Set<ProjectFileVersion>();
        public DbSet<ProjectFileVersionComment> ProjectFileVersionComments => Set<ProjectFileVersionComment>();
        public DbSet<SharedProjectFile> SharedProjectFiles => Set<SharedProjectFile>();
        public DbSet<UserProfileBase> UserProfiles => Set<UserProfileBase>();
        public DbSet<TenantPreferencesProfile> TenantPreferencesProfiles => Set<TenantPreferencesProfile>();
        public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Chat> Chats => Set<Chat>();
        public DbSet<ChatMember> ChatMembers => Set<ChatMember>();
        public DbSet<MessageHistory> MessageHistories => Set<MessageHistory>();
        public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
        public DbSet<WorkScheduleStage> WorkScheduleStages => Set<WorkScheduleStage>();
        public DbSet<WorkScheduleStageWork> WorkScheduleStageWorks => Set<WorkScheduleStageWork>();
        public DbSet<WorkScheduleStageWorkAssignment> WorkScheduleStageWorkAssignments => Set<WorkScheduleStageWorkAssignment>();
        public DbSet<ProjectCost> ProjectCosts => Set<ProjectCost>();
        public DbSet<SharedProjectCost> SharedProjectCosts => Set<SharedProjectCost>();
        public DbSet<CostEstimateTemplate> CostEstimateTemplates => Set<CostEstimateTemplate>();
        public DbSet<CostEstimate> CostEstimates => Set<CostEstimate>();

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
