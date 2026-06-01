using Business.AIAgent.Tools.Base;
using Entities.Models.CostEstimates;
using Repositories.Repository.Interfaces;
using System.Text.Json;

namespace Business.AIAgent.Tools.CostEstimate;

public sealed class GetCostEstimateTool : AgentToolBase
{
    private readonly IRepository<Entities.Models.CostEstimates.CostEstimate> _repo;

    public GetCostEstimateTool(IRepository<Entities.Models.CostEstimates.CostEstimate> repo)
    {
        _repo = repo;
    }

    public override string Name => "get_cost_estimate";

    public override string Description =>
        "Returns list of cost estimates for a project with summary (name, status, total values).";

    public override JsonElement ParametersSchema => BuildSchema("""
        {
          "type": "object",
          "properties": {
            "project_id": {
              "type": "string",
              "description": "UUID of the project"
            },
            "status": {
              "type": "string",
              "description": "Optional filter: Draft, Active, Archived"
            }
          },
          "required": ["project_id"]
        }
        """);

    public override async Task<ToolResult> ExecuteAsync(
        JsonElement arguments,
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        Guid? projectId = GetGuid(arguments, "project_id") ?? context.ProjectId;
        if (projectId is null)
        {
            return ToolResult.Failure("project_id is required");
        }

        string? statusFilter = GetString(arguments, "status");

        IEnumerable<Entities.Models.CostEstimates.CostEstimate> estimates = await _repo.GetBySearch(
            e => e.ProjectId == projectId && e.TenantId == context.TenantId && !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<CostEstimateStatus>(statusFilter, true, out CostEstimateStatus statusEnum))
        {
            estimates = estimates.Where(e => e.Status == statusEnum);
        }

        IEnumerable<object> result = estimates.Select(e => new
        {
            id = e.Id,
            name = e.Name,
            description = e.Description,
            status = e.Status.ToString(),
            total_net = e.TotalNet,
            total_gross = e.TotalGross,
            total_vat = e.TotalVat,
            created_at = e.CreatedAt,
            last_calculated_at = e.LastCalculatedAt
        });

        return ToolResult.Success(JsonSerializer.Serialize(result));
    }
}
