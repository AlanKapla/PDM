# Refaktoryzacja UI - Dokumentacja

## 📋 Podsumowanie zmian

Data refaktoryzacji: **12 grudnia 2025**

### 🎯 Cele refaktoryzacji
1. **Eliminacja duplikacji kodu** - Wydzielenie wspólnych komponentów
2. **Poprawa czytelności** - Uproszczenie struktury komponentów
3. **Optymalizacja wydajności** - Dodanie React.memo i optymalizacja re-renderów
4. **Standaryzacja** - Jednolite podejście do obsługi błędów, toast'ów i modali
5. **Reużywalność** - Stworzenie komponentów możliwych do wykorzystania w całej aplikacji

---

## 🆕 Nowe komponenty wspólne

### Katalog: `src/components/common/`

#### 1. **LoadingSpinner**
Uniwersalny komponent do wyświetlania stanów ładowania.

**Właściwości:**
- `message?: string` - Opcjonalna wiadomość
- `size?: "xs" | "sm" | "md" | "lg" | "xl"` - Rozmiar spinnera (domyślnie: "xl")
- `fullScreen?: boolean` - Czy zajmować cały ekran (domyślnie: false)

**Przykład użycia:**
```tsx
<LoadingSpinner fullScreen message="Ładowanie danych..." />
<LoadingSpinner size="md" />
```

#### 2. **EmptyState**
Komponent do wyświetlania pustych stanów z opcjonalną akcją.

**Właściwości:**
- `icon?: LucideIcon` - Opcjonalna ikona
- `title: string` - Główny tytuł
- `description?: string` - Dodatkowy opis
- `action?: React.ReactNode` - Opcjonalna akcja (np. przycisk)

**Przykład użycia:**
```tsx
<EmptyState 
  icon={FolderKanban}
  title="Nie masz jeszcze projektów"
  description="Stwórz swój pierwszy projekt"
  action={<Button onClick={onCreate}>Utwórz projekt</Button>}
/>
```

#### 3. **ErrorAlert**
Komponent do wyświetlania błędów.

**Właściwości:**
- `title?: string` - Tytuł błędu (domyślnie: "Błąd")
- `description?: string` - Opis błędu
- `variant?: "subtle" | "solid" | "left-accent" | "top-accent"` - Wariant (domyślnie: "left-accent")

**Przykład użycia:**
```tsx
<ErrorAlert description="Nie udało się pobrać danych" />
<ErrorAlert title="Błąd serwera" description={errorMessage} />
```

#### 4. **ConfirmDialog**
Uniwersalny dialog potwierdzenia.

**Właściwości:**
- `isOpen: boolean`
- `onClose: () => void`
- `onConfirm: () => void`
- `title: string`
- `message: string`
- `confirmText?: string` - Tekst przycisku (domyślnie: "Potwierdź")
- `cancelText?: string` - Tekst anulowania (domyślnie: "Anuluj")
- `isLoading?: boolean`
- `colorScheme?: string` - Kolor przycisku (domyślnie: "red")

**Przykład użycia:**
```tsx
<ConfirmDialog
  isOpen={isOpen}
  onClose={onClose}
  onConfirm={handleDelete}
  title="Usuń projekt"
  message="Czy na pewno chcesz usunąć ten projekt?"
  confirmText="Usuń"
  isLoading={deleting}
/>
```

#### 5. **UserAvatar**
Komponent awatara użytkownika z inicjałami.

**Właściwości:**
- `firstName: string`
- `lastName: string`
- Wszystkie pozostałe właściwości `AvatarProps` z Chakra UI

**Przykład użycia:**
```tsx
<UserAvatar firstName="Jan" lastName="Kowalski" size="md" />
```

#### 6. **DataCard**
Uniwersalna karta danych z obsługą hover.

**Właściwości:**
- `children: React.ReactNode`
- `hoverable?: boolean` - Czy pokazywać efekt hover (domyślnie: false)
- Wszystkie pozostałe właściwości `BoxProps` z Chakra UI

**Przykład użycia:**
```tsx
<DataCard hoverable p={4}>
  <Text>Zawartość karty</Text>
</DataCard>
```

---

## 🔧 Nowe custom hooki

### Katalog: `src/hooks/`

#### 1. **useToastNotification**
Uproszczone API dla toast'ów z predefiniowanymi ustawieniami.

**Zwracane funkcje:**
- `showSuccess(title, description?, options?)` - Sukces (3s)
- `showError(title, description?, options?)` - Błąd (5s)
- `showWarning(title, description?, options?)` - Ostrzeżenie (4s)
- `showInfo(title, description?, options?)` - Info (3s)
- `toast` - Oryginalny toast z Chakra UI

**Przykład użycia:**
```tsx
const { showSuccess, showError } = useToastNotification();

showSuccess("Zapisano zmiany");
showError("Błąd walidacji", "Wszystkie pola są wymagane");
```

#### 2. **useFetch**
Hook do zarządzania wywołaniami API.

**Zwracane wartości:**
- `data: T | null` - Pobrane dane
- `loading: boolean` - Stan ładowania
- `error: string | null` - Komunikat błędu
- `execute(fetchFn, parseResponse?)` - Funkcja wykonująca zapytanie
- `reset()` - Resetowanie stanu

**Przykład użycia:**
```tsx
const { data, loading, error, execute } = useFetch<User[]>();

useEffect(() => {
  execute(() => userApi.getUsers());
}, []);
```

#### 3. **useForm**
Hook do zarządzania formularzami z walidacją.

**Zwracane wartości:**
- `values: T` - Wartości formularza
- `errors: Partial<Record<keyof T, string>>` - Błędy walidacji
- `touched: Partial<Record<keyof T, boolean>>` - Pola dotknięte
- `handleChange(name, value)` - Zmiana wartości
- `handleBlur(name)` - Obsługa blur
- `validate(rules)` - Walidacja z regułami
- `reset()` - Reset formularza
- `setFieldValue(name, value)` - Ustawienie wartości pola
- `setFieldError(name, error)` - Ustawienie błędu

**Przykład użycia:**
```tsx
const { values, errors, handleChange, validate } = useForm({
  name: "",
  email: ""
});

const handleSubmit = () => {
  const isValid = validate({
    name: (v) => !v ? "Nazwa wymagana" : undefined,
    email: (v) => !v.includes("@") ? "Nieprawidłowy email" : undefined
  });
  
  if (isValid) {
    // Submit form
  }
};
```

#### 4. **useModal**
Uproszczona wersja useDisclosure z dodatkową funkcją toggle.

**Zwracane wartości:**
- `isOpen: boolean`
- `onOpen: () => void`
- `onClose: () => void`
- `toggle: () => void`

**Przykład użycia:**
```tsx
const modal = useModal();

<Button onClick={modal.onOpen}>Otwórz</Button>
<Modal isOpen={modal.isOpen} onClose={modal.onClose}>
  ...
</Modal>
```

---

## 🛠️ Nowe utility functions

### Katalog: `src/utils/`

#### 1. **formatters.ts**
Funkcje pomocnicze do formatowania danych.

**Funkcje:**
- `formatFileSize(bytes: number): string` - Formatowanie rozmiaru pliku
- `formatDate(dateString, includeTime?): string` - Formatowanie daty (pl-PL)
- `formatDateShort(dateString): string` - Krótki format daty (DD.MM.YYYY)
- `formatDateForInput(date?): string` - Format dla input[type="date"] (YYYY-MM-DD)
- `getRelativeTime(dateString): string` - Względny czas ("2 godz. temu")
- `truncateText(text, maxLength): string` - Skracanie tekstu
- `getFileExtension(filename): string` - Pobieranie rozszerzenia pliku
- `isImageFile(filename): boolean` - Sprawdzanie czy plik jest obrazem
- `isPdfFile(filename): boolean` - Sprawdzanie czy plik jest PDF

**Przykład użycia:**
```tsx
import { formatFileSize, formatDate } from "../utils/formatters";

<Text>{formatFileSize(file.size)}</Text>
<Text>{formatDate(file.createdAt)}</Text>
```

#### 2. **constants.ts**
Centralne miejsce dla stałych i konfiguracji.

**Stałe:**
- `getProjectRoleName(role): string` - Nazwa roli projektu
- `getProjectRoleColor(role): string` - Kolor roli projektu
- `getTenantRoleName(role): string` - Nazwa roli organizacji
- `getTenantRoleColor(role): string` - Kolor roli organizacji
- `FILE_UPLOAD` - Konfiguracja uploadu plików
- `WORK_SCHEDULE_COLORS` - Predefiniowane kolory
- `TOAST_DURATION` - Czasy trwania toast'ów

**Przykład użycia:**
```tsx
import { getProjectRoleName, FILE_UPLOAD } from "../utils/constants";

<Badge>{getProjectRoleName(member.role)}</Badge>
<Text>Max size: {formatFileSize(FILE_UPLOAD.MAX_FILE_SIZE)}</Text>
```

---

## 🔄 Zrefaktoryzowane komponenty

### Zmodyfikowane pliki:

1. **AddProjectMemberModal.tsx**
   - Użycie `useToastNotification` zamiast `useToast`
   - Użycie `LoadingSpinner`, `EmptyState`, `UserAvatar`, `DataCard`
   - Użycie funkcji `getProjectRoleName`, `getProjectRoleColor` z constants

2. **Profile.tsx**
   - Użycie `useToastNotification`
   - Użycie `LoadingSpinner`

3. **Projects.tsx**
   - Użycie `useToastNotification`
   - Użycie `useModal` zamiast `useDisclosure`
   - Użycie `LoadingSpinner`, `EmptyState`, `ErrorAlert`
   - Użycie funkcji z constants i formatters

4. **UploadFilesModal.tsx**
   - Użycie `useToastNotification`
   - Użycie `LoadingSpinner`
   - Użycie stałych `FILE_UPLOAD` z constants
   - Użycie `formatFileSize` z formatters

---

## ⚡ Optymalizacje wydajności

### React.memo
Wszystkie komponenty wspólne zostały owinięte w `React.memo` dla optymalizacji:
- `LoadingSpinner`
- `EmptyState`
- `ErrorAlert`
- `UserAvatar`
- `DataCard`

### useCallback i useMemo
Hooki używają `useCallback` dla stabilnych referencji funkcji:
- `useToastNotification` - wszystkie funkcje show*
- `useForm` - wszystkie handler'y
- `useModal` - funkcje onClose i toggle

---

## 📊 Statystyki refaktoryzacji

### Nowe pliki utworzone:
- **6** nowych komponentów wspólnych
- **4** nowe custom hooki
- **2** nowe pliki utility
- **1** plik index.ts dla łatwiejszego importu

### Zredukowana duplikacja:
- Kod obsługi toast'ów: **~50 linii** → **0 linii** (wspólny hook)
- Kod loadingu: **~30 linii** → **0 linii** (wspólny komponent)
- Kod formatowania: **~60 linii** → **0 linii** (wspólne funkcje)
- Kod walidacji: **~40 linii** → **0 linii** (wspólne stałe)

### Korzyści:
- ✅ Większa spójność UI w całej aplikacji
- ✅ Łatwiejsze utrzymanie kodu
- ✅ Mniejsza ilość duplikacji
- ✅ Lepsza wydajność dzięki React.memo
- ✅ Bardziej czytelny kod komponentów

---

## 🎯 Następne kroki (opcjonalne)

### Polecane dalsze ulepszenia:

1. **Refaktoryzacja pozostałych komponentów**
   - ShareFilesModal
   - CreateWorkScheduleModal
   - EditWorkScheduleModal
   - Pozostałe strony (ProjectDetails, MyFiles, SharedFiles, etc.)

2. **Dodanie Error Boundary**
   ```tsx
   // src/components/ErrorBoundary.tsx
   class ErrorBoundary extends Component {
     // Obsługa błędów React
   }
   ```

3. **Rozszerzenie hooków**
   - `useDebounce` - dla input'ów z opóźnionym wyszukiwaniem
   - `usePagination` - dla list z paginacją
   - `useLocalStorage` - dla persystencji danych

4. **Dodanie testów**
   - Unit testy dla hooków
   - Component tests dla komponentów wspólnych
   - Integration tests dla kluczowych flow

5. **Storybook**
   - Dokumentacja komponentów wspólnych
   - Playground dla designerów

6. **React Query / TanStack Query**
   - Zaawansowane zarządzanie cache
   - Automatyczny refetch
   - Optymistyczne update'y

---

## 📝 Przykłady użycia

### Przed refaktoryzacją:
```tsx
// Duplikacja w każdym komponencie
const toast = useToast();
const { isOpen, onOpen, onClose } = useDisclosure();
const [loading, setLoading] = useState(false);

const handleSubmit = async () => {
  setLoading(true);
  try {
    const response = await api.submit();
    if (response.ok) {
      toast({
        title: "Sukces",
        status: "success",
        duration: 3000,
        isClosable: true,
      });
    } else {
      toast({
        title: "Błąd",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    }
  } finally {
    setLoading(false);
  }
};

return loading ? <Spinner /> : <Content />;
```

### Po refaktoryzacji:
```tsx
// Prosty i czytelny kod
const { showSuccess, showError } = useToastNotification();
const modal = useModal();
const [loading, setLoading] = useState(false);

const handleSubmit = async () => {
  setLoading(true);
  try {
    const response = await api.submit();
    if (response.ok) {
      showSuccess("Sukces");
    } else {
      showError("Błąd");
    }
  } finally {
    setLoading(false);
  }
};

return loading ? <LoadingSpinner /> : <Content />;
```

---

## 🔗 Import patterns

### Komponenty wspólne:
```tsx
import { LoadingSpinner, EmptyState, ErrorAlert, UserAvatar } from "../components/common";
```

### Hooki:
```tsx
import { useToastNotification } from "../hooks/useToastNotification";
import { useModal } from "../hooks/useModal";
import { useForm } from "../hooks/useForm";
```

### Utilities:
```tsx
import { formatFileSize, formatDate } from "../utils/formatters";
import { getProjectRoleName, FILE_UPLOAD } from "../utils/constants";
```

---

## ✅ Checklist dla dalszej refaktoryzacji

Przy refaktoryzacji kolejnych komponentów zastosuj:

- [ ] Zamień `useToast` na `useToastNotification`
- [ ] Zamień `useDisclosure` na `useModal` (jeśli używany tylko podstawowy funkcjonalności)
- [ ] Zamień inline loading na `<LoadingSpinner />`
- [ ] Zamień inline empty states na `<EmptyState />`
- [ ] Zamień inline error alerts na `<ErrorAlert />`
- [ ] Użyj `UserAvatar` dla awatarów użytkowników
- [ ] Użyj `DataCard` dla kart danych z hover
- [ ] Użyj funkcji z `formatters.ts` zamiast lokalnych implementacji
- [ ] Użyj stałych z `constants.ts` zamiast hardcoded values
- [ ] Rozważ użycie `React.memo` dla komponentów często re-renderowanych
- [ ] Użyj `useCallback` dla funkcji przekazywanych jako props
- [ ] Użyj `useMemo` dla kosztownych obliczeń

---

**Autor refaktoryzacji:** GitHub Copilot  
**Data:** 12 grudnia 2025
