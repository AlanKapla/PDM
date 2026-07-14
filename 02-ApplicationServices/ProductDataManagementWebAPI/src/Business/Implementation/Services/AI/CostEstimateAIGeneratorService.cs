using Business.AIAgent;
using Business.AIAgent.Abstractions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ToonFormat;

namespace Business.Implementation.Services.AI;

public sealed class CostEstimateAIGeneratorService : ICostEstimateAIGeneratorService
{
    private readonly IAgentRunner _agentRunner;
    private readonly ILogger<CostEstimateAIGeneratorService> _logger;

    // Limit concurrent LLM calls to avoid Azure OpenAI rate-limit (429) bursts
    private static readonly SemaphoreSlim _groupSemaphore = new(5, 5);

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
            return EmptyPreview("Planer kosztorysu nie zwrócił odpowiedzi.");
        }

        (string suggestedName, string? suggestedDescription, List<GroupStub> groupPlan) =
            ParseGroupPlan(planResult.Response);

        if (groupPlan.Count == 0)
        {
            return EmptyPreview("Planer nie zwrócił listy grup.");
        }

        // Krok 2: Generuj każdą grupę równolegle, ale złóż wynik wg kolejności z planera
        string templateSchema = BuildBasicFieldSchema();

        Task<GroupGenerationResult>[] groupTasks = groupPlan.Select(async (stub, index) =>
        {
            await _groupSemaphore.WaitAsync(cancellationToken);
            try
            {
                string groupMessage = BuildGroupGeneratorMessage(
                    stub, index + 1, groupPlan.Count, templateSchema, request);

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
                    return new GroupGenerationResult(stub, null);
                }

                AIGroupPreviewWeb? group = ParseSingleGroup(groupResult.Response);
                return new GroupGenerationResult(stub, group);
            }
            finally
            {
                _groupSemaphore.Release();
            }
        }).ToArray();

        GroupGenerationResult[] groupResults = await Task.WhenAll(groupTasks);
        (List<AIGroupPreviewWeb> groups, List<string> groupWarnings) =
            BuildOrderedGroups(groupResults);

        AICostEstimatePreviewWeb preview = new()
        {
            SuggestedName = suggestedName,
            SuggestedDescription = suggestedDescription,
            Groups = groups,
            Warnings = groupWarnings
        };

        return RemoveInvalidFieldValues(preview);
    }

    private sealed record GroupStub(string TempId, string Name, int Order);
    private sealed record GroupGenerationResult(GroupStub Stub, AIGroupPreviewWeb? Group);

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
        sb.AppendLine("KOLEJNOSC: groups w tablicy i pole order muszą odzwierciedlać chronologię robót budowlanych (np. fundamenty przed elektryką).");
        sb.AppendLine("""Zwróć JSON: {"suggestedName":"...","suggestedDescription":"...","groups":[{"tempId":"g1","name":"...","order":1},...]}""");
        return sb.ToString();
    }

    /// <summary>
    /// Buduje schemat podstawowych pól systemowych dla AI.
    /// Zawsze 9 standardowych pól: Name, Quantity, Unit, UnitPriceNet, VatRate, UnitPriceGross, ValueNet, ValueGross, TotalVat
    /// </summary>
    private static string BuildBasicFieldSchema()
    {
        // Stałe Guid dla podstawowych pól (takie same jak w CreateCostEstimateCommand przy tworzeniu default schema)
        // Te wartości będą używane przez AI do generowania fieldValues
        string groupNameFieldName = "00000000-0000-0000-0000-000000000001";  // GroupName
        string itemNameFieldName = "00000000-0000-0000-0000-000000000100";   // ItemSystemName
        string qtyFieldName = "00000000-0000-0000-0000-000000000101";        // ItemSystemQuantity
        string unitFieldName = "00000000-0000-0000-0000-000000000102";       // ItemSystemUnit
        string priceNetFieldName = "00000000-0000-0000-0000-000000000200";   // ItemCalculatedUnitPriceNet
        string vatFieldName = "00000000-0000-0000-0000-000000000201";        // ItemCalculatedVatRate
        string priceGrossFieldName = "00000000-0000-0000-0000-000000000202"; // ItemCalculatedUnitPriceGross
        string valueNetFieldName = "00000000-0000-0000-0000-000000000203";   // ItemCalculatedValueNet
        string valueGrossFieldName = "00000000-0000-0000-0000-000000000204"; // ItemCalculatedValueGross
        string totalVatFieldName = "00000000-0000-0000-0000-000000000205";   // ItemCalculatedTotalVat

        StringBuilder sb = new();
        sb.AppendLine("SCHEMAT:Podstawowy|Grupy:T|Podgrupy:T");
        sb.AppendLine("POLA(role,guid,vk; guid=fieldDefinitionId):");
        
        var writableFields = new List<object>
        {
            new { role = "group_name",  guid = groupNameFieldName,  vk = "stringValue" },
            new { role = "item_name",   guid = itemNameFieldName,   vk = "stringValue" },
            new { role = "qty",         guid = qtyFieldName,        vk = "decimalValue" },
            new { role = "unit",        guid = unitFieldName,       vk = "stringValue" },
            new { role = "price_net",   guid = priceNetFieldName,   vk = "decimalValue" },
            new { role = "vat_rate",    guid = vatFieldName,        vk = "decimalValue:0.08=8%,0.23=23%" },
            new { role = "price_gross", guid = priceGrossFieldName, vk = "decimalValue=price_net*(1+vat)" }
        };
        sb.AppendLine(Toon.Encode(writableFields.ToArray()));

        var readonlyFields = new List<object>
        {
            new { role = "value_net_READONLY",   guid = valueNetFieldName },
            new { role = "value_gross_READONLY", guid = valueGrossFieldName },
            new { role = "total_vat_READONLY",   guid = totalVatFieldName }
        };
        sb.AppendLine("READONLY(system oblicza,NIE wpisuj):");
        sb.AppendLine(Toon.Encode(readonlyFields.ToArray()));

        sb.AppendLine("JEDN(przykłady): m², m³, szt, mb, kg, t, m, godz, komplet");
        
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
            List<GroupStub> orderedGroups = groups
                .OrderBy(g => g.Order)
                .ThenBy(g => g.TempId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return (dto.SuggestedName ?? "Kosztorys AI", dto.SuggestedDescription, orderedGroups);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse group plan JSON");
            return ("Kosztorys AI", null, []);
        }
    }

    /// <summary>
    /// Składa grupy wg kolejności z planera — niezależnie od tego, w jakiej kolejności zakończyły się taski równoległe.
    /// </summary>
    private static (List<AIGroupPreviewWeb> Groups, List<string> Warnings) BuildOrderedGroups(
        IEnumerable<GroupGenerationResult> groupResults)
    {
        List<AIGroupPreviewWeb> orderedGroups = [];
        List<string> warnings = [];
        int order = 1;

        foreach (GroupGenerationResult result in groupResults
                     .OrderBy(r => r.Stub.Order)
                     .ThenBy(r => r.Stub.TempId, StringComparer.OrdinalIgnoreCase))
        {
            if (result.Group is null)
            {
                warnings.Add($"Pominięto grupę '{result.Stub.Name}' — generator nie zwrócił danych.");
                continue;
            }

            orderedGroups.Add(result.Group with
            {
                TempId = result.Stub.TempId,
                Name = string.IsNullOrWhiteSpace(result.Group.Name) ? result.Stub.Name : result.Group.Name,
                Order = order++
            });
        }

        return (orderedGroups, warnings);
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

    private static AICostEstimatePreviewWeb EmptyPreview(string warning)
        => new()
        {
            SuggestedName = "Kosztorys AI",
            Groups = [],
            Warnings = [warning]
        };

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


    /// <summary>
    /// Filtruje field values — zostawia tylko te które pasują do podstawowych pól (9 standardowych)
    /// </summary>
    private static AICostEstimatePreviewWeb RemoveInvalidFieldValues(AICostEstimatePreviewWeb preview)
    {
        // Stałe Guid dla podstawowych pól (takie same jak w BuildBasicFieldSchema)
        HashSet<Guid> validFieldNames = new()
        {
            Guid.Parse("00000000-0000-0000-0000-000000000001"),  // GroupName
            Guid.Parse("00000000-0000-0000-0000-000000000100"),  // ItemSystemName
            Guid.Parse("00000000-0000-0000-0000-000000000101"),  // ItemSystemQuantity
            Guid.Parse("00000000-0000-0000-0000-000000000102"),  // ItemSystemUnit
            Guid.Parse("00000000-0000-0000-0000-000000000200"),  // ItemCalculatedUnitPriceNet
            Guid.Parse("00000000-0000-0000-0000-000000000201"),  // ItemCalculatedVatRate
            Guid.Parse("00000000-0000-0000-0000-000000000202"),  // ItemCalculatedUnitPriceGross
            Guid.Parse("00000000-0000-0000-0000-000000000203"),  // ItemCalculatedValueNet
            Guid.Parse("00000000-0000-0000-0000-000000000204"),  // ItemCalculatedValueGross
            Guid.Parse("00000000-0000-0000-0000-000000000205")   // ItemCalculatedTotalVat
        };

        List<AIGroupPreviewWeb> cleanGroups = preview.Groups.Select(g =>
        {
            List<AIFieldValueWeb> cleanGroupFields = g.FieldValues
                .Where(fv => validFieldNames.Contains(fv.FieldDefinitionId))
                .ToList();

            List<AIItemPreviewWeb> cleanItems = g.Items.Select(i =>
            {
                List<AIFieldValueWeb> cleanItemFields = i.FieldValues
                    .Where(fv => validFieldNames.Contains(fv.FieldDefinitionId))
                    .ToList();

                List<AIComponentPreviewWeb> cleanComponents = i.Components.Select(c =>
                    c with { FieldValues = c.FieldValues.Where(fv => validFieldNames.Contains(fv.FieldDefinitionId)).ToList() }
                ).ToList();

                return i with { FieldValues = cleanItemFields, Components = cleanComponents };
            }).ToList();

            return g with { FieldValues = cleanGroupFields, Items = cleanItems };
        }).ToList();

        return preview with { Groups = cleanGroups };
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
