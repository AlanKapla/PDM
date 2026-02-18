using Business.Implementation.Helpers;
using Business.Implementation.Validators;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

/// <summary>
/// Service for managing CostEstimate lifecycle
/// Extracted from CreateCostEstimateCommandHandler and UpdateCostEstimateCommandHandler
/// </summary>
public sealed partial class CostEstimateService : ICostEstimateService
{
    private readonly IRepository<CostEstimate> _costEstimateRepository;
    private readonly IRepository<CostEstimateTemplate> _templateRepository;
    private readonly IRepository<CostEstimateGroup> _groupRepository;
    private readonly IRepository<CostEstimateGroupFieldValue> _groupFieldValueRepository;
    private readonly IRepository<CostEstimateItem> _itemRepository;
    private readonly IRepository<CostEstimateItemFieldValue> _itemFieldValueRepository;
    private readonly ICostEstimateCalculationService _calculationService;
    private readonly CostEstimateGroupValidator _groupValidator;
    private readonly CostEstimateItemValidator _itemValidator;
    private readonly ICurrentUser _currentUser;

    public CostEstimateService(
        IRepository<CostEstimate> costEstimateRepository,
        IRepository<CostEstimateTemplate> templateRepository,
        IRepository<CostEstimateGroup> groupRepository,
        IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository,
        IRepository<CostEstimateItem> itemRepository,
        IRepository<CostEstimateItemFieldValue> itemFieldValueRepository,
        ICostEstimateCalculationService calculationService,
        CostEstimateGroupValidator groupValidator,
        CostEstimateItemValidator itemValidator,
        ICurrentUser currentUser)
    {
        _costEstimateRepository = costEstimateRepository;
        _templateRepository = templateRepository;
        _groupRepository = groupRepository;
        _groupFieldValueRepository = groupFieldValueRepository;
        _itemRepository = itemRepository;
        _itemFieldValueRepository = itemFieldValueRepository;
        _calculationService = calculationService;
        _groupValidator = groupValidator;
        _itemValidator = itemValidator;
        _currentUser = currentUser;
    }

    public async Task<Guid> CreateAsync(
        Guid tenantId,
        Guid projectId,
        Guid templateId,
        Guid selectedCurrencyId,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        // Verify template exists
        var templates = await _templateRepository.GetBySearch(
            t => t.Id == templateId && !t.IsDeleted && t.OwnerId == _currentUser.Id,
            q => q.Include(v => v.Currencies));

        var template = templates.FirstOrDefault()
            ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), templateId.ToString());

        // Verify selected currency exists in template
        var selectedCurrency = template.Currencies.FirstOrDefault(c => c.Id == selectedCurrencyId)
            ?? throw new ValidationApiException(
                $"Currency with ID {selectedCurrencyId} not found in template. Available currencies: {string.Join(", ", template.Currencies.Select(c => $"{c.Code} ({c.Id})"))}");

        var now = DateTime.UtcNow;

        // Create cost estimate
        var costEstimate = new CostEstimate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            TemplateId = templateId,
            OwnerId = _currentUser.Id,
            Name = name,
            Description = description,
            Status = CostEstimateStatus.Draft,
            SelectedCurrencyId = selectedCurrencyId,
            TotalNet = null,
            TotalGross = null,
            TotalVat = null,
            CreatedAt = now,
            IsDeleted = false
        };

        await _costEstimateRepository.Insert(costEstimate);
        await _costEstimateRepository.SaveChangesAsync(cancellationToken);

        return costEstimate.Id;
    }

    public async Task UpdateAsync(
        CostEstimateUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        // Get cost estimate
        var costEstimates = await _costEstimateRepository.GetBySearch(
            c => c.Id == dto.CostEstimateId &&
                 c.TenantId == dto.TenantId &&
                 c.ProjectId == dto.ProjectId &&
                 !c.IsDeleted &&
                 c.OwnerId == _currentUser.Id,
            q => q.Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                      .ThenInclude(g => g.FieldValues)
                  .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                      .ThenInclude(g => g.Items.Where(w => !w.IsDeleted))
                          .ThenInclude(w => w.FieldValues)
                  .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                      .ThenInclude(g => g.Items.Where(w => !w.IsDeleted))
                          .ThenInclude(w => w.Options.Where(o => !o.IsDeleted))
                              .ThenInclude(o => o.FieldValues));

        var costEstimate = costEstimates.FirstOrDefault()
            ?? throw new NotFoundApiException(nameof(CostEstimate), dto.CostEstimateId.ToString());

        // Get template with definitions
        var templates = await _templateRepository.GetBySearch(
            t => t.Id == costEstimate.TemplateId,
            q => q.Include(v => v.GroupFieldDefinitions)
                  .Include(v => v.SystemFieldDefinitions)
                  .Include(v => v.CalculatedFieldDefinitions)
                  .Include(v => v.GenericFieldDefinitions));

        var template = templates.FirstOrDefault()
            ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), costEstimate.TemplateId.ToString());

        // Build field definition dictionaries
        var groupFieldDefinitionsById = template.GroupFieldDefinitions.ToDictionary(f => f.Id);
        var allItemFieldDefinitionsById = template.SystemFieldDefinitions
            .Cast<CostEstimateTemplateFieldDefinitionBase>()
            .Concat(template.CalculatedFieldDefinitions)
            .Concat(template.GenericFieldDefinitions)
            .ToDictionary(f => f.Id);

        // Validate group hierarchy
        var tempGroups = BuildTemporaryGroupsForValidation(dto.RootGroups, costEstimate.Id);
        var hierarchyValidation = _groupValidator.ValidateGroupHierarchy(
            template,
            tempGroups,
            cancellationToken);

        if (!hierarchyValidation.IsValid)
        {
            throw new ValidationApiException(string.Join("; ", hierarchyValidation.Errors));
        }

        var now = DateTime.UtcNow;

        // Snapshot existing group IDs before update
        var existingGroupIds = costEstimate.AllGroups
            .Select(g => g.Id)
            .ToHashSet();

        // Update basic properties
        costEstimate.Name = dto.Name;
        costEstimate.Description = dto.Description;
        costEstimate.Status = dto.Status;
        costEstimate.UpdatedAt = now;

        await _costEstimateRepository.Update(costEstimate);

        // Update group hierarchy
        await UpdateGroupHierarchyAsync(
            costEstimate.Id,
            groupFieldDefinitionsById,
            allItemFieldDefinitionsById,
            dto.RootGroups,
            costEstimate.AllGroups.ToList(),
            null,
            0,
            now,
            cancellationToken);

        // Delete groups no longer in request
        var requestedGroupIds = CollectAllGroupIds(dto.RootGroups);
        var groupIdsToDelete = existingGroupIds
            .Where(id => !requestedGroupIds.Contains(id))
            .ToHashSet();

        if (groupIdsToDelete.Any())
        {
            var groupsToDelete = await _groupRepository.GetBySearch(
                g => groupIdsToDelete.Contains(g.Id) && !g.IsDeleted);

            foreach (var group in groupsToDelete)
            {
                group.IsDeleted = true;
                group.DeletedAt = now;
                await _groupRepository.Update(group);
            }

            await _groupRepository.SaveChangesAsync(cancellationToken);
        }

        // Reload for calculation
        var costEstimateForCalculation = await _costEstimateRepository.GetFirstBySearch(
            c => c.Id == dto.CostEstimateId && !c.IsDeleted,
            q => q.Include(c => c.Template)
                    .ThenInclude(t => t.CalculatedFieldDefinitions)
                  .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                      .ThenInclude(g => g.Items.Where(w => !w.IsDeleted && w.RelationType == ItemRelationType.None))
                          .ThenInclude(w => w.FieldValues)
                              .ThenInclude(fv => fv.FieldDefinition)
                  .Include(c => c.AllItems.Where(i => !i.IsDeleted && i.ParentItemId != null))
                      .ThenInclude(i => i.FieldValues)
                          .ThenInclude(fv => fv.FieldDefinition));

        if (costEstimateForCalculation == null)
        {
            throw new NotFoundApiException(nameof(CostEstimate), dto.CostEstimateId.ToString());
        }

        // Populate item hierarchy
        costEstimateForCalculation.PopulateItemHierarchy();

        // Recalculate totals
        _calculationService.RecalculateCostEstimate(costEstimateForCalculation);

        // Save calculated totals
        await _costEstimateRepository.Update(costEstimateForCalculation);

        foreach (var group in costEstimateForCalculation.AllGroups.Where(g => !g.IsDeleted))
        {
            await _groupRepository.Update(group);
        }

        await _costEstimateRepository.SaveChangesAsync(cancellationToken);
    }

    // Private helper methods in CostEstimateService.Helpers.cs (partial)
}
