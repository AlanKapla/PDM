using System.Text.Json;
using System.Text.Json.Serialization;
using Business.AIAgent.Interfaces;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositiories.Repository.Interfaces;

namespace CQRS.WorkSchedules.AnalyzeWorkSchedule;

/// <summary>
/// Handler for AnalyzeWorkScheduleCommand
/// Uses AI Orchestrator with specialized work schedule analysis tools
/// </summary>
public sealed class AnalyzeWorkScheduleCommandHandler : IRequestHandler<AnalyzeWorkScheduleCommand, WorkScheduleAnalysisResponse>
{
    private readonly IOrchestrator orchestrator;
    private readonly ICurrentUser currentUser;
    private readonly IReadRepository<WorkSchedule> workScheduleRepo;
    private readonly ILogger<AnalyzeWorkScheduleCommandHandler> logger;

    public AnalyzeWorkScheduleCommandHandler(
        IOrchestrator orchestrator,
        ICurrentUser currentUser,
        IReadRepository<WorkSchedule> workScheduleRepo,
        ILogger<AnalyzeWorkScheduleCommandHandler> logger)
    {
        this.orchestrator = orchestrator;
        this.currentUser = currentUser;
        this.workScheduleRepo = workScheduleRepo;
        this.logger = logger;
    }

    public async Task<WorkScheduleAnalysisResponse> Handle(AnalyzeWorkScheduleCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Analyzing work schedule {WorkScheduleId} for tenant {TenantId}",
            request.WorkScheduleId, request.TenantId);

        // Verify work schedule exists and user has access
        var workSchedule = await workScheduleRepo.GetFirstBySearch(ws =>
            ws.Id == request.WorkScheduleId &&
            ws.TenantId == request.TenantId &&
            ws.ProjectId == request.ProjectId)

            ?? throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

        bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
        bool isOwner = workSchedule.CreatedByUserId == currentUser.Id;

        if (!isAdmin && !isOwner)
        {
            throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());
        }

        // Build comprehensive system prompt for AI
        var systemPrompt = $@"
You are a professional project management analyst specializing in construction scheduling and resource allocation.
Your task is to analyze the work schedule '{workSchedule.Name}' and provide actionable insights.

Analysis Instructions:
1. Use available tools to gather complete schedule data
2. Identify time conflicts (overlapping periods, sequential work issues)
3. Detect resource conflicts (team members assigned to overlapping work)
4. Find unassigned works or understaffed activities
5. Calculate workload statistics and identify bottlenecks
6. Provide specific, actionable recommendations

Return your analysis as a valid JSON object with this structure:
{{
  ""summary"": ""Brief executive summary (2-3 sentences) highlighting the most critical issues"",
  ""key_findings"": [
    ""Finding 1: Specific issue with data points"",
    ""Finding 2: Another specific issue"",
    ""..."" 
  ],
  ""recommendations"": [
    ""Recommendation 1: Actionable step to resolve an issue"",
    ""Recommendation 2: Another actionable step"",
    ""...""
  ],
  ""conflicts"": [
    {{
      ""type"": ""time_conflict|resource_conflict|unassigned_work"",
      ""severity"": ""high|medium|low"",
      ""description"": ""Detailed description of the conflict"",
      ""recommendation"": ""How to resolve this specific conflict""
    }}
  ],
  ""workload_summary"": {{
    ""total_team_members"": 0,
    ""overloaded_members"": 0,
    ""underutilized_members"": 0,
    ""average_completion_percentage"": 0.0,
    ""total_duration_days"": 0
  }}
}}

Focus on:
- High-severity conflicts that could delay the project
- Resource allocation inefficiencies
- Missing assignments or time periods
- Workload imbalances across team members
- Timeline inconsistencies

Work Schedule Context:
- Work Schedule ID: {request.WorkScheduleId}
- Project ID: {request.ProjectId}
- Tenant ID: {request.TenantId}
- Analyst: {currentUser.FirstName} {currentUser.LastName}
- Analysis Date: {DateTime.UtcNow:yyyy-MM-dd}
";

        var userQuery = $@"Analyze work schedule '{workSchedule.Name}' and identify all conflicts, resource issues, and areas for improvement. 
Be specific about:
1. Which team members are overallocated and when
2. Which works have overlapping time periods
3. Which works lack assigned personnel or time periods
4. Overall schedule health and completion likelihood

Return your analysis as structured JSON matching the schema provided in the system prompt.";

        // Select work schedule analysis tools
        var toolNames = new[]
        {
            "get_work_schedule_details",      // First: get full structure
            "detect_time_conflicts",           // Detect overlapping periods
            "detect_resource_conflicts",       // Detect overallocated people
            "detect_unassigned_periods",       // Find unassigned works
            "calculate_workload_stats"         // Calculate utilization metrics
        };

        // Additional context for tools
        var additionalContext = new Dictionary<string, object>
        {
            { "WorkScheduleId", request.WorkScheduleId },
            { "ProjectId", request.ProjectId },
            { "TenantId", request.TenantId },
            { "UserId", currentUser.Id },
            { "WorkScheduleName", workSchedule.Name }
        };

        // Execute AI analysis with orchestrator
        var result = await orchestrator.ExecuteAsync(
            systemPrompt: systemPrompt,
            userQuery: userQuery,
            toolNames: toolNames,
            additionalContext: additionalContext,
            cancellationToken: cancellationToken);

        // Check execution success
        if (!result.Success)
        {
            logger.LogError("Work schedule analysis failed: {Error}", result.Error);
            throw new InvalidOperationException($"Work schedule analysis failed: {result.Error}");
        }

        // Parse AI response (expecting JSON)
        var analysisJson = result.FinalMessage.Content ?? "{}";

        logger.LogDebug("AI analysis result: {Result}", analysisJson);

        try
        {
            // Parse the AI-generated JSON
            var aiAnalysis = JsonSerializer.Deserialize<AIAnalysisResult>(analysisJson)
                ?? throw new InvalidOperationException("Failed to parse AI analysis result");

            logger.LogInformation(
                "Work schedule analysis completed. Tokens: {Tokens}, Findings: {Findings}, Recommendations: {Recommendations}, Conflicts: {Conflicts}",
                result.TotalTokensUsed,
                aiAnalysis.KeyFindings?.Count ?? 0,
                aiAnalysis.Recommendations?.Count ?? 0,
                aiAnalysis.Conflicts?.Count ?? 0);

            return new WorkScheduleAnalysisResponse(
                Summary: aiAnalysis.Summary ?? "No summary provided",
                KeyFindings: aiAnalysis.KeyFindings ?? new List<string>(),
                Recommendations: aiAnalysis.Recommendations ?? new List<string>(),
                Conflicts: aiAnalysis.Conflicts?.Select(c => new ScheduleConflict(
                    Type: c.Type ?? "unknown",
                    Severity: c.Severity ?? "medium",
                    Description: c.Description ?? string.Empty,
                    Recommendation: c.Recommendation ?? string.Empty
                )).ToList() ?? new List<ScheduleConflict>(),
                WorkloadSummary: new WorkloadSummary(
                    TotalTeamMembers: aiAnalysis.WorkloadSummary?.TotalTeamMembers ?? 0,
                    OverloadedMembers: aiAnalysis.WorkloadSummary?.OverloadedMembers ?? 0,
                    UnderutilizedMembers: aiAnalysis.WorkloadSummary?.UnderutilizedMembers ?? 0,
                    AverageCompletionPercentage: aiAnalysis.WorkloadSummary?.AverageCompletionPercentage ?? 0.0,
                    TotalDurationDays: aiAnalysis.WorkloadSummary?.TotalDurationDays ?? 0
                ),
                TokensUsed: result.TotalTokensUsed,
                ExecutionTimeMs: result.TotalExecutionTimeMs
            );
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse AI response as JSON. Returning raw response.");

            // Fallback: return raw response if JSON parsing fails
            return new WorkScheduleAnalysisResponse(
                Summary: analysisJson.Length > 500 ? analysisJson.Substring(0, 500) + "..." : analysisJson,
                KeyFindings: new List<string> { "Analysis completed but response format was unexpected" },
                Recommendations: new List<string> { "Review the raw analysis output for details" },
                Conflicts: new List<ScheduleConflict>(),
                WorkloadSummary: new WorkloadSummary(0, 0, 0, 0.0, 0),
                TokensUsed: result.TotalTokensUsed,
                ExecutionTimeMs: result.TotalExecutionTimeMs
            );
        }
    }

    /// <summary>
    /// Expected structure of AI analysis result (for JSON deserialization)
    /// </summary>
    private sealed class AIAnalysisResult
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("key_findings")]
        public List<string>? KeyFindings { get; set; }

        [JsonPropertyName("recommendations")]
        public List<string>? Recommendations { get; set; }

        [JsonPropertyName("conflicts")]
        public List<AIConflict>? Conflicts { get; set; }

        [JsonPropertyName("workload_summary")]
        public AIWorkloadSummary? WorkloadSummary { get; set; }
    }

    private sealed class AIConflict
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("severity")]
        public string? Severity { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("recommendation")]
        public string? Recommendation { get; set; }
    }

    private sealed class AIWorkloadSummary
    {
        [JsonPropertyName("total_team_members")]
        public int TotalTeamMembers { get; set; }

        [JsonPropertyName("overloaded_members")]
        public int OverloadedMembers { get; set; }

        [JsonPropertyName("underutilized_members")]
        public int UnderutilizedMembers { get; set; }

        [JsonPropertyName("average_completion_percentage")]
        public double AverageCompletionPercentage { get; set; }

        [JsonPropertyName("total_duration_days")]
        public int TotalDurationDays { get; set; }
    }
}
