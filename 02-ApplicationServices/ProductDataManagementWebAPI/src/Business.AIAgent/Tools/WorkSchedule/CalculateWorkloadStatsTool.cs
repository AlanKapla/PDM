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
/// Tool that calculates workload statistics and team utilization metrics
/// Provides insights into resource distribution and identifies bottlenecks
/// </summary>
public sealed class CalculateWorkloadStatsTool : ToolBase
{
    private readonly IReadRepository<Entities.Models.WorkSchedule> workScheduleRepo;
    private readonly ILogger<CalculateWorkloadStatsTool> logger;

    public CalculateWorkloadStatsTool(
        IReadRepository<Entities.Models.WorkSchedule> workScheduleRepo,
        ILogger<CalculateWorkloadStatsTool> logger)
    {
        this.workScheduleRepo = workScheduleRepo;
        this.logger = logger;
    }

    public override string Name => "calculate_workload_stats";

    public override string Description =>
        "Calculates comprehensive workload statistics including team member utilization, " +
        "work distribution across stages, timeline analysis, and identifies overworked or underutilized resources.";

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
                    description = "The GUID of the work schedule to analyze"
                },
                tenant_id = new
                {
                    type = "string",
                    description = "The tenant ID"
                },
                project_id = new
                {
                    type = "string",
                    description = "The project ID"
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
            var args = JsonSerializer.Deserialize<CalculateWorkloadStatsArgs>(arguments);
            if (args == null || !Guid.TryParse(args.WorkScheduleId, out var workScheduleId) ||
                !Guid.TryParse(args.TenantId, out var tenantId) ||
                !Guid.TryParse(args.ProjectId, out var projectId))
            {
                return ToolResult.Failure(string.Empty, Name, "Invalid arguments", stopwatch.ElapsedMilliseconds);
            }

            logger.LogDebug("Calculating workload statistics for work schedule {WorkScheduleId}", workScheduleId);

            var workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == workScheduleId && ws.TenantId == tenantId && ws.ProjectId == projectId,
                cancellationToken,
                query => query
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
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

            // Overall schedule statistics
            var allWorks = workSchedule.Stages.SelectMany(s => s.Works).ToList();
            var worksWithPeriods = allWorks.Where(w => w.Periods.Any()).ToList();

            DateTime? scheduleStart = worksWithPeriods.Any() 
                ? worksWithPeriods.SelectMany(w => w.Periods).Min(p => p.StartDate)
                : null;
            
            DateTime? scheduleEnd = worksWithPeriods.Any()
                ? worksWithPeriods.SelectMany(w => w.Periods).Max(p => p.EndDate)
                : null;

            int totalDuration = scheduleStart.HasValue && scheduleEnd.HasValue
                ? (scheduleEnd.Value - scheduleStart.Value).Days + 1
                : 0;

            // Stage statistics
            var stageStats = workSchedule.Stages.OrderBy(s => s.Order).Select(stage =>
            {
                var stageWorks = stage.Works.ToList();
                var stageWorksWithPeriods = stageWorks.Where(w => w.Periods.Any()).ToList();

                var stageStart = stageWorksWithPeriods.Any()
                    ? stageWorksWithPeriods.SelectMany(w => w.Periods).Min(p => p.StartDate)
                    : (DateTime?)null;

                var stageEnd = stageWorksWithPeriods.Any()
                    ? stageWorksWithPeriods.SelectMany(w => w.Periods).Max(p => p.EndDate)
                    : (DateTime?)null;

                return new
                {
                    stage_name = stage.Name,
                    stage_order = stage.Order,
                    total_works = stageWorks.Count,
                    works_completed = stageWorks.Count(w => w.IsClosed),
                    works_in_progress = stageWorks.Count(w => !w.IsClosed),
                    completion_percentage = stageWorks.Any() 
                        ? Math.Round((double)stageWorks.Count(w => w.IsClosed) / stageWorks.Count * 100, 2)
                        : 0.0,
                    total_assigned_members = stageWorks.SelectMany(w => w.Assignments).Select(a => a.UserId).Distinct().Count(),
                    start_date = stageStart?.ToString("yyyy-MM-dd"),
                    end_date = stageEnd?.ToString("yyyy-MM-dd"),
                    duration_days = stageStart.HasValue && stageEnd.HasValue
                        ? (stageEnd.Value - stageStart.Value).Days + 1
                        : 0
                };
            }).ToList();

            // Team member workload analysis
            var userWorkloads = new Dictionary<Guid, UserWorkload>();

            foreach (var stage in workSchedule.Stages)
            {
                foreach (var work in stage.Works)
                {
                    foreach (var assignment in work.Assignments)
                    {
                        if (!userWorkloads.ContainsKey(assignment.UserId))
                        {
                            userWorkloads[assignment.UserId] = new UserWorkload
                            {
                                UserId = assignment.UserId,
                                UserName = $"{assignment.ProjectMember.TenantMember.User.FirstName} {assignment.ProjectMember.TenantMember.User.LastName}".Trim()
                            };
                        }

                        var userWorkload = userWorkloads[assignment.UserId];
                        userWorkload.TotalWorks++;

                        if (work.IsClosed)
                        {
                            userWorkload.CompletedWorks++;
                        }

                        userWorkload.TotalDays += work.Periods.Sum(p => (p.EndDate - p.StartDate).Days + 1);

                        if (work.Periods.Any())
                        {
                            var workStart = work.Periods.Min(p => p.StartDate);
                            var workEnd = work.Periods.Max(p => p.EndDate);

                            if (!userWorkload.EarliestStart.HasValue || workStart < userWorkload.EarliestStart)
                            {
                                userWorkload.EarliestStart = workStart;
                            }

                            if (!userWorkload.LatestEnd.HasValue || workEnd > userWorkload.LatestEnd)
                            {
                                userWorkload.LatestEnd = workEnd;
                            }
                        }
                    }
                }
            }

            var teamStats = userWorkloads.Values.Select(u => new
            {
                user_id = u.UserId,
                user_name = u.UserName,
                total_works_assigned = u.TotalWorks,
                completed_works = u.CompletedWorks,
                in_progress_works = u.TotalWorks - u.CompletedWorks,
                completion_percentage = u.TotalWorks > 0 
                    ? Math.Round((double)u.CompletedWorks / u.TotalWorks * 100, 2)
                    : 0.0,
                total_days_assigned = u.TotalDays,
                active_period_start = u.EarliestStart?.ToString("yyyy-MM-dd"),
                active_period_end = u.LatestEnd?.ToString("yyyy-MM-dd"),
                active_period_days = u.EarliestStart.HasValue && u.LatestEnd.HasValue
                    ? (u.LatestEnd.Value - u.EarliestStart.Value).Days + 1
                    : 0,
                utilization_status = u.TotalWorks switch
                {
                    0 => "not_assigned",
                    <= 2 => "underutilized",
                    <= 5 => "normal",
                    <= 8 => "busy",
                    _ => "overloaded"
                }
            }).OrderByDescending(u => u.total_works_assigned).ToList();

            // Timeline distribution
            var timelineDistribution = new List<object>();
            
            if (scheduleStart.HasValue && scheduleEnd.HasValue)
            {
                // Group works by month
                var monthlyGroups = worksWithPeriods
                    .SelectMany(w => w.Periods.Select(p => new { Work = w, Period = p }))
                    .GroupBy(x => new { Year = x.Period.StartDate.Year, Month = x.Period.StartDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

                foreach (var monthGroup in monthlyGroups)
                {
                    timelineDistribution.Add(new
                    {
                        year = monthGroup.Key.Year,
                        month = monthGroup.Key.Month,
                        month_name = new DateTime(monthGroup.Key.Year, monthGroup.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc).ToString("MMMM yyyy"),
                        works_starting = monthGroup.Count(),
                        unique_works = monthGroup.Select(x => x.Work.Id).Distinct().Count()
                    });
                }
            }

            var response = new
            {
                work_schedule_id = workScheduleId,
                work_schedule_name = workSchedule.Name,
                overall_stats = new
                {
                    total_stages = workSchedule.Stages.Count,
                    total_works = allWorks.Count,
                    works_completed = allWorks.Count(w => w.IsClosed),
                    works_in_progress = allWorks.Count(w => !w.IsClosed),
                    overall_completion_percentage = allWorks.Any()
                        ? Math.Round((double)allWorks.Count(w => w.IsClosed) / allWorks.Count * 100, 2)
                        : 0.0,
                    total_team_members = userWorkloads.Count,
                    schedule_start_date = scheduleStart?.ToString("yyyy-MM-dd"),
                    schedule_end_date = scheduleEnd?.ToString("yyyy-MM-dd"),
                    total_duration_days = totalDuration
                },
                stage_statistics = stageStats,
                team_workload_analysis = teamStats,
                workload_distribution = new
                {
                    not_assigned = teamStats.Count(t => t.utilization_status == "not_assigned"),
                    underutilized = teamStats.Count(t => t.utilization_status == "underutilized"),
                    normal = teamStats.Count(t => t.utilization_status == "normal"),
                    busy = teamStats.Count(t => t.utilization_status == "busy"),
                    overloaded = teamStats.Count(t => t.utilization_status == "overloaded")
                },
                timeline_distribution = timelineDistribution
            };

            var resultJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            stopwatch.Stop();

            logger.LogInformation("Calculated workload statistics for work schedule {WorkScheduleId}: {TeamCount} members, {WorkCount} works",
                workScheduleId, userWorkloads.Count, allWorks.Count);

            return ToolResult.Success(string.Empty, Name, resultJson, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error calculating workload statistics");
            return ToolResult.Failure(string.Empty, Name, $"Error: {ex.Message}", stopwatch.ElapsedMilliseconds);
        }
    }

    private sealed class CalculateWorkloadStatsArgs
    {
        [JsonPropertyName("work_schedule_id")]
        public string WorkScheduleId { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;
    }

    private sealed class UserWorkload
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TotalWorks { get; set; }
        public int CompletedWorks { get; set; }
        public int TotalDays { get; set; }
        public DateTime? EarliestStart { get; set; }
        public DateTime? LatestEnd { get; set; }
    }
}
