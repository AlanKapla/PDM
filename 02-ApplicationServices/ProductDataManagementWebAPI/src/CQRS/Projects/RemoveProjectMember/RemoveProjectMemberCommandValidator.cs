using Business.Interfaces.Model;
using FluentValidation;

namespace CQRS.Projects.RemoveProjectMember
{
    public class RemoveProjectMemberCommandValidator : AbstractValidator<RemoveProjectMemberCommand>
    {
        private readonly ICurrentUser currentUser;

        public RemoveProjectMemberCommandValidator(ICurrentUser currentUser)
        {
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");

            RuleFor(x => x.UserId)
                .Must(userId => userId != currentUser.Id)
                .WithMessage("Cannot remove yourself from the project");
        }
    }
}
