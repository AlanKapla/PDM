using Microsoft.EntityFrameworkCore; // required for DbContext, DbSet
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
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
        public DbSet<WorkScheduleStageWorkComment> WorkScheduleStageWorkComments => Set<WorkScheduleStageWorkComment>();
        public DbSet<WorkScheduleStageWorkDependency> WorkScheduleStageWorkDependencies => Set<WorkScheduleStageWorkDependency>();
        public DbSet<ProjectCost> ProjectCosts => Set<ProjectCost>();
        public DbSet<SharedProjectCost> SharedProjectCosts => Set<SharedProjectCost>();
        public DbSet<CostEstimateTemplate> CostEstimateTemplates => Set<CostEstimateTemplate>();
        public DbSet<CostEstimateTemplateCurrency> CostEstimateTemplateCurrencies => Set<CostEstimateTemplateCurrency>();
        public DbSet<CostEstimateTemplateUnit> CostEstimateTemplateUnits => Set<CostEstimateTemplateUnit>();
        public DbSet<CostEstimateTemplateCategory> CostEstimateTemplateCategories => Set<CostEstimateTemplateCategory>();
        public DbSet<CostEstimateTemplateGroupFieldDefinition> CostEstimateTemplateGroupFieldDefinitions => Set<CostEstimateTemplateGroupFieldDefinition>();
        public DbSet<CostEstimateTemplateItemSystemFieldDefinition> CostEstimateTemplateItemSystemFieldDefinitions => Set<CostEstimateTemplateItemSystemFieldDefinition>();
        public DbSet<CostEstimateTemplateItemCalculatedFieldDefinition> CostEstimateTemplateItemCalculatedFieldDefinitions => Set<CostEstimateTemplateItemCalculatedFieldDefinition>();
        public DbSet<CostEstimateTemplateItemGenericFieldDefinition> CostEstimateTemplateItemGenericFieldDefinitions => Set<CostEstimateTemplateItemGenericFieldDefinition>();
        public DbSet<CostEstimate> CostEstimates => Set<CostEstimate>();
        public DbSet<CostEstimateGroup> CostEstimateGroups => Set<CostEstimateGroup>();
        public DbSet<CostEstimateGroupFieldValue> CostEstimateGroupFieldValues => Set<CostEstimateGroupFieldValue>();
        public DbSet<CostEstimateItem> CostEstimateItems => Set<CostEstimateItem>();
        public DbSet<CostEstimateItemFieldValue> CostEstimateItemFieldValues => Set<CostEstimateItemFieldValue>();
        public DbSet<CostEstimateFieldFile> CostEstimateFieldFiles => Set<CostEstimateFieldFile>();
        public DbSet<SharedCostEstimate> SharedCostEstimates => Set<SharedCostEstimate>();
        public DbSet<CostTracker> CostTrackers => Set<CostTracker>();
        public DbSet<TrackedCost> TrackedCosts => Set<TrackedCost>();
        public DbSet<TrackedCostAttachment> TrackedCostAttachments => Set<TrackedCostAttachment>();
        public DbSet<ProjectCostTrackedCostLink> ProjectCostTrackedCostLinks => Set<ProjectCostTrackedCostLink>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

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
                 .HasValue<TenantPreferencesProfile>("TenantPreferences")
                 .HasValue<PermissionsVersionProfile>("PermissionsVersion");
            });
        }
    }
}
