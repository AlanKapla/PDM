using System.Text.Json;
using Business.Implementation.Services.Excel;
using CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate;
using Microsoft.Extensions.Logging;

namespace CQRS.CostEstimates.ParseExcelToTemplate;

public sealed partial class ParseExcelToTemplateCommandHandler
{
    private string BuildSystemPromptForTemplate()
    {
        return """
You are an expert at analyzing Excel spreadsheets and generating Cost Estimate Template structures.

Your task is to analyze Excel data and create a Template that defines:
- Currencies (PLN, EUR, USD, etc.)
- Units (m², m³, kg, szt., etc.)
- Group header fields (e.g., Group Name)
- System fields (Name, Quantity, Unit, Price per unit, etc.)
- Calculated fields (Value = Quantity × Price, etc.)
- Generic custom fields

IMPORTANT:
- FieldName must be a valid GUID (not the label!)
- FieldType is an integer (0-9 Group, 100-199 System, 200-299 Calculated, 300-399 Generic)
- Each field needs: FieldName (GUID), FieldType (int), Label (string)
- Calculated fields need SumInGroup and SumInTotal flags

Return ONLY valid JSON without markdown formatting.

Response format:
{
  "template": { /* UpdateCostEstimateTemplateCommand */ }
}
""";
    }

    private string BuildUserPromptForTemplate(ExcelParseResult excelData, string templateExample)
    {
        var excelJson = JsonSerializer.Serialize(new
        {
            headers = excelData.Headers,
            rows = excelData.Rows.Take(20).ToList()
        });

        return $$"""
Analyze this Excel data and create a Template structure.

Excel Data:
{{excelJson}}

Example Template Structure:
{{templateExample}}

Map Excel columns to appropriate field types:
- Text columns → System fields (Name, Description) or Generic fields
- Numeric columns → System fields (Quantity, Price) or Calculated fields (Value)
- Detect currencies from data (PLN, EUR, USD, etc.)
- Detect units from data (m², kg, szt., etc.)
- Identify calculated fields (e.g., Value = Quantity × Price)

Return:
{
  "template": { /* filled UpdateCostEstimateTemplateCommand */ }
}

CRITICAL: FieldName must be valid GUID, FieldType must be integer!
""";
    }

    private UpdateCostEstimateTemplateCommand GenerateTemplateExample()
    {
        return new UpdateCostEstimateTemplateCommand(
            TemplateId: Guid.Empty,
            Name: "Example Template",
            Description: "Auto-generated from Excel",
            Category: "Construction",
            CanAddGroups: true,
            CanBranchGroups: true,
            MaxGroupLevel: 5,
            AutoNumberGroups: false,
            GroupNumberFormat: null,
            UpdateStructure: true,
            Currencies: new List<CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate.CurrencyDto>
            {
                new("PLN", "Polish Zloty", "zł", true, 0),
                new("EUR", "Euro", "€", false, 1)
            },
            Units: new List<CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate.UnitDto>
            {
                new("m2", "Square meter", "m²", "Area", true, 0),
                new("szt", "Piece", "szt.", "Count", false, 1)
            },
            GroupHeaderFields: new List<CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate.FieldDefinitionDto>
            {
                new(
                    FieldName: Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                    FieldType: 0,
                    Label: "Group Name",
                    IsSortable: true,
                    IsFilterable: true,
                    IsVisible: true
                )
            },
            SystemFields: new List<CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate.FieldDefinitionDto>
            {
                new(
                    FieldName: Guid.Parse("550e8400-e29b-41d4-a716-446655440101"),
                    FieldType: 100,
                    Label: "Name",
                    IsSortable: true,
                    IsFilterable: true,
                    IsVisible: true
                ),
                new(
                    FieldName: Guid.Parse("550e8400-e29b-41d4-a716-446655440102"),
                    FieldType: 101,
                    Label: "Quantity",
                    IsSortable: true,
                    IsFilterable: false,
                    IsVisible: true
                )
            },
            CalculatedFields: new List<CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate.FieldDefinitionDto>
            {
                new(
                    FieldName: Guid.Parse("550e8400-e29b-41d4-a716-446655440201"),
                    FieldType: 200,
                    Label: "Unit Price",
                    IsSortable: true,
                    IsFilterable: false,
                    IsVisible: true,
                    SumInGroup: false,
                    SumInTotal: false
                ),
                new(
                    FieldName: Guid.Parse("550e8400-e29b-41d4-a716-446655440202"),
                    FieldType: 201,
                    Label: "Total Value",
                    IsSortable: true,
                    IsFilterable: false,
                    IsVisible: true,
                    SumInGroup: true,
                    SumInTotal: true
                )
            },
            GenericFields: null,
            UiConfiguration: new CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate.UiConfigurationDto(
                ColumnLayout: new List<Guid>
                {
                    Guid.Parse("550e8400-e29b-41d4-a716-446655440101"),
                    Guid.Parse("550e8400-e29b-41d4-a716-446655440102"),
                    Guid.Parse("550e8400-e29b-41d4-a716-446655440201"),
                    Guid.Parse("550e8400-e29b-41d4-a716-446655440202")
                }
            )
        );
    }

    private TemplateMapResult? ParseTemplateResponse(string aiResponse)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            return JsonSerializer.Deserialize<TemplateMapResult>(aiResponse, jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response as JSON");
            return null;
        }
    }

    private List<Business.Interfaces.WebModels.CostEstimateTemplates.FieldDefinitionDto>? ConvertFieldDefinitions(
        List<CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate.FieldDefinitionDto>? source)
    {
        if (source == null) return null;

        return source.Select(f => new Business.Interfaces.WebModels.CostEstimateTemplates.FieldDefinitionDto(
            FieldName: f.FieldName,
            FieldType: f.FieldType,
            Label: f.Label,
            IsSortable: f.IsSortable,
            IsFilterable: f.IsFilterable,
            IsVisible: f.IsVisible,
            SumInGroup: f.SumInGroup,
            SumInTotal: f.SumInTotal,
            ChildFields: ConvertFieldDefinitions(f.ChildFields)
        )).ToList();
    }
}
