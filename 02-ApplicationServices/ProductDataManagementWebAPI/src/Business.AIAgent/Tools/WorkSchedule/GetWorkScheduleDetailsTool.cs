using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Business.AIAgent.Interfaces;
using Business.AIAgent.Models;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositiories.Repository.Interfaces;

namespace Business.AIAgent.Tools.WorkSchedule;

/// <summary>
/// Tool that retrieves complete work schedule details with all stages, works, periods and assignments
/// Used by AI to understand the full structure of the schedule before analysis
/// </summary>
public sealed class GetWorkScheduleDetailsTool : ToolBase
{
    private readonly IReadRepository<Entities.Models.WorkSchedule> workScheduleRepo;
    private readonly ILogger<GetWorkScheduleDetailsTool> logger;

    public GetWorkScheduleDetailsTool(
        IReadRepository<Entities.Models.WorkSchedule> workScheduleRepo,
        ILogger<GetWorkScheduleDetailsTool> logger)
    {
        this.workScheduleRepo = workScheduleRepo;
        this.logger = logger;
    }

    public override string Name => "get_work_schedule_details";

    public override string Description =>
        "Retrieves complete work schedule details including all stages, works, time periods and assigned team members. " +
        "Use this first to understand the structure before analyzing conflicts or issues.";

    public override object GetParametersSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                work_schedule_id = new
                {
                    type = "string",
                    description = "The GUID of the work schedule to retrieve"
                },
                tenant_id = new
                {
                    type = "string",
                    description = "The tenant ID (for security validation)"
                },
                project_id = new
                {
                    type = "string",
                    description = "The project ID (for security validation)"
                }
            },
            required = new[] { "work_schedule_id", "tenant_id", "project_id" }
        };
    }

    public override async Task<ToolResult> ExecuteAsync(string arguments, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var args = JsonSerializer.Deserialize<GetWorkScheduleDetailsArgs>(arguments);
            if (args == null || !Guid.TryParse(args.WorkScheduleId, out var workScheduleId) ||
                !Guid.TryParse(args.TenantId, out var tenantId) ||
                !Guid.TryParse(args.ProjectId, out var projectId))
            {
                return ToolResult.Failure(string.Empty, Name, "Invalid arguments. Provide valid GUIDs.", stopwatch.ElapsedMilliseconds);
            }

            logger.LogDebug("Retrieving work schedule {WorkScheduleId} for tenant {TenantId}", workScheduleId, tenantId);

            // Fetch work schedule with all related data
            var workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == workScheduleId && ws.TenantId == tenantId && ws.ProjectId == projectId,
                cancellationToken,
                query => query
                    .Include(ws => ws.Stages.OrderBy(s => s.Order))
                        .ThenInclude(s => s.Works.OrderBy(w => w.Order))
                            .ThenInclude(w => w.Periods)
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Assignments)
                                .ThenInclude(a => a.ProjectMember)
                                    .ThenInclude(pm => pm.TenantMember)
                                        .ThenInclude(tm => tm.User)
            );

            if (workSchedule == null)
            {
                return ToolResult.Failure(string.Empty, Name, "Work schedule not found", stopwatch.ElapsedMilliseconds);
            }

            // Build comprehensive response
            var response = new
            {
                work_schedule_id = workSchedule.Id,
                name = workSchedule.Name,
                project_id = workSchedule.ProjectId,
                created_at = workSchedule.CreatedAt.ToString("O"),
                total_stages = workSchedule.Stages.Count,
                total_works = workSchedule.Stages.SelectMany(s => s.Works).Count(),
                stages = workSchedule.Stages.OrderBy(s => s.Order).Select(stage => new
                {
                    stage_id = stage.Id,
                    stage_name = stage.Name,
                    stage_order = stage.Order,
                    total_works = stage.Works.Count,
                    works = stage.Works.OrderBy(w => w.Order).Select(work => new
                    {
                        work_id = work.Id,
                        work_name = work.Name,
                        work_order = work.Order,
                        color = work.ColorRgb,
                        is_closed = work.IsClosed,
                        total_periods = work.Periods.Count,
                        periods = work.Periods.OrderBy(p => p.StartDate).Select(p => new
                        {
                            start_date = p.StartDate.ToString("yyyy-MM-dd"),
                            end_date = p.EndDate.ToString("yyyy-MM-dd"),
                            duration_days = (p.EndDate - p.StartDate).Days + 1,
                            is_closed = p.IsClosed
                        }).ToList(),
                        earliest_start = work.Periods.Any() ? work.Periods.Min(p => p.StartDate).ToString("yyyy-MM-dd") : null,
                        latest_end = work.Periods.Any() ? work.Periods.Max(p => p.EndDate).ToString("yyyy-MM-dd") : null,
                        total_assigned = work.Assignments.Count,
                        assigned_users = work.Assignments.Select(a => new
                        {
                            user_id = a.UserId,
                            user_name = $"{a.ProjectMember.TenantMember.User.FirstName} {a.ProjectMember.TenantMember.User.LastName}".Trim()
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            var resultJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            stopwatch.Stop();

            logger.LogInformation("Retrieved work schedule {WorkScheduleId} with {StageCount} stages and {WorkCount} works",
                workScheduleId, workSchedule.Stages.Count, workSchedule.Stages.SelectMany(s => s.Works).Count());

            return ToolResult.Success(string.Empty, Name, resultJson, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error retrieving work schedule details");
            return ToolResult.Failure(string.Empty, Name, $"Error: {ex.Message}", stopwatch.ElapsedMilliseconds);
        }
    }

    private sealed class GetWorkScheduleDetailsArgs
    {
        [JsonPropertyName("work_schedule_id")]
        public string WorkScheduleId { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;
    }
}
