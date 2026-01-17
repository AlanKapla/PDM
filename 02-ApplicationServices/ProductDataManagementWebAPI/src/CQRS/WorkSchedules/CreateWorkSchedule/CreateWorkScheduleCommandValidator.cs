using CQRS.WorkSchedules.Shared;
using Entities.Models;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public class CreateWorkScheduleCommandValidator : WorkScheduleCommandValidatorBase<CreateWorkScheduleCommand>
    {
        public CreateWorkScheduleCommandValidator(IRepository<ProjectMember> projectMemberRepo)
            : base(projectMemberRepo)
        {
        }

        protected override Expression<Func<CreateWorkScheduleCommand, string>> GetNameSelector()
        {
            return cmd => cmd.Name;
        }

        protected override Func<CreateWorkScheduleCommand, Guid> GetTenantIdSelector()
        {
            return cmd => cmd.TenantId;
        }

        protected override Func<CreateWorkScheduleCommand, Guid> GetProjectIdSelector()
        {
            return cmd => cmd.ProjectId;
        }

        protected override Expression<Func<CreateWorkScheduleCommand, IEnumerable<WorkScheduleStageDto>?>> GetStagesSelector()
        {
            return cmd => cmd.Stages;
        }

        protected override Func<CreateWorkScheduleCommand, IEnumerable<WorkScheduleStageDto>?> GetStagesSelectorFunc()
        {
            return cmd => cmd.Stages;
        }
    }
}
