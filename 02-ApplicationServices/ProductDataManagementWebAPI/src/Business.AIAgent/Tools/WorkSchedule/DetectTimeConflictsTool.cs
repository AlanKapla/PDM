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
/// Tool that detects time period conflicts within the work schedule
/// Identifies overlapping periods in the same stage and works that should be sequential but overlap
/// </summary>
public sealed class DetectTimeConflictsTool : ToolBase
{
    private readonly IReadRepository<Entities.Models.WorkSchedule> workScheduleRepo;
    private readonly ILogger<DetectTimeConflictsTool> logger;

    public DetectTimeConflictsTool(
        IReadRepository<Entities.Models.WorkSchedule> workScheduleRepo,
        ILogger<DetectTimeConflictsTool> logger)
    {
        this.workScheduleRepo = workScheduleRepo;
        this.logger = logger;
    }

    public override string Name => "detect_time_conflicts";

    public override string Description =>
        "Detects time conflicts in work schedule: overlapping periods within works, " +
        "stages that overlap when they shouldn't, and gaps between sequential activities. " +
        "Returns list of conflicts with severity levels.";

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
            var args = JsonSerializer.Deserialize<DetectTimeConflictsArgs>(arguments);
            if (args == null || !Guid.TryParse(args.WorkScheduleId, out var workScheduleId) ||
                !Guid.TryParse(args.TenantId, out var tenantId) ||
                !Guid.TryParse(args.ProjectId, out var projectId))
            {
                return ToolResult.Failure(string.Empty, Name, "Invalid arguments", stopwatch.ElapsedMilliseconds);
            }

            logger.LogDebug("Detecting time conflicts in work schedule {WorkScheduleId}", workScheduleId);

            var workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == workScheduleId && ws.TenantId == tenantId && ws.ProjectId == projectId,
                cancellationToken,
                query => query
                    .Include(ws => ws.Stages.OrderBy(s => s.Order))
                        .ThenInclude(s => s.Works.OrderBy(w => w.Order))
                            .ThenInclude(w => w.Periods)
            );

            if (workSchedule == null)
            {
                return ToolResult.Failure(string.Empty, Name, "Work schedule not found", stopwatch.ElapsedMilliseconds);
            }

            var conflicts = new List<object>();

            // Analyze each stage
            foreach (var stage in workSchedule.Stages.OrderBy(s => s.Order))
            {
                foreach (var work in stage.Works.OrderBy(w => w.Order))
                {
                    // Check for overlapping periods within same work
                    var periods = work.Periods.OrderBy(p => p.StartDate).ToList();
                    
                    for (int i = 0; i < periods.Count; i++)
                    {
                        for (int j = i + 1; j < periods.Count; j++)
                        {
                            var period1 = periods[i];
                            var period2 = periods[j];

                            // Check if periods overlap
                            if (period1.EndDate >= period2.StartDate && period1.StartDate <= period2.EndDate)
                            {
                                var overlapStart = period1.StartDate > period2.StartDate ? period1.StartDate : period2.StartDate;
                                var overlapEnd = period1.EndDate < period2.EndDate ? period1.EndDate : period2.EndDate;
                                var overlapDays = (overlapEnd - overlapStart).Days + 1;
                                
                                conflicts.Add(new
                                {
                                    type = "overlapping_periods_in_work",
                                    severity = "high",
                                    stage_name = stage.Name,
                                    work_name = work.Name,
                                    period1_start = period1.StartDate.ToString("yyyy-MM-dd"),
                                    period1_end = period1.EndDate.ToString("yyyy-MM-dd"),
                                    period2_start = period2.StartDate.ToString("yyyy-MM-dd"),
                                    period2_end = period2.EndDate.ToString("yyyy-MM-dd"),
                                    overlap_days = overlapDays,
                                    description = $"Work '{work.Name}' has overlapping periods"
                                });
                            }
                        }
                    }

                    // Check for gaps between periods (potential missing time)
                    for (int i = 0; i < periods.Count - 1; i++)
                    {
                        var currentPeriod = periods[i];
                        var nextPeriod = periods[i + 1];

                        var gapDays = (nextPeriod.StartDate - currentPeriod.EndDate).Days - 1;

                        if (gapDays > 0)
                        {
                            conflicts.Add(new
                            {
                                type = "gap_between_periods",
                                severity = gapDays > 7 ? "medium" : "low",
                                stage_name = stage.Name,
                                work_name = work.Name,
                                period1_end = currentPeriod.EndDate.ToString("yyyy-MM-dd"),
                                period2_start = nextPeriod.StartDate.ToString("yyyy-MM-dd"),
                                gap_days = gapDays,
                                description = $"Gap of {gapDays} days between periods in work '{work.Name}'"
                            });
                        }
                    }
                }

                // Check for overlapping works within same stage
                var stageWorks = stage.Works.OrderBy(w => w.Order).ToList();
                
                for (int i = 0; i < stageWorks.Count - 1; i++)
                {
                    var work1 = stageWorks[i];
                    var work2 = stageWorks[i + 1];

                    if (!work1.Periods.Any() || !work2.Periods.Any()) continue;

                    var work1End = work1.Periods.Max(p => p.EndDate);
                    var work2Start = work2.Periods.Min(p => p.StartDate);

                    // Works should be sequential (based on order), check if they overlap
                    if (work2Start <= work1End)
                    {
                        var overlapDays = (work1End - work2Start).Days + 1;
                        
                        conflicts.Add(new
                        {
                            type = "sequential_works_overlap",
                            severity = "medium",
                            stage_name = stage.Name,
                            work1_name = work1.Name,
                            work1_order = work1.Order,
                            work2_name = work2.Name,
                            work2_order = work2.Order,
                            work1_latest_end = work1End.ToString("yyyy-MM-dd"),
                            work2_earliest_start = work2Start.ToString("yyyy-MM-dd"),
                            overlap_days = overlapDays,
                            description = $"Sequential works '{work1.Name}' and '{work2.Name}' overlap by {overlapDays} days"
                        });
                    }
                }
            }

            // Check for stage overlaps (stages should typically be sequential or parallel by design)
            var stages = workSchedule.Stages.OrderBy(s => s.Order).ToList();
            
            for (int i = 0; i < stages.Count - 1; i++)
            {
                var stage1 = stages[i];
                var stage2 = stages[i + 1];

                var stage1Works = stage1.Works.Where(w => w.Periods.Any()).ToList();
                var stage2Works = stage2.Works.Where(w => w.Periods.Any()).ToList();

                if (!stage1Works.Any() || !stage2Works.Any()) continue;

                var stage1End = stage1Works.SelectMany(w => w.Periods).Max(p => p.EndDate);
                var stage2Start = stage2Works.SelectMany(w => w.Periods).Min(p => p.StartDate);

                if (stage2Start < stage1End)
                {
                    conflicts.Add(new
                    {
                        type = "stage_overlap",
                        severity = "low",
                        stage1_name = stage1.Name,
                        stage1_order = stage1.Order,
                        stage2_name = stage2.Name,
                        stage2_order = stage2.Order,
                        stage1_latest_end = stage1End.ToString("yyyy-MM-dd"),
                        stage2_earliest_start = stage2Start.ToString("yyyy-MM-dd"),
                        overlap_days = (stage1End - stage2Start).Days + 1,
                        description = $"Stages '{stage1.Name}' and '{stage2.Name}' have overlapping timelines"
                    });
                }
            }

            var response = new
            {
                work_schedule_id = workScheduleId,
                total_conflicts = conflicts.Count,
                conflicts_by_severity = new
                {
                    high = conflicts.Count(c => ((dynamic)c).severity == "high"),
                    medium = conflicts.Count(c => ((dynamic)c).severity == "medium"),
                    low = conflicts.Count(c => ((dynamic)c).severity == "low")
                },
                conflicts = conflicts
            };

            var resultJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            stopwatch.Stop();

            logger.LogInformation("Detected {ConflictCount} time conflicts in work schedule {WorkScheduleId}",
                conflicts.Count, workScheduleId);

            return ToolResult.Success(string.Empty, Name, resultJson, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error detecting time conflicts");
            return ToolResult.Failure(string.Empty, Name, $"Error: {ex.Message}", stopwatch.ElapsedMilliseconds);
        }
    }

    private sealed class DetectTimeConflictsArgs
    {
        [JsonPropertyName("work_schedule_id")]
        public string WorkScheduleId { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;
    }
}
