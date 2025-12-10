using Business.Interfaces.Model;
using FluentValidation;

namespace CQRS.WorkSchedules.GetUserAssignedWorks
{
    public class GetUserAssignedWorksQueryValidator : AbstractValidator<GetUserAssignedWorksQuery>
    {
        private readonly ICurrentUser currentUser;

        public GetUserAssignedWorksQueryValidator(ICurrentUser currentUser)
        {
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");
        }
    }
}
