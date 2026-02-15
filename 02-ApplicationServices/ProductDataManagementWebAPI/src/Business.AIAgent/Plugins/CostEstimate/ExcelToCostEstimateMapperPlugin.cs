using Business.AIAgent.Plugins.Base;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace Business.AIAgent.Plugins.CostEstimate;

/// <summary>
/// Plugin for mapping Excel data to CostEstimate DTOs using AI
/// LLM analyzes Excel structure and returns ready-to-use Command DTOs
/// </summary>
public sealed class ExcelToCostEstimateMapperPlugin : BasePlugin
{
    public ExcelToCostEstimateMapperPlugin(ILogger<ExcelToCostEstimateMapperPlugin> logger) : base(logger)
    {
    }

    [KernelFunction]
    [Description("Maps Excel data (headers + rows) to Template and CostEstimate structure. Returns JSON with UpdateCostEstimateTemplateCommand and CreateCostEstimateCommand DTOs.")]
    public async Task<string> MapExcelToCostEstimateAsync(
        [Description("Excel headers as JSON array of strings")] string headersJson,
        [Description("Excel rows as JSON array of arrays (first 50 rows max)")] string rowsJson,
        [Description("Example template structure as JSON")] string templateExampleJson,
        [Description("Example cost estimate structure as JSON")] string costEstimateExampleJson,
        CancellationToken cancellationToken = default)
    {
        LogFunctionInvocation(nameof(MapExcelToCostEstimateAsync), headersJson.Length, rowsJson.Length);

        try
        {
            // This function doesn't do actual mapping - LLM does!
            // We just validate that we got all required inputs
            
            var headers = JsonSerializer.Deserialize<List<string>>(headersJson);
            var rows = JsonSerializer.Deserialize<List<List<string>>>(rowsJson);

            if (headers == null || rows == null || !headers.Any() || !rows.Any())
            {
                return JsonSerializer.Serialize(new { error = "Invalid Excel data - empty headers or rows" });
            }

            // Return placeholder - LLM will replace this with actual mapping
            var result = new
            {
                template = new { },  // LLM fills this
                costEstimate = new { }  // LLM fills this
            };

            LogFunctionResult(nameof(MapExcelToCostEstimateAsync), "Mapping requested");
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            LogFunctionError(nameof(ExcelToCostEstimateMapperPlugin), ex);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
