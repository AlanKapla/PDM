using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.SetItemIsSelected
{
    public sealed class SetItemIsSelectedCommandHandler
        : IRequestHandler<SetItemIsSelectedCommand, Unit>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly ICostEstimateRecalculationService recalculationService;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ILogger<SetItemIsSelectedCommandHandler> logger;

        public SetItemIsSelectedCommandHandler(
            IRepository<CostEstimateItem> itemRepository,
            ICostEstimateRecalculationService recalculationService,
            ICostEstimateCacheService cacheService,
            ILogger<SetItemIsSelectedCommandHandler> logger)
        {
            this.itemRepository = itemRepository;
            this.recalculationService = recalculationService;
            this.cacheService = cacheService;
            this.logger = logger;
        }

        public async Task<Unit> Handle(
            SetItemIsSelectedCommand request,
            CancellationToken cancellationToken)
        {
            CostEstimateItem item = await itemRepository.GetFirstBySearch(
                i => i.Id == request.ItemId && i.CostEstimateId == request.CostEstimateId)
                ?? throw new NotFoundApiException(
                    nameof(CostEstimateItem),
                    request.ItemId.ToString());

            switch (item.RelationType)
            {
                case ItemRelationType.None:
                case ItemRelationType.Component:
                    await HandleNoneOrComponentAsync(item, request, cancellationToken);
                    break;

                case ItemRelationType.Option:
                    await HandleOptionAsync(item, request, cancellationToken);
                    break;
            }

            item.UpdatedAt = DateTime.UtcNow;

            await itemRepository.SaveChangesAsync(cancellationToken);

            await cacheService.InvalidateItemsAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            await recalculationService.RecalculateAsync(
                request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            logger.LogInformation(
                "Set IsSelected={IsSelected} for item {ItemId} (RelationType={RelationType}) in cost estimate {CostEstimateId}",
                request.IsSelected,
                request.ItemId,
                item.RelationType,
                request.CostEstimateId);

            return Unit.Value;
        }

        private static Task HandleNoneOrComponentAsync(
            CostEstimateItem item,
            SetItemIsSelectedCommand request,
            CancellationToken cancellationToken)
        {
            item.IsSelected = request.IsSelected;
            return Task.CompletedTask;
        }

        private async Task HandleOptionAsync(
            CostEstimateItem item,
            SetItemIsSelectedCommand request,
            CancellationToken cancellationToken)
        {
            if (request.IsSelected)
            {
                await HandleOptionSelectedAsync(item, request, cancellationToken);
            }
            else
            {
                await HandleOptionDeselectedAsync(item, request, cancellationToken);
            }
        }

        private async Task HandleOptionSelectedAsync(
            CostEstimateItem item,
            SetItemIsSelectedCommand request,
            CancellationToken cancellationToken)
        {
            CostEstimateItem parentItem = await itemRepository.GetFirstBySearch(
                i => i.Id == item.ParentItemId && i.CostEstimateId == request.CostEstimateId)
                ?? throw new NotFoundApiException(
                    nameof(CostEstimateItem),
                    item.ParentItemId?.ToString() ?? "unknown");

            List<CostEstimateItem> allOptions = (await itemRepository.GetBySearch(
                i => i.ParentItemId == item.ParentItemId
                    && i.RelationType == ItemRelationType.Option
                    && i.Id != request.ItemId)).ToList();

            foreach (CostEstimateItem option in allOptions)
            {
                option.IsSelected = false;
            }

            item.IsSelected = true;

            // NIE nadpisuj parentItem.Quantity/Unit/UnitPriceNet/VatRate wartościami z opcji.
            // Pozycja nadrzędna zachowuje własne oryginalne wartości pól podstawowych.
            // Wartości kalkulowane (NetValue/GrossValue/VatValue) są obsługiwane
            // przez CostEstimateCalculationService.CalculateItemValues na poziomie odczytu.
        }

        private async Task HandleOptionDeselectedAsync(
            CostEstimateItem item,
            SetItemIsSelectedCommand request,
            CancellationToken cancellationToken)
        {
            item.IsSelected = false;

            // NIE zeruj parentItem.Quantity/Unit/UnitPriceNet/VatRate po odznaczeniu opcji.
            // Pozycja nadrzędna zachowuje własne oryginalne wartości pól podstawowych.
            // Wartości kalkulowane (NetValue/GrossValue/VatValue) są przeliczane
            // przez CostEstimateCalculationService z parentItem's własnych pól.
        }
    }
}
