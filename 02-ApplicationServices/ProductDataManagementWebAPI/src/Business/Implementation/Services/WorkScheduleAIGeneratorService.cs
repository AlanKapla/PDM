using System.Text.Json;
using Business.AIAgent;
using Business.AIAgent.Abstractions;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models.WorkSchedules;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services
{
    public sealed class WorkScheduleAIGeneratorService : IWorkScheduleAIGeneratorService
    {
        private readonly IAgentRunner _agentRunner;
        private readonly ILogger<WorkScheduleAIGeneratorService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public WorkScheduleAIGeneratorService(
            IAgentRunner agentRunner,
            ILogger<WorkScheduleAIGeneratorService> logger)
        {
            _agentRunner = agentRunner;
            _logger = logger;
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
            // 1. Group works by stage for per-scope analysis
            Dictionary<Guid, List<WorkInput>> worksByStage = works
                .GroupBy(w => w.StageId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build a lookup: stage name by stage ID for logging
            Dictionary<Guid, string> stageNameById = works
                .GroupBy(w => w.StageId)
                .ToDictionary(g => g.Key, g => g.First().StageName);

            AgentContext context = new()
            {
                TenantId = tenantId,
                ProjectId = projectId,
            };

            int stageCount = worksByStage.Count;
            _logger.LogInformation(
                "Starting per-stage AI agents: {StageCount} stages, {WorkCount} works",
                stageCount, works.Count);

            // ── 2. Launch per-stage DURATION agents ──
            List<Task<AgentRunResult>> durationTasks = new List<Task<AgentRunResult>>(stageCount);
            List<Guid> durationStageOrder = new List<Guid>(stageCount);

            foreach (KeyValuePair<Guid, List<WorkInput>> stageGroup in worksByStage)
            {
                Guid stageId = stageGroup.Key;
                List<WorkInput> stageWorks = stageGroup.Value;
                string stageName = stageNameById[stageId];

                string prompt = BuildStageDurationPrompt(stageWorks, stageName, overallStartDate, overallEndDate);

                _logger.LogDebug(
                    "Queueing duration agent for stage '{StageName}' ({StageId}): {WorkCount} works",
                    stageName, stageId, stageWorks.Count);

                durationStageOrder.Add(stageId);
                durationTasks.Add(_agentRunner.RunAsync(
                    "schedule-duration-agent",
                    prompt,
                    context,
                    cancellationToken));
            }

            // ── 3. Launch per-stage DEPENDENCY agents ──
            List<Task<AgentRunResult>> dependencyTasks = new List<Task<AgentRunResult>>(stageCount);
            List<Guid> dependencyStageOrder = new List<Guid>(stageCount);

            foreach (KeyValuePair<Guid, List<WorkInput>> stageGroup in worksByStage)
            {
                Guid stageId = stageGroup.Key;
                List<WorkInput> stageWorks = stageGroup.Value;
                string stageName = stageNameById[stageId];

                string prompt = BuildStageDependencyPrompt(
                    stageWorks, works, stages, stageName, overallStartDate, overallEndDate);

                _logger.LogDebug(
                    "Queueing dependency agent for stage '{StageName}' ({StageId}): {WorkCount} focus works, {TotalWorks} total context",
                    stageName, stageId, stageWorks.Count, works.Count);

                dependencyStageOrder.Add(stageId);
                dependencyTasks.Add(_agentRunner.RunAsync(
                    "schedule-dependency-agent",
                    prompt,
                    context,
                    cancellationToken));
            }

            // ── 4. Wait for ALL agents in parallel ──
            _logger.LogInformation(
                "Waiting for {Total} AI agents ({DurationCount} duration + {DepCount} dependency)...",
                durationTasks.Count + dependencyTasks.Count, durationTasks.Count, dependencyTasks.Count);

            await Task.WhenAll(durationTasks);
            await Task.WhenAll(dependencyTasks);

            // ── 5. Merge duration results ──
            List<AIDuration> mergedDurations = new List<AIDuration>();

            for (Int32 i = 0; i < durationTasks.Count; i++)
            {
                Guid stageId = durationStageOrder[i];
                AgentRunResult result = durationTasks[i].Result;

                if (!result.IsSuccess)
                {
                    _logger.LogError(
                        "Duration agent for stage '{StageName}' ({StageId}) failed: {Error}",
                        stageNameById.GetValueOrDefault(stageId, "unknown"), stageId, result.ErrorMessage);
                    throw new ValidationApiException(
                        $"AI duration estimation failed for stage '{stageNameById.GetValueOrDefault(stageId, "unknown")}': " +
                        $"{result.ErrorMessage ?? "Unknown error"}");
                }

                DurationResponse? parsed = ParseJson<DurationResponse>(
                    result.Response, $"schedule-duration-agent/stage-{stageId}");

                if (parsed?.durations == null || parsed.durations.Count == 0)
                {
                    _logger.LogWarning(
                        "Duration agent for stage '{StageName}' ({StageId}) returned no durations",
                        stageNameById.GetValueOrDefault(stageId, "unknown"), stageId);
                    throw new ValidationApiException(
                        $"AI duration agent for stage '{stageNameById.GetValueOrDefault(stageId, "unknown")}' " +
                        "returned no durations. Please try again.");
                }

                _logger.LogDebug(
                    "Duration agent for stage '{StageName}' returned {Count} durations",
                    stageNameById.GetValueOrDefault(stageId, "unknown"), parsed.durations.Count);

                mergedDurations.AddRange(parsed.durations);
            }

            // ── 6. Merge dependency results (with deduplication) ──
            HashSet<(String pred, String succ, String type)> seenDeps = new();
            List<AIDependency> mergedDependencies = new List<AIDependency>();

            for (Int32 i = 0; i < dependencyTasks.Count; i++)
            {
                Guid stageId = dependencyStageOrder[i];
                AgentRunResult result = dependencyTasks[i].Result;

                if (!result.IsSuccess)
                {
                    _logger.LogError(
                        "Dependency agent for stage '{StageName}' ({StageId}) failed: {Error}",
                        stageNameById.GetValueOrDefault(stageId, "unknown"), stageId, result.ErrorMessage);
                    throw new ValidationApiException(
                        $"AI dependency analysis failed for stage '{stageNameById.GetValueOrDefault(stageId, "unknown")}': " +
                        $"{result.ErrorMessage ?? "Unknown error"}");
                }

                DependencyResponse? parsed = ParseJson<DependencyResponse>(
                    result.Response, $"schedule-dependency-agent/stage-{stageId}");

                if (parsed?.dependencies == null)
                {
                    _logger.LogDebug(
                        "Dependency agent for stage '{StageName}' returned null (no dependencies)",
                        stageNameById.GetValueOrDefault(stageId, "unknown"));
                    continue;
                }

                foreach (AIDependency dep in parsed.dependencies)
                {
                    String key = $"{dep.predecessor_work_id}|{dep.successor_work_id}|{dep.dependency_type}";
                    if (seenDeps.Add((dep.predecessor_work_id ?? "", dep.successor_work_id ?? "", dep.dependency_type ?? "")))
                    {
                        mergedDependencies.Add(dep);
                    }
                }

                _logger.LogDebug(
                    "Dependency agent for stage '{StageName}' returned {Count} deps ({TotalNew} cumulative)",
                    stageNameById.GetValueOrDefault(stageId, "unknown"),
                    parsed.dependencies.Count,
                    mergedDependencies.Count);
            }

            // ── 6b. Add sequential FinishToStart deps within each stage (by Order) ──
            AddIntraStageDependencies(mergedDependencies, seenDeps, works);

            _logger.LogInformation(
                "After intra-stage deps: {TotalDeps} dependencies",
                mergedDependencies.Count);

            // ── 7. Build unified raw response ──
            AIScheduleRawResponse rawResponse = new AIScheduleRawResponse
            {
                durations = mergedDurations,
                dependencies = mergedDependencies
            };

            _logger.LogInformation(
                "AI merge complete: {TotalDurations} durations, {TotalDeps} dependencies (from {StageCount} stages)",
                mergedDurations.Count, mergedDependencies.Count, stageCount);

            // ── 8. Validate ──
            ValidateAIScheduleResult(rawResponse, works);

            // ── 9. Calculate dates from durations and dependencies ──
            return CalculateSchedule(rawResponse, works, overallStartDate, overallEndDate);
        }

        /// <summary>
        /// Adds sequential FinishToStart (lag=0) dependencies between consecutive works
        /// within the same stage, ordered by <see cref="WorkInput.Order"/>.
        /// Cross-stage dependencies remain AI-driven; intra-stage ordering is deterministic.
        /// </summary>
        private static void AddIntraStageDependencies(
            List<AIDependency> mergedDependencies,
            HashSet<(String pred, String succ, String type)> seenDeps,
            List<WorkInput> works)
        {
            IEnumerable<IGrouping<Guid, WorkInput>> groups = works.GroupBy(w => w.StageId);
            foreach (IGrouping<Guid, WorkInput> group in groups)
            {
                List<WorkInput> ordered = group.OrderBy(w => w.Order).ToList();
                for (Int32 i = 0; i < ordered.Count - 1; i++)
                {
                    String predId = ordered[i].Id.ToString();
                    String succId = ordered[i + 1].Id.ToString();
                    String depType = "FinishToStart";

                    if (!seenDeps.Add((predId, succId, depType)))
                    {
                        continue;
                    }

                    mergedDependencies.Add(new AIDependency
                    {
                        predecessor_work_id = predId,
                        successor_work_id = succId,
                        dependency_type = depType,
                        lag_days = 0
                    });
                }
            }
        }

        /// <summary>
        /// Validates the merged AI result for basic correctness before calculating the schedule.
        /// Throws <see cref="ValidationApiException"/> on invalid data.
        /// </summary>
        private static void ValidateAIScheduleResult(AIScheduleRawResponse rawResponse, List<WorkInput> works)
        {
            HashSet<Guid> workIds = works.Select(w => w.Id).ToHashSet();

            // All works must have a duration
            HashSet<Guid> worksWithDuration = new HashSet<Guid>();
            foreach (AIDuration duration in rawResponse.durations)
            {
                if (!Guid.TryParse(duration.work_id, out Guid workId))
                {
                    throw new ValidationApiException(
                        $"AI returned invalid work_id in durations: '{duration.work_id}' is not a valid GUID.");
                }

                if (!workIds.Contains(workId))
                {
                    throw new ValidationApiException(
                        $"AI returned duration for unknown work: {workId}. This work does not exist in the current schedule.");
                }

                if (duration.duration_days < 1)
                {
                    throw new ValidationApiException(
                        $"AI returned invalid duration ({duration.duration_days}) for work {workId}. Duration must be at least 1 working day.");
                }

                worksWithDuration.Add(workId);
            }

            // Check for works missing duration
            List<Guid> missingDuration = workIds.Except(worksWithDuration).ToList();
            if (missingDuration.Count > 0)
            {
                throw new ValidationApiException(
                    $"AI did not return durations for {missingDuration.Count} work(s). Please try again.");
            }

            // Validate dependencies
            if (rawResponse.dependencies != null)
            {
                foreach (AIDependency dep in rawResponse.dependencies)
                {
                    if (string.IsNullOrWhiteSpace(dep.predecessor_work_id) ||
                        string.IsNullOrWhiteSpace(dep.successor_work_id))
                    {
                        throw new ValidationApiException(
                            "AI returned a dependency with missing work IDs.");
                    }

                    if (!Guid.TryParse(dep.predecessor_work_id, out Guid predId))
                    {
                        throw new ValidationApiException(
                            $"AI returned invalid predecessor_work_id: '{dep.predecessor_work_id}'.");
                    }

                    if (!Guid.TryParse(dep.successor_work_id, out Guid succId))
                    {
                        throw new ValidationApiException(
                            $"AI returned invalid successor_work_id: '{dep.successor_work_id}'.");
                    }

                    if (predId == succId)
                    {
                        throw new ValidationApiException(
                            $"AI returned a self-referencing dependency (work {predId} depends on itself).");
                    }

                    if (!workIds.Contains(predId))
                    {
                        throw new ValidationApiException(
                            $"AI returned dependency referencing unknown predecessor: {predId}.");
                    }

                    if (!workIds.Contains(succId))
                    {
                        throw new ValidationApiException(
                            $"AI returned dependency referencing unknown successor: {succId}.");
                    }

                    // Validate dependency type
                    string depType = dep.dependency_type?.ToLowerInvariant() ?? string.Empty;
                    if (depType is not ("finishtostart" or "starttostart" or "finishtofinish" or "starttofinish"))
                    {
                        throw new ValidationApiException(
                            $"AI returned invalid dependency_type: '{dep.dependency_type}'. Must be one of: FinishToStart, StartToStart, FinishToFinish, StartToFinish.");
                    }
                }
            }
        }

        /// <summary>
        /// Builds a prompt for the duration agent focused on a single stage's works.
        /// The agent only sees its assigned works for focused, realistic duration estimation.
        /// </summary>
        private static string BuildStageDurationPrompt(
            List<WorkInput> stageWorks,
            string stageName,
            DateTime overallStartDate,
            DateTime overallEndDate)
        {
            string worksJson = JsonSerializer.Serialize(stageWorks.Select(w => new
            {
                id = w.Id.ToString(),
                name = w.Name,
                order = w.Order
            }));

            return $@"You are estimating work durations for the stage '{stageName}' in a construction/engineering project.

Overall project time frame: {overallStartDate:yyyy-MM-dd} to {overallEndDate:yyyy-MM-dd}

Works in this stage:
{worksJson}

Estimate a realistic duration in WORKING DAYS for each work item.
Consider the specific construction/engineering trade implied by the work name:

- Simple/inspection tasks (e.g., sprawdzenie, odbior, dostawa): 1-3 days
- Medium tasks (e.g., wykopy, zbrojenie, szalowanie): 5-15 days
- Complex/long-duration tasks (e.g., murowanie scian, fundamenty, wience, dach): 10-30 days
- Major structural elements (e.g., stropy, konstrukcja zelbetowa): 15-45 days
- Finishing/installation works (e.g., tynki, instalacje, posadzki): 10-40 days

IMPORTANT: Be realistic, not minimal. Murowanie scian cannot be 2 days — it is typically 15-30 days depending on scope.

Every work item MUST have exactly one duration entry.
Durations must be positive integers (minimum 1 working day).

Respond with ONLY valid JSON — no markdown, no code fences, no explanations.
Use the EXACT format:
{{""durations"": [{{""work_id"": ""guid"", ""duration_days"": 14}}]}}";
        }

        /// <summary>
        /// Builds a prompt for the dependency agent focused on a single stage.
        /// The agent receives ALL works grouped by stage and must determine
        /// at most 2 dependencies involving its focus stage — one predecessor
        /// (what must finish before this stage's works) and optionally one successor
        /// (what depends on this stage's works finishing).
        ///
        /// A reference construction stage ordering is provided so the agent
        /// can correctly place the focus stage in the logical build sequence.
        /// </summary>
        private static string BuildStageDependencyPrompt(
            List<WorkInput> focusWorks,
            List<WorkInput> allWorks,
            List<StageInput> stages,
            string focusStageName,
            DateTime overallStartDate,
            DateTime overallEndDate)
        {
            // Collect all unique stage names with their works for the prompt
            Dictionary<string, List<WorkInput>> worksByStageName = allWorks
                .GroupBy(w => w.StageName)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, int> stageOrderById = stages
                .ToDictionary(s => s.Id, s => s.Order);

            // Mark which works belong to the focus stage
            HashSet<Guid> focusWorkIds = focusWorks.Select(w => w.Id).ToHashSet();

            // Build a simplified "stage map": for each stage, list its works (ordered by stage Order)
            List<KeyValuePair<string, List<WorkInput>>> orderedStageGroups = worksByStageName
                .OrderBy(kvp =>
                {
                    WorkInput? first = kvp.Value.FirstOrDefault();
                    if (first is null)
                    {
                        return int.MaxValue;
                    }

                    return stageOrderById.TryGetValue(first.StageId, out int ord) ? ord : int.MaxValue;
                })
                .ToList();

            List<string> stageBlocks = new List<string>();
            foreach (KeyValuePair<string, List<WorkInput>> kvp in orderedStageGroups)
            {
                bool isFocus = kvp.Key == focusStageName;
                string marker = isFocus ? " *** FOCUS STAGE ***" : "";
                int stageOrder = 0;
                WorkInput? firstWork = kvp.Value.FirstOrDefault();
                if (firstWork is not null && stageOrderById.TryGetValue(firstWork.StageId, out int ord))
                {
                    stageOrder = ord;
                }

                stageBlocks.Add($"Stage: {kvp.Key} (order: {stageOrder}){marker}");

                foreach (WorkInput w in kvp.Value.OrderBy(x => x.Order))
                {
                    string focusMarker = focusWorkIds.Contains(w.Id) ? " (focus)" : "";
                    stageBlocks.Add($"  - [{w.Id}] {w.Name} (order: {w.Order}){focusMarker}");
                }
            }

            string worksOverview = string.Join(Environment.NewLine, stageBlocks);

            return $@"You are a construction sequence expert. Your task is to determine at most 2 logical dependencies for the stage '{focusStageName}'.

=== REFERENCE CONSTRUCTION STAGE ORDER ===
Here is the typical order of construction stages. Use this as a reference to determine which stage comes before or after your focus stage. Match your actual stage names to the closest category below:

1. ROBOTY ZIEMNE I FUNDAMENTY (Earthworks & Foundations)
   - wykopy, fundamenty, lavy, stopy fundamentowe, izolacje fundamentow
2. STAN SUROWY (Raw Shell)
   - sciany konstrukcyjne, wieńce, slupy, stropy, nadproza
3. DACH I KONSTRUKCJA DREWNIANA (Roof & Timber Structure)
   - dach, wiezba dachowa, pokrycie dachu, rynny, orymowanie
4. STOLARKA ZEWNETRZNA (External Joinery)
   - okna, drzwi zewnetrzne, brama garazowa
5. INSTALACJE (Installations)
   - instalacje elektryczne, wodno-kanalizacyjne, CO, wentylacja
6. TYNKI I OBLICOWANIA (Plaster & Cladding)
   - tynki wewnetrzne, oblicowania scian, sufity podwieszane
7. POSADZKI I PODLOGI (Flooring)
   - posadzki, wylewki, panele, plytki
8. STOLARKA WEWNETRZNA (Internal Joinery)
   - drzwi wewnetrzne, listwy, wykończenia stolarskie
9. MALOWANIE (Painting)
   - malowanie scian i sufitow, tapety
10. BIALY MONTAZ (White Installation)
    - umywalki, sedesy, kabiny prysznicowe, baterie, armatura
11. WYKONCZENIE I ODBIORY (Finishing & Handover)
    - czyszczenie, odbiory techniczne, dokumentacja

=== YOUR TASK ===

Your focus stage is: {focusStageName}

Using the reference order above, identify:
1. Which number in the sequence does your focus stage belong to?
2. What is the previous stage (if any) that MUST finish before your focus stage can start?
3. What is the next stage (if any) that depends on your focus stage finishing?

=== ACTUAL PROJECT STAGES AND WORKS ===

{worksOverview}

=== RULES ===

1. MAXIMUM 2 DEPENDENCIES for the focus stage — typically:
   - At most 1 predecessor: a work from the logically PRECEDING stage that must finish before this stage can start
   - At most 1 successor: a work from the logically FOLLOWING stage that depends on this stage finishing
   - It is perfectly OK to have 0 or 1 dependency if the stage is first/last.

2. Cross-stage dependencies ONLY — do NOT create dependencies between works within the same stage. Intra-stage ordering is handled separately.

3. Use FinishToStart with lag_days = 0 for all dependencies (a work must finish before the next can start).

4. Do NOT create circular dependencies.
5. Do NOT create self-referencing dependencies.

=== EXAMPLES ===

If focus stage is Fundamenty (stage 1 - first stage):
- No predecessor exists (first stage starts immediately)
- Successor: a work from sciany/stropy stage depends on foundations
- Dependencies: [{{""predecessor_work_id"": ""guid-of-last-foundation-work"", ""successor_work_id"": ""guid-of-first-wall-work"", ""dependency_type"": ""FinishToStart"", ""lag_days"": 0}}]
-> Return 1 dependency (foundation work -> wall work)

If focus stage is Malowanie (stage 9):
- Predecessor: a work from posadzki/plytki that must finish before painting starts
- Successor: a work from bialy montaz that depends on painting finishing
- Dependencies: [{{""predecessor_work_id"": ""guid-of-floor-work"", ""successor_work_id"": ""guid-of-paint-work"", ""dependency_type"": ""FinishToStart"", ""lag_days"": 0}}, {{""predecessor_work_id"": ""guid-of-paint-work"", ""successor_work_id"": ""guid-of-white-install-work"", ""dependency_type"": ""FinishToStart"", ""lag_days"": 0}}]
-> Return 2 dependencies (floor -> paint, paint -> white install)

If focus stage is the LAST stage:
- Predecessor: a work from the previous stage
- No successor (last stage, nothing depends on it)
- Dependencies: [{{""predecessor_work_id"": ""guid-of-previous-work"", ""successor_work_id"": ""guid-of-last-work"", ""dependency_type"": ""FinishToStart"", ""lag_days"": 0}}]
-> Return 1 dependency (previous work -> this work)

=== OUTPUT ===

Respond with ONLY valid JSON — no markdown, no code fences, no explanations.
Use the EXACT format:
{{""dependencies"": [{{""predecessor_work_id"": ""guid"", ""successor_work_id"": ""guid"", ""dependency_type"": ""FinishToStart"", ""lag_days"": 0}}]}}

- For a predecessor dependency: predecessor_work_id = a work GUID from the preceding stage, successor_work_id = a work GUID from the focus stage.
- For a successor dependency: predecessor_work_id = a work GUID from the focus stage, successor_work_id = a work GUID from the following stage.
- Only include entries for dependencies that ACTUALLY EXIST. If no predecessor exists (first stage), simply do not include a predecessor entry.
- Maximum 2 entries total (at most 1 predecessor + at most 1 successor).

If the focus stage has no dependencies at all (e.g., it is the only stage), return: {{""dependencies"": []}}";
        }

        /// <summary>
        /// Extracts JSON from a response text (handles markdown fences, surrounding text)
        /// and deserializes to the specified type.
        /// </summary>
        private T? ParseJson<T>(string responseText, string agentName = "unknown") where T : class
        {
            int jsonStart = responseText.IndexOf('{');
            int jsonEnd = responseText.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
            {
                _logger.LogWarning(
                    "Agent {Agent} response does not contain valid JSON. Type: {Type}, Response: {Response}",
                    agentName, typeof(T).Name, responseText);
                return null;
            }

            string cleanJson = responseText[jsonStart..(jsonEnd + 1)].Trim();

            try
            {
                return JsonSerializer.Deserialize<T>(cleanJson, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "Agent {Agent} returned malformed JSON. Type: {Type}, " +
                    "RawResponseLength: {Length}, JsonStart: {JsonStart}, JsonEnd: {JsonEnd}, " +
                    "CleanJsonPreview: {Preview}",
                    agentName,
                    typeof(T).Name,
                    responseText.Length,
                    jsonStart,
                    jsonEnd,
                    cleanJson.Length > 200 ? cleanJson[..200] + "..." : cleanJson);
                throw new ValidationApiException(
                    $"Agent '{agentName}' returned invalid JSON. " +
                    $"The response could not be parsed. Please try again or contact support.");
            }
        }

        private static AIScheduleResult CalculateSchedule(
            AIScheduleRawResponse rawResponse,
            List<WorkInput> works,
            DateTime overallStartDate,
            DateTime overallEndDate)
        {
            // Build lookup: work_id -> duration_days
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
            Dictionary<Guid, (DateTime earliestStart, int lagDays)> dependencyConstraints =
                new Dictionary<Guid, (DateTime, int)>();

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
                        endDateByWorkId[currentId] = constraintStart.AddDays(
                            durationByWorkId.GetValueOrDefault(currentId, 1) - 1);
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
                            default: // FinishToStart — matches AdjustSuccessorPeriodsAsync (no +1)
                                successorStart = currentEnd.AddDays(lag);
                                break;
                        }
                    }
                    else
                    {
                        // Default: FinishToStart with lag=0 (successor may start on pred end day)
                        successorStart = currentEnd.AddDays(0);
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
                        endDateByWorkId[successorId] = finalStart.AddDays(
                            durationByWorkId.GetValueOrDefault(successorId, 1) - 1);
                        queue.Enqueue(successorId);
                    }
                }
            }

            // Detect cycles: nodes still with inDegree > 0 after Kahn's algorithm
            bool hasCycle = inDegree.Any(kvp => kvp.Value > 0);
            if (hasCycle)
            {
                throw new ValidationApiException(
                    "AI returned dependencies that form a cycle. Please regenerate the schedule.");
            }

            // Handle any works not reached (isolated works without deps — start at overallStartDate)
            foreach (WorkInput work in works)
            {
                if (!startDateByWorkId.ContainsKey(work.Id))
                {
                    DateTime wStart = overallStartDate;
                    startDateByWorkId[work.Id] = wStart;
                    endDateByWorkId[work.Id] = wStart.AddDays(
                        durationByWorkId.GetValueOrDefault(work.Id, 1) - 1);
                }
            }

            // Compress schedule into [overallStartDate, overallEndDate] when it overflows
            ScaleScheduleToOverallEndDate(
                startDateByWorkId,
                endDateByWorkId,
                durationByWorkId,
                parsedDeps,
                overallStartDate,
                overallEndDate);

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

        /// <summary>
        /// When max(EndDate) exceeds overallEndDate, proportionally compress offsets and durations
        /// so the chain fits in [overallStartDate, overallEndDate]. Min duration is 1 day.
        /// Throws if the window is still too short after compression.
        /// </summary>
        private static void ScaleScheduleToOverallEndDate(
            Dictionary<Guid, DateTime> startDateByWorkId,
            Dictionary<Guid, DateTime> endDateByWorkId,
            Dictionary<Guid, int> durationByWorkId,
            List<AIDependency> parsedDeps,
            DateTime overallStartDate,
            DateTime overallEndDate)
        {
            if (endDateByWorkId.Count == 0)
            {
                return;
            }

            DateTime maxEnd = endDateByWorkId.Values.Max();
            if (maxEnd <= overallEndDate)
            {
                return;
            }

            double availableDays = (overallEndDate - overallStartDate).TotalDays + 1;
            double usedDays = (maxEnd - overallStartDate).TotalDays + 1;
            if (usedDays <= 0)
            {
                return;
            }

            double scale = availableDays / usedDays;
            ApplyLinearScale(startDateByWorkId, endDateByWorkId, durationByWorkId, overallStartDate, overallEndDate, scale);
            EnforceFinishToStartAfterScale(startDateByWorkId, endDateByWorkId, durationByWorkId, parsedDeps);

            DateTime finalMaxEnd = endDateByWorkId.Values.Max();
            if (finalMaxEnd > overallEndDate)
            {
                throw new ValidationApiException(
                    "The overall schedule window is too short for the number of works " +
                    "even with minimum durations of 1 day each.");
            }
        }

        private static void ApplyLinearScale(
            Dictionary<Guid, DateTime> startDateByWorkId,
            Dictionary<Guid, DateTime> endDateByWorkId,
            Dictionary<Guid, int> durationByWorkId,
            DateTime overallStartDate,
            DateTime overallEndDate,
            double scale)
        {
            List<Guid> workIds = startDateByWorkId.Keys.ToList();
            foreach (Guid workId in workIds)
            {
                DateTime start = startDateByWorkId[workId];
                int duration = durationByWorkId.GetValueOrDefault(workId, 1);
                double offset = (start - overallStartDate).TotalDays * scale;
                int scaledDuration = Math.Max(1, (int)Math.Floor(duration * scale));

                DateTime newStart = overallStartDate.AddDays(offset);
                DateTime newEnd = newStart.AddDays(scaledDuration - 1);
                if (newEnd > overallEndDate)
                {
                    newEnd = overallEndDate;
                }

                startDateByWorkId[workId] = newStart;
                endDateByWorkId[workId] = newEnd;
                durationByWorkId[workId] = scaledDuration;
            }
        }

        /// <summary>
        /// One topological pass: if successor starts before predecessor end + lag (FinishToStart),
        /// shift the successor forward to preserve dependency order after scaling.
        /// </summary>
        private static void EnforceFinishToStartAfterScale(
            Dictionary<Guid, DateTime> startDateByWorkId,
            Dictionary<Guid, DateTime> endDateByWorkId,
            Dictionary<Guid, int> durationByWorkId,
            List<AIDependency> parsedDeps)
        {
            Dictionary<Guid, List<(Guid succId, int lag)>> successorsByPred =
                new Dictionary<Guid, List<(Guid, int)>>();
            Dictionary<Guid, int> inDegree = new Dictionary<Guid, int>();

            foreach (Guid workId in startDateByWorkId.Keys)
            {
                successorsByPred[workId] = new List<(Guid, int)>();
                inDegree[workId] = 0;
            }

            foreach (AIDependency dep in parsedDeps)
            {
                if (!Guid.TryParse(dep.predecessor_work_id, out Guid predId) ||
                    !Guid.TryParse(dep.successor_work_id, out Guid succId))
                {
                    continue;
                }

                if (!startDateByWorkId.ContainsKey(predId) || !startDateByWorkId.ContainsKey(succId))
                {
                    continue;
                }

                string depType = dep.dependency_type?.ToLowerInvariant() ?? "finishtostart";
                if (depType is not ("finishtostart" or ""))
                {
                    continue;
                }

                int lag = dep.lag_days ?? 0;
                successorsByPred[predId].Add((succId, lag));
                inDegree[succId] = inDegree.GetValueOrDefault(succId) + 1;
            }

            Queue<Guid> queue = new Queue<Guid>(
                inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));

            while (queue.Count > 0)
            {
                Guid currentId = queue.Dequeue();
                DateTime predEnd = endDateByWorkId[currentId];

                foreach ((Guid succId, int lag) in successorsByPred[currentId])
                {
                    DateTime minStart = predEnd.AddDays(lag);
                    if (startDateByWorkId[succId] < minStart)
                    {
                        int duration = durationByWorkId.GetValueOrDefault(succId, 1);
                        startDateByWorkId[succId] = minStart;
                        endDateByWorkId[succId] = minStart.AddDays(duration - 1);
                    }

                    inDegree[succId]--;
                    if (inDegree[succId] == 0)
                    {
                        queue.Enqueue(succId);
                    }
                }
            }
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

        // ─── Response DTOs for individual agents ─────────────────────────────

        /// <summary>
        /// Response from schedule-duration-agent: only durations.
        /// </summary>
        internal sealed class DurationResponse
        {
            public List<AIDuration> durations { get; init; } = [];
        }

        /// <summary>
        /// Response from schedule-dependency-agent: only dependencies.
        /// </summary>
        internal sealed class DependencyResponse
        {
            public List<AIDependency> dependencies { get; init; } = [];
        }

        // ─── Raw response models (shared) ────────────────────────────────────

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
}
