using Business.Implementation.Helpers;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpdateItemBaseFields
{
    public sealed class UpdateItemBaseFieldsCommandHandler
        : IRequestHandler<UpdateItemBaseFieldsCommand, Unit>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly ICostEstimateRecalculationService recalculationService;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ILogger<UpdateItemBaseFieldsCommandHandler> logger;

        public UpdateItemBaseFieldsCommandHandler(
            IRepository<CostEstimateItem> itemRepository,
            ICostEstimateRecalculationService recalculationService,
            ICostEstimateCacheService cacheService,
            ILogger<UpdateItemBaseFieldsCommandHandler> logger)
        {
            this.itemRepository = itemRepository;
            this.recalculationService = recalculationService;
            this.cacheService = cacheService;
            this.logger = logger;
        }

        public async Task<Unit> Handle(
            UpdateItemBaseFieldsCommand request,
            CancellationToken cancellationToken)
        {
            CostEstimateItem item = await itemRepository.GetFirstBySearch(
                i => i.Id == request.ItemId && i.CostEstimateId == request.CostEstimateId)
                ?? throw new NotFoundApiException(
                    nameof(CostEstimateItem),
                    request.ItemId.ToString());

            bool hasSelectedOption = (await itemRepository.GetBySearch(
                i => i.ParentItemId == request.ItemId
                    && i.RelationType == ItemRelationType.Option
                    && i.IsSelected
                    && !i.IsDeleted)).Any();

            if (hasSelectedOption)
            {
                bool isChangingAnyContentField = request.Name is not null || request.ClearName
                    || request.Quantity.HasValue || request.ClearQuantity
                    || request.Unit is not null || request.ClearUnit
                    || request.UnitPriceNet.HasValue || request.ClearUnitPriceNet
                    || request.VatRate.HasValue || request.ClearVatRate
                    || request.NetValue.HasValue || request.ClearNetValue
                    || request.GrossValue.HasValue || request.ClearGrossValue
                    || request.VatValue.HasValue || request.ClearVatValue
                    || request.UnitPriceGross.HasValue || request.ClearUnitPriceGross;

                if (isChangingAnyContentField)
                {
                    throw new ValidationApiException(
                        "Cannot edit item fields because a selected option exists. Deselect the option first.");
                }
            }

            bool hasComponents = (await itemRepository.GetBySearch(
                i => i.ParentItemId == request.ItemId
                    && i.RelationType == ItemRelationType.Component
                    && !i.IsDeleted)).Any();

            if (hasComponents)
            {
                bool isChangingRestrictedField = request.Quantity.HasValue || request.ClearQuantity
                    || request.Unit is not null || request.ClearUnit
                    || request.UnitPriceNet.HasValue || request.ClearUnitPriceNet
                    || request.VatRate.HasValue || request.ClearVatRate
                    || request.NetValue.HasValue || request.ClearNetValue
                    || request.GrossValue.HasValue || request.ClearGrossValue
                    || request.VatValue.HasValue || request.ClearVatValue
                    || request.UnitPriceGross.HasValue || request.ClearUnitPriceGross;

                if (isChangingRestrictedField)
                {
                    throw new ValidationApiException(
                        "Cannot edit financial fields because components exist. Values are calculated from component sums.");
                }
            }

            bool financialFieldChanged = false;

            if (request.ClearName)
            {
                item.Name = string.Empty;
            }
            else if (request.Name is not null)
            {
                item.Name = request.Name;
            }

            if (request.ClearQuantity)
            {
                item.Quantity = null;
                item.NetValue = null;
                item.VatValue = null;
                item.GrossValue = null;
                item.UnitPriceGross = null;
                financialFieldChanged = true;
            }
            else if (request.Quantity.HasValue)
            {
                item.Quantity = request.Quantity.Value;
                financialFieldChanged = true;
            }

            if (request.ClearUnit)
            {
                item.Unit = null;
            }
            else if (request.Unit is not null)
            {
                item.Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit;
            }

            if (request.ClearUnitPriceNet)
            {
                item.UnitPriceNet = null;
                item.NetValue = null;
                item.VatValue = null;
                item.GrossValue = null;
                item.UnitPriceGross = null;
                financialFieldChanged = true;
            }
            else if (request.UnitPriceNet.HasValue)
            {
                item.UnitPriceNet = request.UnitPriceNet.Value;
                financialFieldChanged = true;
            }

            if (request.ClearVatRate)
            {
                item.VatRate = null;
                item.VatValue = null;
                item.GrossValue = null;
                item.UnitPriceGross = null;
                financialFieldChanged = true;
            }
            else if (request.VatRate.HasValue)
            {
                item.VatRate = request.VatRate.Value;
                financialFieldChanged = true;
            }

            ValidateComputedFieldEdit(item, request);

            if (request.ClearNetValue)
            {
                item.NetValue = null;
                item.VatValue = null;
                item.GrossValue = null;
                item.UnitPriceGross = null;
                financialFieldChanged = true;
            }
            else if (request.NetValue.HasValue)
            {
                item.NetValue = request.NetValue.Value;
                financialFieldChanged = true;
            }

            if (request.ClearVatValue)
            {
                item.VatValue = null;
                item.GrossValue = null;
                item.UnitPriceGross = null;
                financialFieldChanged = true;
            }
            else if (request.VatValue.HasValue)
            {
                item.VatValue = request.VatValue.Value;
                financialFieldChanged = true;
            }

            if (request.ClearGrossValue)
            {
                item.GrossValue = null;
                item.UnitPriceGross = null;
                financialFieldChanged = true;
            }
            else if (request.GrossValue.HasValue)
            {
                item.GrossValue = request.GrossValue.Value;
                financialFieldChanged = true;
            }

            if (request.ClearUnitPriceGross)
            {
                item.UnitPriceGross = null;
                financialFieldChanged = true;
            }
            else if (request.UnitPriceGross.HasValue)
            {
                item.UnitPriceGross = request.UnitPriceGross.Value;
                financialFieldChanged = true;
            }

            if (request.IsSelected.HasValue)
            {
                item.IsSelected = request.IsSelected.Value;
            }

            if (request.IsStageWork.HasValue)
            {
                item.IsStageWork = request.IsStageWork.Value;
            }

            item.UpdatedAt = DateTime.UtcNow;

            await itemRepository.Update(item);
            await itemRepository.SaveChangesAsync(cancellationToken);

            await cacheService.InvalidateItemsAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (financialFieldChanged)
            {
                await recalculationService.RecalculateAsync(
                    request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);
            }

            logger.LogInformation(
                "Updated base fields for item {ItemId} in cost estimate {CostEstimateId}",
                request.ItemId,
                request.CostEstimateId);

            return Unit.Value;
        }

        private static void ValidateComputedFieldEdit(
            CostEstimateItem item,
            UpdateItemBaseFieldsCommand request)
        {
            decimal? effectiveNet = CostEstimateItemFinancialCalculator.CalculateValueNet(
                item.UnitPriceNet,
                item.Quantity,
                item.NetValue);
            decimal? effectiveVat = CostEstimateItemFinancialCalculator.CalculateTotalVat(
                effectiveNet,
                item.VatRate,
                item.VatValue);
            decimal? effectiveUnitPriceGross = CostEstimateItemFinancialCalculator.CalculateUnitPriceGross(
                item.UnitPriceNet,
                item.VatRate,
                item.GrossValue,
                item.Quantity,
                item.UnitPriceGross);

            if ((request.NetValue.HasValue || request.ClearNetValue)
                && CostEstimateItemFinancialCalculator.IsNetValueComputed(item.UnitPriceNet, item.Quantity))
            {
                throw new ValidationApiException(
                    "Net value is calculated from unit price and quantity and cannot be edited.");
            }

            if ((request.VatValue.HasValue || request.ClearVatValue)
                && CostEstimateItemFinancialCalculator.IsVatValueComputed(effectiveNet, item.VatRate))
            {
                throw new ValidationApiException(
                    "VAT value is calculated from net value and VAT rate and cannot be edited.");
            }

            if ((request.GrossValue.HasValue || request.ClearGrossValue)
                && CostEstimateItemFinancialCalculator.IsGrossValueComputed(
                    effectiveNet,
                    effectiveVat,
                    item.VatRate,
                    effectiveUnitPriceGross,
                    item.Quantity))
            {
                throw new ValidationApiException(
                    "Gross value is calculated from net value and VAT and cannot be edited.");
            }

            if ((request.UnitPriceGross.HasValue || request.ClearUnitPriceGross)
                && item.VatRate.HasValue)
            {
                throw new ValidationApiException(
                    "Unit gross price is calculated automatically when VAT rate is set and cannot be edited.");
            }
        }
    }
}
