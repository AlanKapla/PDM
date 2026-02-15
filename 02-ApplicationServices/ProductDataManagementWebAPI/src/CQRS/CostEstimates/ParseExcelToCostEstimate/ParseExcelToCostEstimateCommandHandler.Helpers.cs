using System.Text.Json;
using Business.Implementation.Services.Excel;
using Entities.Models.CostEstimateTemplates;
using Microsoft.Extensions.Logging;

namespace CQRS.CostEstimates.ParseExcelToCostEstimate;

public sealed partial class ParseExcelToCostEstimateCommandHandler
{
    private string BuildSystemPromptForCostEstimate()
    {
        return """
You are an expert at mapping Excel data to Cost Estimate structures using a predefined template.

Your task is to:
1. Read the Template structure (field definitions, currencies, units)
2. Map Excel rows to groups and items
3. Assign correct field values based on Template field definitions

CRITICAL RULES:
- Use ONLY FieldDefinitionId from Template (NOT FieldName!)
- Groups use GroupFieldDefinitions
- Items use SystemFieldDefinitions, CalculatedFieldDefinitions, GenericFieldDefinitions
- Match field types: String → StringValue, Decimal → DecimalValue, Bool → BoolValue, DateTime → DateTimeValue

Return ONLY valid JSON without markdown formatting.

Response format:
{
  "costEstimate": { 
    "name": "...",
    "description": "...",
    "status": 0,
    "rootGroups": [ /* CostEstimateGroupDto array */ ]
  }
}
""";
    }

    private string BuildUserPromptForCostEstimate(ExcelParseResult excelData, string templateContext)
    {
        var excelJson = JsonSerializer.Serialize(new
        {
            headers = excelData.Headers,
            rows = excelData.Rows.Take(50).ToList()
        });

        return $$"""
Map this Excel data to CostEstimate structure using the Template.

Template Context:
{{templateContext}}

Excel Data:
{{excelJson}}

Instructions:
1. Group rows logically (if hierarchical) or create single group
2. For each row, create CostEstimateItemDto
3. Map Excel columns to Template fields using FieldDefinitionId
4. Set correct typed values (StringValue, DecimalValue, etc.)
5. Name: Extract from Excel or generate meaningful name
6. Description: Optional summary
7. Status: 0 (Draft)

Return:
{
  "costEstimate": {
    "name": "Cost Estimate from Excel",
    "description": "Auto-generated",
    "status": 0,
    "rootGroups": [ /* filled groups with items */ ]
  }
}

CRITICAL: Use FieldDefinitionId from Template, NOT FieldName!
""";
    }

    private string BuildTemplateContext(CostEstimateTemplate template)
    {
        var context = new
        {
            templateName = template.Name,
            currencies = template.Currencies.Select(c => new
            {
                id = c.Id,
                code = c.Code,
                name = c.Name
            }),
            units = template.Units.Select(u => new
            {
                id = u.Id,
                code = u.Code,
                name = u.Name
            }),
            groupFields = template.GroupFieldDefinitions.Select(f => new
            {
                id = f.Id,
                fieldName = f.FieldName,
                fieldType = (int)f.FieldType,
                label = f.Label
            }),
            systemFields = template.SystemFieldDefinitions.Select(f => new
            {
                id = f.Id,
                fieldName = f.FieldName,
                fieldType = (int)f.FieldType,
                label = f.Label
            }),
            calculatedFields = template.CalculatedFieldDefinitions.Select(f => new
            {
                id = f.Id,
                fieldName = f.FieldName,
                fieldType = (int)f.FieldType,
                label = f.Label
            }),
            genericFields = template.GenericFieldDefinitions.Select(f => new
            {
                id = f.Id,
                fieldName = f.FieldName,
                fieldType = (int)f.FieldType,
                label = f.Label
            })
        };

        return JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true });
    }

    private CostEstimateMapResult? ParseCostEstimateResponse(string aiResponse)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            return JsonSerializer.Deserialize<CostEstimateMapResult>(aiResponse, jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response as JSON");
            return null;
        }
    }
}
