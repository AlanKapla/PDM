## Clean Code, SOLID & DRY

### General
- Code must be self-explanatory — names of classes, methods and variables must reveal intent
- Methods do one thing only (Single Responsibility)
- No magic numbers or strings — use named constants or enums
- Avoid deep nesting — extract to private methods or guard clauses

### Handlers must be thin orchestrators
Handler `Handle` method contains ONLY:
1. Load and validate required data (via private methods)
2. Execute business logic
3. Return result

NEVER inline in Handle: access checks, entity fetching, mapping, multi-step logic

### Extract to private methods when:
- Logic is used more than once (DRY)
- A block of code can be named meaningfully (Clean Code)
- A method exceeds ~20 lines (readability)

Private method naming: `GetAndValidate{Entity}Async`, `Validate{Rule}`, `Map{Entity}To{Dto}`

### Extract to base class when:
- Logic is shared across multiple Handlers in the same feature area
- Examples: `CostTrackerHandlerBase`, `ProjectHandlerBase`
- Base class methods must remain protected and async

### SOLID in Handlers
- S — one Handler, one use case. Never handle multiple commands in one class
- O — extend via base class or composition, never modify shared logic directly  
- D — depend on interfaces (`IRepository<T>`, `ICurrentUser`), never on concrete implementations

### DRY — forbidden patterns
- NEVER repeat access validation logic inline across handlers → extract to base class or private method
- NEVER repeat entity-fetching + null-check blocks → extract to `GetAndValidate{Entity}Async`
- NEVER duplicate mapping logic → extract to private `Map` method or dedicated mapper

### Explicit Types & Braces
- ALWAYS use explicit types — `var` is FORBIDDEN
```csharp
  // FORBIDDEN
  var project = await _repository.GetByIdAsync(id);
  
  // CORRECT
  Project project = await _repository.GetByIdAsync(id);
```
- ALWAYS use braces `{}` for every block — even single-line `if`, `else`, `for`, `foreach`
```csharp
  // FORBIDDEN
  if (project == null) throw new NotFoundApiException("Project not found.");

  // CORRECT
  if (project == null)
  {
      throw new NotFoundApiException("Project not found.");
  }
```

### Records & Value Objects
- ALWAYS use `record` types for DTOs, query results, CQRS query and command and any immutable data structures
- Records must use **explicit properties** with `{ get; init; }` — never mutable `{ get; set; }`
- NEVER use `class` for data transfer objects or read-only results

\```csharp
// FORBIDDEN — mutable class
public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

// FORBIDDEN — mutable record
public record ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

// CORRECT
public record ProjectDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
\```

- Command and Query objects passed to handlers should also be records:
\```csharp
// CORRECT
public record CreateProjectCommand
{
    public required string Name { get; init; }
    public required Guid OwnerId { get; init; }
}

public record GetProjectQuery
{
    public required Guid ProjectId { get; init; }
}
\```
