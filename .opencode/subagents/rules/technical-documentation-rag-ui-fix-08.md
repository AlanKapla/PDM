# UI Fix 08 — Routing, kafelek ProjectDetails, Breadcrumbs, testy AXE

## Cel
Integracja modułu w aplikacji: trasy, nawigacja, kafelek z count (ikona `ScanLine`), testy dostępności.

## Decyzje MVP
- Trasa: `/projects/:projectId/technical-documentation` i `.../:docId`
- Ikona kafelka: **`ScanLine`** (lucide-react)
- Count z endpointu `GET .../count` (nie z długości listy)
- Toast SignalR już globalny (**ui-fix-03**) — nie duplikuj na stronach

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skills
- `.cursor/skills/ui-accessibility/SKILL.md`
- `.cursor/skills/ui-unit-tests/SKILL.md`

## Zależności
- **ui-fix-01** do **ui-fix-07** ukończone

## Pliki referencyjne
- `src/routes/AppRouter.tsx`
- `src/components/Breadcrumbs.tsx`
- `src/pages/ProjectDetails.tsx` (~SimpleGrid kafelków)
- `src/components/ui/__tests__/SharedComponents.axe.test.tsx`
- `src/test/render-with-chakra.tsx`

---

## 1. `AppRouter.tsx`

Dodaj trasy w sekcji projektowej (`ProtectedRoute`):

```tsx
<Route
  path="/projects/:projectId/technical-documentation"
  element={<ProjectTechnicalDocumentationPage />}
/>
<Route
  path="/projects/:projectId/technical-documentation/:docId"
  element={<ProjectTechnicalDocumentationDetailsPage />}
/>
```

Import stron z `src/pages/`.

## 2. `Breadcrumbs.tsx`

Rozszerz mapowanie segmentów:
- `technical-documentation` → „Dokumentacja techniczna”
- Opcjonalnie: segment `docId` → nazwa dokumentacji (z `useTechnicalDocumentationDetails` lub state z navigate)

## 3. `ProjectDetails.tsx`

W `SimpleGrid` kafelków dodaj (warunek `permissions.canViewTechnicalDocumentation`):

```tsx
<Box
  as="button"
  aria-label={`Dokumentacja techniczna${count !== undefined ? `, ${count} pozycji` : ''}`}
  onClick={() => navigate(`/projects/${projectId}/technical-documentation`)}
  // ... ten sam styl co inne kafelki (_hover, transform, cardBg)
>
  <VStack spacing={3}>
    <Icon as={ScanLine} boxSize={8} color="teal.600" aria-hidden="true" />
    <Text fontWeight="bold" fontSize="md">Dokumentacja techniczna</Text>
    {count !== undefined && (
      <Badge colorScheme="teal" borderRadius="full">{count}</Badge>
    )}
  </VStack>
</Box>
```

- `useTechnicalDocumentationCount(tenantId, projectId)` z `enabled: canViewTechnicalDocumentation`
- Import `ScanLine` z `lucide-react`

**Uwaga:** To pierwszy kafelek z licznikiem w ProjectDetails — nowy wzorzec w tym widoku.

## 4. Testy AXE

Utwórz pliki testów (Vitest + vitest-axe + `renderWithChakra`):

| Plik | Komponent |
|------|-----------|
| `src/components/ui/__tests__/MultiDocumentDropzone.axe.test.tsx` | MultiDocumentDropzone |
| `src/components/technicalDocumentation/__tests__/TechnicalDocumentationStatusBadge.axe.test.tsx` | StatusBadge |
| `src/components/technicalDocumentation/__tests__/AddTechnicalDocumentationModal.axe.test.tsx` | Modal (isOpen=true) |
| `src/components/technicalDocumentation/__tests__/TechnicalDocumentationDetailsView.axe.test.tsx` | DetailsView z mock details |
| `src/pages/__tests__/ProjectTechnicalDocumentationPage.axe.test.tsx` | Lista (mock query) |

Każdy test: `expect(results).toHaveNoViolations()`.

Mock data: minimalny `ProjectTechnicalDocumentationDetailsWeb` z 1 drawing, 1 room.

## 5. Demo mode (opcjonalnie)

Jeśli projekt używa `mockHandlers.ts` dla offline demo — dodaj handlery dla 5 endpointów dokumentacji. **Niski priorytet** — pomiń jeśli demo nie jest wymagane.

## Weryfikacja końcowa
```powershell
cd 01-Applications/ProjectDataManagementUI
npx tsc --noEmit
npm run lint
npm run test:run
npm run test:axe
```

## Koniec warstwy UI
Po tym kroku feature jest gotowy do testów integracyjnych end-to-end z API.
