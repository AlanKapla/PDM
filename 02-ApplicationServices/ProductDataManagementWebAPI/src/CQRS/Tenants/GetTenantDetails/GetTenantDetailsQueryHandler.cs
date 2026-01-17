using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetTenantDetails
{
    public class GetTenantDetailsQueryHandler : IRequestHandler<GetTenantDetailsQuery, TenantDetailsWeb>
    {
        private readonly IRepository<Tenant> tenantRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly ICurrentUser currentUser;

        public GetTenantDetailsQueryHandler(
            IRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<TenantInvitation> invitationRepo,
            IReadRepository<User> userRepo,
            IReadRepository<Role> roleRepo,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.invitationRepo = invitationRepo;
            this.userRepo = userRepo;
            this.roleRepo = roleRepo;
            this.currentUser = currentUser;
        }

        public async Task<TenantDetailsWeb> Handle(GetTenantDetailsQuery request, CancellationToken cancellationToken)
        {
            Tenant? tenant = await tenantRepo.GetFirstBySearch(
                t => t.Id == request.TenantId
            ) ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            IEnumerable<TenantMember> members = await tenantMemberRepo.GetBySearch(
                tm => tm.TenantId == request.TenantId && tm.IsActive);

            TenantMember? currentUserMembership = members
                .FirstOrDefault(m => m.UserId == currentUser.Id);

            var memberUserIds = members.Select(m => m.UserId).ToList();
            var users = await userRepo.GetBySearch(u => memberUserIds.Contains(u.Id));
            var userDict = users.ToDictionary(u => u.Id);

            var memberRoleIds = members.Where(m => m.RoleId.HasValue).Select(m => m.RoleId!.Value).Distinct().ToList();
            var roles = await roleRepo.GetBySearch(r => memberRoleIds.Contains(r.Id));
            var roleDict = roles.ToDictionary(r => r.Id);

            IEnumerable<TenantInvitation> invitations = await invitationRepo.GetBySearch(
                i => i.TenantId == request.TenantId
                     && i.IsActive
                     && i.Status == InvitationStatus.Pending
                     && i.ExpiresAt > DateTime.UtcNow);

            var inviterUserIds = invitations.Select(i => i.InvitedByUserId).ToList();
            var inviterUsers = await userRepo.GetBySearch(u => inviterUserIds.Contains(u.Id));
            var inviterDict = inviterUsers.ToDictionary(u => u.Id);

            List<TenantMemberWeb> memberDtos = members
                .Select(m =>
                {
                    userDict.TryGetValue(m.UserId, out var user);
                    
                    string roleCode = RoleCodes.TenantMember;
                    if (m.RoleId.HasValue && roleDict.TryGetValue(m.RoleId.Value, out var role))
                    {
                        roleCode = role.Code;
                    }

                    return new TenantMemberWeb(
                        UserId: m.UserId,
                        Email: user?.Email ?? string.Empty,
                        FirstName: user?.FirstName ?? string.Empty,
                        LastName: user?.LastName ?? string.Empty,
                        RoleCode: roleCode,
                        IsActive: m.IsActive,
                        JoinedAt: m.CreatedAt
                    );
                })
                .OrderBy(m => m.LastName)
                .ThenBy(m => m.FirstName)
                .ToList();

            List<TenantInvitationWeb> invitationDtos = invitations
                .Select(i =>
                {
                    inviterDict.TryGetValue(i.InvitedByUserId, out var inviter);

                    return new TenantInvitationWeb
                    {
                        InvitationId = i.Id,
                        TenantId = i.TenantId,
                        TenantName = tenant.Name,
                        Email = i.Email,
                        InvitedByUserEmail = inviter?.Email ?? string.Empty,
                        InvitedByUserName = inviter == null 
                            ? string.Empty 
                            : $"{inviter.FirstName} {inviter.LastName}",
                        CreatedAt = i.CreatedAt,
                        ExpiresAt = i.ExpiresAt,
                        Status = i.Status,
                        Token = string.Empty
                    };
                })
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            string currentUserRoleCode = RoleCodes.TenantMember;
            
            if (currentUserMembership != null && currentUserMembership.RoleId.HasValue 
                && roleDict.TryGetValue(currentUserMembership.RoleId.Value, out var currentRole))
            {
                currentUserRoleCode = currentRole.Code;
            }

            return new TenantDetailsWeb
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt,
                IsActive = tenant.IsActive,
                RoleCode = currentUserRoleCode,
                Members = memberDtos,
                Invitations = invitationDtos
            };
        }
    }
}
