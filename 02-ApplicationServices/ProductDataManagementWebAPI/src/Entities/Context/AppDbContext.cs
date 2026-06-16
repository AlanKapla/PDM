using Microsoft.EntityFrameworkCore; // required for DbContext, DbSet
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models;
using Entities.Models.WorkSchedules;
using Entities.Models.Base;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;

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
        public DbSet<ProjectMemberModulePermission> ProjectMemberModulePermissions => Set<ProjectMemberModulePermission>();
        public DbSet<ProjectParams> ProjectParams => Set<ProjectParams>();
        public DbSet<ProjectCurrency> ProjectCurrencies => Set<ProjectCurrency>();
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
        public DbSet<WorkScheduleStageWorkPeriod> WorkScheduleStageWorkPeriods => Set<WorkScheduleStageWorkPeriod>();
        public DbSet<WorkScheduleStageWorkAssignment> WorkScheduleStageWorkAssignments => Set<WorkScheduleStageWorkAssignment>();
        public DbSet<WorkScheduleStageWorkComment> WorkScheduleStageWorkComments => Set<WorkScheduleStageWorkComment>();
        public DbSet<WorkScheduleStageWorkDependency> WorkScheduleStageWorkDependencies => Set<WorkScheduleStageWorkDependency>();

        public DbSet<CostEstimate> CostEstimates => Set<CostEstimate>();
        public DbSet<CostEstimateGroup> CostEstimateGroups => Set<CostEstimateGroup>();
        public DbSet<CostEstimateItem> CostEstimateItems => Set<CostEstimateItem>();
        public DbSet<SharedCostEstimate> SharedCostEstimates => Set<SharedCostEstimate>();
        public DbSet<CostEstimateFieldSchema> CostEstimateFieldSchemas => Set<CostEstimateFieldSchema>();
        public DbSet<CostEstimateAdditionalFieldValue> CostEstimateAdditionalFieldValues => Set<CostEstimateAdditionalFieldValue>();
        public DbSet<CostEstimateItemFile> CostEstimateItemFiles => Set<CostEstimateItemFile>();
        public DbSet<Contractor> Contractors => Set<Contractor>();
        public DbSet<BaseCost> Costs => Set<BaseCost>();
        public DbSet<TrackedCost> TrackedCosts => Set<TrackedCost>();
        public DbSet<ProjectCost> ProjectCosts => Set<ProjectCost>();
        public DbSet<BaseCostAttachment> CostAttachments => Set<BaseCostAttachment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            DateTime now = DateTime.UtcNow;

            foreach (EntityEntry<DeletableEntity> entry in ChangeTracker.Entries<DeletableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.IsDeleted = false;
                    entry.Entity.DeletedAt = null;
                }
            }

            foreach (EntityEntry entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added
                    && entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
                {
                    entry.Property("CreatedAt").CurrentValue = now;
                }

                if (entry.State == EntityState.Modified
                    && entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                {
                    entry.Property("UpdatedAt").CurrentValue = now;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
