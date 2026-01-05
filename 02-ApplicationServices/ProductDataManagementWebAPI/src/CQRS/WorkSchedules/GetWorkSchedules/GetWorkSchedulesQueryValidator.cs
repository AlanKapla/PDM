using FluentValidation;

namespace CQRS.WorkSchedules.GetWorkSchedules
{
    /// <summary>
    /// Validator for GetWorkSchedulesQuery
    /// </summary>
    public class GetWorkSchedulesQueryValidator : AbstractValidator<GetWorkSchedulesQuery>
    {
        public GetWorkSchedulesQueryValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("ProjectId is required");

            RuleFor(x => x.Scope)
                .IsInEnum()
                .WithMessage("Invalid ResourceScope value");
        }
    }
}
