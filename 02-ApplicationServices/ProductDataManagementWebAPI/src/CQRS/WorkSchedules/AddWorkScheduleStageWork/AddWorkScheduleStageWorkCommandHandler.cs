using Business.Interfaces.Exceptions;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.AddWorkScheduleStageWork
{
    public sealed class AddWorkScheduleStageWorkCommandHandler : IRequestHandler<AddWorkScheduleStageWorkCommand, Guid>
    {
        private readonly IRepository<WorkScheduleStage> stageRepository;
        private readonly IRepository<WorkScheduleStageWork> workRepository;

        public AddWorkScheduleStageWorkCommandHandler(
            IRepository<WorkScheduleStage> stageRepository,
            IRepository<WorkScheduleStageWork> workRepository)
        {
            this.stageRepository = stageRepository;
            this.workRepository = workRepository;
        }

        public async Task<Guid> Handle(AddWorkScheduleStageWorkCommand request, CancellationToken cancellationToken)
        {
            bool stageExists = await stageRepository.AnyAsync(
                s => s.Id == request.WorkScheduleStageId
                  && s.WorkScheduleId == request.WorkScheduleId
                  && s.TenantId == request.TenantId
                  && !s.IsDeleted,
                cancellationToken);

            if (!stageExists)
            {
                throw new NotFoundApiException(nameof(WorkScheduleStage), request.WorkScheduleStageId.ToString());
            }

            WorkScheduleStageWork work = new WorkScheduleStageWork
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                WorkScheduleStageId = request.WorkScheduleStageId,
                CostEstimateItemId = request.CostEstimateItemId,
                Name = request.Name,
                Order = request.Order,
                ColorRgb = request.ColorRgb
            };

            await workRepository.Insert(work);
            await workRepository.SaveChangesAsync(cancellationToken);
            return work.Id;
        }
    }
}
