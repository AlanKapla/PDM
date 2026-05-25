using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Admin;
using Entities.Models.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Users.GetAdminUserDetails;

public sealed class GetAdminUserDetailsQueryHandler
    : IRequestHandler<GetAdminUserDetailsQuery, AdminUserDetailsWeb>
{
    private readonly IReadRepository<User> userRepo;

    public GetAdminUserDetailsQueryHandler(IReadRepository<User> userRepo)
    {
        this.userRepo = userRepo;
    }

    public async Task<AdminUserDetailsWeb> Handle(
        GetAdminUserDetailsQuery request,
        CancellationToken cancellationToken)
    {
        User? user = await userRepo.GetById(
            request.UserId,
            q => q.Include(u => u.TenantMemberships)
                  .ThenInclude(tm => tm.Tenant),
            q => q.Include(u => u.TenantMemberships)
                  .ThenInclude(tm => tm.MemberRole));

        if (user is null)
        {
            throw new NotFoundApiException(nameof(User), request.UserId.ToString());
        }

        IEnumerable<AdminUserTenantMembershipWeb> memberships = user.TenantMemberships
            .Select(tm => new AdminUserTenantMembershipWeb(
                tm.TenantId,
                tm.Tenant?.Name ?? string.Empty,
                tm.MemberRole?.Name ?? string.Empty,
                tm.CreatedAt));

        return new AdminUserDetailsWeb(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.SystemRole.ToString(),
            user.IsActive,
            user.CreatedAt,
            user.PhoneNumber,
            user.CompanyName,
            user.TaxId,
            user.Street,
            user.City,
            user.PostalCode,
            user.Country,
            memberships);
    }
}
