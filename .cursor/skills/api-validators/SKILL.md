---
name: api-validators
description: Tworzenie walidatorów FluentValidation dla Commands i Queries. Użyj gdy tworzysz lub modyfikujesz walidator (*Validator.cs).
---

# Skill: API / Walidatory

## Opis
Tworzenie walidatorów FluentValidation dla Commands i Queries.

## Kiedy używać
Użyj tego skilla gdy tworzysz lub modyfikujesz walidator (*Validator.cs).

---

## Lokalizacja

```
src/CQRS/{Domena}/{NazwaOperacji}/{Nazwa}CommandValidator.cs
```

## Wzorzec

```csharp
public sealed class CreateProjectCommandValidator
    : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("'Name' is required.")
            .MaximumLength(200).WithMessage("'Name' must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);
    }
}
```

## Extension methods (CommonValidationExtensions)

```csharp
// Guid fields:
RuleFor(x => x.TenantId).RequiredId();

// int Order:
RuleFor(x => x.Order).NonNegativeOrder();

// List<Guid> unique:
RuleFor(x => x.UserIds).UniqueIds();

// Self-check UserId:
RuleFor(x => x.UserId).NotCurrentUser(currentUser);

// Hex color:
RuleFor(x => x.ColorRgb).ValidColorRgb();
```

## Async validator (MustAsync)

```csharp
RuleFor(x => x.UserId)
    .MustAsync(async (userId, ct) =>
        !await memberRepo.AnyAsync(
            m => m.UserId == userId && m.ProjectId == command.ProjectId,
            ct))
    .WithMessage("User is already a member of this project.");
```

## Walidator z klasą bazową

```csharp
public abstract class TrackedCostCommandBaseValidator<T>
    : AbstractValidator<T> where T : TrackedCostCommandBase
{
    protected TrackedCostCommandBaseValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
    }
}

public sealed class CreateTrackedCostCommandValidator
    : TrackedCostCommandBaseValidator<CreateTrackedCostCommand>
{
    public CreateTrackedCostCommandValidator()
    {
        // tylko reguły specyficzne dla Create
        RuleFor(x => x.Net).GreaterThanOrEqualTo(0).When(x => x.Net.HasValue);
    }
}
```

## Zasady

- Walidator zawsze `sealed`
- Jeden walidator per Command/Query
- Używaj `RequiredId()` zamiast `NotEmpty()` dla pól Guid
- Komunikaty po angielsku
- `TenantId` przed `ProjectId` (kolejność spójna z innymi domenami)
- Walidatory dla Queries też — `TenantId.RequiredId()`, `ProjectId.RequiredId()`
