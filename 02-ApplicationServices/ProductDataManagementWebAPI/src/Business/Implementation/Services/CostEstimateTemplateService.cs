using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using Business.Implementation.Helpers;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

/// <summary>
/// Service for managing CostEstimateTemplate lifecycle
/// Extracted from CreateCostEstimateTemplateCommandHandler and UpdateCostEstimateTemplateCommandHandler
/// </summary>
public sealed partial class CostEstimateTemplateService : ICostEstimateTemplateService
{
    private readonly IRepository<CostEstimateTemplate> _templateRepository;
    private readonly IRepository<CostEstimateTemplateCurrency> _currencyRepository;
    private readonly IRepository<CostEstimateTemplateUnit> _unitRepository;
    private readonly IRepository<CostEstimateTemplateGroupFieldDefinition> _groupFieldRepository;
    private readonly IRepository<CostEstimateTemplateItemSystemFieldDefinition> _systemFieldRepository;
    private readonly IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> _calculatedFieldRepository;
    private readonly IRepository<CostEstimateTemplateItemGenericFieldDefinition> _genericFieldRepository;
    private readonly IRepository<CostEstimate> _costEstimateRepository;
    private readonly ICostEstimateCalculationService _calculationService;
    private readonly ICurrentUser _currentUser;

    public CostEstimateTemplateService(
        IRepository<CostEstimateTemplate> templateRepository,
        IRepository<CostEstimateTemplateCurrency> currencyRepository,
        IRepository<CostEstimateTemplateUnit> unitRepository,
        IRepository<CostEstimateTemplateGroupFieldDefinition> groupFieldRepository,
        IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository,
        IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> calculatedFieldRepository,
        IRepository<CostEstimateTemplateItemGenericFieldDefinition> genericFieldRepository,
        IRepository<CostEstimate> costEstimateRepository,
        ICostEstimateCalculationService calculationService,
        ICurrentUser currentUser)
    {
        _templateRepository = templateRepository;
        _currencyRepository = currencyRepository;
        _unitRepository = unitRepository;
        _groupFieldRepository = groupFieldRepository;
        _systemFieldRepository = systemFieldRepository;
        _calculatedFieldRepository = calculatedFieldRepository;
        _genericFieldRepository = genericFieldRepository;
        _costEstimateRepository = costEstimateRepository;
        _calculationService = calculationService;
        _currentUser = currentUser;
    }

    public async Task<Guid> CreateAsync(
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var template = new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = _currentUser.Id,
            Name = name,
            Description = description,
            Category = null,
            CanAddGroups = true,
            CanBranchGroups = true,
            MaxGroupLevel = null,
            AutoNumberGroups = false,
            GroupNumberFormat = null,
            CreatedAt = now,
            IsDeleted = false
        };

        await _templateRepository.Insert(template);
        await _templateRepository.SaveChangesAsync(cancellationToken);

        return template.Id;
    }

    public async Task UpdateAsync(
        CostEstimateTemplateUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var template = await _templateRepository.GetFirstBySearch(
            t => t.Id == dto.TemplateId && t.OwnerId == _currentUser.Id && !t.IsDeleted);

        if (template == null)
        {
            throw new NotFoundApiException(nameof(CostEstimateTemplate), dto.TemplateId.ToString());
        }

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.Category = dto.Category;
        template.CanAddGroups = dto.CanAddGroups;
        template.CanBranchGroups = dto.CanBranchGroups;
        template.MaxGroupLevel = dto.MaxGroupLevel;
        template.AutoNumberGroups = dto.AutoNumberGroups;
        template.GroupNumberFormat = dto.GroupNumberFormat;
        template.UpdatedAt = now;

        await _templateRepository.Update(template);
        await _templateRepository.SaveChangesAsync(cancellationToken);

        if (dto.Currencies != null)
        {
            await UpdateCurrenciesAsync(template.Id, dto.Currencies, cancellationToken);
        }

        if (dto.Units != null)
        {
            await UpdateUnitsAsync(template.Id, dto.Units, cancellationToken);
        }

        if (!dto.UpdateStructure)
        {
            return;
        }

        await DeleteRemovedFieldsAsync(
            template.Id,
            dto.GroupHeaderFields,
            dto.SystemFields,
            dto.CalculatedFields,
            dto.GenericFields,
            cancellationToken);

        var fieldNameToIdMap = new Dictionary<Guid, Guid>();
        var columnLayoutOrderMap = BuildColumnLayoutOrderMap(dto.UiConfiguration?.ColumnLayout);

        if (dto.GroupHeaderFields != null)
        {
            await UpsertFieldsInBatchAsync(
                dto.GroupHeaderFields,
                template.Id,
                FieldScope.Group,
                fieldNameToIdMap,
                columnLayoutOrderMap,
                cancellationToken);
        }

        if (dto.SystemFields != null)
        {
            await UpsertFieldsInBatchAsync(
                dto.SystemFields,
                template.Id,
                FieldScope.ItemSystem,
                fieldNameToIdMap,
                columnLayoutOrderMap,
                cancellationToken);
        }

        if (dto.CalculatedFields != null)
        {
            await UpsertFieldsInBatchAsync(
                dto.CalculatedFields,
                template.Id,
                FieldScope.ItemCalculated,
                fieldNameToIdMap,
                columnLayoutOrderMap,
                cancellationToken);
        }

        if (dto.GenericFields != null)
        {
            await UpsertFieldsInBatchAsync(
                dto.GenericFields,
                template.Id,
                FieldScope.ItemGeneric,
                fieldNameToIdMap,
                columnLayoutOrderMap,
                cancellationToken);
        }

        await RecalculateAllCostEstimatesForTemplateAsync(template.Id, cancellationToken);
    }

    // Private helper methods in CostEstimateTemplateService.Helpers.cs (partial)
}


