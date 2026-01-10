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
/// Tool that detects unassigned time periods and works without team members
/// Identifies gaps in resource allocation that need attention
/// </summary>
public sealed class DetectUnassignedPeriodsTool : ToolBase
{
    private readonly IReadRepository<Entities.Models.WorkSchedule> workScheduleRepo;
    private readonly ILogger<DetectUnassignedPeriodsTool> logger;

    public DetectUnassignedPeriodsTool(
        IReadRepository<Entities.Models.WorkSchedule> workScheduleRepo,
        ILogger<DetectUnassignedPeriodsTool> logger)
    {
        this.workScheduleRepo = workScheduleRepo;
        this.logger = logger;
    }

    public override string Name => "detect_unassigned_periods";

    public override string Description =>
        "Detects works that have no assigned team members or have time periods but insufficient staffing. " +
        "Helps identify resource allocation gaps that could delay the project.";

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
            var args = JsonSerializer.Deserialize<DetectUnassignedPeriodsArgs>(arguments);
            if (args == null || !Guid.TryParse(args.WorkScheduleId, out var workScheduleId) ||
                !Guid.TryParse(args.TenantId, out var tenantId) ||
                !Guid.TryParse(args.ProjectId, out var projectId))
            {
                return ToolResult.Failure(string.Empty, Name, "Invalid arguments", stopwatch.ElapsedMilliseconds);
            }

            logger.LogDebug("Detecting unassigned periods in work schedule {WorkScheduleId}", workScheduleId);

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
            );

            if (workSchedule == null)
            {
                return ToolResult.Failure(string.Empty, Name, "Work schedule not found", stopwatch.ElapsedMilliseconds);
            }

            var issues = new List<object>();
            var totalWorks = 0;
            var worksWithNoAssignments = 0;
            var worksWithNoPeriods = 0;
            var worksWithIncompleteStaffing = 0;

            foreach (var stage in workSchedule.Stages.OrderBy(s => s.Order))
            {
                foreach (var work in stage.Works.OrderBy(w => w.Order))
                {
                    totalWorks++;

                    // Check if work has no time periods defined
                    if (!work.Periods.Any())
                    {
                        worksWithNoPeriods++;
                        issues.Add(new
                        {
                            type = "work_without_periods",
                            severity = "high",
                            stage_name = stage.Name,
                            work_id = work.Id,
                            work_name = work.Name,
                            work_order = work.Order,
                            description = $"Work '{work.Name}' has no time periods defined",
                            recommendation = "Define start and end dates for this work to enable proper scheduling"
                        });
                    }

                    // Check if work has no assigned team members
                    if (!work.Assignments.Any())
                    {
                        worksWithNoAssignments++;
                        
                        var severity = work.Periods.Any() ? "high" : "medium"; // Higher severity if periods exist
                        
                        issues.Add(new
                        {
                            type = "work_without_assignments",
                            severity,
                            stage_name = stage.Name,
                            work_id = work.Id,
                            work_name = work.Name,
                            work_order = work.Order,
                            has_periods = work.Periods.Any(),
                            total_period_days = work.Periods.Sum(p => (p.EndDate - p.StartDate).Days + 1),
                            period_range = work.Periods.Any() 
                                ? $"{work.Periods.Min(p => p.StartDate):yyyy-MM-dd} to {work.Periods.Max(p => p.EndDate):yyyy-MM-dd}"
                                : null,
                            description = $"Work '{work.Name}' has no team members assigned",
                            recommendation = "Assign at least one team member to this work to ensure it can be completed"
                        });
                    }
                    else if (work.Periods.Any())
                    {
                        // Check if work might be understaffed (heuristic: long duration with only 1 person)
                        var totalDays = work.Periods.Sum(p => (p.EndDate - p.StartDate).Days + 1);
                        var assignedCount = work.Assignments.Count;

                        // Flag if work spans more than 30 days but has only 1 person assigned
                        if (totalDays > 30 && assignedCount == 1)
                        {
                            worksWithIncompleteStaffing++;
                            
                            issues.Add(new
                            {
                                type = "potentially_understaffed_work",
                                severity = "low",
                                stage_name = stage.Name,
                                work_id = work.Id,
                                work_name = work.Name,
                                work_order = work.Order,
                                total_days = totalDays,
                                assigned_count = assignedCount,
                                period_range = $"{work.Periods.Min(p => p.StartDate):yyyy-MM-dd} to {work.Periods.Max(p => p.EndDate):yyyy-MM-dd}",
                                description = $"Work '{work.Name}' spans {totalDays} days but has only {assignedCount} person assigned",
                                recommendation = "Consider assigning additional team members for long-duration work"
                            });
                        }
                    }
                }

                // Check if entire stage has no works
                if (!stage.Works.Any())
                {
                    issues.Add(new
                    {
                        type = "empty_stage",
                        severity = "medium",
                        stage_id = stage.Id,
                        stage_name = stage.Name,
                        stage_order = stage.Order,
                        description = $"Stage '{stage.Name}' has no works defined",
                        recommendation = "Add works to this stage or remove it if not needed"
                    });
                }
            }

            var response = new
            {
                work_schedule_id = workScheduleId,
                total_issues = issues.Count,
                summary = new
                {
                    total_works = totalWorks,
                    works_without_periods = worksWithNoPeriods,
                    works_without_assignments = worksWithNoAssignments,
                    potentially_understaffed = worksWithIncompleteStaffing,
                    coverage_percentage = totalWorks > 0 
                        ? Math.Round(((double)(totalWorks - worksWithNoAssignments) / totalWorks) * 100, 2)
                        : 100.0
                },
                issues_by_severity = new
                {
                    high = issues.Count(i => ((dynamic)i).severity == "high"),
                    medium = issues.Count(i => ((dynamic)i).severity == "medium"),
                    low = issues.Count(i => ((dynamic)i).severity == "low")
                },
                issues = issues
            };

            var resultJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            stopwatch.Stop();

            logger.LogInformation("Detected {IssueCount} unassigned/understaffed issues in work schedule {WorkScheduleId}",
                issues.Count, workScheduleId);

            return ToolResult.Success(string.Empty, Name, resultJson, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error detecting unassigned periods");
            return ToolResult.Failure(string.Empty, Name, $"Error: {ex.Message}", stopwatch.ElapsedMilliseconds);
        }
    }

    private sealed class DetectUnassignedPeriodsArgs
    {
        [JsonPropertyName("work_schedule_id")]
        public string WorkScheduleId { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;
    }
}
