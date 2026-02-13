using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;
using CQRS.WorkSchedules.Shared;
using System.Linq.Expressions;

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public class UpdateWorkScheduleCommandValidator : WorkScheduleCommandValidatorBase<UpdateWorkScheduleCommand>
    {
        public UpdateWorkScheduleCommandValidator(IRepository<ProjectMember> projectMemberRepo)
            : base(projectMemberRepo)
        {
            // Additional validation specific to Update command
            RuleFor(x => x.WorkScheduleId)
                .NotEmpty().WithMessage("WorkScheduleId is required");
        }

        protected override Expression<Func<UpdateWorkScheduleCommand, string>> GetNameSelector()
        {
            return cmd => cmd.Name;
        }

        protected override Func<UpdateWorkScheduleCommand, Guid> GetTenantIdSelector()
        {
            return cmd => cmd.TenantId;
        }

        protected override Func<UpdateWorkScheduleCommand, Guid> GetProjectIdSelector()
        {
            return cmd => cmd.ProjectId;
        }

        protected override Expression<Func<UpdateWorkScheduleCommand, IEnumerable<WorkScheduleStageDto>?>> GetStagesSelector()
        {
            return cmd => cmd.Stages;
        }

        protected override Func<UpdateWorkScheduleCommand, IEnumerable<WorkScheduleStageDto>?> GetStagesSelectorFunc()
        {
            return cmd => cmd.Stages;
        }
    }
}
