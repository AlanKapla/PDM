using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.WorkSchedules.AnalyzeWorkSchedule;

/// <summary>
/// Command to analyze work schedule using AI
/// AI will automatically detect conflicts, resource issues, and provide recommendations
/// </summary>
public sealed record AnalyzeWorkScheduleCommand(
    Guid TenantId,
    Guid ProjectId,
    Guid WorkScheduleId
) : IRequest<WorkScheduleAnalysisResponse>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}

/// <summary>
/// Response containing AI analysis of work schedule
/// </summary>
public sealed record WorkScheduleAnalysisResponse(
    string Summary,
    List<string> KeyFindings,
    List<string> Recommendations,
    List<ScheduleConflict> Conflicts,
    WorkloadSummary WorkloadSummary,
    int TokensUsed,
    long ExecutionTimeMs
);

/// <summary>
/// Detected schedule conflict
/// </summary>
public sealed record ScheduleConflict(
    string Type,
    string Severity,
    string Description,
    string Recommendation
);

/// <summary>
/// Summary of team workload
/// </summary>
public sealed record WorkloadSummary(
    int TotalTeamMembers,
    int OverloadedMembers,
    int UnderutilizedMembers,
    double AverageCompletionPercentage,
    int TotalDurationDays
);
