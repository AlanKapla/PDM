using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetTenantProjects
{
    public class GetTenantProjectsQueryHandler : IRequestHandler<GetTenantProjectsQuery, IEnumerable<ProjectDetailsWeb>>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetTenantProjectsQueryHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IRepository<TenantMember> tenantMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<ProjectDetailsWeb>> Handle(GetTenantProjectsQuery request, CancellationToken cancellationToken)
        {
            // SuperAdmin sees all projects in tenant with membership roles where applicable
            if (currentUser.IsSuperAdmin)
            {
                // Get all projects in tenant
                var allProjects = await projectRepo.GetBySearch(p => p.TenantId == request.TenantId);
                var projectIds = allProjects.Select(p => p.Id).ToList();

                // Get user's project memberships to show actual roles
                var userProjectMemberships = await projectMemberRepo.GetBySearch(
                    pm => pm.UserId == currentUser.Id && projectIds.Contains(pm.ProjectId),
                    include => include.Include(pm => pm.MemberRole)
                );

                var membershipDict = userProjectMemberships.ToDictionary(pm => pm.ProjectId);

                // Get all members for member count
                var allProjectMembers = await projectMemberRepo.GetBySearch(
                    pm => projectIds.Contains(pm.ProjectId));

                var membersCountDict = allProjectMembers
                    .GroupBy(pm => pm.ProjectId)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Get creators info
                var creatorUserIds = allProjects.Select(p => p.CreatedByUserId).Distinct().ToList();
                var creators = await tenantMemberRepo.GetBySearch(
                    tm => tm.TenantId == request.TenantId && creatorUserIds.Contains(tm.UserId),
                    include => include.Include(tm => tm.User));

                var creatorsDict = creators.ToDictionary(tm => tm.UserId);

                return allProjects
                    .Select(project =>
                    {
                        int membersCount = membersCountDict.TryGetValue(project.Id, out int count) ? count : 0;
                        
                        var creator = creatorsDict.TryGetValue(project.CreatedByUserId, out var creatorMember) 
                            ? creatorMember 
                            : null;

                        // If has membership, use membership role; otherwise SYSTEM.SUPERADMIN
                        string userRoleCode = membershipDict.TryGetValue(project.Id, out var membership)
                            ? (membership.MemberRole?.Code ?? RoleCodes.ProjectMember)
                            : RoleCodes.SystemSuperAdmin;

                        return new ProjectDetailsWeb(
                            Id: project.Id,
                            TenantId: project.TenantId,
                            Name: project.Name,
                            IsActive: project.IsActive,
                            CreatedAt: project.CreatedAt,
                            CreatedByUserId: project.CreatedByUserId,
                            CreatedByUserName: creator?.User != null 
                                ? $"{creator.User.FirstName} {creator.User.LastName}".Trim()
                                : "Unknown",
                            UserRoleCode: userRoleCode,
                            MembersCount: membersCount
                        );
                    })
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList();
            }

            // Regular users see only their projects (admins see inactive, members only active)
            var regularUserProjectMembers = await projectMemberRepo.GetBySearch(
                pm => pm.TenantId == request.TenantId 
                    && pm.UserId == currentUser.Id
                    && (pm.MemberRole!.Code == RoleCodes.ProjectAdmin || pm.Project.IsActive),
                include => include.Include(pm => pm.Project)
                                 .ThenInclude(p => p.CreatedBy)
                                 .ThenInclude(cb => cb.User)
                                 .Include(pm => pm.MemberRole));

            var regularProjectIds = regularUserProjectMembers.Select(pm => pm.ProjectId).ToList();
            
            var regularAllProjectMembers = await projectMemberRepo.GetBySearch(
                pm => regularProjectIds.Contains(pm.ProjectId));

            var regularMembersCountDict = regularAllProjectMembers
                .GroupBy(pm => pm.ProjectId)
                .ToDictionary(g => g.Key, g => g.Count());

            return regularUserProjectMembers
                .Select(projectMember =>
                {
                    var project = projectMember.Project;
                    int membersCount = regularMembersCountDict.TryGetValue(project.Id, out int count) ? count : 0;

                    return new ProjectDetailsWeb(
                        Id: project.Id,
                        TenantId: project.TenantId,
                        Name: project.Name,
                        IsActive: project.IsActive,
                        CreatedAt: project.CreatedAt,
                        CreatedByUserId: project.CreatedByUserId,
                        CreatedByUserName: $"{project.CreatedBy?.User?.FirstName} {project.CreatedBy?.User?.LastName}".Trim(),
                        UserRoleCode: projectMember.MemberRole?.Code ?? RoleCodes.ProjectMember,
                        MembersCount: membersCount
                    );
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }
    }
}
