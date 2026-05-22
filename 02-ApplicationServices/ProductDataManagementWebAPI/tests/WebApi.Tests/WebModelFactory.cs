using Business.Interfaces.WebModels.Chats;
using Business.Interfaces.WebModels.CostTrackers;
using Business.Interfaces.WebModels.Projects;
using Business.Interfaces.WebModels.Tenants;
using Business.Interfaces.WebModels.WorkSchedules;

namespace WebApi.Tests
{
    /// <summary>
    /// Factory helpers producing minimal valid instances of Web models so that
    /// controller code paths that dereference response (.Id, etc.) do not NRE
    /// during unit tests.
    /// </summary>
    internal static class WebModelFactory
    {
        public static ProjectDetailsWeb Project(Guid id) => new()
        {
            Id = id,
            TenantId = Guid.NewGuid(),
            Name = "p",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = Guid.NewGuid(),
            CreatedByUserName = string.Empty,
            UserRoleCode = string.Empty,
            MembersCount = 0,
            UserPermissions = new HashSet<string>()
        };

        public static TenantDetailsWeb Tenant(Guid id) => new()
        {
            Id = id,
            Name = "t",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            RoleCode = string.Empty
        };

        public static ActiveTenantWeb ActiveTenant(Guid id) => new()
        {
            ActiveTenantId = id
        };

        public static TrackedCostWeb TrackedCost(Guid id) => new()
        {
            Id = id,
            IsAdditional = false,
            Name = "c",
            CreatedAt = DateTime.UtcNow,
            Attachments = new List<TrackedCostAttachmentWeb>(),
            SourceType = CostSourceType.ProjectAdditional
        };

        public static CreateChatResultWeb ChatResult(Guid id) => new(id, false);

        public static WorkScheduleDetailsWeb WorkSchedule(Guid id) => new(
            Id: id,
            TenantId: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            CostEstimateId: null,
            Name: "ws",
            CreatedAt: DateTime.UtcNow,
            CreatedByUserId: Guid.NewGuid(),
            CreatedByUserName: string.Empty,
            Stages: new List<WorkScheduleStageWeb>(),
            Dependencies: new List<WorkScheduleWorkDependencyWeb>()
        );
    }
}
