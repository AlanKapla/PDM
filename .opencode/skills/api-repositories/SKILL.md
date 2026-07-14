---
name: api-repositories
description: "Wzorce używania IRepository<T> i IReadRepository<T> w handlerach i serwisach. Użyj gdy piszesz zapytania do bazy danych przez repozytorium."
---

# Skill: API / Repozytoria

## Opis
Wzorce używania IRepository<T> i IReadRepository<T> w handlerach i serwisach.

## Kiedy używać
Użyj tego skilla gdy piszesz zapytania do bazy danych przez repozytorium.

---

## Interfejsy

```csharp
// IReadRepository<T> — tylko odczyt (extends IRepository<T>)
// Używaj gdy handler tylko czyta dane

// IRepository<T> — odczyt + zapis
// Używaj gdy handler zapisuje dane
```

## Metody dostępne

```csharp
// Pobieranie pojedynczego rekordu
T? entity = await repo.GetFirstBySearch(
    e => e.TenantId == tenantId && e.Id == id,
    cancellationToken);

// Z Include
T? entity = await repo.GetFirstBySearch(
    e => e.TenantId == tenantId && e.Id == id,
    cancellationToken,
    q => q.Include(e => e.Members));

// Pobieranie kolekcji
IEnumerable<T> entities = await repo.GetBySearch(
    e => e.TenantId == tenantId && e.ProjectId == projectId,
    cancellationToken);

// Pobieranie po Id
T? entity = await repo.GetById(id, cancellationToken);

// Projekcja (optymalizacja)
List<Guid> ids = await repo.SelectAsync(
    e => e.TenantId == tenantId,
    e => e.Id,
    cancellationToken);

// Sprawdzenie istnienia
bool exists = await repo.AnyAsync(
    e => e.Name == name && e.TenantId == tenantId,
    cancellationToken);

// Liczenie
int count = await repo.CountAsync(
    e => e.ProjectId == projectId,
    cancellationToken);

// Zapis
await repo.Insert(entity);
await repo.InsertRange(entities);
await repo.Update(entity);
await repo.SaveChangesAsync(cancellationToken);

// Bulk operacje (bez ładowania do pamięci)
await repo.ExecuteUpdateAsync(
    e => e.ProjectId == projectId,
    e => e.SetProperty(x => x.IsDeleted, true),
    cancellationToken);

await repo.ExecuteDeleteAsync(
    e => e.ProjectId == projectId,
    cancellationToken);
```

## Wzorzec GetAndValidate

```csharp
private async Task<Project> GetAndValidateProjectAsync(
    Guid tenantId, Guid projectId, CancellationToken ct)
{
    Project? project = await projectRepo.GetFirstBySearch(
        p => p.TenantId == tenantId && p.Id == projectId,
        ct);

    if (project is null)
    {
        throw new NotFoundApiException(nameof(Project), projectId.ToString());
    }

    return project;
}
```

## Zasady

- Predykaty **zawsze** zawierają `TenantId` i `ProjectId` (jeśli encja je ma)
- `IReadRepository<T>` dla handlerów Query i serwisów tylko odczytujących
- `IRepository<T>` dla handlerów Command
- `ExecuteUpdateAsync` / `ExecuteDeleteAsync` dla bulk operacji (nie ładuj do pamięci)
- `SelectAsync` dla projekcji gdy potrzebujesz tylko podzbioru pól
- Zakaz `var` — zawsze explicit type w wynikach zapytań
