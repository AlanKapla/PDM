using Business.AIAgent.Models;
using Business.AIAgent.Services;
using Business.Implementation.Services.Excel;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CQRS.CostEstimates.ParseExcelToTemplate;

/// <summary>
/// AI mapping result for template only
/// </summary>
internal sealed class TemplateMapResult
{
    public UpdateCostEstimateTemplateCommand? Template { get; set; }
}

/// <summary>
/// Handler for ParseExcelToTemplateCommand
/// Uses AI to parse Excel file and generate template structure
/// Returns preview DTO - does NOT save to database
/// </summary>
public sealed partial class ParseExcelToTemplateCommandHandler 
    : IRequestHandler<ParseExcelToTemplateCommand, CostEstimateTemplateUpdateDto>
{
    private readonly IExcelParserService _excelParser;
    private readonly IAgentService _agentService;
    private readonly ICostEstimateExcelStorageService _excelStorage;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ParseExcelToTemplateCommandHandler> _logger;

    public ParseExcelToTemplateCommandHandler(
        IExcelParserService excelParser,
        IAgentService agentService,
        ICostEstimateExcelStorageService excelStorage,
        ICurrentUser currentUser,
        ILogger<ParseExcelToTemplateCommandHandler> logger)
    {
        _excelParser = excelParser;
        _agentService = agentService;
        _excelStorage = excelStorage;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<CostEstimateTemplateUpdateDto> Handle(
        ParseExcelToTemplateCommand request,
        CancellationToken cancellationToken)
    {
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
            _logger.LogInformation("Parsing Excel file: {FileName} (FileId: {FileId})", fileName, fileId);

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

            // Generate template example for AI
            var templateExample = GenerateTemplateExample();

            // Build AI prompt (template only)
            var systemPrompt = BuildSystemPromptForTemplate();
            var templateExampleJson = JsonSerializer.Serialize(templateExample, new JsonSerializerOptions { WriteIndented = true });
            var userPrompt = BuildUserPromptForTemplate(excelData, templateExampleJson);

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
            var mappedResult = ParseTemplateResponse(aiResult.Content);

            if (mappedResult?.Template == null)
            {
                throw new ValidationApiException("AI did not return valid template structure");
            }

            // Convert Command → DTO
            var templateDto = new CostEstimateTemplateUpdateDto(
                TemplateId: Guid.Empty,
                Name: mappedResult.Template.Name,
                Description: mappedResult.Template.Description,
                Category: mappedResult.Template.Category,
                CanAddGroups: mappedResult.Template.CanAddGroups,
                CanBranchGroups: mappedResult.Template.CanBranchGroups,
                MaxGroupLevel: mappedResult.Template.MaxGroupLevel,
                AutoNumberGroups: mappedResult.Template.AutoNumberGroups,
                GroupNumberFormat: mappedResult.Template.GroupNumberFormat,
                UpdateStructure: mappedResult.Template.UpdateStructure,
                Currencies: mappedResult.Template.Currencies?.Select(c =>
                    new Business.Interfaces.WebModels.CostEstimateTemplates.CurrencyDto(
                        c.Code, c.Name, c.Symbol, c.IsDefault, c.Order)).ToList(),
                Units: mappedResult.Template.Units?.Select(u =>
                    new Business.Interfaces.WebModels.CostEstimateTemplates.UnitDto(
                        u.Code, u.Name, u.Symbol, u.Category, u.IsDefault, u.Order)).ToList(),
                GroupHeaderFields: ConvertFieldDefinitions(mappedResult.Template.GroupHeaderFields),
                SystemFields: ConvertFieldDefinitions(mappedResult.Template.SystemFields),
                CalculatedFields: ConvertFieldDefinitions(mappedResult.Template.CalculatedFields),
                GenericFields: ConvertFieldDefinitions(mappedResult.Template.GenericFields),
                UiConfiguration: mappedResult.Template.UiConfiguration != null
                    ? new Business.Interfaces.WebModels.CostEstimateTemplates.UiConfigurationDto(
                        mappedResult.Template.UiConfiguration.ColumnLayout)
                    : null
            );

            _logger.LogInformation("Template parsing completed: {TemplateName}", templateDto.Name);

            return templateDto;
        }
        finally
        {
            // Dispose stream (only if it wasn't from uploaded file, as service handles that)
            await excelStream.DisposeAsync();
        }
    }
}

