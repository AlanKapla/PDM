using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.GetUserWorkSchedules
{
    public class GetUserWorkSchedulesQueryHandler : IRequestHandler<GetUserWorkSchedulesQuery, List<WorkScheduleSummaryWeb>>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetUserWorkSchedulesQueryHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<List<WorkScheduleSummaryWeb>> Handle(GetUserWorkSchedulesQuery request, CancellationToken cancellationToken)
        {
            var workSchedules = await workScheduleRepo.GetBySearch(
                ws => ws.ProjectId == request.ProjectId &&
                      ws.TenantId == request.TenantId &&
                      ws.CreatedByUserId == currentUser.Id,
                include => include
                    .Include(ws => ws.CreatedBy)
                        .ThenInclude(tm => tm.User));

            var result = workSchedules
                .OrderByDescending(ws => ws.CreatedAt)
                .Select(ws => new WorkScheduleSummaryWeb(
                    Id: ws.Id,
                    Name: ws.Name,
                    CreatedAt: ws.CreatedAt,
                    CreatedByUserId: ws.CreatedByUserId,
                    CreatedByUserName: $"{ws.CreatedBy.User.FirstName} {ws.CreatedBy.User.LastName}".Trim()
                ))
                .ToList();

            return result;
        }
    }
}
