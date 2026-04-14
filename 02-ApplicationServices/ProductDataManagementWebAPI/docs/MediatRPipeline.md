# Potok MediatR — Behaviours

## Przegląd

Każde żądanie (`Command` lub `Query`) przesyłane przez `MediatR` przechodzi przez potok trzech zachowań (behaviours) zanim dotrze do właściwego handlera. Kolejność wykonania:

```
Request
  │
  ▼
AuthorizationBehavior      ← (1) weryfikacja uprawnień
  │
  ▼
ValidationBehavior          ← (2) walidacja struktury żądania
  │
  ▼
TransactionBehavior         ← (3) zarządzanie transakcją DB (tylko Commands)
  │
  ▼
Handler                     ← właściwa logika biznesowa
```

---

## 1. AuthorizationBehavior

**Plik:** `src/CQRS/Behaviours/AuthorizationBehavior.cs`

### Odpowiedzialność

Weryfikuje, czy zalogowany użytkownik (`ICurrentUser`) posiada wymagane uprawnienie do wykonania żądania. Działa wyłącznie dla żądań implementujących `IAuthorizableRequest`.

### Przepływ

```
Request implementuje IAuthorizableRequest?
  ├─ NIE  → przekaż dalej bez sprawdzania
  └─ TAK  → pobierz PermissionCode + Resource + ResourceScope
              │
              ▼
          AccessService.AuthorizeAsync(...)
              │
              ├─ BRAK DOSTĘPU → throw ForbiddenApiException
              └─ DOSTĘP       → przekaż dalej
```

### Interfejs IAuthorizableRequest

```csharp
// Żądanie musi udostępniać:
string PermissionCode          // kod uprawnienia np. "WorkSchedule.Write"
ResourceContext GetResource()  // TenantId, ProjectId itp.
ResourceScope GetResourceScope()
```

### Wyjątki

| Sytuacja | Wyjątek | HTTP |
|:---------|:--------|:----:|
| Brak uprawnienia | `ForbiddenApiException` | 403 |

### Logowanie

- `LogDebug` — start autoryzacji z parametrami
- `LogDebug` — sukces autoryzacji
- `LogWarning` — nieudana autoryzacja (użytkownik, typ żądania, kod uprawnienia)

---

## 2. ValidationBehavior

**Plik:** `src/CQRS/Behaviours/ValidationBehavior.cs`

### Odpowiedzialność

Uruchamia wszystkie zarejestrowane w DI walidatory FluentValidation dla danego żądania. Jeśli walidacja wykryje błędy — zgłasza wyjątek przed wywołaniem handlera.

### Przepływ

```
Czy istnieją walidatory dla TRequest?
  ├─ NIE  → przekaż dalej bez walidacji
  └─ TAK  → uruchom wszystkie walidatory równolegle (Task.WhenAll)
              │
              ▼
          Zbierz wszystkie błędy ValidationFailure
              │
              ├─ BRAK BŁĘDÓW → przekaż dalej
              └─ SĄ BŁĘDY   → throw ValidationApiException(komunikat)
```

### Format komunikatu błędu

Każdy błąd jest opisany jako:

```
Property name: {PropertyName}, Error: {ErrorMessage}, Severity: {Severity}
```

Wielokrotne błędy są łączone przecinkiem.

**Przykład:**
```
Property name: Name, Error: 'Name' must not be empty., Severity: Error,
Property name: LagDays, Error: Lag days must be between -365 and 365., Severity: Error
```

### Wyjątki

| Sytuacja | Wyjątek | HTTP |
|:---------|:--------|:----:|
| Błędy walidacji | `ValidationApiException` | 422 |

### Ważne zasady

- Walidatory są uruchamiane **równolegle** (`Task.WhenAll`) — nie zakładaj kolejności
- Walidator dla danego żądania rejestruje się automatycznie przez `AddValidatorsFromAssembly` w DI
- Brak zarejestrowanego walidatora = żądanie przechodzi bez walidacji
- `MustAsync` w walidatorze może wykonywać zapytania DB (np. sprawdzenie istnienia encji)

### Odpowiedzialność walidatorów vs handlerów

| Sprawdzenie | Gdzie |
|:------------|:------|
| Wymagane pola, formaty, długości, zakresy | Validator |
| Istnienie encji w DB (tylko: czy istnieje) | Validator (`MustAsync`) |
| Reguły biznesowe wymagające właściwości encji | Handler |
| Izolacja tenant (TenantId zgodny z currentUser) | Handler |
| Autoryzacja właściciela zasobu | Handler |

---

## 3. TransactionBehavior

**Plik:** `src/CQRS/Behaviours/TransactionBehavior.cs`

### Odpowiedzialność

Opakowuje wykonanie **Command** w transakcję bazodanową i automatycznie wywołuje `SaveChangesAsync` po zakończeniu handlera. Zapytania (`Query`) są pomijane.

### Przepływ

```
Request implementuje IRequestCommand<TResponse>?
  ├─ NIE (Query) → przekaż dalej bez transakcji
  └─ TAK (Command)
        │
        ▼
    CreateExecutionStrategy()   ← obsługa retry dla Azure SQL
        │
        ▼
    BeginTransactionAsync()
        │
        ▼
    Handler (logika biznesowa)
        │
        ▼
    SaveChangesAsync()          ← automatyczny zapis zmian
        │
        ▼
    CommitAsync()
```

### Kluczowe konsekwencje

- **`SaveChangesAsync` NIE MOŻE być wywoływany jawnie w handlerach** (za wyjątkiem sytuacji gdy potrzebny jest DB-generated Id jako foreign key w kolejnej operacji)
- Wyjątek rzucony w handlerze = rollback transakcji
- `CreateExecutionStrategy` zapewnia automatyczne retry przy przejściowych błędach sieci (Azure SQL transient faults)
- Queries nigdy nie powinny modyfikować stanu — `TransactionBehavior` tego nie wymusza, ale jest to wymóg architektoniczny

### Wyjątki

Wyjątki z handlera są propagowane bez zmian — `TransactionBehavior` nie przechwytuje ich.

---

## Interfejsy CQRS

### IRequestCommand\<TResponse\>

```csharp
public interface IRequestCommand<TResponse> : IRequest<TResponse> { }
```

Marker interface dla **Commands** — żądań zmieniających stan systemu.  
Rozpoznawany przez `TransactionBehavior` jako sygnał do otworzenia transakcji.

**Dozwolone typy zwracane:** `Unit`, `Guid`, prosty obiekt rezultatu.  
**Niedozwolone:** złożone projekcje DTO — to rola Query.

### IRequestQuery\<TResponse\>

```csharp
public interface IRequestQuery<IResponse> : IRequest<IResponse> { }
```

Marker interface dla **Queries** — żądań tylko do odczytu.  
`TransactionBehavior` pomija ten typ.

**Zwracane typy:** wyłącznie DTOs z sufiksem `*Web`.

---

## Rejestracja w DI

```csharp
// ServiceCollectionExtensions.cs
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(SomeHandler).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
});

services.AddValidatorsFromAssembly(typeof(SomeValidator).Assembly);
```

> Kolejność rejestracji `AddBehavior` determinuje kolejność wykonania w potoku.

---

## Mapowanie wyjątków na HTTP

Wszystkie wyjątki są przechwytywane przez `ApiExceptionMiddleware`:

| Wyjątek | HTTP | Opis |
|:--------|:----:|:-----|
| `ValidationApiException` | 422 | Błędy walidacji FluentValidation |
| `NotFoundApiException` | 404 | Encja nie istnieje lub brak dostępu do niej |
| `ForbiddenApiException` | 403 | Brak uprawnień, naruszenie izolacji tenant |
| `UnauthorizedApiException` | 401 | Brak uwierzytelnienia |
| `ConflictApiException` | 409 | Konflikt stanu (duplikat, naruszenie unique) |
