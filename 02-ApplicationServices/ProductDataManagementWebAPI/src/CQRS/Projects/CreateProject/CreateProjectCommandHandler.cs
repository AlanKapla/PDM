using Business.Implementation.Services;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.CreateProject
{
    public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDetailsWeb>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly PermissionsVersionService permissionsVersionService;
        private readonly ICurrentUser currentUser;

        public CreateProjectCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IReadRepository<Role> roleRepo,
            PermissionsVersionService permissionsVersionService,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.roleRepo = roleRepo;
            this.permissionsVersionService = permissionsVersionService;
            this.currentUser = currentUser;
        }

        public async Task<ProjectDetailsWeb> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = currentUser.ActiveTenantId!.Value;

            TenantMember tenantMember = (await tenantMemberRepo.GetFirstBySearch(
                tm => tm.TenantId == tenantId && tm.UserId == currentUser.Id,
                include => include.Include(tm => tm.User)))!;

            Project project = new Project
            {
                TenantId = tenantId,
                Name = request.Name,
                CreatedByUserId = currentUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await projectRepo.Insert(project);

            // Get PROJECT.ADMIN role
            var adminRole = await roleRepo.GetFirstBySearch(
                r => r.Scope == RoleScope.Project && r.Code == RoleCodes.ProjectAdmin,
                cancellationToken);

            if (adminRole == null)
                throw new InvalidOperationException("PROJECT.ADMIN role not found");

            ProjectMember projectMember = new ProjectMember
            {
                TenantId = tenantId,
                ProjectId = project.Id,
                UserId = currentUser.Id,
                RoleId = adminRole.Id,
                JoinedAt = DateTime.UtcNow
            };

            await projectMemberRepo.Insert(projectMember);

            // Bump permissions version
            await permissionsVersionService.BumpVersionAsync(currentUser.Id, cancellationToken);

            string createdByUserName = $"{tenantMember!.User?.FirstName} {tenantMember.User?.LastName}".Trim();

            return new ProjectDetailsWeb(
                Id: project.Id,
                TenantId: project.TenantId,
                Name: project.Name,
                IsActive: project.IsActive,
                CreatedAt: project.CreatedAt,
                CreatedByUserId: project.CreatedByUserId,
                CreatedByUserName: createdByUserName,
                UserRoleCode: RoleCodes.ProjectAdmin,
                MembersCount: 1
            );
        }
    }
}
