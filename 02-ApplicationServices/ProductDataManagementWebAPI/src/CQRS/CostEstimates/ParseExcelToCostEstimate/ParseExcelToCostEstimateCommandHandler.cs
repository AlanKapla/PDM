using Business.AIAgent.Models;
using Business.AIAgent.Services;
using Business.Implementation.Services.Excel;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using CQRS.CostEstimates.UpdateCostEstimate;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using System.Text.Json;

namespace CQRS.CostEstimates.ParseExcelToCostEstimate;

/// <summary>
/// AI mapping result for cost estimate only
/// </summary>
internal sealed class CostEstimateMapResult
{
    public UpdateCostEstimateCommand? CostEstimate { get; set; }
}

/// <summary>
/// Handler for ParseExcelToCostEstimateCommand
/// Uses AI to parse Excel with template context and generate CostEstimate structure
/// Returns preview DTO - does NOT save to database
/// </summary>
public sealed partial class ParseExcelToCostEstimateCommandHandler 
    : IRequestHandler<ParseExcelToCostEstimateCommand, CostEstimateUpdateDto>
{
    private readonly IExcelParserService _excelParser;
    private readonly IAgentService _agentService;
    private readonly ICostEstimateExcelStorageService _excelStorage;
    private readonly IReadRepository<CostEstimateTemplate> _templateRepo;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ParseExcelToCostEstimateCommandHandler> _logger;

    public ParseExcelToCostEstimateCommandHandler(
        IExcelParserService excelParser,
        IAgentService agentService,
        ICostEstimateExcelStorageService excelStorage,
        IReadRepository<CostEstimateTemplate> templateRepo,
        ICurrentUser currentUser,
        ILogger<ParseExcelToCostEstimateCommandHandler> logger)
    {
        _excelParser = excelParser;
        _agentService = agentService;
        _excelStorage = excelStorage;
        _templateRepo = templateRepo;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<CostEstimateUpdateDto> Handle(
        ParseExcelToCostEstimateCommand request,
        CancellationToken cancellationToken)
    {
        // Load template
        var template = await _templateRepo.GetFirstBySearch(
            t => t.Id == request.TemplateId && !t.IsDeleted && t.OwnerId == _currentUser.Id,
            cancellationToken,
            q => q.Include(t => t.Currencies)
                  .Include(t => t.Units)
                  .Include(t => t.GroupFieldDefinitions)
                  .Include(t => t.SystemFieldDefinitions)
                  .Include(t => t.CalculatedFieldDefinitions)
                  .Include(t => t.GenericFieldDefinitions));

        if (template == null)
        {
            throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
        }

        // Get or upload Excel stream (service handles both scenarios)
        var (excelStream, fileName, fileId) = await _excelStorage.GetOrUploadExcelStreamAsync(
            request.ExcelFile,
            request.FileId,
            request.TenantId,
            request.ProjectId,
            _currentUser.Id,
            displayName: null,
            cancellationToken);

        try
        {
            _logger.LogInformation(
                "Parsing Excel file: {FileName} (FileId: {FileId}) with template: {TemplateId}",
                fileName, fileId, template.Id);

            // Parse Excel
            var excelData = await _excelParser.ParseExcelFileAsync(excelStream, cancellationToken);

            if (!excelData.Success)
            {
                _logger.LogError("Failed to parse Excel: {Error}", excelData.ErrorMessage);
                throw new ValidationApiException($"Failed to parse Excel file: {excelData.ErrorMessage}");
            }

            _logger.LogInformation(
                "Parsed Excel: {Headers} headers, {Rows} rows",
                excelData.Headers.Count, excelData.Rows.Count);

            // Build template context for AI
            var templateContext = BuildTemplateContext(template);

            // Build AI prompt
            var systemPrompt = BuildSystemPromptForCostEstimate();
            var userPrompt = BuildUserPromptForCostEstimate(excelData, templateContext);

            // Call AI
            var aiRequest = new AgentRequest
            {
                SystemPrompt = systemPrompt,
                Prompt = userPrompt,
                TenantId = request.TenantId,
                EnableTools = false
            };

            var aiResult = await _agentService.ProcessRequestAsync(aiRequest, cancellationToken);

            if (!aiResult.IsSuccess)
            {
                _logger.LogError("AI mapping failed: {Error}", aiResult.ErrorMessage);
                throw new InvalidOperationException($"AI mapping failed: {aiResult.ErrorMessage}");
            }

            // Parse AI response
            var mappedResult = ParseCostEstimateResponse(aiResult.Content);

            if (mappedResult?.CostEstimate == null)
            {
                throw new ValidationApiException("AI did not return valid cost estimate structure");
            }

            // Convert Command → DTO
            var costEstimateDto = new CostEstimateUpdateDto(
                CostEstimateId: Guid.Empty,
                TenantId: request.TenantId,
                ProjectId: request.ProjectId,
                Name: mappedResult.CostEstimate.Name,
                Description: mappedResult.CostEstimate.Description,
                Status: mappedResult.CostEstimate.Status,
                RootGroups: mappedResult.CostEstimate.RootGroups
            );

            _logger.LogInformation(
                "Cost estimate parsing completed: {Name}, {Groups} groups",
                costEstimateDto.Name,
                costEstimateDto.RootGroups.Count);

            return costEstimateDto;
        }
        finally
        {
            // Dispose stream
            await excelStream.DisposeAsync();
        }
    }
}

