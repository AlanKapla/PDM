using Business.Interfaces.WebModels.Admin;
using Entities.Models.Tenants;
using Entities.Models.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Users.GetAdminUsers;

public sealed class GetAdminUsersQueryHandler
    : IRequestHandler<GetAdminUsersQuery, IEnumerable<AdminUserListItemWeb>>
{
    private readonly IReadRepository<User> userRepo;

    public GetAdminUsersQueryHandler(IReadRepository<User> userRepo)
    {
        this.userRepo = userRepo;
    }

    public async Task<IEnumerable<AdminUserListItemWeb>> Handle(
        GetAdminUsersQuery request,
        CancellationToken cancellationToken)
    {
        IEnumerable<User> users = await userRepo.GetBySearch(
            u => true,
            q => q.Include(u => u.TenantMemberships));

        return users.Select(u => new AdminUserListItemWeb(
            u.Id,
            u.FirstName,
            u.LastName,
            u.Email,
            u.SystemRole.ToString(),
            u.IsActive,
            u.CreatedAt,
            u.TenantMemberships.Count
        ));
    }
}
