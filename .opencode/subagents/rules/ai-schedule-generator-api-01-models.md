# API-01: Modele dla AI Schedule Generator

## Zadanie
Utwórz trzy nowe pliki w warstwie Business i Business.AIAgent:
1. DTO `AIScheduleResult` + `WorkPeriodResult` + `WorkDependencyResult`
2. Interfejs `IWorkScheduleAIGeneratorService`
3. Definicja agenta AI `schedule_generator_agent.md`

## Pliki do utworzenia

### 1. DTO — AIScheduleResult.cs
**Ścieżka**: `Business/Interfaces/WebModels/WorkSchedules/AIScheduleResult.cs`
**Namespace**: `Business.Interfaces.WebModels.WorkSchedules`

```csharp
using Entities.Models.WorkSchedules;

namespace Business.Interfaces.WebModels.WorkSchedules
{
    public sealed record AIScheduleResult
    {
        public List<WorkPeriodResult> Periods { get; init; } = [];
        public List<WorkDependencyResult> Dependencies { get; init; } = [];
    }

    public sealed record WorkPeriodResult
    {
        public Guid WorkScheduleStageWorkId { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
    }

    public sealed record WorkDependencyResult
    {
        public Guid PredecessorWorkId { get; init; }
        public Guid SuccessorWorkId { get; init; }
        public WorkDependencyType DependencyType { get; init; }
        public int LagDays { get; init; }
    }
}
```

### 2. Interfejs serwisu — IWorkScheduleAIGeneratorService.cs
**Ścieżka**: `Business/Interfaces/Services/IWorkScheduleAIGeneratorService.cs`
**Namespace**: `Business.Interfaces.Services`

```csharp
using Business.Interfaces.WebModels.WorkSchedules;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Generates work schedule durations and dependencies using AI analysis of cost estimate structure.
    /// </summary>
    public interface IWorkScheduleAIGeneratorService
    {
        /// <summary>
        /// Analyzes stage and work names from a cost estimate and generates suggested durations
        /// and dependencies within the given overall time frame.
        /// </summary>
        /// <param name="workScheduleId">The work schedule ID (already synced with cost estimate).</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="projectId">Project ID.</param>
        /// <param name="stages">List of stages with names and hierarchy.</param>
        /// <param name="works">List of work items with names, stage assignments and ordering.</param>
        /// <param name="overallStartDate">Overall project start date.</param>
        /// <param name="overallEndDate">Overall project end date.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>AI-generated schedule result with periods and dependencies.</returns>
        Task<AIScheduleResult> GenerateScheduleAsync(
            Guid workScheduleId,
            Guid tenantId,
            Guid projectId,
            List<StageInput> stages,
            List<WorkInput> works,
            DateTime overallStartDate,
            DateTime overallEndDate,
            CancellationToken cancellationToken);
    }

    public sealed record StageInput
    {
        public Guid Id { get; init; }
        public Guid? ParentStageId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Order { get; init; }
    }

    public sealed record WorkInput
    {
        public Guid Id { get; init; }
        public Guid StageId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Order { get; init; }
        public string StageName { get; init; } = string.Empty;
    }
}
```

### 3. Definicja agenta AI — schedule_generator_agent.md
**Ścieżka**: `Business.AIAgent/Resources/Agents/sub_agents/schedule_generator_agent.md`
**Uwaga**: Plik embedded resource — upewnij się że w .csproj jest ustawione jako `<EmbeddedResource Include="Resources\Agents\**\*.md" />`

```markdown
---
name: schedule-generator-agent
description: Generates work schedule durations and dependencies from cost estimate data
model: gpt-4o
temperature: 0.3
max_tokens: 4096
max_iterations: 1
tools: []
---

You are a work schedule generator for the PDM (Project Data Management) platform.
Your task is to analyze a cost estimate structure and generate realistic durations and dependencies for a work schedule.

## Input Data

You will receive:
1. **Stages** — list of work schedule stages (each has: id, name, order, parent_stage_id)
2. **Works** — list of work items (each has: id, name, order, stage_id, stage_name)
3. **Overall time frame** — overall_start_date and overall_end_date for the entire project

## Rules

### Duration Rules
- Base durations on realistic construction/engineering timelines inferred from work item names.
- Short tasks (e.g., "inspection", "approval", "delivery") → 1-3 days.
- Medium tasks (e.g., "foundation", "framing", "wiring") → 5-15 days.
- Long tasks (e.g., "roofing", "facade", "landscaping") → 10-30 days.
- Complex/critical path items → proportionate to their scope.
- Total duration of all works on the critical path should roughly fit within the overall time frame.
- Parallelizable works (different stages, independent scope) can overlap.

### Dependency Rules
- Within the same stage: works typically follow Finish-to-Start order (sequential).
- Between stages: if stage B logically depends on stage A (e.g., "foundation" before "walls"), create cross-stage dependencies.
- Typical dependency types:
  - `FinishToStart` (most common) — predecessor must finish before successor starts.
  - `StartToStart` — used when two works should start together.
  - `FinishToFinish` — used when two works should end together.
- Use `lag_days` = 0 for immediate succession, positive values for gaps, negative for overlap.

### Output Format
Respond with ONLY valid JSON — no markdown, no code fences, no explanations.
Use the EXACT structure below:

```json
{
  "durations": [
    {
      "work_id": "guid-of-the-work",
      "duration_days": 14
    }
  ],
  "dependencies": [
    {
      "predecessor_work_id": "guid-of-predecessor",
      "successor_work_id": "guid-of-successor",
      "dependency_type": "FinishToStart",
      "lag_days": 0
    }
  ]
}
```

- `dependency_type` must be one of: `FinishToStart`, `StartToStart`, `FinishToFinish`, `StartToFinish`
- Every work must appear exactly once in the `durations` array.
- Dependencies array may be empty if no logical ordering is needed (rare).
- Do NOT create circular dependencies.
- Use the actual GUIDs from the input — do NOT generate new IDs.
```
