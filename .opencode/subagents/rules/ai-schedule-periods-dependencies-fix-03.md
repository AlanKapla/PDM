# Refactor 03 — Cleanup serwisu AI: concurrency, .Result, dead agent, docs

## Decyzje domenowe
- Limit concurrency: max **4** równoległe wywołania `IAgentRunner.RunAsync` (SemaphoreSlim).
- Usunąć dead `schedule_generator_agent.md`.
- Zaktualizować feature doc do per-stage duration/dependency.
- Unikać `.Result` — zbierać wyniki z `Task.WhenAll` przez awaitowane tablice.

## Zmiany

### 1. `WorkScheduleAIGeneratorService.cs`

**Zamiast** osobnych list Task + `.Result`:
```csharp
AgentRunResult[] durationResults = await Task.WhenAll(durationTasks);
AgentRunResult[] dependencyResults = await Task.WhenAll(dependencyTasks);
// potem durationResults[i] / dependencyResults[i]
```

**Concurrency**: opakuj każde `RunAsync` w helper:
```csharp
private async Task<AgentRunResult> RunWithLimitAsync(
    SemaphoreSlim gate,
    string agentName,
    string prompt,
    AgentContext context,
    CancellationToken ct)
{
    await gate.WaitAsync(ct);
    try
    {
        return await _agentRunner.RunAsync(agentName, prompt, context, ct);
    }
    finally
    {
        gate.Release();
    }
}
```
Użyj jednego `SemaphoreSlim(4)` współdzielonego dla duration+dependency tasks w danym `GenerateScheduleAsync` (dispose na końcu metody przez `using`).

Usuń nieużywaną lokalną zmienną `key` przy dedupe (N1) jeśli nadal istnieje.

### 2. Usuń dead agent
Usuń plik:
`src/Business.AIAgent/Resources/Agents/sub_agents/schedule_generator_agent.md`

Sprawdź czy jest hardcodowane odwołanie do `"schedule-generator-agent"` w C# — jeśli tak, usuń/zamień. Nie rejestruj go nigdzie.

### 3. Dokumentacja
Zaktualizuj `/workspace/.opencode/features/ai-schedule-generator.md`:
- Runtime: per-stage `schedule-duration-agent` + `schedule-dependency-agent` (równolegle, limit concurrency).
- Monolityczny `schedule-generator-agent` / tool `analyze_schedule_structure` — nieaktualne.
- Intra-stage FS dodawane w kodzie wg Order.
- OverallEndDate skaluje łańcuch.

Zaktualizuj `/workspace/.opencode/subagents/rules/ai-schedule-generator-summary.md` krótką notką „superseded by ai-schedule-periods-dependencies-*” lub zaktualizuj opis agentów.

### 4. (Opcjonalnie jeśli proste) Handler slim
W `GenerateScheduleFromEstimateAICommandHandler` wydziel prywatne metody:
- `LoadStagesAndWorksAsync`
- `BuildWorkInputs`
- `PersistPeriodsAsync`
- `PersistDependenciesAsync`
tak aby `Handle` miał ~20–40 linii orkiestracji. Nie twórz nowego batch command w tym prompcie.

## Zakaz
- Zakaz `var`
- Nie zmieniaj UI poza docs
- Nie rozbijaj jeszcze na osobne klasy plików (PromptBuilder/Calculator) — to osobny duży refaktor; tu tylko concurrency + cleanup

## Weryfikacja
```
dotnet build 02-ApplicationServices/ProductDataManagementWebAPI --configuration Release
```
Upewnij się że usunięty .md nie jest wymagany jako EmbeddedResource z hard fail — sprawdź csproj jeśli build failuje.

## Raport
Status build, pliki, blokery.
