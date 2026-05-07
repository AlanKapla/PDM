using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetTenantDetails
{
    public class GetTenantDetailsQueryValidator : AbstractValidator<GetTenantDetailsQuery>
    {
        public GetTenantDetailsQueryValidator(
            ICurrentUser currentUser,
            IRepository<TenantMember> tenantMemberRepo)
        {
            RuleFor(x => x.TenantId)
                .NotEqual(Guid.Empty)
                .WithMessage("Invalid tenant ID");
        }
    }
}
