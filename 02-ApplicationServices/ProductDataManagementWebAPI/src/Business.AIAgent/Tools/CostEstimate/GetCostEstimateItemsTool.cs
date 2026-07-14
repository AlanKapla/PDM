using Business.AIAgent.Tools.Base;
using Entities.Models.CostEstimates;
using Repositories.Repository.Interfaces;
using System.Text.Json;

namespace Business.AIAgent.Tools.CostEstimate;

public sealed class GetCostEstimateItemsTool : AgentToolBase
{
    private readonly IRepository<CostEstimateItem> _itemRepo;
    private readonly IRepository<CostEstimateGroup> _groupRepo;

    public GetCostEstimateItemsTool(
        IRepository<CostEstimateItem> itemRepo,
        IRepository<CostEstimateGroup> groupRepo)
    {
        _itemRepo = itemRepo;
        _groupRepo = groupRepo;
    }

    public override string Name => "get_cost_estimate_items";

    public override string Description =>
        "Returns items (positions) of a specific cost estimate with their values. Use after get_cost_estimate to drill down.";

    public override JsonElement ParametersSchema => BuildSchema("""
        {
          "type": "object",
          "properties": {
            "cost_estimate_id": {
              "type": "string",
              "description": "UUID of the cost estimate"
            },
            "limit": {
              "type": "integer",
              "description": "Max number of items to return (default 50)"
            }
          },
          "required": ["cost_estimate_id"]
        }
        """);

    public override async Task<ToolResult> ExecuteAsync(
        JsonElement arguments,
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        Guid? costEstimateId = GetGuid(arguments, "cost_estimate_id");
        if (costEstimateId is null)
        {
            return ToolResult.Failure("cost_estimate_id is required");
        }

        int limit = GetInt(arguments, "limit", 50);

        IEnumerable<CostEstimateItem> items = await _itemRepo.GetBySearch(
            i => i.CostEstimateId == costEstimateId);

        IEnumerable<CostEstimateGroup> groups = await _groupRepo.GetBySearch(
            g => g.CostEstimateId == costEstimateId);

        IEnumerable<object> result = items.Take(limit).Select(i => new
        {
            id = i.Id,
            name = i.Name,
            order = i.Order,
            net_value = i.NetValue,
            gross_value = i.GrossValue,
            group_id = i.GroupId,
            parent_item_id = i.ParentItemId,
            relation_type = i.RelationType.ToString()
        });

        IEnumerable<object> groupResult = groups.Select(g => new
        {
            id = g.Id,
            name = g.Name,
            parent_group_id = g.ParentGroupId
        });

        object response = new
        {
            groups = groupResult,
            items = result,
            total_items = items.Count()
        };

        return ToolResult.Success(JsonSerializer.Serialize(response));
    }
}
