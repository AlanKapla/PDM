# Prompt: admin-cold-mail-api-fix-01 — Encja + migracja ColdMailHistory

## Cel
Dodać encję historii cold maili + konfigurację EF + migrację.

## Spec / audyt
- `.opencode/features/admin-cold-mail.md`
- `.opencode/subagents/rules/admin-cold-mail-api-audit.md`
- Skill: `.opencode/skills/api-entities/SKILL.md`

## Wymagania
1. Encja `ColdMailHistory` dziedzicząca po `BaseEntity` (jak inne encje systemowe, **bez** TenantId/ProjectId).
2. Pola:
   - `BatchId` (Guid)
   - `RecipientEmail` (string, max 320)
   - `Subject` (string, max 500)
   - `Body` (string — wystarczająco duży limit, np. max length w config)
   - `Status` (string lub enum: `Queued`, `Failed`)
   - `ErrorMessage` (string?, nullable)
   - `SentByUserId` (Guid) — FK do Users jeśli konwencja repo to wspiera
   - `SentAt` (DateTime, UTC)
3. `DbSet<ColdMailHistory>` w DbContext.
4. EF configuration (Fluent API): max lengths, indexes na `RecipientEmail`, `SentAt`, `BatchId`.
5. Migracja EF Core: `dotnet ef migrations add AddColdMailHistory --startup-project ../WebApi` z katalogu `src/Entities`. CI pin: `dotnet-ef` 10.0.1.

## Konwencje
- Brak `var`
- `is null` / `is not null`
- Zgodność z istniejącymi encjami w `src/Entities`

## Poza zakresem
- Handlery, kontroler, testy (kolejne prompty)

## Definition of done
- Encja + config + DbSet + migracja skompilowane
- `dotnet build` rozwiązania OK (lub przynajmniej projekt Entities + WebApi)
