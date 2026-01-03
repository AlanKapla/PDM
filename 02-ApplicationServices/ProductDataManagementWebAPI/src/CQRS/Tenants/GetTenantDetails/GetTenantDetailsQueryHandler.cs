using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Extensions;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetTenantDetails
{
    public class GetTenantDetailsQueryHandler : IRequestHandler<GetTenantDetailsQuery, TenantDetailsWeb>
    {
        private readonly IRepository<Tenant> tenantRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly ICurrentUser currentUser;

        public GetTenantDetailsQueryHandler(
            IRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<TenantInvitation> invitationRepo,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.invitationRepo = invitationRepo;
            this.currentUser = currentUser;
        }

        public async Task<TenantDetailsWeb> Handle(GetTenantDetailsQuery request, CancellationToken cancellationToken)
        {
            Tenant? tenant = await tenantRepo.GetFirstBySearch(
                t => t.Id == request.TenantId
            ) ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            // Jedno zapytanie pobierające wszystkich członków tenanta
            IEnumerable<TenantMember> members = await tenantMemberRepo.GetBySearch(
                tm => tm.TenantId == request.TenantId && tm.IsActive,
                q => q.Include(tm => tm.User).Include(tm => tm.MemberRole)
            );

            // Sprawdzenie czy current user jest adminem na pobranej kolekcji
            TenantMember? currentUserMembership = members
                .FirstOrDefault(m => m.UserId == currentUser.Id);

            if (currentUserMembership == null || !currentUserMembership.MemberRole!.Code.IsTenantAdmin())
                throw new ForbiddenApiException("Only tenant admins can view tenant details");

            IEnumerable<TenantInvitation> invitations = await invitationRepo.GetBySearch(
                i => i.TenantId == request.TenantId
                     && i.IsActive
                     && i.Status == InvitationStatus.Pending
                     && i.ExpiresAt > DateTime.UtcNow,
                q => q.Include(i => i.InvitedByUser)
            );

            List<TenantMemberWeb> memberDtos = members
                .Select(m => new TenantMemberWeb(
                    UserId: m.UserId,
                    Email: m.User.Email,
                    FirstName: m.User.FirstName,
                    LastName: m.User.LastName,
                    RoleCode: m.MemberRole?.Code ?? RoleCodes.TenantMember,
                    IsActive: m.IsActive,
                    JoinedAt: m.CreatedAt
                ))
                .OrderBy(m => m.LastName)
                .ThenBy(m => m.FirstName)
                .ToList();

            List<TenantInvitationWeb> invitationDtos = invitations
                .Select(i => new TenantInvitationWeb
                {
                    InvitationId = i.Id,
                    TenantId = i.TenantId,
                    TenantName = tenant.Name,
                    Email = i.Email,
                    InvitedByUserEmail = i.InvitedByUser?.Email ?? string.Empty,
                    InvitedByUserName = i.InvitedByUser == null 
                        ? string.Empty 
                        : $"{i.InvitedByUser.FirstName} {i.InvitedByUser.LastName}",
                    CreatedAt = i.CreatedAt,
                    ExpiresAt = i.ExpiresAt,
                    Status = i.Status,
                    Token = string.Empty
                })
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            return new TenantDetailsWeb
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt,
                IsActive = tenant.IsActive,
                RoleCode = currentUserMembership.MemberRole?.Code ?? RoleCodes.TenantMember,
                Members = memberDtos,
                Invitations = invitationDtos
            };
        }
    }
}
