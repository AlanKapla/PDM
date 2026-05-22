using Business.Interfaces.WebModels.Roles;
using Entities.Enums;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Roles.GetAvailableRoles
{
    public class GetAvailableRolesQueryHandler : IRequestHandler<GetAvailableRolesQuery, IEnumerable<RoleWeb>>
    {
        private readonly IReadRepository<Role> roleRepo;

        public GetAvailableRolesQueryHandler(IReadRepository<Role> roleRepo)
        {
            this.roleRepo = roleRepo;
        }

        public async Task<IEnumerable<RoleWeb>> Handle(GetAvailableRolesQuery request, CancellationToken cancellationToken)
        {
            // Get all active roles for the specified scope
            var roles = await roleRepo.GetBySearch(
                r => r.Scope == request.Scope && r.IsActive);

            var result = roles
                .Select(r => new RoleWeb(
                    Id: r.Id,
                    Code: r.Code,
                    Name: r.Name,
                    Description: r.Description,
                    Scope: r.Scope
                ))
                .OrderBy(r => r.Name)
                .ToList();

            return result;
        }
    }
}
