# Raport pokrycia testami — WebApi.Tests

## Data
2025-07-15

## Wyniki

### WebApi.Tests
| Klasa | Kontroler | Testy | Status |
|-------|-----------|-------|--------|
| DictionaryControllerTests | DictionaryController | ~8 | ✅ |
| ProjectDashboardControllerTests | ProjectDashboardController | ~3 | ✅ |
| RoleControllerTests | RoleController | ~3 | ✅ |
| NotificationControllerTests | NotificationController | ~5 | ✅ |
| UserControllerTests | UserController | ~8 | ✅ |
| ProjectControllerTests | ProjectController | ~12 | ✅ |
| TenantControllerTests | TenantController | ~14 | ✅ |
| CostTrackerControllerTests | CostTrackerController | ~10 | ✅ |
| ProjectCostControllerTests | ProjectCostController | ~8 | ✅ |
| FileControllerTests | FileController | ~12 | ✅ |
| CostEstimateControllerTests | CostEstimateController | ~18 | ✅ |
| CostEstimateTemplateControllerTests | CostEstimateTemplateController | ~12 | ✅ |
| DirectChatsControllerTests | DirectChatsController | ~9 | ✅ |
| TenantChatsControllerTests | TenantChatsController | ~14 | ✅ |
| WorkScheduleControllerTests | WorkScheduleController | ~28 | ✅ |

## Statystyki
- Łącznie klas kontrolerów: 15
- Łącznie testów: **147**
- Build OK: ✅
- Blokery: brak

## Architektura testów

### Projekt testowy
`tests/WebApi.Tests/WebApi.Tests.csproj` (.NET 10, xUnit 2.9.3, Moq 4.20.72, FluentAssertions 6.12.2)

### Wzorzec bazowy
`TestBase.cs` — `ControllerTestBase` z:
- `Mock<IMediator>(MockBehavior.Loose)` — mock MediatR
- `SetupMediatorReturns<TRequest, TResponse>()` — konfiguracja via `As<ISender>()`
- `VerifyMediatorCalledOnce<TRequest>(predicate)` — weryfikacja przez `MediatorMock.Invocations`
- `WebModelFactory.cs` — fabryka minimalnych instancji Web modeli (Project, Tenant, TrackedCost, ChatResult, WorkSchedule)

### Kluczowe problemy rozwiązane
1. `WorkScheduleDetailsWeb` — duplikaty pól w konstruktorze pozycyjnym (naprawiono)
2. `CostSourceType.Standalone` nie istnieje → `CostSourceType.ProjectAdditional`
3. Request modele Chats używają konstruktorów pozycyjnych (`CreateChatRequest(Guid?, List<Guid>, string?)`)
4. `FindChatsByMembersQuery` — właściwość `MemberUserIds` (nie `MemberIds`)
5. `ItemRelationType.Main` nie istnieje → `ItemRelationType.None`
6. `AddWorkScheduleStageWorkCommentCommand` — właściwość `Content` (nie `Comment`)
7. Moq `Verify(m => m.Send(It.Is<TRequest>(...)))` nie dopasowuje `ISender.Send<TResponse>` — rozwiązano przez inspekcję `MediatorMock.Invocations`
8. `CreateDirectChatCommand` zwraca `CreateChatResultWeb` (reference type) — wymagał explicit `SetupMediatorReturns` aby uniknąć NRE
