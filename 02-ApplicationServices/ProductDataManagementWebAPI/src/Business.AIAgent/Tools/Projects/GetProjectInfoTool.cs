using Business.AIAgent.Tools.Base;
using Entities.Models.Projects;
using Repositories.Repository.Interfaces;
using System.Text.Json;

namespace Business.AIAgent.Tools.Projects;

public sealed class GetProjectInfoTool : AgentToolBase
{
    private readonly IRepository<Project> _projectRepo;
    private readonly IRepository<ProjectMember> _memberRepo;

    public GetProjectInfoTool(IRepository<Project> projectRepo, IRepository<ProjectMember> memberRepo)
    {
        _projectRepo = projectRepo;
        _memberRepo = memberRepo;
    }

    public override string Name => "get_project_info";

    public override string Description =>
        "Returns basic information about a project: name, budget, member count, creation date.";

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

        Project? project = await _projectRepo.GetFirstBySearch(
            p => p.Id == projectId && p.TenantId == context.TenantId);

        if (project is null)
        {
            return ToolResult.Failure($"Project '{projectId}' not found");
        }

        int memberCount = await _memberRepo.CountAsync(
            m => m.ProjectId == projectId && m.IsActive,
            cancellationToken);

        object result = new
        {
            id = project.Id,
            name = project.Name,
            is_active = project.IsActive,
            budget_net = project.BudgetNet,
            budget_gross = project.BudgetGross,
            created_at = project.CreatedAt,
            member_count = memberCount
        };

        return ToolResult.Success(JsonSerializer.Serialize(result));
    }
}
