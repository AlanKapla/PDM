using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models.Tenants;
using Entities.Models.Users;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetTenantDetails
{
    public sealed class GetTenantDetailsQueryHandler : IRequestHandler<GetTenantDetailsQuery, TenantDetailsWeb>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IReadRepository<TenantInvitation> invitationRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly ICurrentUser currentUser;

        public GetTenantDetailsQueryHandler(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IReadRepository<TenantInvitation> invitationRepo,
            IReadRepository<User> userRepo,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.invitationRepo = invitationRepo;
            this.userRepo = userRepo;
            this.currentUser = currentUser;
        }

        public async Task<TenantDetailsWeb> Handle(GetTenantDetailsQuery request, CancellationToken cancellationToken)
        {
            Tenant tenant = await tenantRepo.GetFirstBySearch(
                t => t.Id == request.TenantId)
                ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            IEnumerable<TenantMember> members = await tenantMemberRepo.GetBySearch(
                tm => tm.TenantId == request.TenantId && tm.IsActive);

            TenantMember? currentUserMembership = members
                .FirstOrDefault(m => m.UserId == currentUser.Id);

            List<Guid> memberUserIds = members.Select(m => m.UserId).ToList();
            IEnumerable<User> users = await userRepo.GetBySearch(u => memberUserIds.Contains(u.Id));
            Dictionary<Guid, User> userDict = users.ToDictionary(u => u.Id);

            IEnumerable<TenantInvitation> invitations = await invitationRepo.GetBySearch(
                i => i.TenantId == request.TenantId
                     && i.IsActive
                     && i.Status == InvitationStatus.Pending
                     && i.ExpiresAt > DateTime.UtcNow);

            List<Guid> inviterUserIds = invitations.Select(i => i.InvitedByUserId).ToList();
            IEnumerable<User> inviterUsers = await userRepo.GetBySearch(u => inviterUserIds.Contains(u.Id));
            Dictionary<Guid, User> inviterDict = inviterUsers.ToDictionary(u => u.Id);

            List<TenantMemberWeb> memberDtos = members
                .Select(m =>
                {
                    userDict.TryGetValue(m.UserId, out User? user);

                    return new TenantMemberWeb(
                        UserId: m.UserId,
                        Email: user?.Email ?? string.Empty,
                        FirstName: user?.FirstName ?? string.Empty,
                        LastName: user?.LastName ?? string.Empty,
                        IsAdmin: m.IsAdmin,
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
                    inviterDict.TryGetValue(i.InvitedByUserId, out User? inviter);

                    return new TenantInvitationWeb
                    {
                        InvitationId = i.Id,
                        TenantId = i.TenantId,
                        TenantName = tenant.Name,
                        Email = i.Email,
                        InvitedByUserEmail = inviter?.Email ?? string.Empty,
                        InvitedByUserName = inviter is null
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

            return new TenantDetailsWeb
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt,
                IsActive = tenant.IsActive,
                IsAdmin = currentUserMembership?.IsAdmin ?? false,
                Members = memberDtos,
                Invitations = invitationDtos
            };
        }
    }
}
