# API Fix 06 — Migracja EF Core

## Cel
Wygenerować nową migrację EF Core, która:
1. Dropuje tabelę `SharedProjectCosts`
2. Usuwa kolumnę `IsAccepted` z tabeli `Costs` (dla discriminatora `ProjectCost`)
3. Dodaje kolumnę `ApprovalStatus` (string, default `'Draft'`) do `Costs`
4. Przemianuje `AcceptedByUserId → ApprovedByUserId`
5. Przemianuje `AcceptedAt → ApprovedAt`

## Kroki

### 1. Upewnij się że build Entities przechodzi

```powershell
cd C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI
dotnet build src/Entities/Entities.csproj
```

### 2. Wygeneruj migrację

```powershell
cd C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI
dotnet ef migrations add migration-cost-approval-status `
  --project src/Entities/Entities.csproj `
  --startup-project src/WebApi/WebApi.csproj `
  --output-dir Migrations
```

### 3. Zweryfikuj wygenerowaną migrację

Sprawdź plik migracji w `src/Entities/Migrations/` — upewnij się że zawiera:
- `migrationBuilder.DropTable(name: "SharedProjectCosts");`
- `migrationBuilder.DropColumn(name: "IsAccepted", ...)`
- `migrationBuilder.AddColumn<string>(name: "ApprovalStatus", ...)`
- Operacje na `AcceptedByUserId` → `ApprovedByUserId` i `AcceptedAt` → `ApprovedAt`

Jeśli EF nie wygenerował rename kolumn (tylko Drop+Add), jest to akceptowalne — dane i tak nie są migrowane na potrzeby tej zmiany.

### 4. NIE aplikuj migracji do bazy danych

Migracja zostanie zaaplikowana ręcznie przez dewelopera. Tylko upewnij się że plik migracji istnieje i jest poprawny składniowo.

## Weryfikacja

```powershell
dotnet build src/Entities/Entities.csproj
```

Plik migracji powinien się kompilować.
