# Skill: UI / Testy jednostkowe

## Opis
Pisanie testów jednostkowych dla hooków, utility functions i komponentów (Vitest + RTL).

## Kiedy używać
Użyj tego skilla gdy piszesz testy jednostkowe dla warstwy UI.

---

## Stack

- Vitest + React Testing Library
- Nazewnictwo: `opisCo_warunek_oczekiwanyWynik`
- Projekt: katalog `__tests__` obok testowanego pliku lub `src/__tests__/`

> Jeśli projekt używa innego frameworku testowego — sprawdź przez #codebase
> czy istnieją już testy i jaką bibliotekę używają.

## Test hooka

```typescript
// src/hooks/__tests__/useModal.test.ts
import { renderHook, act } from '@testing-library/react';
import { useModal } from '../useModal';

describe('useModal', () => {
    it('isOpen_domyslnie_jestFalse', () => {
        // Arrange + Act
        const { result } = renderHook(() => useModal());

        // Assert
        expect(result.current.isOpen).toBe(false);
    });

    it('onOpen_wywolane_ustawiaIsOpenNaTrue', () => {
        // Arrange
        const { result } = renderHook(() => useModal());

        // Act
        act(() => {
            result.current.onOpen();
        });

        // Assert
        expect(result.current.isOpen).toBe(true);
    });

    it('onClose_wywolane_ustawiaIsOpenNaFalse', () => {
        // Arrange
        const { result } = renderHook(() => useModal());
        act(() => { result.current.onOpen(); });

        // Act
        act(() => { result.current.onClose(); });

        // Assert
        expect(result.current.isOpen).toBe(false);
    });
});
```

## Test utility/helper

```typescript
// src/utils/__tests__/formatters.test.ts
import { PLN } from '../formatters';

describe('PLN', () => {
    it('PLN_wartoscNull_zwracaMyslnik', () => {
        expect(PLN(null)).toBe('—');
    });

    it('PLN_wartoscUndefined_zwracaMyslnik', () => {
        expect(PLN(undefined)).toBe('—');
    });

    it('PLN_wartoscLiczbowa_zwracaSformatowanaKwote', () => {
        expect(PLN(1234, 'zł')).toContain('zł');
    });
});
```

## Test extension method (CommonValidationExtensions odpowiednik UI)

```typescript
// src/utils/__tests__/validators.test.ts
import { isValidColorRgb, isValidGuid } from '../validators';

describe('isValidColorRgb', () => {
    it('poprawnyHex_zwracaTrue', () => {
        expect(isValidColorRgb('#FF5733')).toBe(true);
    });

    it('niepoprawnyFormat_zwracaFalse', () => {
        expect(isValidColorRgb('FF5733')).toBe(false);
        expect(isValidColorRgb('#XYZ')).toBe(false);
        expect(isValidColorRgb('')).toBe(false);
    });
});
```

## Test komponentu (renderowanie)

```typescript
// src/components/__tests__/EmptyState.test.tsx
import { render, screen } from '@testing-library/react';
import { EmptyState } from '../ui/EmptyState';

describe('EmptyState', () => {
    it('render_zTytulem_wyswietlaTytul', () => {
        // Arrange + Act
        render(<EmptyState title="Brak projektów" />);

        // Assert
        expect(screen.getByText('Brak projektów')).toBeInTheDocument();
    });
});
```

## Mockowanie API w testach hooka

```typescript
import { vi } from 'vitest';
import { projectApi } from '../../api/projectApi';

vi.mock('../../api/projectApi');

const mockProjectApi = vi.mocked(projectApi);

it('useProjects_sukcesApi_zwracaDane', async () => {
    // Arrange
    const projects: ProjectDetailsWeb[] = [
        { id: '1', name: 'Test', isActive: true, /* ... */ }
    ];
    mockProjectApi.getAll.mockResolvedValue(projects);

    // Act
    const { result } = renderHook(() => useProjects('tenant-1'));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    // Assert
    expect(result.current.data).toEqual(projects);
});
```

## Test AXE — dostępność komponentów (obowiązkowy)

Każdy komponent renderujący HTML musi mieć test AXE sprawdzający naruszenia WCAG AA.
`toHaveNoViolations` jest zarejestrowany globalnie przez `src/test/setup.ts`.

```typescript
// src/components/ui/__tests__/AppModal.axe.test.tsx
import { axe } from 'vitest-axe';
import { renderWithChakra } from '../../../test/render-with-chakra';
import AppModal from '../AppModal';

describe('AppModal — AXE', () => {
    it('brakNaruszen_otwartyModal', async () => {
        const { container } = renderWithChakra(
            <AppModal isOpen onClose={() => undefined} title="Testowy modal">
                <p>Treść</p>
            </AppModal>
        );
        const results = await axe(container);
        expect(results).toHaveNoViolations();
    });
});
```

Wrapper dla komponentów Chakra UI (`src/test/render-with-chakra.tsx`):
```typescript
export function renderWithChakra(ui: ReactElement, options?: RenderOptions) {
    return render(ui, {
        wrapper: ({ children }) => <ChakraProvider>{children}</ChakraProvider>,
        ...options,
    });
}
```

## Zasady

- AAA — Arrange/Act/Assert z komentarzami
- Jeden test = jeden przypadek
- Testuj zachowanie, nie implementację
- Mockuj zewnętrzne zależności (API, router)
- Nazwy testów opisowe: `co_warunek_wynik`
- Nie testuj szczegółów implementacji (np. nazwy zmiennych wewnętrznych)
- Testuj: hooki, utility functions, extension methods, proste komponenty
- Każdy komponent z renderowaniem HTML → obowiązkowy test AXE
- Nie testuj: komponenty Chakra UI, style CSS
