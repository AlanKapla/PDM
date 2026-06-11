# Mock Data System dla screenshotów

## Opis
Stworzenie systemu mockowanych danych w głównej aplikacji PDM (ProjectDataManagementUI), który po włączeniu przez admina z poziomu UI prezentuje bardzo realistyczne, kompletne dane testowe we wszystkich modułach aplikacji. Admin może nawigować po zamockowanej aplikacji i robić screenshoty, które posłużą jako grafiki na landing page Brickly (w sekcji Modules).

## Zakres
- **Główna aplikacja PDM**: ProjectDataManagementUI (React 18 + Chakra UI 2 + Vite 7)
- **Landing page**: BricklyLandingPage — brak zmian w kodzie, użycie gotowych screenshotów

## Wymagania

### Funkcjonalne
1. System mock danych wykorzystujący axios interceptor do przechwytywania requestów API
2. Przełącznik Mock ON/OFF w navbarze aplikacji (MainLayout)
3. Widoczny tylko dla użytkowników z rolą `SystemRole.SuperAdmin`
4. Dostępny wyłącznie w środowisku deweloperskim (flaga `VITE_MOCK_ENABLED`)
5. Statyczny pokaz danych — bez potrzeby CRUD, dane tylko do odczytu
6. Nawigacja działa normalnie — wszystkie strony i routy są dostępne
7. Dane w języku polskim
8. Obejmuje wszystkie 8 modułów prezentowanych na landing page

### Moduły do zmockowania
1. **Dokumentacja projektowa** — projekty, pliki, wersje, komentarze, udostępnianie
2. **Dokumentacja kosztowa** — wydatki, akceptacja kosztów
3. **Kosztorysy** — szablony, pozycje, warianty, komponenty
4. **Harmonogram** — etapy, okresy, zakresy prac, zadania, zależności
5. **Synchronizacja** — integracja kosztorys-harmonogram
6. **Dashboard** — analiza kosztowo-czasowa, alerty, KPI
7. **Komunikacja i zadania** — chat, przypisane prace
8. **Organizacja** — kontrahenci, parametryzacja projektu

### Techniczne
- Break down:
  - Mock interceptor w axiosClient (przed istniejącym interceptorem auth)
  - URL pattern matching dla każdego endpointa
  - Response status 200 z realistycznym payloadem
  - Query params i path params obsługiwane
  - Typy odpowiedzi zgodne z istniejącymi interfejsami TypeScript

## Architektura

```
src/mock/
├── MockContext.tsx              # React Context (isMockEnabled, toggle)
├── MockToggle.tsx               # Komponent przełącznika w navbarze
├── MockInterceptor.ts           # Axios interceptor + MockAxiosResponse
├── mockData/
│   ├── index.ts                 # Rejestr URL → handler mapowania
│   ├── projects.ts              # Projekty (lista, detale, parametry)
│   ├── tenants.ts               # Organizacje + członkowie
│   ├── costEstimates.ts         # Kosztorysy + szablony
│   ├── costTracker.ts           # Wydatki i akceptacje
│   ├── workSchedule.ts          # Harmonogram + etapy + zadania
│   ├── files.ts                 # Pliki i dokumenty
│   ├── dashboard.ts             # Dashboard + KPI + alerty
│   ├── chat.ts                  # Wiadomości i czaty
│   ├── contractors.ts           # Kontrahenci
│   └── users.ts                 # Użytkownicy systemowi
└── types.ts                     # Typy pomocnicze
```

## Zasady
- Zero modyfikacji istniejących plików API (`src/api/*.ts`)
- Zero modyfikacji istniejących hooków React Query
- Zero modyfikacji istniejących komponentów UI
- Mock interceptor dodany jako nowy plik, podpięty w axiosClient
- Przełącznik dodany w MainLayout jako nowy komponent

## Kryteria akceptacji
1. Admin (SuperAdmin) widzi przełącznik Mock w navbarze na dev
2. Po włączeniu mocka, wszystkie strony wyświetlają realistyczne dane
3. Po wyłączeniu, aplikacja wraca do normalnego działania
4. Zwykli użytkownicy nie widzą przełącznika
5. Na produkcji przełącznik nie jest dostępny
6. Wszystkie 8 modułów ma dane we wszystkich widokach (lista, detale, itp.)
