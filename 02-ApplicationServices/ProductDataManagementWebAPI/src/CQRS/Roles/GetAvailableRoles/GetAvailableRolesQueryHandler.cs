using Business.Interfaces.WebModels.Roles;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;

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
