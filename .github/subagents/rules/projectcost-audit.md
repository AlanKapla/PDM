# Audyt domeny ProjectCost

## BLOK 1 — INWENTARYZACJA

| Plik | Typ | Ścieżka |
|------|-----|---------|
| CreateProjectCostCommand.cs | Command | [src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommand.cs) |
| CreateProjectCostCommandValidator.cs | Validator | [src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandValidator.cs) |
| CreateProjectCostCommandHandler.cs | Handler | [src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandHandler.cs) |
| UpdateProjectCostCommand.cs | Command | [src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommand.cs) |
| UpdateProjectCostCommandValidator.cs | Validator | [src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandValidator.cs) |
| UpdateProjectCostCommandHandler.cs | Handler | [src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandHandler.cs) |
| DeleteProjectCostCommand.cs | Command | [src/CQRS/ProjectCosts/DeleteProjectCost/DeleteProjectCostCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/DeleteProjectCost/DeleteProjectCostCommand.cs) |
| DeleteProjectCostCommandHandler.cs | Handler | [src/CQRS/ProjectCosts/DeleteProjectCost/DeleteProjectCostCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/DeleteProjectCost/DeleteProjectCostCommandHandler.cs) |
| ShareProjectCostsCommand.cs | Command | [src/CQRS/ProjectCosts/ShareProjectCosts/ShareProjectCostsCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/ShareProjectCosts/ShareProjectCostsCommand.cs) |
| ShareProjectCostsCommandValidator.cs | Validator | [src/CQRS/ProjectCosts/ShareProjectCosts/ShareProjectCostsCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/ShareProjectCosts/ShareProjectCostsCommandValidator.cs) |
| ShareProjectCostsCommandHandler.cs | Handler | [src/CQRS/ProjectCosts/ShareProjectCosts/ShareProjectCostsCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/ShareProjectCosts/ShareProjectCostsCommandHandler.cs) |
| UpdateCostShareCommand.cs | Command | [src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommand.cs) |
| UpdateCostShareCommandValidator.cs | Validator | [src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommandValidator.cs) |
| UpdateCostShareCommandHandler.cs | Handler | [src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommandHandler.cs) |
| GetProjectCostsQuery.cs | Query | [src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQuery.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQuery.cs) |
| GetProjectCostsQueryHandler.cs | Handler | [src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs) |
| ProjectCostHandlerBase.cs | Klasa bazowa | [src/CQRS/ProjectCosts/Shared/ProjectCostHandlerBase.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/Shared/ProjectCostHandlerBase.cs) |
| ProjectCostListItemWeb.cs | WebModel | [src/Business/Interfaces/WebModels/ProjectCosts/ProjectCostListItemWeb.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Business/Interfaces/WebModels/ProjectCosts/ProjectCostListItemWeb.cs) |
| SharedProjectCostWeb.cs | WebModel | [src/Business/Interfaces/WebModels/ProjectCosts/SharedProjectCostWeb.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Business/Interfaces/WebModels/ProjectCosts/SharedProjectCostWeb.cs) |
| ProjectCostController.cs | Controller | [src/WebApi/Controllers/ProjectCostController.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/Controllers/ProjectCostController.cs) |
| ProjectCost.cs | Entity | [src/Entities/Models/Costs/ProjectCost.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Entities/Models/Costs/ProjectCost.cs) |
| SharedProjectCost.cs | Entity | [src/Entities/Models/Costs/SharedProjectCost.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Entities/Models/Costs/SharedProjectCost.cs) |

## BLOK 2 — COMMANDS I QUERIES — STRUKTURA

### 2.1 Positional parameters vs explicit properties

| Command/Query | Używa positional params | Przykład |
|--------------|------------------------|---------|
| CreateProjectCostCommand | NIE (explicit init) | `public Guid TenantId { get; init; }` |
| UpdateProjectCostCommand | NIE (explicit init) | `public Guid TenantId { get; init; }` |
| UpdateCostShareCommand | NIE (explicit init) | `public Guid TenantId { get; init; }` |
| ShareProjectCostsCommand | NIE (explicit init) | `public Guid TenantId { get; init; }` |
| **DeleteProjectCostCommand** | **TAK** | `public sealed record DeleteProjectCostCommand(Guid TenantId, Guid ProjectId, Guid CostId)` |
| **GetProjectCostsQuery** | **TAK** | `public sealed record GetProjectCostsQuery(Guid TenantId, Guid ProjectId, ResourceScope Scope)` |

Brak również modyfikatora `required` w żadnym Command/Query domeny — wszystkie używają wartości domyślnych (`= string.Empty`, `= new()`) lub nie wymuszają obecności.

### 2.2 Sealed

| Command/Query | Jest sealed | Uwagi |
|--------------|------------|-------|
| CreateProjectCostCommand | NIE | brak modyfikatora |
| UpdateProjectCostCommand | NIE | brak modyfikatora |
| UpdateCostShareCommand | NIE | brak modyfikatora |
| ShareProjectCostsCommand | NIE | brak modyfikatora |
| DeleteProjectCostCommand | TAK | `public sealed record` |
| GetProjectCostsQuery | TAK | `public sealed record` |

### 2.3 Interfejsy i autoryzacja

| Command/Query | Interfejs | IAuthorizableRequest | PermissionCode |
|--------------|-----------|---------------------|----------------|
| CreateProjectCostCommand | IRequestCommand<Guid> | TAK | ProjectResourcesWrite |
| UpdateProjectCostCommand | IRequestCommand<Unit> | TAK | ProjectResourcesWrite |
| DeleteProjectCostCommand | IRequestCommand<Unit> | TAK | ProjectResourcesWrite |
| ShareProjectCostsCommand | IRequestCommand<Unit> | TAK | ProjectResourcesShare |
| UpdateCostShareCommand | IRequestCommand<Unit> | TAK | ProjectResourcesWrite (powinno być ProjectResourcesShare — to operacja udostępniania) |
| GetProjectCostsQuery | IRequestQuery<...> | TAK + GetResourceScope | ProjectView |

### 2.4 Wspólne pola — kandydaci do klasy bazowej

| Pole wspólne | Występuje w | Kandydat do wydzielenia |
|-------------|------------|------------------------|
| TenantId, ProjectId | wszystkie 6 Commands/Queries | TAK — `ProjectCostCommandBase` z `TenantId`/`ProjectId` + `ResourceRef` |
| CostId | Update, Delete, UpdateCostShare | TAK — sub-base `ProjectCostScopedCommandBase` |
| Name, Place, Date, Description, NetAmount, GrossAmount, IsAccepted, Document | Create + Update | TAK — wspólny `record` lub bazowy abstrakcyjny `ProjectCostMutationCommandBase` |
| SharedWithUserIds | UpdateCostShare, ShareProjectCosts | częściowy duplikat (lista użytkowników udostępnienia) |

## BLOK 3 — WALIDATORY

### 3.1 Pokrycie walidatorami

| Command/Query | Walidator | Brakujące reguły |
|--------------|----------|-----------------|
| CreateProjectCostCommand | TAK | brak `RequiredId()` na TenantId/ProjectId |
| UpdateProjectCostCommand | TAK | brak `RequiredId()` na TenantId/ProjectId/CostId |
| DeleteProjectCostCommand | **NIE** | całkowity brak walidatora — TenantId, ProjectId, CostId niezweryfikowane |
| ShareProjectCostsCommand | TAK | brak `RequiredId()` na TenantId/ProjectId; brak `UniqueIds()` (manualna implementacja w UpdateCostShare jest, tu jej brak) |
| UpdateCostShareCommand | TAK | brak `RequiredId()` na TenantId/ProjectId/CostId; ręczna kontrola unikalności zamiast `UniqueIds()` |
| GetProjectCostsQuery | **NIE** | brak walidatora; nie sprawdzana wartość Scope |

Pokrycie walidatorami: **4/6 (67%)** — brak dla `DeleteProjectCostCommand` oraz `GetProjectCostsQuery`.

### 3.2 Reguły szczegółowe

Domena nie korzysta z `CommonValidationExtensions` ([src/CQRS/Extensions/CommonValidationExtensions.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/Extensions/CommonValidationExtensions.cs)) wcale.

| Walidator | Pole | Obecna reguła | Brakująca reguła | Uzasadnienie |
|-----------|------|--------------|-----------------|-------------|
| CreateProjectCostCommandValidator | TenantId | brak | `.RequiredId()` | spójność z innymi domenami |
| CreateProjectCostCommandValidator | ProjectId | brak | `.RequiredId()` | jw. |
| UpdateProjectCostCommandValidator | TenantId/ProjectId/CostId | brak | `.RequiredId()` | jw. |
| ShareProjectCostsCommandValidator | TenantId/ProjectId | brak | `.RequiredId()` | jw. |
| ShareProjectCostsCommandValidator | ProjectCostIds | NotNull, NotEmpty, Count<=50 | `.UniqueIds()` | brak ochrony przed duplikatami w żądaniu |
| ShareProjectCostsCommandValidator | SharedWithUserIds | NotNull, NotEmpty, Count<=50, !current | `.UniqueIds()`, `.NotCurrentUser(currentUser)` | DRY z UpdateCostShare |
| UpdateCostShareCommandValidator | SharedWithUserIds | ręczna `Distinct().Count() == Count` | `.UniqueIds()` | DRY |
| UpdateCostShareCommandValidator | self-share | inline `Contains(currentUser.Id)` | `.NotCurrentUser(currentUser)` | DRY |
| GetProjectCostsQueryValidator | (nie istnieje) | — | `IsInEnum()` na Scope, `RequiredId()` na TenantId/ProjectId | brak walidatora |
| DeleteProjectCostCommandValidator | (nie istnieje) | — | `RequiredId()` × 3 | brak walidatora |

### 3.3 Spójność — nieużywane usingi, komunikaty EN/PL, sealed

- W `UpdateCostShareCommandValidator.cs` i `ShareProjectCostsCommandValidator.cs` zaimportowano masę encji (`Chats`, `Files`, `Notifications`, `Roles`, `Tenants`, `Users`, `WorkSchedules`) — żaden z tych namespace'ów nie jest używany. To samo zjawisko w handlerach (`Create/Update/Delete/UpdateCostShare/ShareProjectCosts`).
- Wszystkie walidatory są `public class` — brak `sealed`.
- Wszystkie komunikaty walidacji w EN; spójne.
- `OverridePropertyName("Amount")` w Create/Update — duplikacja reguły "NetAmount lub GrossAmount".

### 3.4 Wspólne reguły walidacji

| Reguła wspólna | Walidatory | Kandydat do extension |
|---------------|-----------|----------------------|
| Distinct/UniqueIds | UpdateCostShare (manual) | użycie istniejącego `.UniqueIds()` |
| User != currentUser | UpdateCostShare, ShareProjectCosts | użycie istniejącego `.NotCurrentUser(currentUser)` |
| All users are project members | UpdateCostShare, ShareProjectCosts | wspólny custom validator / extension |
| Walidacja Document (typ + rozmiar) | Create, Update (×2 dla UpdatedDocument) | wspólny extension `RuleFor(...).ValidDocument()` |
| Net/Gross > 0 + Net or Gross required | Create, Update | wspólna metoda partial validator |
| Date <= today + 1 | Create, Update | wspólny extension |
| Name NotEmpty + MaxLength(200) | Create, Update | wspólny extension |

## BLOK 4 — HANDLERY

### 4.1 Struktura

| Handler | Sealed | Explicit types (brak var) | Uwagi |
|---------|--------|--------------------------|-------|
| CreateProjectCostCommandHandler | NIE | TAK | dziedziczy `ProjectCostHandlerBase` |
| UpdateProjectCostCommandHandler | NIE | TAK | dziedziczy `ProjectCostHandlerBase` |
| DeleteProjectCostCommandHandler | NIE | TAK | dziedziczy `ProjectCostHandlerBase` |
| ShareProjectCostsCommandHandler | NIE | **NIE** (`var` w 15+ miejscach) | brak klasy bazowej, duplikacja powiadomień |
| UpdateCostShareCommandHandler | NIE | **NIE** (`var` masowo) | brak klasy bazowej, duplikacja powiadomień |
| GetProjectCostsQueryHandler | NIE | TAK | używa `IRepository<>` zamiast `IReadRepository<>` |
| ProjectCostHandlerBase | NIE (abstract) | TAK | OK |

### 4.2 Logika biznesowa

| Handler | Linie ~ | Za dużo logiki | Co wydzielić |
|---------|---------|---------------|-------------|
| CreateProjectCostCommandHandler | ~100 | NIE | OK, dobrze rozbity |
| UpdateProjectCostCommandHandler | ~180 | umiarkowanie | logika autoryzacji (admin/owner/share) → wspólny serwis `ProjectCostAccessChecker` |
| DeleteProjectCostCommandHandler | ~80 | NIE | OK |
| **ShareProjectCostsCommandHandler** | ~190 | **TAK** | Handle ma ~80 linii, mieszane: walidacja + dedup + persist + notify; wydzielić `BuildSharesAsync`, `SendShareNotificationAsync` |
| **UpdateCostShareCommandHandler** | ~180 | **TAK** | Handle ~120 linii; wydzielić `RemoveSharesAsync`, `AddSharesAsync`, `SendShareChangedNotificationAsync` |
| GetProjectCostsQueryHandler | ~170 | NIE | OK; wydzielenie generowania SAS do helpera byłoby plusem |

### 4.3 SOLID i DRY

| Handler | Podobny do | Wspólna logika | Kandydat do klasy bazowej / serwisu |
|---------|-----------|---------------|-------------------------------------|
| Update/Delete | Update | `IsTenantOrProjectAdminAsync + isCostOwner + share` | `ProjectCostAccessService.HasWriteAccessAsync(cost)` |
| Update/Delete | Update | pobranie i walidacja `ProjectCost` po `(Id, TenantId, ProjectId)` | `ProjectCostHandlerBase.GetAndValidateAsync(...)` |
| ShareProjectCosts/UpdateCostShare | siebie nawzajem | tworzenie `SharedProjectCost`, pobieranie targetUser, budowa NotificationDto Title/Message PL, wywołanie `NotificationPayloadHelper` + `notificationSender` | wspólny serwis `ProjectCostShareNotificationService` |
| UpdateCostShare | — | autoryzacja admin OR owner | jak wyżej |
| Get | — | generowanie SAS dla preview/download | helper `CostAttachmentSasBuilder` (powtarza się w `CostTrackerHandlerBase.MapProjectCostToWeb`) |

### 4.4 Obsługa błędów

| Handler | Problem | Ryzyko |
|---------|---------|--------|
| Create | `throw new ValidationApiException("Cost created but document upload failed")` po udanym Insert kosztu — pozostawia osierocony rekord bez transakcji wycofującej upload | Niespójność danych (rekord bez załącznika oczekiwanego); efektywnie nie wiadomo czy klient powinien retry |
| ProjectCostHandlerBase.RemoveAttachmentsAsync | `catch { /* swallow */ }` przy `blobStorageService.DeleteAsync` (bez logowania) | Wycieki w Blob Storage; brak telemetrii błędów |
| Update / Delete / UpdateCostShare / ShareProjectCosts | przy braku uprawnień rzucają `NotFoundApiException` zamiast `ForbiddenApiException` | Świadome ukrywanie istnienia (information hiding) — ale niespójne z wzorcem domeny w copilot-instructions; należy udokumentować lub ujednolicić |
| GetProjectCostsQueryHandler | `throw new ArgumentOutOfRangeException(nameof(request.Scope))` zamiast `ValidationApiException` | Niespójność — taki wyjątek przejdzie middleware jako 500 (jeżeli nie mapowany), zamiast 400 |
| UpdateCostShare / ShareProjectCosts | `var members = await projectMemberRepo.GetBySearch(...)` w validatorze — `GetBySearch` z Take(All) bez paginacji; brak null-checków | OK funkcjonalnie, ale walidator wykonuje DB hit per wywołanie (pipeline) |

### 4.5 Zapytania do DB

| Handler | Problem | Ryzyko |
|---------|---------|--------|
| GetProjectCostsQueryHandler | używa `IRepository<ProjectCost>` i `IRepository<SharedProjectCost>` mimo że tylko czyta | naruszenie ISP — powinno być `IReadRepository<>` |
| GetProjectCostsQueryHandler (Mine/All) | `Include(SharedWith)` zawsze, niezależnie czy potrzebne dla scope | nadmiarowe dane |
| GetProjectCostsQueryHandler (Shared) | `Include(spc => spc.ProjectCost).ThenInclude(pc => pc.SharedWith)` + `.Distinct().ToList()` po stronie klienta | brak filtra `IsDeleted`; brak deduplikacji w SQL |
| **Wszystkie scenariusze Get** | brak filtra `pc.IsDeleted == false` w predykatach | zwraca soft-deleted koszty (chyba że globalny filtr w EF, ale nie ma go zarejestrowanego dla `BaseCost`) |
| UpdateProjectCostCommandHandler | `IRepository<ProjectCost>` mimo brak hard-delete; OK |  |
| UpdateProjectCostCommandHandler.GetAndValidate | predykat zawiera TenantId+ProjectId — OK |  |
| UpdateCostShareCommandHandler | `cost.SharedWith` po `Include` — OK; ale `IRepository<Project>` wstrzykiwane lecz nieużywane | nieużywana zależność `projectRepo` |
| UpdateCostShareCommandHandler | `IRepository<SharedProjectCost> sharedProjectCostRepo` używany do `DeleteRange/InsertRange/SaveChangesAsync` — OK; jednak `await sharedProjectCostRepo.SaveChangesAsync(cancellationToken)` po `Update` na innej encji `cost.SharedWith` — brak save dla `projectCostRepo` (zmienione przez nawigację) — może być OK jeśli ten sam DbContext, ale niejawne |
| ShareProjectCostsCommandHandler | brak filtra `IsDeleted` przy ładowaniu kosztów; brak `Include(SharedWith)` mimo wzorca dedup po DB | duplikaty teoretycznie zablokowane przez unique index, ale brak save changes na końcu! `await sharedProjectCostRepo.InsertRange(...)` bez `SaveChangesAsync` — **ryzyko utraty danych** |
| ShareProjectCostsCommandHandler | `projectCosts.Count() != request.ProjectCostIds.Count()` — `Count()` na `IEnumerable` — re-iteruje zapytanie |  |
| DeleteProjectCostCommandHandler | predykat OK; `RemoveAttachmentsAsync` przed `IsDeleted=true` — OK |  |
| Create / Delete / Update wszystkie | brak transakcji explicit — TransactionBehavior pipeline pokrywa, ale w Create osierocenie przy upload fail (patrz 4.4) |  |
| GetProjectCostsQueryHandler | brak `.AsNoTracking()` w odczytach |  |

## BLOK 5 — WEB MODELE

### 5.1 Sealed record z explicit properties

| WebModel | Sealed record | Explicit properties | required init | Uwagi |
|----------|--------------|--------------------|--------------|------|
| ProjectCostListItemWeb | NIE | TAK | NIE | brak `sealed`, brak `required`, używa wartości domyślnych (`= string.Empty`, `= new()`) |
| SharedProjectCostWeb | NIE | TAK | NIE | jw.; pole `CostVatRate` istnieje, ale nigdy nie jest wypełniane (mapper `MapToWeb` go nie ustawia, więc model jest niewykorzystywany w pełni) |

### 5.2 Duplikacje

| Duplikowane pola | W modelach | Kandydat do wydzielenia |
|-----------------|-----------|------------------------|
| Name, Place, Date, Description, NetAmount, GrossAmount, IsAccepted, HasDocument, DocumentFileName, PreviewSasUrl, DownloadSasUrl | ProjectCostListItemWeb, SharedProjectCostWeb (z prefiksem `Cost`), TrackedCostWeb (CostTrackers) | wspólny `CostBaseWeb` lub osobne `CostFinancialsWeb`/`CostAttachmentWeb` |
| SharedWithUserIds (List) | ProjectCostListItemWeb | OK |
| `SharedProjectCostWeb` | obecnie nieużywany (brak referencji w handlerach domeny) | kandydat do usunięcia lub wykorzystania |

## BLOK 6 — PROBLEMY I REKOMENDACJE

### Krytyczne (błędy logiki lub bezpieczeństwa)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| K1 | `ShareProjectCostsCommandHandler.Handle` woła `sharedProjectCostRepo.InsertRange(...)` bez `SaveChangesAsync` na końcu | [ShareProjectCostsCommandHandler.cs#L130-L135](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/ShareProjectCosts/ShareProjectCostsCommandHandler.cs#L130) | Zależy od `TransactionBehavior` lub innego SaveChanges; jeśli pipeline nie wymusza commit — udostępnienia mogą nie zostać zapisane, choć powiadomienia już wysłane | dodać explicit `await sharedProjectCostRepo.SaveChangesAsync(ct)` po `InsertRange` (przed wysyłką notyfikacji idealnie) |
| K2 | `DeleteProjectCostCommand` i `GetProjectCostsQuery` nie mają walidatorów | [DeleteProjectCostCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/DeleteProjectCost/DeleteProjectCostCommand.cs), [GetProjectCostsQuery.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQuery.cs) | `Guid.Empty`, niepoprawny enum przechodzą do handlera; ryzyko 500 zamiast 400 | dodać validatory z `RequiredId()` × 3 i `IsInEnum()` na Scope |
| K3 | `GetProjectCostsQueryHandler` nie filtruje `pc.IsDeleted == false` w żadnym scope | [GetProjectCostsQueryHandler.cs#L77-L106](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs#L77) | Zwraca soft-deleted koszty na liście — błąd biznesowy | dodać `&& !pc.IsDeleted` w każdym predykacie |
| K4 | `CreateProjectCostCommandHandler` przy fail uploadu rzuca wyjątek po zapisie kosztu — brak rollback zapisu | [CreateProjectCostCommandHandler.cs#L41-L50](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandHandler.cs#L41) | Niespójność: koszt istnieje, dokumentu nie ma, klient widzi 400 | albo usunąć koszt w catch, albo polegać na `TransactionBehavior` (zapis nastąpi tylko po pełnym powodzeniu — wtedy `Insert` powinien być przed uploadem ale `SaveChanges` na końcu) |

### Wysokie (naruszenia wzorców, duplikacje, brakujące walidacje)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| W1 | 4/6 Commands brak `sealed`, brak `required` na properties | wszystkie Command/Query | mutowalność wartości domyślnych, niezgodność z wzorcem | przejść na `public sealed record X : ... { public required Guid TenantId { get; init; } ... }` |
| W2 | DeleteProjectCostCommand i GetProjectCostsQuery używają positional params | [DeleteProjectCostCommand.cs#L11](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/DeleteProjectCost/DeleteProjectCostCommand.cs#L11), [GetProjectCostsQuery.cs#L10](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQuery.cs#L10) | niezgodność z wzorcem docelowym | przepisać na explicit `required init` |
| W3 | Brak użycia `CommonValidationExtensions` (RequiredId, UniqueIds, NotCurrentUser) we wszystkich walidatorach domeny | wszystkie validatory | duplikacja, brak spójności z resztą solution | zastąpić istniejące reguły rozszerzeniami |
| W4 | Walidacja "wszyscy userzy są członkami projektu" zduplikowana w UpdateCostShare i ShareProjectCosts | [UpdateCostShareCommandValidator.cs#L40-L57](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommandValidator.cs#L40), [ShareProjectCostsCommandValidator.cs#L33-L48](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/ShareProjectCosts/ShareProjectCostsCommandValidator.cs#L33) | DRY | wspólny validator helper / extension |
| W5 | `UpdateCostShareCommand.PermissionCode = ProjectResourcesWrite`, ale `ShareProjectCostsCommand = ProjectResourcesShare` | [UpdateCostShareCommand.cs#L22](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommand.cs#L22) | semantycznie obie operacje = sharing; controller wymusza `ProjectResourcesShare` na endpoincie | ujednolicić na `ProjectResourcesShare` |
| W6 | `Get`, `Update`, `Delete` rzucają `NotFoundApiException` przy braku uprawnień | wiele handlerów | semantyka — kontrakt API niejasny | udokumentować zamierzone information-hiding lub przejść na `ForbiddenApiException` |
| W7 | Logika autoryzacji `isAdmin || isOwner [|| share]` powtarzana w 4 handlerach | Update, Delete, UpdateCostShare, Share | DRY | wydzielić `IProjectCostAccessService` |
| W8 | Logika tworzenia `NotificationDto` + `NotificationPayloadHelper.CreatePayloadAsync` + `notificationSender.EnqueueAsync` zduplikowana w UpdateCostShare i ShareProjectCosts | [UpdateCostShareCommandHandler.cs#L100-L160](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommandHandler.cs#L100), [ShareProjectCostsCommandHandler.cs#L140-L210](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/ShareProjectCosts/ShareProjectCostsCommandHandler.cs#L140) | DRY | `ProjectCostShareNotificationService` |
| W9 | `var` w `ShareProjectCostsCommandHandler` i `UpdateCostShareCommandHandler` (15+ wystąpień) | oba pliki | konwencja projektu wymaga explicit types | zamienić na typy explicit |
| W10 | `GetProjectCostsQueryHandler` używa `IRepository<>` zamiast `IReadRepository<>` dla operacji odczytu | [GetProjectCostsQueryHandler.cs#L26-L27](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs#L26) | ISP | zmienić typ pól |
| W11 | `UpdateCostShareCommandHandler` wstrzykuje `IRepository<Project> projectRepo`, ale go nie używa | [UpdateCostShareCommandHandler.cs#L29](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommandHandler.cs#L29) | dead code, nadmiarowa zależność | usunąć |
| W12 | Reguła "Net or Gross required + each > 0 + Document type/size" zduplikowana w Create i Update | oba validatory | DRY | wspólny extension `ApplyCostFinancialAndDocumentRules<T>` |
| W13 | `ProjectCostHandlerBase.RemoveAttachmentsAsync` wycisza wyjątki blob bez logowania | [ProjectCostHandlerBase.cs#L74-L80](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/Shared/ProjectCostHandlerBase.cs#L74) | utracone informacje o błędach Azure Storage | dodać `ILogger` z `LogWarning(ex, ...)` |

### Normalne (styl, konwencje, drobne usprawnienia)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| N1 | Web modele `ProjectCostListItemWeb`, `SharedProjectCostWeb` — brak `sealed`, brak `required`, defaults `= string.Empty` | oba pliki WebModel | konwencja | `public sealed record ... { public required Guid Id { get; init; } ... }` |
| N2 | Walidatory bez `sealed` | wszystkie | konwencja | dodać `sealed` |
| N3 | Handlery bez `sealed` | wszystkie | konwencja | dodać `sealed` |
| N4 | Niewykorzystane usingi (Chats, Files, Notifications, Roles, Tenants, Users, WorkSchedules) w wielu plikach | wszystkie handlery i 2 walidatory | szum | wyczyścić (Sort & Remove Usings) |
| N5 | `GetProjectCostsQueryHandler.MapToWeb` generuje SAS dla każdego rekordu w pętli | [GetProjectCostsQueryHandler.cs#L123-L180](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs#L123) | wydajność dla dużych list | rozważyć cache lub batch (chociaż SAS to lokalne podpisywanie, nie call HTTP — niskie ryzyko) |
| N6 | `SharedProjectCostWeb.CostVatRate` deklarowany lecz nigdy nie wypełniany | [SharedProjectCostWeb.cs#L21](02-ApplicationServices/ProductDataManagementWebAPI/src/Business/Interfaces/WebModels/ProjectCosts/SharedProjectCostWeb.cs#L21) | mylący kontrakt | usunąć lub wypełnić |
| N7 | `SharedProjectCostWeb` nie jest używany w żadnym handlerze ProjectCosts | [SharedProjectCostWeb.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Business/Interfaces/WebModels/ProjectCosts/SharedProjectCostWeb.cs) | dead code | usunąć lub udokumentować przeznaczenie |
| N8 | `SharedProjectCost` entity używa `BaseEntity` mimo że ma TenantId/ProjectId — brak współdzielonego `BaseTenantProjectEntity` | [SharedProjectCost.cs#L11-L13](02-ApplicationServices/ProductDataManagementWebAPI/src/Entities/Models/Costs/SharedProjectCost.cs#L11) | duplikacja w modelach | rozważyć bazę z TenantId/ProjectId |
| N9 | `GetProjectCostsQueryHandler.LoadCostsAsync` `default: throw new ArgumentOutOfRangeException` zamiast `ValidationApiException` | [GetProjectCostsQueryHandler.cs#L106](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs#L106) | spójność wyjątków | użyć ApiException |
| N10 | `[HttpGet("{scope}")]` przyjmuje enum przez route — brak walidacji wartości w controllerze | [ProjectCostController.cs#L30](02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/Controllers/ProjectCostController.cs#L30) | nieprawidłowy string → 0 (default enum) lub 400 binding | walidator query (patrz K2) |
| N11 | Komentarz `// 6. Save all changes` w UpdateCostShare następuje po `await ... SaveChangesAsync` — mylący opis | [UpdateCostShareCommandHandler.cs#L173](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommandHandler.cs#L173) | czytelność | zaktualizować komentarz |
| N12 | `var` w `ProjectCostController` (`var query`, `var command`, `var costId`) | [ProjectCostController.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/Controllers/ProjectCostController.cs) | konwencja projektu | explicit types |

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Liczba Commands | 5 |
| Liczba Queries | 1 |
| Liczba Walidatorów | 4 |
| Liczba Handlerów | 6 (+ 1 base) |
| Commands/Queries z positional params | 2 / 6 (33%) |
| Commands/Queries bez `sealed` | 4 / 6 (67%) |
| Commands/Queries bez `required` na properties | 6 / 6 (100%) |
| Queries/Commands bez walidatora | 2 / 6 (33%) — `DeleteProjectCostCommand`, `GetProjectCostsQuery` |
| Pokrycie walidatorami | 67% |
| Walidatory używające `CommonValidationExtensions` | 0 / 4 (0%) |
| Handlery z `var` | 2 / 6 |
| Handlery bez `sealed` | 6 / 6 (100%) |
| WebModels bez `sealed` / bez `required` | 2 / 2 |
| Problemy krytyczne | 4 |
| Problemy wysokie | 13 |
| Problemy normalne | 12 |
