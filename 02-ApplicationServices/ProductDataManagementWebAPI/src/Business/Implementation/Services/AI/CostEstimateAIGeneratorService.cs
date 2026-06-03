using Business.AIAgent;
using Business.AIAgent.Abstractions;
using Business.Implementation.Helpers;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ToonFormat;

namespace Business.Implementation.Services.AI;

public sealed class CostEstimateAIGeneratorService : ICostEstimateAIGeneratorService
{
    private readonly IAgentRunner _agentRunner;
    private readonly ILogger<CostEstimateAIGeneratorService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CostEstimateAIGeneratorService(
        IAgentRunner agentRunner,
        ILogger<CostEstimateAIGeneratorService> logger)
    {
        _agentRunner = agentRunner;
        _logger = logger;
    }

    public async Task<AICostEstimatePreviewWeb> GeneratePreviewAsync(
        AICostEstimateRequestWeb request,
        CostEstimateTemplate template,
        CancellationToken cancellationToken)
    {
        AgentContext context = new();

        // Krok 1: Zaplanuj strukturę grup
        string plannerMessage = BuildPlannerMessage(request);
        AgentRunResult planResult = await _agentRunner.RunAsync(
            "cost-estimate-planner", plannerMessage, context, cancellationToken);

        if (!planResult.IsSuccess)
        {
            _logger.LogWarning("cost-estimate-planner failed: {Error}", planResult.ErrorMessage);
            return EmptyPreview(template.Id, "Planer kosztorysu nie zwrócił odpowiedzi.");
        }

        (string suggestedName, string? suggestedDescription, List<GroupStub> groupPlan) =
            ParseGroupPlan(planResult.Response);

        if (groupPlan.Count == 0)
        {
            return EmptyPreview(template.Id, "Planer nie zwrócił listy grup.");
        }

        // Krok 2: Generuj każdą grupę równolegle
        string templateSchema = BuildTemplateSchema(template);

        IEnumerable<Task<AIGroupPreviewWeb?>> groupTasks = groupPlan.Select(async (stub, i) =>
        {
            string groupMessage = BuildGroupGeneratorMessage(
                stub, i + 1, groupPlan.Count, templateSchema, request);

            AgentRunResult groupResult = await _agentRunner.RunAsync(
                "cost-estimate-group-generator",
                groupMessage,
                context.CreateSubAgentContext(),
                cancellationToken);

            if (!groupResult.IsSuccess)
            {
                _logger.LogWarning(
                    "cost-estimate-group-generator failed for '{Name}': {Error}",
                    stub.Name, groupResult.ErrorMessage);
                return null;
            }

            return ParseSingleGroup(groupResult.Response);
        });

        AIGroupPreviewWeb?[] groupResults = await Task.WhenAll(groupTasks);
        List<AIGroupPreviewWeb> groups = groupResults.OfType<AIGroupPreviewWeb>().ToList();

        AICostEstimatePreviewWeb preview = new()
        {
            TemplateId = template.Id,
            SuggestedName = suggestedName,
            SuggestedDescription = suggestedDescription,
            Groups = groups,
            Warnings = []
        };

        return RemoveInvalidFieldValues(preview, template);
    }

    private sealed record GroupStub(string TempId, string Name, int Order);

    private sealed record FieldRow(string id, string lbl, string key);

    private sealed record GroupPlanJson(string? SuggestedName, string? SuggestedDescription, List<GroupStubJson>? Groups);
    private sealed record GroupStubJson(string? TempId, string? Name, int? Order);

    private static string BuildPlannerMessage(AICostEstimateRequestWeb request)
    {
        int minGroups = BuildGroupMinCount(request);
        StringBuilder sb = new();
        sb.AppendLine("KOSZTORYS:");
        sb.AppendLine($"typ:{request.InvestmentType}");
        if (!string.IsNullOrEmpty(request.FinishingStandard))
            sb.AppendLine($"wykończenie:{request.FinishingStandard}");
        if (request.Budget.HasValue)
            sb.AppendLine($"budżet:{request.Budget:F0}PLN");
        if (request.Area.HasValue)
            sb.AppendLine($"pow:{request.Area}{request.AreaUnit ?? "m²"}");
        if (!string.IsNullOrEmpty(request.Location))
            sb.AppendLine($"lok:{request.Location}");
        if (request.CompletionYear.HasValue)
            sb.AppendLine($"rok:{request.CompletionYear}");
        if (!string.IsNullOrEmpty(request.AdditionalRequirements))
            sb.AppendLine($"wymag:{request.AdditionalRequirements}");
        sb.AppendLine($"WYMAGANA_LICZBA_GRUP:{minGroups}");
        sb.AppendLine("""Zwróć JSON: {"suggestedName":"...","suggestedDescription":"...","groups":[{"tempId":"g1","name":"...","order":1},...]}""");
        return sb.ToString();
    }

    private static string BuildTemplateSchema(CostEstimateTemplate template)
    {
        string groupNameGuid  = FindFieldIdByType(template, FieldType.GroupName)?.ToString() ?? string.Empty;
        string itemNameGuid   = FindFieldIdByType(template, FieldType.ItemSystemName)?.ToString() ?? string.Empty;
        string qtyGuid        = FindFieldIdByType(template, FieldType.ItemSystemQuantity)?.ToString() ?? string.Empty;
        string unitGuid       = FindFieldIdByType(template, FieldType.ItemSystemUnit)?.ToString() ?? string.Empty;
        string categoryGuid   = FindFieldIdByType(template, FieldType.ItemSystemCategory)?.ToString() ?? string.Empty;
        string priceGuid      = FindFieldIdByType(template, FieldType.ItemCalculatedUnitPriceNet)?.ToString() ?? string.Empty;
        string vatGuid        = FindFieldIdByType(template, FieldType.ItemCalculatedVatRate)?.ToString() ?? string.Empty;
        string priceGrossGuid = FindFieldIdByType(template, FieldType.ItemCalculatedUnitPriceGross)?.ToString() ?? string.Empty;
        string valueNetGuid   = FindFieldIdByType(template, FieldType.ItemCalculatedValueNet)?.ToString() ?? string.Empty;
        string valueGrossGuid = FindFieldIdByType(template, FieldType.ItemCalculatedValueGross)?.ToString() ?? string.Empty;
        string unitVatGuid    = FindFieldIdByType(template, FieldType.ItemCalculatedUnitVat)?.ToString() ?? string.Empty;
        string totalVatGuid   = FindFieldIdByType(template, FieldType.ItemCalculatedTotalVat)?.ToString() ?? string.Empty;

        StringBuilder sb = new();
        sb.AppendLine($"SZABLON:{template.Name}|Grupy:{(template.CanAddGroups ? "T" : "N")}|Podgrupy:{(template.CanBranchGroups ? "T" : "N")}{(template.MaxGroupLevel.HasValue ? $"|Max:{template.MaxGroupLevel}" : "")}");
        sb.AppendLine("POLA(role,guid,vk; guid=fieldDefinitionId):");
        var writableFields = new List<object>();
        if (!string.IsNullOrEmpty(groupNameGuid))  writableFields.Add(new { role = "group_name",  guid = groupNameGuid,  vk = "stringValue" });
        if (!string.IsNullOrEmpty(itemNameGuid))   writableFields.Add(new { role = "item_name",   guid = itemNameGuid,   vk = "stringValue" });
        if (!string.IsNullOrEmpty(qtyGuid))        writableFields.Add(new { role = "qty",          guid = qtyGuid,        vk = "decimalValue" });
        if (!string.IsNullOrEmpty(unitGuid))       writableFields.Add(new { role = "unit",         guid = unitGuid,       vk = "stringValue" });
        if (!string.IsNullOrEmpty(priceGuid))      writableFields.Add(new { role = "price_net",    guid = priceGuid,      vk = "decimalValue" });
        if (!string.IsNullOrEmpty(priceGrossGuid)) writableFields.Add(new { role = "price_gross",  guid = priceGrossGuid, vk = "decimalValue=price_net*(1+vat)" });
        if (!string.IsNullOrEmpty(vatGuid))        writableFields.Add(new { role = "vat_rate",     guid = vatGuid,        vk = "decimalValue:0.08=8%,0.23=23%" });
        if (!string.IsNullOrEmpty(categoryGuid))   writableFields.Add(new { role = "category",     guid = categoryGuid,   vk = "stringValue" });
        sb.AppendLine(Toon.Encode(writableFields.ToArray()));

        var readonlyFields = new List<object>();
        if (!string.IsNullOrEmpty(valueNetGuid))   readonlyFields.Add(new { role = "value_net_READONLY",   guid = valueNetGuid });
        if (!string.IsNullOrEmpty(valueGrossGuid)) readonlyFields.Add(new { role = "value_gross_READONLY", guid = valueGrossGuid });
        if (!string.IsNullOrEmpty(unitVatGuid))    readonlyFields.Add(new { role = "unit_vat_READONLY",    guid = unitVatGuid });
        if (!string.IsNullOrEmpty(totalVatGuid))   readonlyFields.Add(new { role = "total_vat_READONLY",   guid = totalVatGuid });
        if (readonlyFields.Count > 0)
        {
            sb.AppendLine("READONLY(system oblicza,NIE wpisuj):");
            sb.AppendLine(Toon.Encode(readonlyFields.ToArray()));
        }

        FieldRow[] groupFldArr = BuildFieldRows(
            template.GroupFieldDefinitions.Cast<CostEstimateTemplateFieldDefinitionBase>());
        if (groupFldArr.Length > 0)
        {
            sb.AppendLine("POLA_GRUPY(id,lbl,vk):");
            sb.AppendLine(Toon.Encode(groupFldArr));
        }

        FieldRow[] itemFldArr = BuildFieldRows(
            template.SystemFieldDefinitions
                .Where(f => !f.ParentFieldId.HasValue)
                .Cast<CostEstimateTemplateFieldDefinitionBase>()
            .Concat(template.CalculatedFieldDefinitions
                .Where(f => !f.IsReadonly)
                .Cast<CostEstimateTemplateFieldDefinitionBase>())
            .Concat(template.GenericFieldDefinitions
                .Cast<CostEstimateTemplateFieldDefinitionBase>()));
        if (itemFldArr.Length > 0)
        {
            sb.AppendLine("POLA_POZ(id,lbl,vk):");
            sb.AppendLine(Toon.Encode(itemFldArr));
        }

        if (template.Units.Any())
        {
            sb.AppendLine("JEDN(sym,n):");
            sb.AppendLine(Toon.Encode(
                template.Units.Select(u => new { sym = u.Symbol, n = u.Name }).ToArray()));
        }
        return sb.ToString();
    }

    private static string BuildGroupGeneratorMessage(
        GroupStub stub,
        int groupIndex,
        int totalGroups,
        string templateSchema,
        AICostEstimateRequestWeb request)
    {
        decimal? budgetPerGroup = request.Budget.HasValue
            ? Math.Round(request.Budget.Value / totalGroups, 0)
            : null;

        StringBuilder sb = new();
        sb.AppendLine($"GRUPA: {stub.Name} ({groupIndex}/{totalGroups})");
        sb.AppendLine($"tempId:{stub.TempId}|order:{stub.Order}");
        if (budgetPerGroup.HasValue)
            sb.AppendLine($"BUDŻET_GRUPY:~{budgetPerGroup:F0}PLN");
        if (request.Area.HasValue)
        {
            string areaUnit = request.AreaUnit ?? "m²";
            sb.AppendLine($"POW:{request.Area}{areaUnit}");
            if (areaUnit == "m²")
            {
                decimal area = request.Area.Value;
                sb.AppendLine($"Szac.ilości: tynki≈{area * 3:F0}m², wylewki≈{area:F0}m², podłogi≈{area:F0}m², elewacja≈{area * 2:F0}m², dach≈{area * 1.4m:F0}m²");
            }
        }
        AppendLocationPricingHint(sb, request.Location);
        if (request.CompletionYear.HasValue && request.CompletionYear.Value > 2026)
        {
            int yearsAhead = request.CompletionYear.Value - 2026;
            sb.AppendLine($"ROK {request.CompletionYear}: ceny+{yearsAhead * 3}-{yearsAhead * 5}% vs 2026.");
        }
        sb.AppendLine();
        sb.AppendLine(templateSchema);
        sb.AppendLine("""Zwróć JSON jednej grupy: {"tempId":"...","name":"...","fieldValues":[...],"items":[...]}""");
        return sb.ToString();
    }

    private (string SuggestedName, string? SuggestedDescription, List<GroupStub> Groups) ParseGroupPlan(string rawJson)
    {
        string json = ExtractJson(rawJson);
        if (string.IsNullOrWhiteSpace(json)) return ("Kosztorys AI", null, []);
        try
        {
            GroupPlanJson? dto = JsonSerializer.Deserialize<GroupPlanJson>(json, _jsonOptions);
            if (dto?.Groups is null) return (dto?.SuggestedName ?? "Kosztorys AI", dto?.SuggestedDescription, []);

            List<GroupStub> groups = [];
            for (int i = 0; i < dto.Groups.Count; i++)
            {
                GroupStubJson g = dto.Groups[i];
                if (string.IsNullOrEmpty(g.Name)) continue;
                groups.Add(new GroupStub(
                    g.TempId ?? $"g{i + 1}",
                    g.Name,
                    g.Order ?? i + 1));
            }
            return (dto.SuggestedName ?? "Kosztorys AI", dto.SuggestedDescription, groups);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse group plan JSON");
            return ("Kosztorys AI", null, []);
        }
    }

    private AIGroupPreviewWeb? ParseSingleGroup(string rawJson)
    {
        string json = ExtractJson(rawJson);
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<AIGroupPreviewWeb>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse single group JSON");
            return null;
        }
    }

    private static string ExtractJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        int firstBrace = raw.IndexOf('{');
        int lastBrace = raw.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace >= firstBrace)
            return raw[firstBrace..(lastBrace + 1)];
        return raw;
    }

    private static AICostEstimatePreviewWeb EmptyPreview(Guid templateId, string warning)
        => new()
        {
            TemplateId = templateId,
            SuggestedName = "Kosztorys AI",
            Groups = [],
            Warnings = [warning]
        };

    private static FieldRow[] BuildFieldRows(IEnumerable<CostEstimateTemplateFieldDefinitionBase> defs)
        => defs
            .Select(f => (f, cfg: CostEstimateFieldTypeHelper.GetFieldTypeConfig(f.FieldType)))
            .Where(x => x.cfg is not null && !x.cfg.IsCollection && !x.cfg.IsFile)
            .Select(x => new FieldRow(x.f.Id.ToString(), x.f.Label, GetValueKey(x.cfg!)))
            .ToArray();

    /// <summary>Zwraca Id (PK encji) pierwszego pola z danym FieldType we wszystkich kolekcjach szablonu.</summary>
    private static Guid? FindFieldIdByType(CostEstimateTemplate template, FieldType fieldType)
    {
        return template.GroupFieldDefinitions
            .Cast<CostEstimateTemplateFieldDefinitionBase>()
            .Concat(template.SystemFieldDefinitions)
            .Concat(template.CalculatedFieldDefinitions)
            .Concat(template.GenericFieldDefinitions)
            .FirstOrDefault(f => f.FieldType == fieldType)?.Id;
    }


    private static void AppendLocationPricingHint(StringBuilder sb, string? location)
    {
        if (string.IsNullOrEmpty(location))
            return;

        string loc = location.ToLowerInvariant();
        bool isMetropolia = loc.Contains("warszawa") || loc.Contains("sopot") || loc.Contains("trójmiasto") || loc.Contains("trojmiasto");
        bool isDuzeMiasto = loc.Contains("kraków") || loc.Contains("krakow") || loc.Contains("gdańsk") || loc.Contains("gdansk")
            || loc.Contains("gdynia") || loc.Contains("wrocław") || loc.Contains("wroclaw") || loc.Contains("poznań") || loc.Contains("poznan");
        bool isSrednieMiasto = loc.Contains("łódź") || loc.Contains("lodz") || loc.Contains("lublin")
            || loc.Contains("katowice") || loc.Contains("białystok") || loc.Contains("bialystok")
            || loc.Contains("szczecin") || loc.Contains("bydgoszcz") || loc.Contains("rzeszów") || loc.Contains("rzeszow")
            || loc.Contains("toruń") || loc.Contains("torun") || loc.Contains("kielce") || loc.Contains("olsztyn");

        if (isMetropolia)
            sb.AppendLine($"LOK {location}: metropolia,robocizna+25-35%.");
        else if (isDuzeMiasto)
            sb.AppendLine($"LOK {location}: duże miasto,robocizna+15-25%.");
        else if (isSrednieMiasto)
            sb.AppendLine($"LOK {location}: śred.miasto,robocizna±0%.");
        else
            sb.AppendLine($"LOK {location}: małe,robocizna-5-15%.");

        sb.AppendLine();
    }


    private static AICostEstimatePreviewWeb RemoveInvalidFieldValues(
        AICostEstimatePreviewWeb preview,
        CostEstimateTemplate template)
    {
        Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allFieldDefs = BuildFieldDefDictionary(template);

        List<AIGroupPreviewWeb> cleanGroups = preview.Groups.Select(g =>
        {
            List<AIFieldValueWeb> cleanGroupFields = g.FieldValues
                .Where(fv => IsValidFieldValue(fv, allFieldDefs))
                .ToList();

            List<AIItemPreviewWeb> cleanItems = g.Items.Select(i =>
            {
                List<AIFieldValueWeb> cleanItemFields = i.FieldValues
                    .Where(fv => IsValidFieldValue(fv, allFieldDefs))
                    .ToList();

                List<AIComponentPreviewWeb> cleanComponents = i.Components.Select(c =>
                    c with { FieldValues = c.FieldValues.Where(fv => IsValidFieldValue(fv, allFieldDefs)).ToList() }
                ).ToList();

                return i with { FieldValues = cleanItemFields, Components = cleanComponents };
            }).ToList();

            return g with { FieldValues = cleanGroupFields, Items = cleanItems };
        }).ToList();

        return preview with { Groups = cleanGroups };
    }

    private static bool IsValidFieldValue(
        AIFieldValueWeb fv,
        Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allFieldDefs)
    {
        if (!allFieldDefs.TryGetValue(fv.FieldDefinitionId, out CostEstimateTemplateFieldDefinitionBase? fieldDef))
            return false;

        Interfaces.WebModels.CostEstimateTemplates.CostEstimateFieldTypeConfigWeb? typeConfig =
            CostEstimateFieldTypeHelper.GetFieldTypeConfig(fieldDef.FieldType);
        if (typeConfig is null)
            return false;
        if (typeConfig.IsCollection || typeConfig.IsFile)
            return false;

        return true;
    }

    private static Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> BuildFieldDefDictionary(
        CostEstimateTemplate template)
    {
        return template.GroupFieldDefinitions
            .Cast<CostEstimateTemplateFieldDefinitionBase>()
            .Concat(template.SystemFieldDefinitions)
            .Concat(template.CalculatedFieldDefinitions)
            .Concat(template.GenericFieldDefinitions)
            .ToDictionary(f => f.Id);
    }

    private static string GetValueKey(Interfaces.WebModels.CostEstimateTemplates.CostEstimateFieldTypeConfigWeb cfg)
    {
        if (cfg.IsNumeric) return "decimalValue";
        if (cfg.IsBoolean) return "boolValue";
        if (cfg.IsDate) return "dateTimeValue";
        return "stringValue";
    }

    private static int BuildGroupMinCount(AICostEstimateRequestWeb request)
    {
        string inv = (request.InvestmentType ?? string.Empty).ToLowerInvariant();
        string fin = (request.FinishingStandard ?? string.Empty).ToLowerInvariant();
        bool podKlucz = fin.Contains("klucz") || inv.Contains("pod klucz") || inv.Contains("pod-klucz");
        bool deweloper = fin.Contains("deweloper") || fin.Contains("standard");
        bool isDom = inv.Contains("dom") || inv.Contains("budow") || inv.Contains("willa") || inv.Contains("budynek mieszk");
        bool isMieszkanie = inv.Contains("mieszkan") || inv.Contains("apartament");
        if (isDom && podKlucz) return 18;
        if (isDom && deweloper) return 11;
        if (isDom) return 6;
        if (isMieszkanie) return 8;
        return 8;
    }
}
