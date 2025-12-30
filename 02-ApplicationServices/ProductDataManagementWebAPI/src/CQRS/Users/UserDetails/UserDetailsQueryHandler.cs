using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Users;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Users.UserDetails
{
    public class UserDetailsQueryHandler : IRequestHandler<UserDetailsQuery, UserDetailsWeb>
    {
        private readonly ICurrentUser currentUser;
        private readonly IRepository<ProjectMember> projectMemberRepo;

        public UserDetailsQueryHandler(
            ICurrentUser currentUser,
            IRepository<ProjectMember> projectMemberRepo)
        {
            this.currentUser = currentUser;
            this.projectMemberRepo = projectMemberRepo;
        }

        public async Task<UserDetailsWeb> Handle(UserDetailsQuery request, CancellationToken cancellationToken)
        {
            var projectRoles = new Dictionary<Guid, ProjectRole>();

            if (currentUser.IsAuthenticated && currentUser.ActiveTenantId.HasValue)
            {
                var projectMemberships = await projectMemberRepo.GetBySearch(
                    pm => pm.UserId == currentUser.Id && 
                          pm.TenantId == currentUser.ActiveTenantId.Value &&
                          pm.Project.IsActive,
                    include => include.Include(pm => pm.Project));

                projectRoles = projectMemberships.ToDictionary(pm => pm.ProjectId, pm => pm.Role);
            }

            return new UserDetailsWeb(
                currentUser.Id, 
                currentUser.FirstName, 
                currentUser.LastName, 
                currentUser.Email, 
                currentUser.ActiveTenantId,
                projectRoles);
        }
    }
}
