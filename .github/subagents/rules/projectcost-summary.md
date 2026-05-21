# ProjectCost — podsumowanie audytu i refaktoru

## Status końcowy
- ✅ Build: **0 błędów**
- ✅ 4 prompty refaktoru wykonane (fix-01..fix-04)

## Wykonane zmiany — według promptów

### fix-01 — Krytyczne (bezpieczeństwo danych i autoryzacja)
- **K1** — `ShareProjectCostsCommandHandler`: dodany `SaveChangesAsync` po `InsertRange` (przed wysyłką notyfikacji).
- **K2** — Nowe walidatory: `DeleteProjectCostCommandValidator`, `GetProjectCostsQueryValidator`.
- **K3** — `GetProjectCostsQueryHandler`: filtr `!IsDeleted` w All / Mine / Shared.
- **K4** — `CreateProjectCostCommandHandler`: upload PRZED Insert (brak ryzyka osierocenia rekordu). Pomocnicze `UploadBlobAndBuildAttachmentAsync` + `PersistAttachmentAsync` w bazie.
- **W6** — `Update` / `Delete` / `UpdateCostShare` / `ShareProjectCosts`: `NotFoundApiException` przy braku uprawnień → `ForbiddenApiException`.

### fix-02 — Struktura Commands/Queries + WebModels
- Wszystkie 5 Commands + 1 Query: `sealed record` z `required { get; init; }`, eliminacja positional params (`Delete`, `Get`).
- `Date` → `DateTime?` (walidator wymusza wartość).
- `ProjectCostListItemWeb`: `sealed record` + `required` + `IReadOnlyList<Guid>` na `SharedWithUserIds`.
- **N7** — Usunięty `SharedProjectCostWeb` (dead code).
- **W11** — Usunięty nieużywany `IRepository<Project> projectRepo` z `UpdateCostShareCommandHandler`.
- `ProjectCostController` zaktualizowany na object initializery + explicit types.

### fix-03 — Walidatory (CommonValidationExtensions, DRY)
- Wszystkie walidatory: `sealed`, `RequiredId()`, `UniqueIds()`, `NotCurrentUser()`.
- Dodany overload `NotCurrentUser` dla `List<Guid>` w `CommonValidationExtensions`.
- Nowy plik `ProjectCostValidationExtensions` z helperami: `AllAreProjectMembers<T>`, `ApplyCostNameRules<T>`, `ApplyCostFinancialRules<T>`, `ApplyCostDateRules<T>`, `ApplyDocumentRules<T>` — DRY między Create/Update/Share/UpdateShare.
- Cleanup nieużywanych usingów w walidatorach Share/UpdateCostShare.

### fix-04 — Handlery (serwisy, sealed, IReadRepository, var, cleanup)
- Nowy `IProjectCostAccessService` + implementacja — wspólna autoryzacja (admin / owner / share).
- Nowy `IProjectCostShareNotificationService` — wspólna budowa `NotificationDto` + payload + enqueue dla Share / UpdateShare.
- `Share` i `UpdateCostShare` handler: zastąpienie `var` typami explicit.
- `GetProjectCostsQueryHandler`: `IRepository<>` → `IReadRepository<>`.
- `ProjectCostHandlerBase`: `ILogger` w `RemoveAttachmentsAsync` (W13).
- Wszystkie handlery: `sealed`.
- Cleanup nieużywanych usingów (Chats, Files, Roles, Tenants, WorkSchedules itp.).
- `LoadCostsAsync` default branch: `ArgumentOutOfRangeException` → `ValidationApiException`.
- Komentarz `// 6. Save all changes` w `UpdateCostShare` poprawiony.
- `ProjectCostController` — `var` → explicit types.
- **Hotfix** (manualny): `ProjectCostShareNotificationService.LoadUsersAsync` — błędne wywołanie `GetBySearch(predicate, cancellationToken)` (niepoprawna sygnatura — drugi param to `params includes`); poprawione na `GetBySearch(predicate)`.

## Metryki przed → po

| Metryka | Przed | Po |
|---------|------|----|
| Pokrycie walidatorami | 67% (4/6) | **100% (6/6)** |
| Walidatory używające `CommonValidationExtensions` | 0/4 (0%) | **6/6 (100%)** |
| Commands/Queries `sealed` | 2/6 (33%) | **6/6 (100%)** |
| Commands/Queries z `required init` | 0/6 (0%) | **6/6 (100%)** |
| Handlery `sealed` | 0/6 (0%) | **6/6 (100%)** |
| Handlery z `var` | 2/6 | **0/6** |
| `IReadRepository` w odczycie | 0/1 | **1/1** |
| WebModels `sealed` + `required` | 0/2 | **1/1** (drugi usunięty jako dead code) |
| Krytyczne problemy nierozwiązane | 4 | **0** |
| Wysokie problemy nierozwiązane | 13 | **0** |

## Decyzje domenowe (zatwierdzone przez użytkownika)
1. `UpdateCostShareCommand.PermissionCode` — pozostaje `ProjectResourcesWrite` (decyzja: nie zmieniać).
2. `NotFoundApiException` przy braku uprawnień → `ForbiddenApiException` (ujednolicone).
3. Wydzielenie `IProjectCostAccessService` + `IProjectCostShareNotificationService` — wykonane teraz.
4. `SharedProjectCostWeb` — usunięty.
5. Kolejność w `Create`: upload PRZED Insert.

## Pozostałe drobne rekomendacje (nie wykonane — niski priorytet)
- N5: SAS w pętli — wydajność OK dla obecnej skali, można rozważyć batch jeśli pojawi się problem.
- N8: `SharedProjectCost` mógłby dziedziczyć po wspólnej bazie z `TenantId`/`ProjectId` — refaktor cross-domenowy, do osobnej iteracji.

## Pliki referencyjne
- Audyt: [.github/subagents/rules/projectcost-audit.md](.github/subagents/rules/projectcost-audit.md)
- Prompty: [projectcost-fix-01.md](.github/subagents/rules/projectcost-fix-01.md), [projectcost-fix-02.md](.github/subagents/rules/projectcost-fix-02.md), [projectcost-fix-03.md](.github/subagents/rules/projectcost-fix-03.md), [projectcost-fix-04.md](.github/subagents/rules/projectcost-fix-04.md)
