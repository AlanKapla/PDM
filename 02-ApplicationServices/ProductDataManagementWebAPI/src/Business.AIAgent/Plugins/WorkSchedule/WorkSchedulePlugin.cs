using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Business.AIAgent.Plugins.Base;
using Repositories.Repository.Interfaces;

namespace Business.AIAgent.Plugins.WorkSchedule;

/// <summary>
/// Plugin for Work Schedule analysis - provides comprehensive schedule insights
/// </summary>
public sealed class WorkSchedulePlugin : BasePlugin
{
    private readonly IReadRepository<Entities.Models.WorkSchedule> _workScheduleRepo;

    public WorkSchedulePlugin(
        IReadRepository<Entities.Models.WorkSchedule> workScheduleRepo,
        ILogger<WorkSchedulePlugin> logger) : base(logger)
    {
        _workScheduleRepo = workScheduleRepo;
    }

    [KernelFunction]
    [Description("Gets detailed information about a work schedule including stages, works, assignments and timeline")]
    public async Task<WorkScheduleDetailsDto?> GetWorkScheduleDetailsAsync(
        [Description("Work schedule unique identifier")] Guid workScheduleId,
        [Description("Tenant ID for multi-tenancy isolation")] Guid tenantId,
        [Description("Project ID")] Guid projectId,
        CancellationToken cancellationToken = default)
    {
        LogFunctionInvocation(nameof(GetWorkScheduleDetailsAsync), workScheduleId, tenantId, projectId);

        try
        {
            var schedule = await _workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == workScheduleId 
                   && ws.TenantId == tenantId 
                   && ws.ProjectId == projectId,
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
                                        .ThenInclude(tm => tm.User));

            if (schedule == null)
            {
                LogFunctionResult(nameof(GetWorkScheduleDetailsAsync), "Not found");
                return null;
            }

            var result = new WorkScheduleDetailsDto
            {
                Id = schedule.Id,
                Name = schedule.Name,
                CreatedAt = schedule.CreatedAt,
                StagesCount = schedule.Stages.Count,
                TotalWorks = schedule.Stages.Sum(s => s.Works.Count)
            };

            LogFunctionResult(nameof(GetWorkScheduleDetailsAsync), "Success");
            return result;
        }
        catch (Exception ex)
        {
            LogFunctionError(nameof(GetWorkScheduleDetailsAsync), ex);
            throw;
        }
    }

    [KernelFunction]
    [Description("Calculates comprehensive workload statistics including team utilization")]
    public async Task<WorkloadStatisticsDto?> CalculateWorkloadStatsAsync(
        [Description("Work schedule unique identifier")] Guid workScheduleId,
        [Description("Tenant ID for multi-tenancy isolation")] Guid tenantId,
        [Description("Project ID")] Guid projectId,
        CancellationToken cancellationToken = default)
    {
        LogFunctionInvocation(nameof(CalculateWorkloadStatsAsync), workScheduleId, tenantId, projectId);

        try
        {
            var schedule = await _workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == workScheduleId 
                   && ws.TenantId == tenantId 
                   && ws.ProjectId == projectId,
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
                                        .ThenInclude(tm => tm.User));

            if (schedule == null)
            {
                LogFunctionResult(nameof(CalculateWorkloadStatsAsync), "Not found");
                return null;
            }

            var allWorks = schedule.Stages.SelectMany(s => s.Works).ToList();

            var result = new WorkloadStatisticsDto
            {
                WorkScheduleId = workScheduleId,
                WorkScheduleName = schedule.Name,
                TotalWorks = allWorks.Count,
                WorksCompleted = allWorks.Count(w => w.IsClosed),
                WorksInProgress = allWorks.Count(w => !w.IsClosed),
                CompletionPercentage = allWorks.Any()
                    ? Math.Round((double)allWorks.Count(w => w.IsClosed) / allWorks.Count * 100, 2)
                    : 0.0
            };

            LogFunctionResult(nameof(CalculateWorkloadStatsAsync), "Success");
            return result;
        }
        catch (Exception ex)
        {
            LogFunctionError(nameof(CalculateWorkloadStatsAsync), ex);
            throw;
        }
    }
}

/// <summary>
/// DTO for work schedule details response
/// </summary>
public sealed class WorkScheduleDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int StagesCount { get; set; }
    public int TotalWorks { get; set; }
}

/// <summary>
/// DTO for workload statistics response
/// </summary>
public sealed class WorkloadStatisticsDto
{
    public Guid WorkScheduleId { get; set; }
    public string WorkScheduleName { get; set; } = string.Empty;
    public int TotalWorks { get; set; }
    public int WorksCompleted { get; set; }
    public int WorksInProgress { get; set; }
    public double CompletionPercentage { get; set; }
}
