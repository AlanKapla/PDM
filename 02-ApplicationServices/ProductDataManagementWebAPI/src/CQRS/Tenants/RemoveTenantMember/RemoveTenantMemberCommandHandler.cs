using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.RemoveTenantMember
{
    public class RemoveTenantMemberCommandHandler : IRequestHandler<RemoveTenantMemberCommand, Unit>
    {
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public RemoveTenantMemberCommandHandler(IRepository<TenantMember> tenantMemberRepo, ICurrentUser currentUser)
        {
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(RemoveTenantMemberCommand request, CancellationToken cancellationToken)
        {
            TenantMember? membership = await tenantMemberRepo.GetFirstBySearch(m => m.TenantId == request.TenantId && m.UserId == request.UserId && m.IsActive)
                ?? throw new NotFoundApiException("TenantMember", request.UserId.ToString());

            membership.IsActive = false;
            await tenantMemberRepo.Update(membership);

            return Unit.Value;
        }
    }
}