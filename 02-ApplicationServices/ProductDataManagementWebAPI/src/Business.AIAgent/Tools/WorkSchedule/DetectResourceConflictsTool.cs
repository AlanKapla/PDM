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
/// Tool that detects resource (team member) assignment conflicts
/// Identifies when the same person is assigned to overlapping work periods
/// </summary>
public sealed class DetectResourceConflictsTool : ToolBase
{
    private readonly IReadRepository<Entities.Models.WorkSchedule> workScheduleRepo;
    private readonly ILogger<DetectResourceConflictsTool> logger;

    public DetectResourceConflictsTool(
        IReadRepository<Entities.Models.WorkSchedule> workScheduleRepo,
        ILogger<DetectResourceConflictsTool> logger)
    {
        this.workScheduleRepo = workScheduleRepo;
        this.logger = logger;
    }

    public override string Name => "detect_resource_conflicts";

    public override string Description =>
        "Detects when team members are assigned to multiple works with overlapping time periods. " +
        "Identifies overallocated resources and provides suggestions for reallocation.";

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
            var args = JsonSerializer.Deserialize<DetectResourceConflictsArgs>(arguments);
            if (args == null || !Guid.TryParse(args.WorkScheduleId, out var workScheduleId) ||
                !Guid.TryParse(args.TenantId, out var tenantId) ||
                !Guid.TryParse(args.ProjectId, out var projectId))
            {
                return ToolResult.Failure(string.Empty, Name, "Invalid arguments", stopwatch.ElapsedMilliseconds);
            }

            logger.LogDebug("Detecting resource conflicts in work schedule {WorkScheduleId}", workScheduleId);

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

            var conflicts = new List<object>();
            var userWorkloads = new Dictionary<Guid, List<WorkAssignment>>();

            // Build user workload map
            foreach (var stage in workSchedule.Stages)
            {
                foreach (var work in stage.Works)
                {
                    if (!work.Periods.Any()) continue;

                    foreach (var assignment in work.Assignments)
                    {
                        if (!userWorkloads.ContainsKey(assignment.UserId))
                        {
                            userWorkloads[assignment.UserId] = new List<WorkAssignment>();
                        }

                        foreach (var period in work.Periods)
                        {
                            userWorkloads[assignment.UserId].Add(new WorkAssignment
                            {
                                UserId = assignment.UserId,
                                UserName = $"{assignment.ProjectMember.TenantMember.User.FirstName} {assignment.ProjectMember.TenantMember.User.LastName}".Trim(),
                                StageName = stage.Name,
                                WorkName = work.Name,
                                WorkId = work.Id,
                                StartDate = period.StartDate,
                                EndDate = period.EndDate
                            });
                        }
                    }
                }
            }

            // Detect conflicts for each user
            foreach (var kvp in userWorkloads)
            {
                var userId = kvp.Key;
                var assignments = kvp.Value.OrderBy(a => a.StartDate).ToList();

                // Check for overlapping assignments
                for (int i = 0; i < assignments.Count; i++)
                {
                    for (int j = i + 1; j < assignments.Count; j++)
                    {
                        var assign1 = assignments[i];
                        var assign2 = assignments[j];

                        // Check if periods overlap
                        if (assign1.EndDate >= assign2.StartDate && assign1.StartDate <= assign2.EndDate)
                        {
                            var overlapStart = assign1.StartDate > assign2.StartDate ? assign1.StartDate : assign2.StartDate;
                            var overlapEnd = assign1.EndDate < assign2.EndDate ? assign1.EndDate : assign2.EndDate;
                            var overlapDays = (overlapEnd - overlapStart).Days + 1;

                            // Calculate total workload percentage (assuming each work = 100% during period)
                            var totalWorkload = 200; // Two overlapping works = 200% workload

                            conflicts.Add(new
                            {
                                type = "resource_overallocation",
                                severity = overlapDays > 5 ? "high" : "medium",
                                user_id = userId,
                                user_name = assign1.UserName,
                                work1_name = assign1.WorkName,
                                work1_stage = assign1.StageName,
                                work1_period = $"{assign1.StartDate:yyyy-MM-dd} to {assign1.EndDate:yyyy-MM-dd}",
                                work2_name = assign2.WorkName,
                                work2_stage = assign2.StageName,
                                work2_period = $"{assign2.StartDate:yyyy-MM-dd} to {assign2.EndDate:yyyy-MM-dd}",
                                overlap_period = $"{overlapStart:yyyy-MM-dd} to {overlapEnd:yyyy-MM-dd}",
                                overlap_days = overlapDays,
                                workload_percentage = totalWorkload,
                                description = $"{assign1.UserName} is assigned to '{assign1.WorkName}' and '{assign2.WorkName}' during overlapping periods ({overlapDays} days)",
                                recommendation = $"Consider reassigning one of the works or adjusting time periods to avoid {overlapDays}-day overlap"
                            });
                        }
                    }
                }
            }

            // Calculate overall user utilization
            var userUtilization = userWorkloads.Select(kvp => new
            {
                user_id = kvp.Key,
                user_name = kvp.Value.FirstOrDefault()?.UserName ?? "Unknown",
                total_assignments = kvp.Value.Count,
                total_days_assigned = kvp.Value.Sum(a => (a.EndDate - a.StartDate).Days + 1),
                earliest_start = kvp.Value.Min(a => a.StartDate).ToString("yyyy-MM-dd"),
                latest_end = kvp.Value.Max(a => a.EndDate).ToString("yyyy-MM-dd"),
                works_assigned = kvp.Value.Select(a => a.WorkName).Distinct().ToList(),
                conflicts_count = conflicts.Count(c => ((dynamic)c).user_id == kvp.Key)
            }).OrderByDescending(u => u.conflicts_count).ToList();

            var response = new
            {
                work_schedule_id = workScheduleId,
                total_resource_conflicts = conflicts.Count,
                affected_users_count = conflicts.Select(c => ((dynamic)c).user_id).Distinct().Count(),
                conflicts_by_severity = new
                {
                    high = conflicts.Count(c => ((dynamic)c).severity == "high"),
                    medium = conflicts.Count(c => ((dynamic)c).severity == "medium"),
                    low = conflicts.Count(c => ((dynamic)c).severity == "low")
                },
                user_utilization_summary = userUtilization,
                conflicts = conflicts
            };

            var resultJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            stopwatch.Stop();

            logger.LogInformation("Detected {ConflictCount} resource conflicts affecting {UserCount} users in work schedule {WorkScheduleId}",
                conflicts.Count, userUtilization.Count, workScheduleId);

            return ToolResult.Success(string.Empty, Name, resultJson, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error detecting resource conflicts");
            return ToolResult.Failure(string.Empty, Name, $"Error: {ex.Message}", stopwatch.ElapsedMilliseconds);
        }
    }

    private sealed class DetectResourceConflictsArgs
    {
        [JsonPropertyName("work_schedule_id")]
        public string WorkScheduleId { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;
    }

    private sealed class WorkAssignment
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public string WorkName { get; set; } = string.Empty;
        public Guid WorkId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
