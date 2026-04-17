using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.GetMyWorkSchedules
{
    public sealed class GetMyWorkSchedulesQueryHandler : IRequestHandler<GetMyWorkSchedulesQuery, List<MyWorkSchedulesTenantDto>>
    {
        private readonly IRepository<Project> projectRepository;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepository;
        private readonly ICurrentUser currentUser;

        public GetMyWorkSchedulesQueryHandler(
            IRepository<Project> projectRepository,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepository,
            ICurrentUser currentUser)
        {
            this.projectRepository = projectRepository;
            this.assignmentRepository = assignmentRepository;
            this.currentUser = currentUser;
        }

        public async Task<List<MyWorkSchedulesTenantDto>> Handle(
            GetMyWorkSchedulesQuery request,
            CancellationToken cancellationToken)
        {
            Project project = await projectRepository.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                q => q.Include(p => p.Tenant))
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            IEnumerable<WorkScheduleStageWorkAssignment> assignments = await assignmentRepository.GetBySearch(
                a => a.UserId == currentUser.Id
                  && a.TenantId == request.TenantId
                  && a.ProjectId == request.ProjectId,
                q => q.Include(a => a.Work)
                      .ThenInclude(w => w.Stage)
                      .ThenInclude(s => s.WorkSchedule));

            List<MyWorkSchedulesItemDto> workSchedules = assignments
                .Where(a => !a.Work.Stage.WorkSchedule.IsDeleted)
                .Select(a => a.Work.Stage.WorkSchedule)
                .DistinctBy(ws => ws.Id)
                .Select(ws => new MyWorkSchedulesItemDto(ws.Id, ws.Name))
                .ToList();

            if (workSchedules.Count == 0)
            {
                return new List<MyWorkSchedulesTenantDto>();
            }

            MyWorkSchedulesTenantDto tenantDto = new MyWorkSchedulesTenantDto(
                project.TenantId,
                project.Tenant.Name,
                new List<MyWorkSchedulesProjectDto>
                {
                    new MyWorkSchedulesProjectDto(project.Id, project.Name, workSchedules)
                });

            return new List<MyWorkSchedulesTenantDto> { tenantDto };
        }
    }
}
