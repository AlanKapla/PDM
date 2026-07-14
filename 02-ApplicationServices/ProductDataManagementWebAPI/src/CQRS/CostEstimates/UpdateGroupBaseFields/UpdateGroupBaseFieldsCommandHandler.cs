using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Context;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CQRS.CostEstimates.UpdateGroupBaseFields
{
    public sealed class UpdateGroupBaseFieldsCommandHandler
        : IRequestHandler<UpdateGroupBaseFieldsCommand, Unit>
    {
        private readonly AppDbContext context;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ILogger<UpdateGroupBaseFieldsCommandHandler> logger;

        public UpdateGroupBaseFieldsCommandHandler(
            AppDbContext context,
            ICostEstimateCacheService cacheService,
            ILogger<UpdateGroupBaseFieldsCommandHandler> logger)
        {
            this.context = context;
            this.cacheService = cacheService;
            this.logger = logger;
        }

        public async Task<Unit> Handle(
            UpdateGroupBaseFieldsCommand request,
            CancellationToken cancellationToken)
        {
            CostEstimateGroup group = await context.CostEstimateGroups
                .FirstOrDefaultAsync(
                    g => g.Id == request.GroupId
                        && g.CostEstimateId == request.CostEstimateId,
                    cancellationToken)
                ?? throw new NotFoundApiException(
                    nameof(CostEstimateGroup),
                    request.GroupId.ToString());

            if (request.ClearName)
            {
                group.Name = string.Empty;
            }
            else if (request.Name is not null)
            {
                group.Name = request.Name;
            }

            group.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            await cacheService.InvalidateGroupsAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            logger.LogInformation(
                "Updated base fields for group {GroupId} in cost estimate {CostEstimateId}",
                request.GroupId,
                request.CostEstimateId);

            return Unit.Value;
        }
    }
}
