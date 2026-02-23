using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Business.Implementation.Helpers;

namespace CQRS.CostEstimates.GetCostEstimateDetails
{
    /// <summary>
    /// Handler dla pobrania szczegółów kosztorysu
    /// Returns cost estimate with full hierarchy of groups and work scope items + template structure
    /// </summary>
    public class GetCostEstimateDetailsQueryHandler : IRequestHandler<GetCostEstimateDetailsQuery, CostEstimateDetailsWeb>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly ICostEstimateTemplateService costEstimateTemplateService;
        private readonly ICurrentUser currentUser;

        public GetCostEstimateDetailsQueryHandler(
            IReadRepository<CostEstimate> costEstimateRepository,
            ICostEstimateTemplateService costEstimateTemplateService,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.costEstimateTemplateService = costEstimateTemplateService;
            this.currentUser = currentUser;
        }

        public async Task<CostEstimateDetailsWeb> Handle(GetCostEstimateDetailsQuery request, CancellationToken cancellationToken)
        {
            // Get cost estimate with all related data
            // UWAGA: Struktura zagnieżdżenia: Position (Level 1) → Component (Level 2) → Option (Level 3)
            var costEstimates = await costEstimateRepository.GetBySearch(
                c => c.Id == request.CostEstimateId && 
                     c.TenantId == request.TenantId &&
                     c.ProjectId == request.ProjectId &&
                     !c.IsDeleted,
                q => q.Include(c => c.Template)
                      .Include(c => c.Owner)
                      .Include(c => c.SelectedCurrency)
                      // Grupy + Group FieldValues
                      .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                          .ThenInclude(g => g.FieldValues)
                              .ThenInclude(fv => fv.FieldDefinition)
                      // Główne pozycje (Level 1: RelationType = None) + ich FieldValues
                      .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                          .ThenInclude(g => g.Items.Where(w => !w.IsDeleted && w.RelationType == ItemRelationType.None))
                              .ThenInclude(w => w.FieldValues)
                                  .ThenInclude(fv => fv.FieldDefinition)
                      // Child items Level 2 (Components/Options z ParentItemId != null) + ich FieldValues
                      .Include(c => c.AllItems.Where(i => !i.IsDeleted && i.ParentItemId != null))
                          .ThenInclude(i => i.FieldValues)
                              .ThenInclude(fv => fv.FieldDefinition)
                );
                
            var costEstimate = costEstimates.FirstOrDefault();

            if (costEstimate == null)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }
            
            // ✅ Populate Options i Components dla wszystkich pozycji
            costEstimate.PopulateItemHierarchy();

            // Pobierz strukturę szablonu przez wspólny serwis
            var templateStructure = await costEstimateTemplateService.GetTemplateStructureCachedAsync(
                costEstimate.Template, 
                cancellationToken);

            // Build hierarchical structure of root groups
            var rootGroups = BuildGroupHierarchy(costEstimate.AllGroups.Where(g => g.ParentGroupId == null).ToList(), costEstimate.AllGroups.ToList());

            return new CostEstimateDetailsWeb(
                Id: costEstimate.Id,
                TenantId: costEstimate.TenantId,
                ProjectId: costEstimate.ProjectId,
                TemplateId: costEstimate.TemplateId,
                TemplateName: costEstimate.Template.Name,
                SelectedCurrencyId: costEstimate.SelectedCurrencyId,
                SelectedCurrencyCode: costEstimate.SelectedCurrency.Code,
                SelectedCurrencySymbol: costEstimate.SelectedCurrency.Symbol,
                Name: costEstimate.Name,
                Description: costEstimate.Description,
                Status: costEstimate.Status,
                RootGroups: rootGroups,
                TotalNet: costEstimate.TotalNet,
                TotalGross: costEstimate.TotalGross,
                TotalVat: costEstimate.TotalVat,
                CreatedAt: costEstimate.CreatedAt,
                UpdatedAt: costEstimate.UpdatedAt,
                LastCalculatedAt: costEstimate.LastCalculatedAt,
                OwnerId: costEstimate.OwnerId,
                OwnerName: $"{costEstimate.Owner.FirstName} {costEstimate.Owner.LastName}",
                TemplateStructure: templateStructure
            );
        }

        private List<CostEstimateGroupWeb> BuildGroupHierarchy(List<CostEstimateGroup> currentLevelGroups, List<CostEstimateGroup> allGroups)
        {
            return currentLevelGroups
                .OrderBy(g => g.Order)
                .Select(group => new CostEstimateGroupWeb(
                    Id: group.Id,
                    ParentGroupId: group.ParentGroupId,
                    Level: group.Level,
                    Order: group.Order,
                    FieldValues: group.FieldValues.Select(fv =>
                    {
                        // Pobierz wartość w odpowiednim typie
                        var (stringValue, decimalValue, boolValue, dateTimeValue) = FieldValueConverter.GetTypedValue(fv, (int)fv.FieldDefinition.FieldType);
                        
                        return new CostEstimateFieldValueWeb(
                            Id: fv.Id,
                            FieldDefinitionId: fv.FieldDefinitionId,
                            FieldType: (int)fv.FieldDefinition.FieldType,
                            FieldScope: (int)fv.FieldDefinition.FieldScope,
                            FieldName: null, // Group fields nie mają FieldName (GUID)
                            FieldLabel: fv.FieldDefinition.Label,
                            StringValue: stringValue,
                            DecimalValue: decimalValue,
                            BoolValue: boolValue,
                            DateTimeValue: dateTimeValue
                        );
                    }).ToList(),
                    TotalNet: group.TotalNet,
                    TotalGross: group.TotalGross,
                    TotalVat: group.TotalVat,
                    LastCalculatedAt: group.LastCalculatedAt,
                    ChildGroups: BuildGroupHierarchy(
                        allGroups.Where(g => g.ParentGroupId == group.Id).ToList(),
                        allGroups
                    ),
                    Items: group.Items
                        .Where(w => w.RelationType == ItemRelationType.None)  // ✅ Tylko główne pozycje (nie opcje ani komponenty)
                        .OrderBy(w => w.Order)
                        .Select(item => BuildItemWeb(item))
                        .ToList(),
                    CreatedAt: group.CreatedAt,
                    UpdatedAt: group.UpdatedAt
                ))
                .ToList();
        }

        private CostEstimateItemWeb BuildItemWeb(CostEstimateItem item)
        {
            return new CostEstimateItemWeb(
                Id: item.Id,
                GroupId: item.GroupId,
                ParentItemId: item.ParentItemId,
                RelationType: (int)item.RelationType,  // ✅ Dodano RelationType
                Order: item.Order,
                NetValue: item.NetValue,       // ✅ Dodano obliczone wartości
                GrossValue: item.GrossValue,
                VatValue: item.VatValue,
                FieldValues: item.FieldValues.Select(fv =>
                {
                    // Pobierz wartość w odpowiednim typie
                    var (stringValue, decimalValue, boolValue, dateTimeValue) = FieldValueConverter.GetTypedValue(fv, (int)fv.FieldDefinition.FieldType);
                    
                    return new CostEstimateFieldValueWeb(
                        Id: fv.Id,
                        FieldDefinitionId: fv.FieldDefinitionId,
                        FieldType: (int)fv.FieldDefinition.FieldType,
                        FieldScope: (int)fv.FieldDefinition.FieldScope,
                        FieldName: fv.FieldDefinition.FieldName,
                        FieldLabel: fv.FieldDefinition.Label,
                        StringValue: stringValue,
                        DecimalValue: decimalValue,
                        BoolValue: boolValue,
                        DateTimeValue: dateTimeValue
                    );
                }).ToList(),
                Options: item.Options  // ✅ Już filtrowane przez property (RelationType == Option)
                    .Where(o => !o.IsDeleted)
                    .OrderBy(o => o.Order)
                    .Select(option => BuildItemWeb(option))
                    .ToList(),
                Components: item.Components  // ✅ Dodano Components (filtrowane przez property: RelationType == Component)
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.Order)
                    .Select(component => BuildItemWeb(component))
                    .ToList(),
                CreatedAt: item.CreatedAt,
                UpdatedAt: item.UpdatedAt
            );
        }
    }
}
