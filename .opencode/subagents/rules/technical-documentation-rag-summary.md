# Feature wdrożony: technical-documentation-rag

## Status: MVP zaimplementowany (API + UI)

Data: 2026-06-22

## Co zostało zrobione

### API
- Encje `ProjectTechnicalDocumentation`, `ProjectTechnicalDocumentationFile`, enum statusu
- Moduł uprawnień `ProjectModule.TechnicalDocumentation = 7`, kod `PROJECT.TECHNICAL_DOCUMENTATION`
- CQRS: lista, szczegóły, count, create (202), retry (202)
- Azure Queue + `TechnicalDocumentationWorker` (auto-retry max 3)
- Pipeline: PDF→JPG (Docnet.Core), 4 agenci AI + orchestrator, agregacja JSON
- SignalR hub `/api/hubs/technical-documentation` → `ProcessingCompleted` (wszyscy z uprawnieniem)
- Kontroler REST `TechnicalDocumentationController`

### UI
- Typy, API client, hooki React Query, hub + globalny toast
- Kafelek ScanLine na `ProjectDetails` z licznikiem
- Lista dokumentacji, modal dodawania, szczegóły (Accordion), retry z ConfirmDialog
- Trasy `/projects/:projectId/technical-documentation` i `/:docId`
- Testy AXE (5 plików)

## Decyzje MVP
- Jeden permission (view + write)
- Wszystkie strony PDF, brak DELETE, brak SchemaVersion, brak RAG
- Toast globalnie, ikona ScanLine

## Blokery / następne kroki
1. **Migracja DB** — `add-technical-documentation` — wymaga `dotnet ef database update` na środowisku
2. **Azure** — kontener blob `technicaldocumentation`, kolejka `technical-documentation-process`
3. **Docker** — weryfikacja Docnet/PDFium na linux-x64
4. **Testy API** — brak nowych testów jednostkowych (do dodania opcjonalnie)
5. **E2E** — ręczny test uploadu PDF + SignalR

## Build
- `dotnet build --configuration Release` — PASS
- `npx tsc --noEmit` — PASS
