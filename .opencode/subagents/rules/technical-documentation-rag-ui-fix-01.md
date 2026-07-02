# UI Fix 01 — Typy TypeScript + uprawnienia modułowe

## Cel
Fundament typów i uprawnień UI dla modułu dokumentacji technicznej.

## Decyzje MVP
- Jeden kod: `PROJECT.TECHNICAL_DOCUMENTATION`
- `ProjectModule.TechnicalDocumentation = 7` (zsynchronizuj z backendem)
- **Brak** `retryCount` w typach UI
- **Brak** `schemaVersion` w typach Details

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skills
- `.cursor/skills/ui-types/SKILL.md`

## Pliki referencyjne
- `src/types/projectModulePermissions.ts`
- `src/constants/roleCodes.ts`
- `src/hooks/useProjectPermissions.ts`
- `.opencode/features/technical-documentation-rag.md` — model JSON

---

## 1. Nowy plik typów

Plik: `src/types/technicalDocumentation.types.ts`

### Status
```typescript
export const TechnicalDocumentationStatus = {
  Pending: 0,
  Processing: 1,
  Completed: 2,
  Failed: 3,
} as const;

export type TechnicalDocumentationStatus =
  (typeof TechnicalDocumentationStatus)[keyof typeof TechnicalDocumentationStatus];
```

### Interfejsy listy/szczegółów
- `TechnicalDocumentationListItemWeb` — id, projectId, name, description?, status, fileCount, createdAt, completedAt?, errorMessage?
- `TechnicalDocumentationFileWeb` — id, fileName, contentType, fileSize, sasUriPreview?, sasUriDownload?
- `TechnicalDocumentationDetailsWeb` — pola listy + `details?: ProjectTechnicalDocumentationDetailsWeb`, `files: TechnicalDocumentationFileWeb[]`
- `TechnicalDocumentationProcessingEvent` — documentationId, projectId, tenantId, name, status, errorMessage?

### Model JSON (mirror C#)
`ProjectTechnicalDocumentationDetailsWeb` + zagnieżdżone:
`ProjectInfoWeb`, `DrawingWeb`, `DrawingSourceWeb`, `RoomWeb`, `DimensionsWeb`, `WallWeb`, `OpeningWeb`, `InsulationInfoWeb`, `FinishingWeb`, `RoofDetailsWeb`, `InstallationInfoWeb`, `StockItemWeb`, `MaterialSummaryWeb`

### Request types
```typescript
export interface CreateTechnicalDocumentationRequest {
  name: string;
  description?: string;
  files: File[];
}
```

## 2. `roleCodes.ts`

Dodaj:
```typescript
ProjectTechnicalDocumentation: "PROJECT.TECHNICAL_DOCUMENTATION",
```

## 3. `projectModulePermissions.ts`

Dodaj do enum:
```typescript
TechnicalDocumentation = 7,
```

W `PROJECT_MODULE_LABELS`:
```typescript
[ProjectModule.TechnicalDocumentation]: "Dokumentacja techniczna",
```

Upewnij się że moduł jest w `SELECTABLE_MODULES` (jeśli lista istnieje).

## 4. `useProjectPermissions.ts`

Dodaj flagi (jeden kod = view + write w MVP):
```typescript
canViewTechnicalDocumentation:
  canViewAllResources ||
  hasPermission(permissions, PermissionCodes.ProjectTechnicalDocumentation),

canWriteTechnicalDocumentation:
  canViewAllResources ||
  hasPermission(permissions, PermissionCodes.ProjectTechnicalDocumentation),
```

Dodaj do early-return (loading state) wartości `false` dla obu flag.

## Weryfikacja
```powershell
cd 01-Applications/ProjectDataManagementUI
npx tsc --noEmit
```

## Zależności
- Pierwszy prompt UI — może startować równolegle z API fix-01/02
- **ui-fix-02** wymaga tego kroku
