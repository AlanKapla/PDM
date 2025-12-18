using Business.Interfaces.Model;
using FluentValidation;

namespace CQRS.WorkSchedules.GetUserWorkSchedules
{
    public class GetUserWorkSchedulesQueryValidator : AbstractValidator<GetUserWorkSchedulesQuery>
    {
        public GetUserWorkSchedulesQueryValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");
        }
    }
}
