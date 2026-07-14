# API-02: Implementacja WorkScheduleAIGeneratorService

## Zadanie
Utwórz implementację serwisu `WorkScheduleAIGeneratorService` który:
1. Przyjmuje dane wejściowe (stages, works, overallStartDate, overallEndDate)
2. Buduje prompt dla agenta AI `schedule-generator-agent`
3. Wywołuje `IAgentRunner.RunAsync("schedule-generator-agent", prompt, context)`
4. Parsuje odpowiedź JSON od AI
5. Rozkłada duration_days na konkretne daty w ramach ram czasowych
6. Zwraca `AIScheduleResult` z okresami i zależnościami

## Plik do utworzenia

**Ścieżka**: `Business/Implementation/Services/WorkScheduleAIGeneratorService.cs`
**Namespace**: `Business.Implementation.Services`

### Wymagane importy/usings:
```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Business.AIAgent;
using Business.AIAgent.Abstractions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models.WorkSchedules;
using Microsoft.Extensions.Logging;
```

### Klasa:
```csharp
namespace Business.Implementation.Services
{
    public sealed class WorkScheduleAIGeneratorService : IWorkScheduleAIGeneratorService
    {
        private readonly IAgentRunner agentRunner;
        private readonly ILogger<WorkScheduleAIGeneratorService> logger;

        public WorkScheduleAIGeneratorService(
            IAgentRunner agentRunner,
            ILogger<WorkScheduleAIGeneratorService> logger)
        {
            this.agentRunner = agentRunner;
            this.logger = logger;
        }

        public async Task<AIScheduleResult> GenerateScheduleAsync(
            Guid workScheduleId,
            Guid tenantId,
            Guid projectId,
            List<StageInput> stages,
            List<WorkInput> works,
            DateTime overallStartDate,
            DateTime overallEndDate,
            CancellationToken cancellationToken)
        {
            // 1. Build prompt for AI
            string prompt = BuildPrompt(stages, works, overallStartDate, overallEndDate);

            // 2. Create agent context
            AgentContext context = new AgentContext
            {
                TenantId = tenantId,
                ProjectId = projectId,
                // No session/user tracking needed for this generation
            };

            // 3. Call AI agent
            AgentRunResult result = await agentRunner.RunAsync(
                "schedule-generator-agent",
                prompt,
                context);

            if (!result.IsSuccess)
            {
                logger.LogError("AI schedule generation failed: {Error}", result.ErrorMessage);
                throw new InvalidOperationException(
                    $"AI schedule generation failed: {result.ErrorMessage ?? "Unknown error"}");
            }

            // 4. Parse response
            string responseText = result.Response ?? string.Empty;
            logger.LogInformation("AI response received ({Length} chars)", responseText.Length);

            AIScheduleRawResponse? rawResponse = ParseAIResponse(responseText);

            if (rawResponse?.durations == null || rawResponse.durations.Count == 0)
            {
                throw new InvalidOperationException(
                    "AI returned invalid or empty schedule data. Please try again.");
            }

            // 5. Calculate dates from durations
            return CalculateSchedule(rawResponse, works, overallStartDate, overallEndDate);
        }

        private static string BuildPrompt(
            List<StageInput> stages,
            List<WorkInput> works,
            DateTime overallStartDate,
            DateTime overallEndDate)
        {
            string stagesJson = JsonSerializer.Serialize(stages.Select(s => new
            {
                id = s.Id.ToString(),
                name = s.Name,
                order = s.Order,
                parent_stage_id = s.ParentStageId?.ToString()
            }));

            string worksJson = JsonSerializer.Serialize(works.Select(w => new
            {
                id = w.Id.ToString(),
                name = w.Name,
                order = w.Order,
                stage_id = w.StageId.ToString(),
                stage_name = w.StageName
            }));

            return $@"Analyze the following cost estimate structure and generate a work schedule.

Overall time frame: {overallStartDate:yyyy-MM-dd} to {overallEndDate:yyyy-MM-dd}

Stages:
{stagesJson}

Works:
{worksJson}

Generate durations (in working days) for each work and logical dependencies between works.
The total project must fit within the overall time frame.
Respond with ONLY valid JSON using the exact format specified.";
        }

        private static AIScheduleRawResponse? ParseAIResponse(string responseText)
        {
            // Clean up response — remove markdown code fences if present
            string cleanJson = responseText.Trim();

            if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                int start = cleanJson.IndexOf('\n', 7) + 1;
                int end = cleanJson.LastIndexOf("```", StringComparison.Ordinal);
                if (end > start)
                {
                    cleanJson = cleanJson[start..end].Trim();
                }
            }
            else if (cleanJson.StartsWith("```", StringComparison.Ordinal))
            {
                int start = cleanJson.IndexOf('\n', 3) + 1;
                int end = cleanJson.LastIndexOf("```", StringComparison.Ordinal);
                if (end > start)
                {
                    cleanJson = cleanJson[start..end].Trim();
                }
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<AIScheduleRawResponse>(cleanJson, options);
        }

        private static AIScheduleResult CalculateSchedule(
            AIScheduleRawResponse rawResponse,
            List<WorkInput> works,
            DateTime overallStartDate,
            DateTime overallEndDate)
        {
            // Build lookup: work_id → duration_days
            Dictionary<Guid, int> durationByWorkId = new Dictionary<Guid, int>();
            foreach (AIDuration duration in rawResponse.durations)
            {
                if (Guid.TryParse(duration.work_id, out Guid workId))
                {
                    durationByWorkId[workId] = Math.Max(1, duration.duration_days);
                }
            }

            // Build adjacency and in-degree for topological sort
            Dictionary<Guid, List<Guid>> adjacency = new Dictionary<Guid, List<Guid>>();
            Dictionary<Guid, int> inDegree = new Dictionary<Guid, int>();

            foreach (WorkInput work in works)
            {
                adjacency[work.Id] = new List<Guid>();
                inDegree[work.Id] = 0;
            }

            // Also collect dependencies info for later use
            List<AIDependency> parsedDeps = new List<AIDependency>();
            if (rawResponse.dependencies != null)
            {
                foreach (AIDependency dep in rawResponse.dependencies)
                {
                    if (Guid.TryParse(dep.predecessor_work_id, out Guid predId) &&
                        Guid.TryParse(dep.successor_work_id, out Guid succId))
                    {
                        if (predId != succId && durationByWorkId.ContainsKey(predId) && durationByWorkId.ContainsKey(succId))
                        {
                            adjacency[predId].Add(succId);
                            inDegree[succId] = inDegree.GetValueOrDefault(succId) + 1;
                            parsedDeps.Add(new AIDependency
                            {
                                predecessor_work_id = predId.ToString(),
                                successor_work_id = succId.ToString(),
                                dependency_type = dep.dependency_type,
                                lag_days = dep.lag_days
                            });
                        }
                    }
                }
            }

            // Topological sort (Kahn's algorithm) to determine start dates
            Dictionary<Guid, DateTime> startDateByWorkId = new Dictionary<Guid, DateTime>();
            Dictionary<Guid, DateTime> endDateByWorkId = new Dictionary<Guid, DateTime>();

            // Also track dependency info for each successor
            Dictionary<Guid, (DateTime earliestStart, int lagDays)> dependencyConstraints = new Dictionary<Guid, (DateTime, int)>();

            // First pass: works with no dependencies start at overallStartDate
            Queue<Guid> queue = new Queue<Guid>();
            
            // Sort by stage order first, then work order for deterministic output
            Dictionary<Guid, WorkInput> workById = works.ToDictionary(w => w.Id);
            
            List<Guid> roots = inDegree
                .Where(kvp => kvp.Value == 0)
                .Select(kvp => kvp.Key)
                .OrderBy(id => workById.TryGetValue(id, out WorkInput? w) ? w.Order : 0)
                .ToList();

            // Calculate how to distribute start dates among root works
            // If there are multiple roots, space them out slightly
            int rootCount = roots.Count;
            double totalRootDuration = roots.Sum(id => (double)durationByWorkId.GetValueOrDefault(id, 1));
            
            double currentOffsetDays = 0;
            foreach (Guid rootId in roots)
            {
                int duration = durationByWorkId.GetValueOrDefault(rootId, 1);
                // Calculate offset as proportion of overall time
                DateTime rootStart = overallStartDate.AddDays(currentOffsetDays);
                startDateByWorkId[rootId] = rootStart;
                endDateByWorkId[rootId] = rootStart.AddDays(duration - 1);
                queue.Enqueue(rootId);
                currentOffsetDays += Math.Max(1, duration * 0.1); // small gap between parallel roots
            }

            // Process remaining works in topological order
            while (queue.Count > 0)
            {
                Guid currentId = queue.Dequeue();
                DateTime currentEnd = endDateByWorkId[currentId];
                
                // Also check if this work has a predecessor dependency for exact lag
                if (dependencyConstraints.TryGetValue(currentId, out (DateTime earliestStart, int lagDays) constraint))
                {
                    DateTime constraintStart = constraint.earliestStart.AddDays(constraint.lagDays);
                    if (constraintStart > startDateByWorkId[currentId])
                    {
                        startDateByWorkId[currentId] = constraintStart;
                        endDateByWorkId[currentId] = constraintStart.AddDays(durationByWorkId.GetValueOrDefault(currentId, 1) - 1);
                    }
                }

                foreach (Guid successorId in adjacency[currentId])
                {
                    inDegree[successorId]--;

                    // Find the dependency info for this edge
                    AIDependency? edgeDep = parsedDeps.FirstOrDefault(d =>
                        Guid.TryParse(d.predecessor_work_id, out Guid predId) && predId == currentId &&
                        Guid.TryParse(d.successor_work_id, out Guid succId) && succId == successorId);

                    int lag = edgeDep?.lag_days ?? 0;
                    int predecessorDuration = durationByWorkId.GetValueOrDefault(currentId, 1);
                    int successorDuration = durationByWorkId.GetValueOrDefault(successorId, 1);

                    DateTime successorStart;

                    if (edgeDep != null)
                    {
                        switch (edgeDep.dependency_type?.ToLowerInvariant())
                        {
                            case "starttostart":
                                successorStart = startDateByWorkId[currentId].AddDays(lag);
                                break;
                            case "finishtofinish":
                                // Successor should end when predecessor ends + lag
                                DateTime successorEnd = currentEnd.AddDays(lag);
                                successorStart = successorEnd.AddDays(-(successorDuration - 1));
                                break;
                            case "starttofinish":
                                DateTime sfEnd = startDateByWorkId[currentId].AddDays(lag);
                                successorStart = sfEnd.AddDays(-(successorDuration - 1));
                                break;
                            default: // FinishToStart
                                successorStart = currentEnd.AddDays(1 + lag);
                                break;
                        }
                    }
                    else
                    {
                        // Default: FinishToStart with no lag
                        successorStart = currentEnd.AddDays(1);
                    }

                    // Ensure we don't go before overall start
                    if (successorStart < overallStartDate)
                    {
                        successorStart = overallStartDate;
                    }

                    // Update constraint for successor
                    if (dependencyConstraints.TryGetValue(successorId, out (DateTime earliest, int lagDays) existingConstraint))
                    {
                        if (successorStart > existingConstraint.earliest)
                        {
                            dependencyConstraints[successorId] = (successorStart, 0);
                        }
                    }
                    else
                    {
                        dependencyConstraints[successorId] = (successorStart, 0);
                    }

                    if (inDegree[successorId] == 0)
                    {
                        DateTime finalStart = dependencyConstraints[successorId].earliestStart;
                        startDateByWorkId[successorId] = finalStart;
                        endDateByWorkId[successorId] = finalStart.AddDays(durationByWorkId.GetValueOrDefault(successorId, 1) - 1);
                        queue.Enqueue(successorId);
                    }
                }
            }

            // Handle any works not reached by topological sort (isolated or cycle)
            foreach (WorkInput work in works)
            {
                if (!startDateByWorkId.ContainsKey(work.Id))
                {
                    // Place at overall start date, distributed by order
                    DateTime wStart = overallStartDate.AddDays(work.Order * 2);
                    startDateByWorkId[work.Id] = wStart;
                    endDateByWorkId[work.Id] = wStart.AddDays(durationByWorkId.GetValueOrDefault(work.Id, 1) - 1);
                }
            }

            // Build result
            AIScheduleResult result = new AIScheduleResult
            {
                Periods = works.Select(w => new WorkPeriodResult
                {
                    WorkScheduleStageWorkId = w.Id,
                    StartDate = startDateByWorkId.TryGetValue(w.Id, out DateTime sDate) ? sDate : overallStartDate,
                    EndDate = endDateByWorkId.TryGetValue(w.Id, out DateTime eDate) ? eDate : overallEndDate
                }).ToList(),

                Dependencies = parsedDeps
                    .Where(d => Guid.TryParse(d.predecessor_work_id, out Guid _) && Guid.TryParse(d.successor_work_id, out Guid _))
                    .Select(d => new WorkDependencyResult
                    {
                        PredecessorWorkId = Guid.Parse(d.predecessor_work_id!),
                        SuccessorWorkId = Guid.Parse(d.successor_work_id!),
                        DependencyType = ParseDependencyType(d.dependency_type),
                        LagDays = d.lag_days ?? 0
                    }).ToList()
            };

            return result;
        }

        private static WorkDependencyType ParseDependencyType(string? type)
        {
            return type?.ToLowerInvariant() switch
            {
                "starttostart" => WorkDependencyType.StartToStart,
                "finishtofinish" => WorkDependencyType.FinishToFinish,
                "starttofinish" => WorkDependencyType.StartToFinish,
                _ => WorkDependencyType.FinishToStart
            };
        }
    }

    // Raw response models from AI
    internal sealed class AIScheduleRawResponse
    {
        public List<AIDuration> durations { get; init; } = [];
        public List<AIDependency> dependencies { get; init; } = [];
    }

    internal sealed class AIDuration
    {
        public string? work_id { get; init; }
        public int duration_days { get; init; }
    }

    internal sealed class AIDependency
    {
        public string? predecessor_work_id { get; init; }
        public string? successor_work_id { get; init; }
        public string? dependency_type { get; init; }
        public int? lag_days { get; init; }
    }
}
```

### Uwagi implementacyjne:
- Użyj `System.Text.Json` — projekt już go używa
- Agent AI może zwrócić JSON w markdown code block — usuń znaczniki przed parsowaniem
- Waliduj czy AI zwróciło wszystkie work_id z durations
- Jeśli AI zwróci cykl w zależnościach, algorytm Kahna poradzi sobie (nieprzetworzone worki zostaną umieszczone na początku)
- Loguj surową odpowiedź AI w przypadku błędu parsowania
