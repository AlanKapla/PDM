# Top 10 — Najważniejsze przypadki testowe systemu PDM

**Data:** 2026-05-29  
**Zakres:** Całość systemu — krytyczne ścieżki  
**Priorytety:** Wyłącznie wysokie

---

## TC-TOP-001 — Logowanie i uwierzytelnienie (Azure AD B2C)

**Priorytet:** Wysoki  
**Typ:** Pozytywny + Negatywny  
**Moduł:** Auth

### Warunki wstępne
- Konto użytkownika istnieje w Azure AD B2C
- Aplikacja dostępna pod poprawnym URL

### Kroki — Happy path
1. Otwórz aplikację w przeglądarce
2. Kliknij „Zaloguj się"
3. Wprowadź poprawny e-mail i hasło
4. Zatwierdź
5. Aplikacja przekierowuje do głównego widoku

### Oczekiwany wynik
- Użytkownik jest zalogowany
- Token JWT widoczny w nagłówku `Authorization: Bearer ...`
- Wyświetlona lista projektów/tenantów dostępna

### Kroki — Negatywny (błędne hasło)
1. Wprowadź poprawny e-mail, błędne hasło
2. Zatwierdź

### Oczekiwany wynik (negatywny)
- Komunikat błędu z Azure AD B2C
- Brak przekierowania do aplikacji
- Żaden endpoint API nie zwraca danych (401)

---

## TC-TOP-002 — Kontrola dostępu oparta na rolach (RBAC)

**Priorytet:** Wysoki  
**Typ:** Negatywny  
**Moduł:** Authorization / Uprawnienia

### Warunki wstępne
- Użytkownik A: rola **Member** bez uprawnienia `ProjectSettings`
- Użytkownik B: rola **ProjectAdmin**
- Projekt P istnieje w tenantie

### Kroki
1. Zaloguj się jako Użytkownik A
2. Przejdź do Projektu P → Ustawienia projektu
3. Spróbuj edytować nazwę projektu i zapisać
4. Wyloguj → zaloguj jako Użytkownik B
5. Powtórz kroki 2–3

### Oczekiwany wynik
- Użytkownik A: przycisk „Zapisz" jest niewidoczny lub nieaktywny; wywołanie API zwraca **403 Forbidden**
- Użytkownik B: edycja zakończona sukcesem; API zwraca **200 OK**

---

## TC-TOP-003 — Tworzenie i udostępnianie kosztorysu

**Priorytet:** Wysoki  
**Typ:** Pozytywny  
**Moduł:** CostEstimate

### Warunki wstępne
- Użytkownik zalogowany jako ProjectAdmin
- Projekt istnieje

### Kroki
1. Przejdź do modułu Kosztorysy w projekcie
2. Kliknij „Utwórz kosztorys"
3. Podaj nazwę kosztorysu (np. „Kosztorys Q1")
4. Dodaj grupę (np. „Robocizna")
5. Dodaj pozycję do grupy (nazwa, ilość, cena jednostkowa)
6. Zapisz kosztorys
7. Kliknij „Udostępnij" → wybierz Użytkownika B z zakresem `READ`
8. Zaloguj się jako Użytkownik B → sprawdź widoczność kosztorysu

### Oczekiwany wynik
- Kosztorys utworzony, widoczny na liście
- Pozycja ma poprawnie obliczoną wartość (ilość × cena)
- Użytkownik B widzi kosztorys w trybie tylko do odczytu
- Użytkownik B nie może edytować pozycji (brak przycisków edycji lub 403)

---

## TC-TOP-004 — Synchronizacja Kosztorys ↔ Harmonogram

**Priorytet:** Wysoki  
**Typ:** Pozytywny + Brzegowy  
**Moduł:** Sync (CostEstimate ↔ WorkSchedule)

### Warunki wstępne
- Kosztorys z co najmniej 3 pozycjami istnieje w projekcie
- Harmonogram projektu istnieje

### Kroki
1. Otwórz harmonogram projektu
2. Kliknij „Synchronizuj z kosztorysem" (SyncWorkScheduleWithEstimateCommand)
3. Potwierdź synchronizację
4. Sprawdź, czy etapy harmonogramu odpowiadają grupom kosztorysu
5. Wróć do kosztorysu → dodaj nową pozycję
6. Ponownie zsynchronizuj
7. Sprawdź, czy nowa pozycja pojawiła się jako zadanie w harmonogramie

### Oczekiwany wynik
- Po pierwszej synchronizacji: etapy harmonogramu = grupy kosztorysu
- Po dodaniu pozycji i ponownej synchronizacji: harmonogram zaktualizowany
- Żadne istniejące daty/przypisania nie zostały utracone przy resynchronizacji

### Przypadek brzegowy
- Kosztorys z 0 pozycjami → synchronizacja → harmonogram bez zadań, brak błędu

---

## TC-TOP-005 — Zarządzanie członkami projektu (dodaj / usuń / zmień rolę)

**Priorytet:** Wysoki  
**Typ:** Pozytywny + Negatywny  
**Moduł:** ProjectMembers

### Warunki wstępne
- Użytkownik A: ProjectAdmin
- Użytkownik B: istnieje w systemie, nie jest członkiem projektu

### Kroki — Dodaj członka
1. Zaloguj jako A → Projekt → Członkowie
2. Kliknij „Dodaj członka" → wybierz Użytkownika B
3. Ustaw uprawnienia modułowe (np. Estimates: READ, Files: WRITE)
4. Zapisz

### Oczekiwany wynik (dodanie)
- Użytkownik B widoczny na liście członków
- Użytkownik B po zalogowaniu widzi moduły zgodne z przyznanymi uprawnieniami

### Kroki — Usuń członka
5. Usuń Użytkownika B z projektu
6. Zaloguj się jako B → spróbuj wejść do projektu

### Oczekiwany wynik (usunięcie)
- API zwraca **403** lub **404** dla żądań do zasobów projektu
- UI nie wyświetla projektu na liście Użytkownika B

### Negatywny
- Member (bez uprawnienia `ProjectMembers`) próbuje dodać innego użytkownika → **403**

---

## TC-TOP-006 — Upload i pobieranie pliku projektu

**Priorytet:** Wysoki  
**Typ:** Pozytywny + Brzegowy  
**Moduł:** Files

### Warunki wstępne
- Użytkownik ma uprawnienie `ProjectFiles: WRITE` w projekcie

### Kroki — Upload
1. Przejdź do modułu Pliki projektu
2. Kliknij „Prześlij plik"
3. Wybierz plik PDF, ok. 2 MB
4. Poczekaj na zakończenie uploadu
5. Sprawdź, czy plik widoczny na liście

### Oczekiwany wynik
- Plik pojawia się na liście z poprawną nazwą i rozmiarem
- Link do pobrania działa → plik pobiera się bez uszkodzeń

### Kroki — Pobierz
6. Kliknij nazwę pliku → „Pobierz"
7. Otwórz pobrany plik

### Oczekiwany wynik (pobieranie)
- Plik identyczny z oryginałem (brak korupcji)

### Przypadek brzegowy
- Upload pliku > dozwolony limit → komunikat błędu, plik nie zapisany
- Upload bez uprawnień (`ProjectFiles: READ` only) → **403**

---

## TC-TOP-007 — Tracker kosztów — rejestracja wydatku

**Priorytet:** Wysoki  
**Typ:** Pozytywny + Negatywny  
**Moduł:** CostTracker

### Warunki wstępne
- Użytkownik ma uprawnienie `ProjectCosts: WRITE`
- Projekt ma zdefiniowany budżet

### Kroki
1. Przejdź do modułu Cost Tracker
2. Kliknij „Dodaj wydatek"
3. Wypełnij: opis, kwota, data, kategoria, kontrahent (opcjonalnie)
4. Zapisz
5. Sprawdź sumę wydatków na dashboardzie

### Oczekiwany wynik
- Wydatek pojawia się na liście
- Suma wydatków zaktualizowana
- Pozostały budżet = Budżet − Suma wydatków

### Negatywny
- Kwota ujemna → walidacja blokuje zapis, komunikat błędu
- Puste pole „opis" → walidacja blokuje zapis

---

## TC-TOP-008 — Widok Dashboard — spójność danych

**Priorytet:** Wysoki  
**Typ:** Pozytywny + Brzegowy  
**Moduł:** Dashboard / ProjectDashboard

### Warunki wstępne
- Projekt z kosztorysem (łączna wartość znana), harmonogramem i wydatkami
- Użytkownik ma uprawnienie `ProjectDashboardTracker`

### Kroki
1. Przejdź do Dashboardu projektu
2. Odczytaj: wartość kosztorysu, łączne wydatki, % realizacji budżetu
3. Dodaj nowy wydatek w Cost Tracker (TC-TOP-007)
4. Wróć do Dashboardu bez odświeżania strony

### Oczekiwany wynik
- Wartości na dashboardzie są spójne z danymi w modułach
- Po dodaniu wydatku dashboard aktualizuje się (realtime SignalR lub po odświeżeniu)
- % realizacji obliczony poprawnie: `wydatki / budżet × 100`

### Przypadek brzegowy
- Projekt bez budżetu → dashboard nie pokazuje NaN ani błędu; wyświetla „—" lub 0

---

## TC-TOP-009 — Wielodostępność (Multi-tenancy) — izolacja danych

**Priorytet:** Wysoki  
**Typ:** Negatywny / Bezpieczeństwo  
**Moduł:** Authorization / Tenant isolation

### Warunki wstępne
- Tenant A z projektem PA i Użytkownikiem UA
- Tenant B z projektem PB i Użytkownikiem UB
- UA nie jest członkiem PB

### Kroki
1. Zaloguj się jako UA
2. Spróbuj odwołać się do zasobu z Tenanta B (np. przez bezpośredni URL):
   - `GET /api/{tenantB-id}/projects/{PB-id}/cost-estimates`
3. Spróbuj przez modyfikację parametrów w żądaniu
4. Sprawdź logi/odpowiedź API

### Oczekiwany wynik
- API zwraca **403 Forbidden** lub **404 Not Found**
- Żadne dane Tenanta B nie są zwrócone
- Logi API nie ujawniają szczegółów zasobów innego tenanta

---

## TC-TOP-010 — Harmonogram pracy — dodanie etapu i zadania z zależnością

**Priorytet:** Wysoki  
**Typ:** Pozytywny + Brzegowy  
**Moduł:** WorkSchedule

### Warunki wstępne
- Użytkownik ma uprawnienie `ProjectSchedule: WRITE`
- Projekt ma harmonogram

### Kroki
1. Przejdź do modułu Harmonogram
2. Dodaj etap (Stage): „Etap 1 — Fundamenty", daty: 2026-06-01 → 2026-06-30
3. Dodaj zadanie (Work) do etapu: „Betonowanie", przypisz Użytkownikowi B
4. Dodaj drugi etap: „Etap 2 — Ściany"
5. Ustaw zależność: „Etap 2 — Ściany" **zależy od** „Etap 1 — Fundamenty" (Finish-to-Start)
6. Zapisz i odśwież

### Oczekiwany wynik
- Oba etapy widoczne na wykresie Gantta
- Zależność wizualnie zaznaczona (strzałka/linia)
- Próba ustawienia daty rozpoczęcia Etapu 2 przed końcem Etapu 1 → ostrzeżenie walidacyjne

### Przypadek brzegowy
- Zależność cykliczna (A zależy od B, B zależy od A) → system blokuje lub ostrzega

---

## Podsumowanie

| ID | Moduł | Typ | Ryzyko |
|----|-------|-----|--------|
| TC-TOP-001 | Auth | Pozytywny + Negatywny | Krytyczne |
| TC-TOP-002 | Authorization RBAC | Negatywny | Krytyczne |
| TC-TOP-003 | CostEstimate + Share | Pozytywny | Wysokie |
| TC-TOP-004 | Sync CE↔WS | Pozytywny + Brzegowy | Wysokie |
| TC-TOP-005 | ProjectMembers | Pozytywny + Negatywny | Wysokie |
| TC-TOP-006 | Files | Pozytywny + Brzegowy | Wysokie |
| TC-TOP-007 | CostTracker | Pozytywny + Negatywny | Wysokie |
| TC-TOP-008 | Dashboard | Pozytywny + Brzegowy | Wysokie |
| TC-TOP-009 | Multi-tenancy | Negatywny / Security | Krytyczne |
| TC-TOP-010 | WorkSchedule | Pozytywny + Brzegowy | Wysokie |
