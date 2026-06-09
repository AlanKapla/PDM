# Testy E2E — System PDM (Project Data Management)

**Wersja dokumentu:** 1.0  
**Data:** 2026-06-09  
**Liczba przypadków testowych:** 10  
**Prefiks:** TC-E2E-{NNN}

---

## Spis przypadków

| # | ID | Tytuł | Zaangażowane moduły | Priorytet |
|---|-----|-------|-------------------|-----------|
| 1 | TC-E2E-001 | Pełny cykl życia projektu — od utworzenia tenantów po dashboard | Tenant, Projekt, Kosztorys, Harmonogram, Synchronizacja, Dashboard | **Krytyczny** |
| 2 | TC-E2E-002 | Obieg kosztu — od utworzenia przez submit/approve do odzwierciedlenia w budżecie | Kosztorys, ProjektCost, Dashboard, Uprawnienia | **Krytyczny** |
| 3 | TC-E2E-003 | Zarządzanie plikami — upload, wersjonowanie, komentowanie i udostępnianie | Pliki, Uprawnienia, Współpraca | **Wysoki** |
| 4 | TC-E2E-004 | Harmonogram z zależnościami i przypisaniami — od utworzenia po synchronizację z kosztorysem | Harmonogram, Kosztorys, Synchronizacja, Członkowie | **Wysoki** |
| 5 | TC-E2E-005 | Granice uprawnień — weryfikacja izolacji danych między rolami i projektami | Uprawnienia, Tenant, Projekt, Wszystkie moduły | **Krytyczny** |
| 6 | TC-E2E-006 | Śledzenie kosztów rzeczywistych z AI — import faktury, powiązanie z pozycją, dashboard | CostTracker, AI, Kosztorys, Dashboard | **Wysoki** |
| 7 | TC-E2E-007 | Współpraca wieloosobowa — pełny flow zespołowy na 5 rolach | Współpraca, Kosztorys, Harmonogram, Pliki, Chat, Uprawnienia | **Krytyczny** |
| 8 | TC-E2E-008 | Komunikacja i powiadomienia — chat, wiadomości grupowew i notyfikacje real-time | Chat, Notyfikacje, SignalR, Członkowie | **Średni** |
| 9 | TC-E2E-009 | Śledzenie budżetu projektu — budget → kosztorysy → koszty → approve → dashboard | Dashboard, Kosztorys, ProjektCost, CostTracker | **Wysoki** |
| 10 | TC-E2E-010 | Izolacja danych między tenantami — brak przecieku danych | Tenant, Wszystkie moduły, Uprawnienia | **Krytyczny** |

---

## TC-E2E-001: Pełny cykl życia projektu

**Tytuł:** Od utworzenia tenantów po dashboard — pełny E2E projektu budowlanego  
**Priorytet:** Krytyczny  
**Typ:** Pozytywny (happy path)  
**Zaangażowane moduły:** Tenant → Projekt → Kosztorys → Harmonogram → Synchronizacja → Dashboard  
**Rola wymagana:** Admin tenant (wszystkie permisje)

### Opis
Użytkownik przechodzi pełny cykl: zakłada organizację (tenant), tworzy projekt, dodaje kosztorys, planuje harmonogram, synchronizuje dane i weryfikuje dashboard.

### Warunki wstępne
- Użytkownik jest zalogowany (Azure AD B2C)
- Użytkownik nie ma jeszcze żadnego tenant

### Kroki testowe

| Krok | Akcja | Oczekiwany rezultat |
|------|-------|---------------------|
| 1 | Otwórz stronę `/dashboard` | Wyświetla się strona z komunikatem „Nie masz jeszcze żadnego projektu" lub pusty dashboard |
| 2 | Kliknij „Utwórz organizację" na stronie głównej lub w menu | Otwiera się formularz `TenantCreateForm` |
| 3 | Wpisz nazwę organizacji np. „Firma Budowlana XYZ" i kliknij „Utwórz" | `POST /api/tenants/create` zwraca 201. Przekierowanie do widoku tenantów. Nowy tenant widoczny na liście w `/tenants/managed` |
| 4 | Wejdź w szczegóły tenantów (`/tenants/:tenantId`) | Wyświetla się panel z danymi tenantów, lista członków (tylko Ty) |
| 5 | Kliknij „Zaproś członka", wpisz email „tomasz@example.com", wybierz rolę „Admin" | `POST /api/tenants/{id}/invitations` zwraca 200. Zaproszenie widoczne w zakładce „Zaproszenia" |
| 6 | Przejdź do listy projektów (`/projects`) | Pusta lista projektów |
| 7 | Kliknij „Nowy projekt", wpisz nazwę „Budowa Osiedla Słonecznego", ustaw budżet netto 5 000 000 PLN | `POST /api/tenants/{t}/projects` zwraca 201. Przekierowanie do widoku projektu |
| 8 | W zakładce „Członkowie" dodaj członka projektu — wybierz użytkownika „tomasz@example.com" z listy członków tenantów, nadaj uprawnienia do wszystkich modułów (Write) | `POST /api/tenants/{t}/projects/{p}/members` zwraca 200. Członek widoczny na liście |
| 9 | Przejdź do „Kosztorysy" (`/projects/:id/cost-estimates`), kliknij „Nowy kosztorys" | `GET /api/cost-estimate-template/defaults` zwraca listę szablonów |
| 10 | Wybierz szablon „Ogólny budowlany", wpisz nazwę „Kosztorys główny" | `POST /api/tenants/{t}/projects/{p}/cost-estimate` zwraca 201. Kosztorys w statusie `Draft` |
| 11 | W edytorze kosztorysu dodaj grupę „Roboty ziemne", dodaj pozycję „Wykopy" z wartością netto 50 000 PLN | `POST /groups` → 201, `POST /items` → 201. Pozycja widoczna w grupie |
| 12 | Dodaj grupę „Roboty fundamentowe", pozycję „Ławy fundamentowe" 120 000 PLN | Analogicznie. Grupy i pozycje wyświetlają się poprawnie |
| 13 | Kliknij „Przelicz" — kosztorys sumuje wartości | `POST /{id}/recalculate` → 200. TotalNet = 170 000 PLN |
| 14 | Zmień status kosztorysu na „Gotowy do przeglądu" | Status widoczny jako `ReadyForReview` |
| 15 | Przejdź do „Harmonogramy" (`/projects/:id/schedules`), kliknij „Nowy harmonogram" | `POST /api/tenants/{t}/projects/{p}/work-schedule` zwraca 201 |
| 16 | Kliknij „Synchronizuj z kosztorysem", wybierz kosztorys „Kosztorys główny" | `POST /work-schedule/{id}/sync-with-estimate` → 200. Etapy harmonogramu odpowiadają grupom kosztorysu („Roboty ziemne", „Roboty fundamentowe") |
| 17 | W harmonogramie dodaj zadanie „Wykopy" w etapie „Roboty ziemne", przypisz daty 01.07-15.07.2026 | Zadanie widoczne na osi czasu (Gantt) |
| 18 | Dodaj zadanie „Ławy fundamentowe" w etapie „Roboty fundamentowe", daty 16.07-31.07.2026 | Zadanie widoczne na osi czasu |
| 19 | Ustaw zależność Finish-to-Start między „Wykopy" → „Ławy fundamentowe" | Na Gantcie strzałka łącząca zadania. `GET /.../work-schedule/{id}` zwraca `dependencies` z poprawnymi ID |
| 20 | Przypisz zadanie „Wykopy" do członka „tomasz@example.com" | `POST /.../assignments` → 200. Przypisanie widoczne w widoku zadania |
| 21 | Przejdź do dashboardu projektu (`/projects/:id/dashboard`) | Wyświetla: Budżet 5 000 000 PLN, Kosztorysy 170 000 PLN (zagregowane), Pozostało 4 830 000 PLN. Wykresy słupkowe/kołowe |
| 22 | Wyloguj się, zaloguj jako „tomasz@example.com" (zaakceptuj zaproszenie) | `POST /api/tenants/invitations/accept` → 200. Tenant i projekt widoczne po odświeżeniu |
| 23 | Jako Tomasz, wejdź w dashboard projektu | Widzi te same dane co admin (uprawnienia Write na wszystkich modułach) |

---

## TC-E2E-002: Obieg kosztu — submit i approval

**Tytuł:** Obieg kosztu projektowego od utworzenia przez submit/approve do odzwierciedlenia w budżecie  
**Priorytet:** Krytyczny  
**Typ:** Pozytywny  
**Zaangażowane moduły:** Kosztorys → ProjectCost → Dashboard → Uprawnienia  
**Rola wymagana:** Użytkownik z uprawnieniami `ProjectCosts.Write` + admin projektu

### Opis
Kierownik tworzy koszt projektu, wysyła do zatwierdzenia, admin zatwierdza — koszt pojawia się w dashboardzie.

### Warunki wstępne
- Istnieje tenant z projektem „Budowa Osiedla Słonecznego"
- Istnieje kosztorys z pozycjami
- Dwóch użytkowników: Marta (admin projektu), Tomasz (kierownik, uprawnienia `Costs: Write`)

### Kroki testowe

| Krok | Akcja | Oczekiwany rezultat |
|------|-------|---------------------|
| 1 | Zaloguj się jako Tomasz. Wejdź w projekt → zakładka „Koszty" (`/projects/:id/costs`) | Pusta lista kosztów (lub istniejące) |
| 2 | Kliknij „Dodaj koszt", wypełnij: nazwa „Fundamenty — beton B20", wartość netto 80 000 PLN, wybierz kontrahenta „Betonix", powiąż z pozycją kosztorysową „Ławy fundamentowe", data 10.07.2026 | `POST /api/tenants/{t}/projects/{p}/cost` → 201. Koszt widoczny na liście w statusie `Draft` |
| 3 | Kliknij koszt „Fundamenty — beton B20", sprawdź szczegóły | Widoczne: nazwa, kwota, kontrahent, status Draft, pola do edycji |
| 4 | Kliknij „Prześlij do zatwierdzenia" | `POST /.../cost/{costId}/submit` → 200. Status zmienia się na `PendingApproval`. Przyciski edycji znikają, pojawia się „Wycofaj" |
| 5 | Spróbuj edytować koszt w statusie PendingApproval | Edycja zablokowana (403 lub pola disabled). Tylko withdraw jest dostępny |
| 6 | Wyloguj się, zaloguj jako Marta (admin) | — |
| 7 | Wejdź w projekt → zakładka „Koszty" | Widzi koszt „Fundamenty — beton B20" w statusie `PendingApproval` |
| 8 | Kliknij koszt, sprawdź szczegóły | Widoczne wszystkie dane, dostępne przyciski „Zatwierdź" i „Odrzuć" |
| 9 | Kliknij „Zatwierdź" | `POST /.../cost/{costId}/approve` → 200. Status zmienia się na `Approved`. Pojawia się data i osoba zatwierdzająca (`ApprovedAt`, `ApprovedByUserId`) |
| 10 | Przejdź do dashboardu projektu (`/projects/:id/dashboard`) | W sekcji „Koszty zatwierdzone" widnieje 80 000 PLN. Wykres budżetu uwzględnia zatwierdzony koszt. Pozostało: 5 000 000 - (170 000 kosztorysy) - (80 000 koszty) |
| 11 | Wyloguj się, zaloguj jako Tomasz | — |
| 12 | Wejdź w koszt — widzi status `Approved` | Przyciski edycji niedostępne. Można tylko wyświetlić |

---

## TC-E2E-003: Zarządzanie plikami — upload, wersje, komentarze i udostępnienie

**Tytuł:** Pełny cykl życia pliku w projekcie  
**Priorytet:** Wysoki  
**Typ:** Pozytywny + negatywny  
**Zaangażowane moduły:** Pliki → Uprawnienia → Współpraca  
**Rola wymagana:** Użytkownik z uprawnieniami `Files.Write` oraz inny z `Files.ReadShared`

### Opis
Użytkownik tworzy strukturę katalogów, uploaduje plik, dodaje nową wersję, komentuje, udostępnia innemu członkowi projektu i weryfikuje kontrolę dostępu.

### Warunki wstępne
- Projekt z co najmniej 2 członkami: Marta (Files: Write) i Rafał (Files: ViewShared)
- Plik testowy „specyfikacja.pdf" (5 MB) i jego zmodyfikowana wersja

### Kroki testowe

| Krok | Akcja | Oczekiwany rezultat |
|------|-------|---------------------|
| 1 | Zaloguj się jako Marta. Wejdź w `Projekt → Pliki` (`/projects/:id/files`) | Wyświetla się lista plików (pusta). Widoczny przycisk „Nowy katalog" i „Prześlij plik" |
| 2 | Kliknij „Nowy katalog", wpisz nazwę „Dokumentacja projektowa" | `POST /api/tenants/{t}/projects/{p}/file/directories` → 201. Katalog widoczny w drzewie |
| 3 | Kliknij „Prześlij plik", wybierz „specyfikacja.pdf", jako lokalizację wybierz katalog „Dokumentacja projektowa" | `POST /api/tenants/{t}/projects/{p}/file` z form data → 201. Plik pojawia się w katalogu jako wersja 1. Wyświetla się nazwa, rozmiar, data, ikona PDF |
| 4 | Kliknij na plik — wejdź w szczegóły | Panel szczegółów: nazwa, rozmiar, wersja 1, data uploadu, przycisk „Prześlij nową wersję" |
| 5 | Kliknij „Prześlij nową wersję", wybierz zmodyfikowany plik | `POST /api/tenants/{t}/projects/{p}/file/versions` → 201. Wersja zmienia się na 2. Historia wersji pokazuje v1 i v2 z datami |
| 6 | Kliknij „Dodaj komentarz" na wersji 2, wpisz „Zaktualizowano zgodnie z wytycznymi" | `POST /.../file/{id}/versions/{versionId}/comments` → 201. Komentarz widoczny pod wersją |
| 7 | Kliknij „Udostępnij", wybierz członka „Rafał" z poziomem dostępu „Podgląd" | `POST /.../file/packages/share` → 200. Udostępnienie skonfigurowane |
| 8 | Wyloguj się, zaloguj jako Rafał | — |
| 9 | Wejdź w `Projekt → Pliki` | Widzi katalog „Dokumentacja projektowa" z plikiem „specyfikacja.pdf" (uprawnienie ViewShared) |
| 10 | Kliknij plik — pobierz | Plik pobiera się poprawnie (wersja 2, najnowsza) |
| 11 | Spróbuj kliknąć „Prześlij nową wersję" | Przycisk niedostępny lub `POST` zwraca 403 (Rafał ma tylko ViewShared) |
| 12 | Spróbuj dodać komentarz | Przycisk „Dodaj komentarz" jest ukryty lub disabled |
| 13 | Wyloguj się, zaloguj jako Marta. Zmień uprawnienia Rafała na `Write` w module Pliki | `PATCH /.../members/{id}` → 200 |
| 14 | Zaloguj się jako Rafał. Odśwież stronę plików | Przycisk „Prześlij nową wersję" i „Dodaj komentarz" są teraz dostępne |
| 15 | Dodaj komentarz „Sprawdzono — OK" | `POST` → 201. Komentarz widoczny dla obu użytkowników |

---

## TC-E2E-004: Harmonogram z zależnościami, przypisaniami i synchronizacją z kosztorysem

**Tytuł:** Zaawansowane planowanie harmonogramu — synchronizacja, zależności, przypisania i zamknięcie okresów  
**Priorytet:** Wysoki  
**Typ:** Pozytywny + brzegowy  
**Zaangażowane moduły:** Harmonogram → Kosztorys → Synchronizacja → Członkowie  
**Rola wymagana:** Uprawnienia `Schedule.Write`

### Opis
Użytkownik tworzy harmonogram, synchronizuje z kosztorysem, dodaje ręczne zadania, konfiguruje zależności (w tym opóźnienia), przypisuje wykonawców, zamyka okresy i weryfikuje widok Gantta.

### Warunki wstępne
- Projekt z kosztorysem zawierającym grupy: „Roboty ziemne" (z itemami), „Stan surowy" (z itemami)
- Co najmniej 3 członków projektu do przypisań

### Kroki testowe

| Krok | Akcja | Oczekiwany rezultat |
|------|-------|---------------------|
| 1 | Wejdź w zakładkę „Harmonogramy" (`/projects/:id/schedules`) | Lista harmonogramów (pusta lub istniejące). Przycisk „Nowy harmonogram" |
| 2 | Kliknij „Nowy harmonogram", nazwa „Harmonogram budowy" | `POST` → 201. Przekierowanie do widoku harmonogramu (pusty Gantt) |
| 3 | Kliknij „Synchronizuj z kosztorysem", wybierz istniejący kosztorys | `POST /.../work-schedule/{id}/sync-with-estimate` → 200. Etapy utworzone na podstawie grup kosztorysu. Zadania utworzone na podstawie itemów |
| 4 | Sprawdź listę etapów: „Roboty ziemne", „Stan surowy" | Etapy widoczne w panelu po lewej. Zadania (itemy) widoczne w ramach etapów |
| 5 | Dodaj ręcznie nowe zadanie „Przygotowanie placu budowy" w etapie „Roboty ziemne", daty 20.06-30.06.2026 | `POST /.../work-schedule/{id}/works` → 201. Zadanie pojawia się w Gantcie jako osobny wiersz |
| 6 | Kliknij zadanie „Wykopy" → dodaj okres: 01.07.2026 - 15.07.2026 | `POST /.../works/{workId}/periods` → 201. Czarny pasek okresu na osi czasu |
| 7 | Ustaw zależność: „Przygotowanie placu" (poprzednik) → „Wykopy" (następnik), typ Finish-to-Start, lag 2 dni | `POST /.../dependencies` → 201. Na Gantcie strzałka od końca „Przygotowanie placu" do początku „Wykopy". Lag widoczny jako przerwa 2 dni |
| 8 | Kliknij zadanie „Wykopy" → zakładka „Przypisania", przypisz do Rafała | `POST /.../assignments` → 201. Awatar Rafała pojawia się przy zadaniu |
| 9 | Przypisz „Ławy fundamentowe" do Tomasza | `POST /.../assignments` → 201. |
| 10 | Otwórz widok „Moje zadania" (`/assigned-works`) | Lista zadań przypisanych do bieżącego użytkownika (jeśli ma przypisania) |
| 11 | Kliknij zadanie „Wykopy" → „Zamknij okres" dla pierwszego okresu | `PATCH /.../periods/{periodId}` z `IsClosed = true` → 200. Okres oznaczony jako zamknięty (wizualnie przekreślony / zmiana koloru) |
| 12 | Spróbuj zamknąć okres zadania, które ma niezamknięte poprzedniki (brak) | Operacja dozwolona (system nie wymusza zamykania sekwencyjnego — weryfikacja) |
| 13 | Kliknij „Edytuj zależność", zmień lag na 5 dni | `PUT /.../dependencies/{id}` → 200. Lag na Gantcie aktualizuje się |
| 14 | Usuń zależność | `DELETE /.../dependencies/{id}` → 204. Strzałka znika |
| 15 | Weryfikacja: odśwież widok Gantta | Wszystkie etapy, zadania, okresy, przypisania widoczne poprawnie. Oś czasu przewija się w zakresie dat |

---

## TC-E2E-005: Granice uprawnień — izolacja danych między rolami i projektami

**Tytuł:** Weryfikacja kontroli dostępu — użytkownik nie może wykonać operacji bez odpowiednich uprawnień  
**Priorytet:** Krytyczny  
**Typ:** Negatywny (negative path)  
**Zaangażowane moduły:** Uprawnienia → Tenant → Projekt → Kosztorys → Harmonogram → Pliki → Koszty  
**Rola wymagana:** Wiele ról

### Opis
System testowany jest pod kątem szczelności uprawnień. Użytkownik z ograniczonymi uprawnieniami (Obserwator) próbuje wykonywać akcje zarezerwowane dla wyższych ról. Każda próba musi zwrócić 403 Forbidden.

### Warunki wstępne
- Projekt z członkami:
  - Marta — admin projektu
  - Bartosz — członek z uprawnieniami wyłącznie do odczytu (module permissions: wszystkie na `View`)
  - Rafał — członek z uprawnieniami tylko `Files.Write`

### Kroki testowe

| Krok | Akcja | Oczekiwany rezultat |
|------|-------|---------------------|
| 1 | Zaloguj się jako Bartosz (tylko View we wszystkich modułach) | — |
| 2 | Wejdź w projekt → zakładka „Kosztorysy" | Lista kosztorysów widoczna (read) |
| 3 | Kliknij „Nowy kosztorys" | Przycisk jest ukryty LUB `POST /cost-estimate` zwraca 403 |
| 4 | Spróbuj wywołać `POST /api/tenants/{t}/projects/{p}/cost-estimate` przez konsolę devtools (lub Postman) | 403 Forbidden. Nagłówek `X-Permission-Code: PROJECT.ESTIMATES.WRITE` |
| 5 | Spróbuj edytować istniejący kosztorys (PUT) | 403 Forbidden |
| 6 | Przejdź do zakładki „Harmonogramy" | Lista harmonogramów widoczna (read) |
| 7 | Spróbuj dodać zadanie w harmonogramie | 403 Forbidden (`PROJECT.SCHEDULE.WRITE`) |
| 8 | Przejdź do zakładki „Koszty" | Lista kosztów widoczna (read) |
| 9 | Spróbuj przesłać koszt do zatwierdzenia | 403 Forbidden (`PROJECT.COSTS.SUBMIT`) |
| 10 | Przejdź do zakładki „Pliki" | Lista plików widoczna (o ile jakieś są udostępnione z uprawnieniem ViewShared) |
| 11 | Spróbuj usunąć plik | 403 Forbidden (`PROJECT.FILES.DELETE`) |
| 12 | Spróbuj usunąć członka projektu | 403 Forbidden (`PROJECT.MEMBERS.DELETE`) |
| 13 | Spróbuj zmienić status projektu | 403 Forbidden |
| 14 | Wyloguj się, zaloguj jako Marta (admin). Zmień uprawnienia Bartosza — daj `Files.Write` | `PATCH /.../members/{id}` → 200 |
| 15 | Zaloguj się jako Bartosz. Wejdź w Pliki | Teraz widzi przycisk „Prześlij plik" i może uploadować |
| 16 | Spróbuj nadal utworzyć kosztorys | 403 — uprawnienia nie zmieniły się dla innych modułów |
| 17 | Wyloguj się, zaloguj jako Rafał (Files.Write tylko) | — |
| 18 | Wejdź w zakładkę „Koszty" | 403 — Rafał nie ma uprawnień `ProjectCosts.View` — cała zakładka może być ukryta lub zwracać 403 |

---

## TC-E2E-006: Śledzenie kosztów rzeczywistych z AI — faktura → dashboard

**Tytuł:** Import kosztu rzeczywistego przez AI, powiązanie z pozycją kosztorysową i weryfikacja na dashboardzie  
**Priorytet:** Wysoki  
**Typ:** Pozytywny  
**Zaangażowane moduły:** CostTracker → AI → Kosztorys → Dashboard  
**Rola wymagana:** Uprawnienia `DashboardTracker.Write`

### Opis
Użytkownik otrzymuje fakturę od kontrahenta (plik JPG/PNG). Korzysta z AI do automatycznego sparsowania danych faktury, zatwierdza, tworzy TrackedCost powiązany z pozycją kosztorysową i weryfikuje na dashboardzie.

### Warunki wstępne
- Projekt z kosztorysem zawierającym pozycję „Ławy fundamentowe" (120 000 PLN)
- Plik faktury `faktura-fundamenty.jpg` (skan faktury z kwotą 45 000 PLN netto, kontrahent „Betonix")
- Włączona integracja z Azure OpenAI

### Kroki testowe

| Krok | Akcja | Oczekiwany rezultat |
|------|-------|---------------------|
| 1 | Wejdź w dashboard projektu (`/projects/:id/dashboard`) | Widok dashboardu: budżet, kosztorysy, koszty |
| 2 | Kliknij „Dodaj wydatek" lub ikonkę + w sekcji „Koszty rzeczywiste" | Otwiera się modal `AICostImportModal` (lub formularz TrackedCost) |
| 3 | Wybierz opcję „Sparsuj z faktury" (AI Import), wybierz plik `faktura-fundamenty.jpg` | `POST /api/tenants/{t}/projects/{p}/ai/cost/parse/tracked-cost` z form data → 200. AI zwraca sparsowane dane: `{ name: "Beton B20", net: 45000, gross: 55350, contractor: "Betonix", date: "2026-06-01" }` |
| 4 | Pojawia się podgląd sparsowanych danych. Sprawdź poprawność: nazwa, kwota netto, kontrahent, data | Wszystkie pola wypełnione, możliwość edycji przed zapisem |
| 5 | Popraw ewentualne błędy (np. zmień nazwę na „Fundamenty — beton B20 — faktura"), wybierz powiązanie z pozycją kosztorysową „Ławy fundamentowe" | Pola edytowalne. Widoczna lista pozycji kosztorysowych do wyboru |
| 6 | Kliknij „Zapisz" | `POST /api/tenants/{t}/projects/{p}/cost-trackers/costs` → 201. TrackedCost utworzony. Modal zamyka się |
| 7 | W sekcji „Koszty rzeczywiste" na dashboardzie pojawia się nowy wpis: „Fundamenty — beton B20 — faktura" — 45 000 PLN | Wpis widoczny z datą, kontrahentem i ikonką AI (oznaczenie importu AI) |
| 8 | Kliknij wpis — rozwiń szczegóły | Widoczne: kwota netto 45 000, brutto 55 350, kontrahent Betonix, powiązanie z „Ławy fundamentowe", data |
| 9 | Sprawdź kalkulacje dashboardu: kosztorysy = 170 000, koszty rzeczywiste = 45 000, łączne = 215 000, pozostało z budżetu 5 000 000 - 215 000 = 4 785 000 | Wartości muszą się zgadzać. Wykres kołowy/słupkowy odzwierciedla proporcje |
| 10 | Kliknij „Edytuj" wpis, zmień kwotę na 47 000 PLN | `PUT /api/.../cost-trackers/costs/{id}` → 200. Dashboard odświeża się: pozostało = 4 783 000 |
| 11 | Kliknij „Usuń" wpis, potwierdź w dialogu | `DELETE /.../cost-trackers/costs/{id}` → 204. Wpis znika z dashboardu. Koszty rzeczywiste wracają do poprzedniej wartości |

---

## TC-E2E-007: Współpraca wieloosobowa — pełny flow zespołowy na 5 rolach

**Tytuł:** Scenariusz zespołowy — każda rola wykonuje swoje zadania w projekcie  
**Priorytet:** Krytyczny  
**Typ:** Pozytywny (happy path)  
**Zaangażowane moduły:** Współpraca → Kosztorys → Harmonogram → Pliki → Chat → Uprawnienia → Dashboard  
**Rola wymagana:** Wszystkie role projektowe

### Opis
Scenariusz obejmujący 5 postaci: Marta (admin), Tomasz (kierownik), Agnieszka (kosztorysant), Rafał (wykonawca), Bartosz (obserwator). Każda osoba pracuje w swoim zakresie, komunikują się przez chat, a admin monitoruje postęp na dashboardzie.

### Warunki wstępne
- Tenant z projektem „Budowa Osiedla Słonecznego"
- 5 kont użytkowników z przypisanymi rolami w projekcie

### Kroki testowe

| Krok | Akcja | Oczekiwany rezultat |
|------|-------|---------------------|
| **Faza 1: Admin konfiguruje projekt** | | |
| 1 | Marta (admin): ustawia budżet projektu na 8 000 000 PLN, dodaje wszystkich członków z odpowiednimi permisjami | `PUT /.../projects/{id}/currency` + `PUT /.../projects/{id}` z budżetem. `POST /.../members` dla każdego. Wszyscy członkowie widoczni w zakładce |
| **Faza 2: Kosztorysant tworzy kosztorys** | | |
| 2 | Agnieszka (kosztorysant, `Estimates.Write`): tworzy kosztorys „Kosztorys inwestorski" z 3 grupami i 10 pozycjami, sumując 2 500 000 PLN | `POST /cost-estimate` → 201. Wszystkie grupy i itemy utworzone. TotalNet = 2 500 000 |
| 3 | Agnieszka: udostępnia kosztorys Tomaszowi jako „Reviewer" | `POST /.../shares` → 200. Tomasz widzi kosztorys w swoim widoku |
| 4 | Agnieszka: zmienia status na `ReadyForReview` | Status zmieniony. Edycja zablokowana dla Agnieszki |
| **Faza 3: Kierownik przegląda i akceptuje** | | |
| 5 | Tomasz (kierownik, `Estimates.Read` + `Schedule.Write`): przegląda kosztorys, dodaje komentarz „Zwiększyć nakłady na roboty ziemne" | Komentarz widoczny. Agnieszka może odpowiedzieć |
| 6 | Tomasz: zmienia status na `Approved` | `POST /.../cost-estimate/{id}/...` → 200. Status `Approved` |
| **Faza 4: Planowanie harmonogramu** | | |
| 7 | Tomasz: tworzy harmonogram, synchronizuje z zatwierdzonym kosztorysem, przypisuje Rafała do zadań wykonawczych | Synchronizacja działa. Rafał widzi przypisane zadania |
| **Faza 5: Prace wykonawcze** | | |
| 8 | Rafał (wykonawca, `Schedule.Write` + `Files.Read`): loguje się, w `/assigned-works` widzi swoje zadania | Lista zadań poprawna. Może oznaczać postęp |
| 9 | Rafał: przesyła plik „Raport z postępu prac.pdf" w zadaniu „Wykopy" | Plik widoczny w kontekście zadania (lub w module Pliki z oznaczeniem zadania) |
| **Faza 6: Czat i komunikacja** | | |
| 10 | Marta: tworzy grupowy chat projektowy „Budowa Osiedla" | `POST /api/tenants/{t}/chats` → 201. Czat widoczny dla wszystkich członków projektu |
| 11 | Marta: pisze wiadomość „Proszę o raport z postępów do piątku" | Wiadomość dostarczona przez SignalR. Wszyscy członkowie widzą ją w czasie rzeczywistym |
| 12 | Rafał: odpowiada „Raport w trakcie przygotowania" | SignalR dostarcza odpowiedź. Widoczna jako wątek (reply) |
| 13 | Agnieszka: wysyła bezpośrednią wiadomość do Marty z pytaniem o budżet | `POST /api/chats/direct` → 201. Marta widzi notyfikację w czasie rzeczywistym |
| **Faza 7: Monitorowanie** | | |
| 14 | Marta: w dashboardzie sprawdza postęp | Widzi: budżet 8 000 000, kosztorysy 2 500 000 (zatwierdzone), koszty rzeczywiste 0, harmonogram 30% ukończenia (jeśli okresy zamknięte) |
| **Faza 8: Obserwator (read-only)** | | |
| 15 | Bartosz (obserwator, wszystkie moduły View): loguje się, przegląda kosztorys, harmonogram, pliki | Wszystkie dane widoczne do odczytu. Próba edycji blokowana (403) |

---

## TC-E2E-008: Komunikacja i powiadomienia — chat, grupy i notyfikacje real-time

**Tytuł:** Komunikacja zespołowa z wykorzystaniem chatu, grup i powiadomień SignalR  
**Priorytet:** Średni  
**Typ:** Pozytywny  
**Zaangażowane moduły:** Chat → Notyfikacje → SignalR → Członkowie  
**Rola wymagana:** Dowolny członek projektu

### Opis
Dwóch użytkowników wymienia wiadomości w czacie bezpośrednim, zakłada grupowy chat projektowy, weryfikuje dostarczanie w czasie rzeczywistym przez SignalR oraz sprawdza system notyfikacji (licznik, oznaczanie jako przeczytane).

### Warunki wstępne
- Dwóch użytkowników: Marta i Tomasz, obaj w tym samym tenancie
- Otwarte dwie sesje przeglądarki (lub dwie karty)

### Kroki testowe

| Krok | Akcja | Oczekiwany rezultat |
|------|-------|---------------------|
| 1 | Marta: otwiera `/chat` | Lista konwersacji (pusta lub istniejące). Przycisk „Nowa wiadomość" |
| 2 | Marta: kliknij „Nowa wiadomość", wybierz Tomasza z listy kontaktów | `POST /api/chats/direct` → 201. Otwiera się okno czatu z Tomaszem |
| 3 | Marta: wpisz „Cześć Tomasz, sprawdź proszę kosztorys" i wyślij | Wiadomość pojawia się w oknie czatu. SignalR wysyła do Tomasza |
| 4 | Tomasz (druga sesja): widzi wiadomość w czasie rzeczywistym (bez odświeżania) | Wiadomość pojawia się w oknie czatu. Licznik nieprzeczytanych (badge) na ikonie czatu pokazuje 1 |
| 5 | Tomasz: odpowiada „Już sprawdzam, daj znać co jeszcze" | Marta widzi odpowiedź w czasie rzeczywistym |
| 6 | Tomasz: edytuje swoją wiadomość („Już sprawdzam, prześlę uwagi") | `PUT /api/chats/{chatId}/messages/{id}` → 200. Przy wiadomości pojawia się znacznik „edytowano" |
| 7 | Marta: otwiera `/notification` | Lista powiadomień. Widoczne powiadomienie: „Nowa wiadomość od Tomasza" |
| 8 | Marta: sprawdza licznik nieprzeczytanych powiadomień (`GET /api/notification/unread-counter`) | Licznik > 0 |
| 9 | Marta: klika „Oznacz wszystkie jako przeczytane" | `PUT /api/notification/mark-all-as-read` → 200. Licznik spada do 0. Wszystkie notyfikacje oznaczone jako przeczytane |
| 10 | Marta: tworzy grupowy czat „Budowa Osiedla" i dodaje Tomasza i Agnieszkę | `POST /api/tenants/{t}/chats` → 201. Czat widoczny dla wszystkich dodanych |
| 11 | Agnieszka: loguje się, widzi nowy grupowy czat na liście | `GET /api/tenants/{t}/chats` zwraca nowy czat. Badge z nieprzeczytanymi |
| 12 | Agnieszka: wysyła plik „kosztorys-v2.xlsx" w czacie grupowym | Plik wysłany jako załącznik. Widoczny dla wszystkich w czacie. Można pobrać |
| 13 | Marta: wyszukuje w czacie frazę „kosztorys" | `GET /api/tenants/{t}/chats/search?q=kosztorys` → wyniki zawierające wiadomości z tym słowem |
| 14 | Marta: usuwa swoją pierwszą wiadomość | `DELETE /api/chats/{chatId}/messages/{id}` → 200. Wiadomość znika (lub oznaczona jako usunięta „[wiadomość usunięta]") |

---

## TC-E2E-009: Śledzenie budżetu projektu — od ustawienia po pełny dashboard

**Tytuł:** End-to-end budżet projektu — budget netto → kosztorysy → koszty projektowe → zatwierdzenie → dashboard  
**Priorytet:** Wysoki  
**Typ:** Pozytywny + weryfikacja spójności danych  
**Zaangażowane moduły:** Dashboard → Kosztorys → ProjektCost → CostTracker  
**Rola wymagana:** Admin projektu + kierownik

### Opis
Użytkownik konfiguruje budżet projektu, tworzy kosztorysy, dodaje koszty projektowe (z submit/approve), rejestruje koszty rzeczywiste i weryfikuje spójność danych na dashboardzie we wszystkich wariantach (Draft vs Approved).

### Warunki wstępne
- Projekt z ustawionym budżetem netto 10 000 000 PLN
- Dwóch użytkowników: admin i kierownik

### Kroki testowe

| Krok | Akcja | Oczekiwany rezultat |
|------|-------|---------------------|
| 1 | Admin: wejdź w dashboard projektu (`/projects/:id/dashboard`) | Widok dashboardu. Budżet netto: 10 000 000 PLN. Wszystkie wartości = 0. Pozostało: 10 000 000 |
| 2 | Admin: przejdź do ustawień projektu, sprawdź pole „Budżet netto" | Pole = 10 000 000 PLN. Można edytować |
| 3 | Admin: zmień budżet na 12 000 000 PLN | Dashboard odświeża się: budżet = 12 000 000, pozostało = 12 000 000 |
| 4 | Kierownik: utwórz kosztorys „Kosztorys ogólny" z grupami na sumę 3 000 000 PLN (status Draft) | Dashboard: kosztorysy = 3 000 000 (Draft wliczone lub nie — zależne od konfiguracji). Jeśli wliczone: pozostało = 9 000 000 |
| 5 | Kierownik: zmień status kosztorysu na `Approved` | Dashboard odświeża się. Kosztorysy zatwierdzone = 3 000 000 |
| 6 | Kierownik: utwórz drugi kosztorys na 1 500 000 PLN, zostaw w Draft | Weryfikacja: czy draft jest wliczany do sumy (oczekiwane: TAK, draft jest wliczany jako planowane wydatki) |
| 7 | Kierownik: utwórz koszt projektowy (ProjectCost): „Materiały budowlane" 500 000 PLN, status Draft | Dashboard: koszty (Draft) = 500 000. Pozostało = 12 000 000 - 4 500 000 - 500 000 = 7 000 000 |
| 8 | Kierownik: prześlij koszt do zatwierdzenia → status `PendingApproval` | Dashboard: koszty oczekujące = 500 000 |
| 9 | Admin: zatwierdź koszt | Dashboard: koszty zatwierdzone = 500 000. Pozostało = 7 000 000 |
| 10 | Admin: dodaj TrackedCost (koszt rzeczywisty) „Faktura za beton" 80 000 PLN, powiąż z pozycją kosztorysową | Dashboard: koszty rzeczywiste = 80 000 |
| 11 | Admin: sprawdź podsumowanie dashboardu: | |
| | - Budżet: 12 000 000 | Zgodność |
| | - Kosztorysy (zatwierdzone): 3 000 000 | Zgodność |
| | - Kosztorysy (wersje robocze): 1 500 000 | Zgodność |
| | - Koszty projektowe (zatwierdzone): 500 000 | Zgodność |
| | - Koszty rzeczywiste (TrackedCosts): 80 000 | Zgodność |
| | - Łączne zaangażowanie: 5 080 000 | Zgodność |
| | - Pozostało w budżecie: 6 920 000 | Zgodność |
| 12 | Admin: otwórz osobno sekcje „Koszty" i „Cost Tracker" i zweryfikuj, czy wartości pojedyncze odpowiadają dashboardowi | Suma zgadza się we wszystkich widokach |
| 13 | Kierownik: usuń kosztorys draft (1 500 000) | `DELETE /cost-estimate/{id}` → 204. Dashboard: łączny kosztorys = 3 000 000 (tylko Approved) |
| 14 | Admin: usuń zatwierdzony koszt projektowy (500 000) | `DELETE /cost/{id}` → 204 (usunięcie zatwierdzonego kosztu — sprawdź, czy dozwolone). Dashboard odświeża się |

---

## TC-E2E-010: Izolacja danych między tenantami — brak przecieku danych

**Tytuł:** Weryfikacja multi-tenancy — dane Tenant A nie są dostępne z poziomu Tenant B  
**Priorytet:** Krytyczny  
**Typ:** Negatywny (security)  
**Zaangażowane moduły:** Tenant → Wszystkie moduły → Uprawnienia  
**Rola wymagana:** Użytkownik w dwóch tenantach

### Opis
Użytkownik należy do dwóch niezależnych organizacji (tenantów). Weryfikuje, że dane projektów, kosztorysów, plików, harmonogramów, kosztów i członków są w pełni izolowane między tenantami.

### Warunki wstępne
- Dwa niezależne tenanty: „Firma A" i „Firma B"
- Użytkownik Marta jest adminem w obu
- Każdy tenant ma własny projekt z danymi:
  - Firma A: projekt „Budowa A", kosztorys na 500 000, plik „specyfikacja-A.pdf"
  - Firma B: projekt „Budowa B", kosztorys na 1 000 000, plik „specyfikacja-B.pdf"

### Kroki testowe

| Krok | Akcja | Oczekiwany rezultat |
|------|-------|---------------------|
| 1 | Marta: zaloguj się, przełącz na Tenant A (`PUT /api/tenants/active` z ID tenant A) | `GET /api/user/auth-status` zwraca `activeTenantId = TenantA` |
| 2 | Otwórz listę projektów (`GET /api/tenants/{t}/projects`) | Widoczne tylko projekty Tenant A („Budowa A"). Projekt „Budowa B" NIE widoczny |
| 3 | Otwórz kosztorysy projektu „Budowa A" | Kosztorys na 500 000 widoczny. Brak kosztorysu z Tenant B |
| 4 | Spróbuj ręcznie wywołać `GET /api/tenants/{TENANT_B}/projects/{PROJECT_B}/cost-estimate` | 403 Forbidden lub 404 NotFound (w zależności od implementacji — tenant ID w predicate powoduje brak wyników) |
| 5 | Spróbuj wywołać `GET /api/tenants/{TENANT_B}/projects` | 403 Forbidden — użytkownik nie ma dostępu do tenant B (nie jest aktywny lub endpoint filtruje po aktywnym tenantcie) |
| 6 | Otwórz pliki projektu „Budowa A" | W plikach widoczny tylko „specyfikacja-A.pdf". Brak plików z Tenant B |
| 7 | Spróbuj pobrać plik z Tenant B przez bezpośrednie ID pliku | 404 — plik nie znaleziony (predicate zawiera TenantId = A, więc plik z Tenant B nie jest widoczny) |
| 8 | Przełącz na Tenant B (`PUT /api/tenants/active` z ID tenant B) | `GET /api/user/auth-status` → `activeTenantId = TenantB` |
| 9 | Otwórz listę projektów | Widoczny tylko „Budowa B". Projekt „Budowa A" zniknął |
| 10 | Otwórz kosztorysy — widoczny kosztorys na 1 000 000 | Brak kosztorysu z Tenant A |
| 11 | Spróbuj wywołać endpointy z jawnym Tenant A w URL | 403/NotFound |
| 12 | Sprawdź członków tenantów: `GET /api/tenants/{TENANT_A}/members` (będąc w Tenant B) | 403 lub wyniki tylko Tenant B — lista członków jest izolowana |
| 13 | Sprawdź chat: `GET /api/chats/direct` (będąc w Tenant B) | Wiadomości z Tenant A nie są widoczne (chat bezpośredni może być cross-tenant — sprawdź czy wiadomości wymienione w kontekście Tenant A są oddzielone) |
| 14 | Sprawdź notyfikacje: `GET /api/notification` (będąc w Tenant B) | Tylko notyfikacje z Tenant B. Żadne notyfikacje z Tenant A nie przeciekają |
| 15 | Wróć do Tenant A, powtórz test izolacji symetrycznie | Tenant A izolowany od B w ten sam sposób |

---

## Podsumowanie

| Lp. | ID | Priorytet | Moduły | Flow |
|-----|-----|-----------|--------|------|
| 1 | TC-E2E-001 | **Krytyczny** | Tenant, Projekt, Kosztorys, Harmonogram, Synchronizacja, Dashboard | Rejestracja → projekt → kosztorys → harmonogram → synchronizacja → dashboard |
| 2 | TC-E2E-002 | **Krytyczny** | Kosztorys, ProjectCost, Dashboard, Uprawnienia | Koszt → submit → approve → dashboard |
| 3 | TC-E2E-003 | Wysoki | Pliki, Uprawnienia, Współpraca | Upload → wersja → komentarz → udostępnienie → kontrola dostępu |
| 4 | TC-E2E-004 | Wysoki | Harmonogram, Kosztorys, Synchronizacja, Członkowie | Synchronizacja → zadania → zależności → przypisania → Gantt |
| 5 | TC-E2E-005 | **Krytyczny** | Uprawnienia, wszystkie moduły | Próby nieautoryzowanych operacji → 403 Forbidden |
| 6 | TC-E2E-006 | Wysoki | CostTracker, AI, Kosztorys, Dashboard | AI faktura → TrackedCost → powiązanie → dashboard |
| 7 | TC-E2E-007 | **Krytyczny** | Wszystkie moduły | 5 ról → pełny workflow zespołowy |
| 8 | TC-E2E-008 | Średni | Chat, Notyfikacje, SignalR | Wiadomość → grupa → SignalR → notyfikacje |
| 9 | TC-E2E-009 | Wysoki | Dashboard, Kosztorys, Koszty | Budżet → kosztorysy → koszty → approve → poprawność danych |
| 10 | TC-E2E-010 | **Krytyczny** | Tenant, wszystkie moduły | Izolacja danych między tenantami |
