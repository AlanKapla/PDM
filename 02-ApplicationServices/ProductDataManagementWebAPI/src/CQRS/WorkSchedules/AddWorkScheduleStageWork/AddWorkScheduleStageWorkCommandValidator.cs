using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.AddWorkScheduleStageWork
{
    public sealed class AddWorkScheduleStageWorkCommandValidator : AbstractValidator<AddWorkScheduleStageWorkCommand>
    {
        public AddWorkScheduleStageWorkCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageId).RequiredId();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Order).NonNegativeOrder();
            RuleFor(x => x.ColorRgb).NotEmpty().ValidColorRgb();
        }
    }
}
