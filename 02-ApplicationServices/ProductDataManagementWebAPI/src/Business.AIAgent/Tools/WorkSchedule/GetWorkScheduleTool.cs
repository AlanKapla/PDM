using Business.AIAgent.Tools.Base;
using Entities.Models.WorkSchedules;
using Repositories.Repository.Interfaces;
using System.Text.Json;

namespace Business.AIAgent.Tools.WorkSchedule;

public sealed class GetWorkScheduleTool : AgentToolBase
{
    private readonly IRepository<Entities.Models.WorkSchedules.WorkSchedule> _repo;

    public GetWorkScheduleTool(IRepository<Entities.Models.WorkSchedules.WorkSchedule> repo)
    {
        _repo = repo;
    }

    public override string Name => "get_work_schedule";

    public override string Description =>
        "Returns work schedules for a project with stage count and basic timeline information.";

    public override JsonElement ParametersSchema => BuildSchema("""
        {
          "type": "object",
          "properties": {
            "project_id": {
              "type": "string",
              "description": "UUID of the project"
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

        IEnumerable<Entities.Models.WorkSchedules.WorkSchedule> schedules = await _repo.GetBySearch(
            s => s.ProjectId == projectId && s.TenantId == context.TenantId && !s.IsDeleted);

        IEnumerable<object> result = schedules.Select(s => new
        {
            id = s.Id,
            name = s.Name,
            cost_estimate_id = s.CostEstimateId,
            created_at = s.CreatedAt,
            stage_count = s.Stages?.Count ?? 0
        });

        return ToolResult.Success(JsonSerializer.Serialize(result));
    }
}
