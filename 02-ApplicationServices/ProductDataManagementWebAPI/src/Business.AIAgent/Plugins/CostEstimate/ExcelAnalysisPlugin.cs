using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Business.AIAgent.Plugins.Base;

namespace Business.AIAgent.Plugins.CostEstimate;

/// <summary>
/// Plugin for analyzing Excel files and mapping them to CostEstimate structure
/// AI assists in recognizing column meanings and suggesting structure
/// </summary>
public sealed class ExcelAnalysisPlugin : BasePlugin
{
    public ExcelAnalysisPlugin(ILogger<ExcelAnalysisPlugin> logger) : base(logger)
    {
    }

    [KernelFunction]
    [Description("Analyzes Excel column headers and suggests their mapping to cost estimate fields")]
    public async Task<ExcelStructureAnalysisDto?> AnalyzeExcelStructureAsync(
        [Description("List of column headers from Excel file")] List<string> columnHeaders,
        [Description("Sample data rows (first 3-5 rows) as JSON array")] string sampleDataJson,
        CancellationToken cancellationToken = default)
    {
        LogFunctionInvocation(nameof(AnalyzeExcelStructureAsync), columnHeaders.Count, sampleDataJson.Length);

        try
        {
            // Parse sample data
            var sampleData = JsonSerializer.Deserialize<List<List<string>>>(sampleDataJson);

            // Analyze and suggest mappings
            var groupColumns = new List<string>();
            var itemColumns = new List<string>();
            var numericColumns = new List<string>();
            var textColumns = new List<string>();

            for (int i = 0; i < columnHeaders.Count; i++)
            {
                var header = columnHeaders[i].ToLower();
                
                // Detect group-like columns
                if (header.Contains("grupa") || header.Contains("group") || 
                    header.Contains("etap") || header.Contains("stage") ||
                    header.Contains("kategoria") || header.Contains("category"))
                {
                    groupColumns.Add(columnHeaders[i]);
                }
                
                // Detect item-like columns
                if (header.Contains("nazwa") || header.Contains("name") ||
                    header.Contains("opis") || header.Contains("description") ||
                    header.Contains("pozycja") || header.Contains("item"))
                {
                    itemColumns.Add(columnHeaders[i]);
                }
                
                // Detect numeric columns
                if (header.Contains("ilość") || header.Contains("quantity") ||
                    header.Contains("cena") || header.Contains("price") ||
                    header.Contains("koszt") || header.Contains("cost") ||
                    header.Contains("wartość") || header.Contains("value"))
                {
                    numericColumns.Add(columnHeaders[i]);
                }
                
                // Text columns (default)
                if (!groupColumns.Contains(columnHeaders[i]) && 
                    !itemColumns.Contains(columnHeaders[i]) && 
                    !numericColumns.Contains(columnHeaders[i]))
                {
                    textColumns.Add(columnHeaders[i]);
                }
            }

            var result = new ExcelStructureAnalysisDto
            {
                TotalColumns = columnHeaders.Count,
                SuggestedGroupColumns = groupColumns,
                SuggestedItemColumns = itemColumns,
                SuggestedNumericColumns = numericColumns,
                SuggestedTextColumns = textColumns,
                HasHierarchicalStructure = groupColumns.Any(),
                ConfidenceScore = CalculateConfidence(groupColumns, itemColumns, numericColumns)
            };

            LogFunctionResult(nameof(AnalyzeExcelStructureAsync), $"Confidence: {result.ConfidenceScore:P0}");
            return result;
        }
        catch (Exception ex)
        {
            LogFunctionError(nameof(AnalyzeExcelStructureAsync), ex);
            return null;
        }
    }

    [KernelFunction]
    [Description("Suggests the best matching existing template for given Excel structure")]
    public async Task<TemplateSuggestionDto?> SuggestTemplateMatchAsync(
        [Description("User's template IDs (comma-separated GUIDs)")] string userTemplateIds,
        [Description("Template names (comma-separated)")] string templateNames,
        [Description("Excel column headers (comma-separated)")] string excelColumns,
        CancellationToken cancellationToken = default)
    {
        LogFunctionInvocation(nameof(SuggestTemplateMatchAsync), userTemplateIds, excelColumns);

        try
        {
            var templateIdList = userTemplateIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(Guid.Parse).ToList();
            var templateNameList = templateNames.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            var excelColumnList = excelColumns.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Simple matching algorithm (AI would do better analysis)
            var matches = new List<(Guid TemplateId, string TemplateName, double Score)>();

            for (int i = 0; i < templateIdList.Count; i++)
            {
                var templateId = templateIdList[i];
                var templateName = i < templateNameList.Count ? templateNameList[i] : "Unknown";
                
                // Calculate similarity score (simple word matching)
                var score = CalculateTemplateMatchScore(templateName, excelColumnList);
                matches.Add((templateId, templateName, score));
            }

            var bestMatch = matches.OrderByDescending(m => m.Score).FirstOrDefault();

            if (bestMatch.Score > 0.5) // 50% confidence threshold
            {
                var result = new TemplateSuggestionDto
                {
                    SuggestedTemplateId = bestMatch.TemplateId,
                    SuggestedTemplateName = bestMatch.TemplateName,
                    MatchConfidence = bestMatch.Score,
                    ShouldCreateNewTemplate = false
                };

                LogFunctionResult(nameof(SuggestTemplateMatchAsync), 
                    $"Matched: {result.SuggestedTemplateName} (Confidence: {result.MatchConfidence:P0})");
                return result;
            }
            else
            {
                var result = new TemplateSuggestionDto
                {
                    SuggestedTemplateId = null,
                    SuggestedTemplateName = "New Template",
                    MatchConfidence = 0,
                    ShouldCreateNewTemplate = true
                };

                LogFunctionResult(nameof(SuggestTemplateMatchAsync), "No good match - suggest new template");
                return result;
            }
        }
        catch (Exception ex)
        {
            LogFunctionError(nameof(SuggestTemplateMatchAsync), ex);
            return null;
        }
    }

    private double CalculateConfidence(List<string> groups, List<string> items, List<string> numerics)
    {
        var hasGroups = groups.Any() ? 0.4 : 0.0;
        var hasItems = items.Any() ? 0.3 : 0.0;
        var hasNumerics = numerics.Any() ? 0.3 : 0.0;
        
        return hasGroups + hasItems + hasNumerics;
    }

    private double CalculateTemplateMatchScore(string templateName, List<string> excelColumns)
    {
        var templateWords = templateName.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var excelWords = excelColumns.SelectMany(c => c.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToHashSet();

        var matchCount = templateWords.Count(tw => excelWords.Contains(tw));
        return templateWords.Any() ? (double)matchCount / templateWords.Length : 0;
    }
}

public sealed class ExcelStructureAnalysisDto
{
    public int TotalColumns { get; set; }
    public List<string> SuggestedGroupColumns { get; set; } = new();
    public List<string> SuggestedItemColumns { get; set; } = new();
    public List<string> SuggestedNumericColumns { get; set; } = new();
    public List<string> SuggestedTextColumns { get; set; } = new();
    public bool HasHierarchicalStructure { get; set; }
    public double ConfidenceScore { get; set; }
}

public sealed class TemplateSuggestionDto
{
    public Guid? SuggestedTemplateId { get; set; }
    public string SuggestedTemplateName { get; set; } = string.Empty;
    public double MatchConfidence { get; set; }
    public bool ShouldCreateNewTemplate { get; set; }
}
