# Przypadki testowe — Scenariusze pracy zespołu projektowego

**Data:** 2026-05-29  
**Typ:** Scenariuszowe (end-to-end, manualne)  
**Liczba przypadków:** 10  

---

## Obsada — Persony testowe

| Persona | Imię | Rola w systemie | Uprawnienia do modułów |
|---------|------|-----------------|------------------------|
| **Admin projektu** | Marta K. | Tenant Admin + Project Admin (`IsAdmin=true`) | Wszystkie moduły (Settings, Files, Estimates, Costs, Schedule, DashboardTracker) |
| **Kierownik budowy** | Tomasz W. | Project Member | Estimates, Schedule, Files |
| **Kosztorysant** | Agnieszka P. | Project Member | Estimates only |
| **Wykonawca** | Rafał S. | Project Member | Schedule only |
| **Obserwator** | Bartosz L. | Project Member | DashboardTracker only |

> **Projekt:** "Budowa hali magazynowej A3" — aktywny, budżet netto: 500 000 PLN

---

## TC-TEAM-001 — Admin konfiguruje projekt i zaprasza zespół

**ID:** TC-TEAM-001  
**Priorytet:** Wysoki  
**Typ:** Pozytywny  
**Persona:** Marta K. (Admin)  

### Kontekst
Marta dostaje nowy projekt od klienta. Musi go skonfigurować i dodać właściwy zespół z odpowiednimi uprawnieniami.

### Kroki

| # | Akcja | Oczekiwany rezultat |
|---|-------|---------------------|
| 1 | Marta loguje się do systemu | Dashboard projektów widoczny |
| 2 | Klika „Utwórz projekt" → wpisuje nazwę „Budowa hali magazynowej A3", budżet netto 500 000 PLN | Projekt zostaje utworzony, Marta widzi go na liście |
| 3 | Otwiera projekt → Ustawienia → Członkowie → „Dodaj członka" | Modal „Dodaj członka" otwiera się |
| 4 | Dodaje **Tomasz W.** z uprawnieniami: `Estimates`, `Schedule`, `Files` | Tomasz pojawia się na liście z odpowiednimi modułami |
| 5 | Dodaje **Agnieszka P.** z uprawnieniem: `Estimates` | Agnieszka pojawia się na liście |
| 6 | Dodaje **Rafał S.** z uprawnieniem: `Schedule` | Rafał pojawia się na liście |
| 7 | Dodaje **Bartosz L.** z uprawnieniem: `DashboardTracker` | Bartosz pojawia się na liście |
| 8 | Marta sprawdza listę — 4 członkowie + ona sama | Lista zawiera 5 osób z prawidłowymi rolami |

### Warunki brzegowe do sprawdzenia
- Nie można dodać użytkownika który NIE jest członkiem tenanta
- Nie można dodać tego samego użytkownika dwukrotnie

---

## TC-TEAM-002 — Kosztorysant tworzy kosztorys i wypełnia pozycje

**ID:** TC-TEAM-002  
**Priorytet:** Wysoki  
**Typ:** Pozytywny  
**Persona:** Agnieszka P. (Kosztorysant — moduł `Estimates`)  

### Kontekst
Agnieszka ma za zadanie przygotować kosztorys robocizny. Tworzy go od zera i wypełnia pozycje.

### Kroki

| # | Akcja | Oczekiwany rezultat |
|---|-------|---------------------|
| 1 | Agnieszka loguje się, otwiera projekt | Widzi zakładki: tylko „Kosztorysy" jest aktywna; „Harmonogram", „Pliki", „Dashboard" — brak dostępu |
| 2 | Przechodzi do Kosztorysy → „Nowy kosztorys" | Modal tworzenia kosztorysu |
| 3 | Wpisuje nazwę „Robocizna Q3 2026", wybiera szablon | Kosztorys tworzony, Agnieszka widzi go w scope `Mine` |
| 4 | Otwiera kosztorys → dodaje grupę „Fundamenty" | Grupa pojawia się w strukturze |
| 5 | W grupie dodaje pozycję „Wykop + szalowanie" z wartościami: ilość 120, j.m. m³, cena jedn. 80 PLN | Pozycja dodana, wartość wyliczana automatycznie: 9 600 PLN |
| 6 | Dodaje drugą pozycję „Beton C25/30" — ilość 85, cena 240 PLN | Pozycja dodana: 20 400 PLN |
| 7 | Sprawdza sumę grupy „Fundamenty" | 9 600 + 20 400 = 30 000 PLN |
| 8 | Agnieszka otwiera inny projekt (do którego NIE ma dostępu) | Projekt nie pojawia się na jej liście projektów |

### Warunki brzegowe
- Pole ilość: wartość ujemna → błąd walidacji
- Pole cena: 0 PLN → dozwolone, suma = 0

---

## TC-TEAM-003 — Agnieszka udostępnia kosztorys Tomaszowi do weryfikacji

**ID:** TC-TEAM-003  
**Priorytet:** Wysoki  
**Typ:** Pozytywny  
**Persona:** Agnieszka P. (owner kosztorysu) → Tomasz W. (odbiorca)  

### Kontekst
Kosztorys jest gotowy. Agnieszka chce, żeby Tomasz sprawdził i uzupełnił pole „komentarz wewnętrzny" — tylko to pole jest edytowalne dla współdzielonych.

### Kroki

| # | Akcja | Oczekiwany rezultat |
|---|-------|---------------------|
| 1 | Agnieszka otwiera kosztorys → „Udostępnij" | Modal udostępniania |
| 2 | Wybiera **Tomasz W.** → poziom dostępu `Restricted` (edycja pól niechronionych) | Potwierdzenie udostępnienia |
| 3 | Tomasz loguje się, otwiera Kosztorysy | Widzi zakładkę „Udostępnione mi" — kosztorys „Robocizna Q3 2026" |
| 4 | Tomasz otwiera kosztorys | Widzi strukturę, BRAK przycisków „Dodaj grupę", „Dodaj pozycję" |
| 5 | Tomasz klika pole „Komentarz wewnętrzny" (IsReadonly=false) | Może edytować, wpisuje „OK — zatwierdzam stawki" |
| 6 | Tomasz próbuje zmienić cenę jednostkową pozycji (IsReadonly=true) | Pole jest zablokowane / tylko do odczytu |
| 7 | Agnieszka ponownie otwiera kosztorys | Widzi komentarz Tomasza w polu |

### Weryfikacja uprawnień
- Tomasz NIE może: dodawać grup, pozycji, usuwać kosztorysu, zmieniać nazwy
- Agnieszka widzi kosztorys w scope `Mine`, Tomasz w scope `Shared`

---

## TC-TEAM-004 — Rafał planuje harmonogram prac w terenie

**ID:** TC-TEAM-004  
**Priorytet:** Wysoki  
**Typ:** Pozytywny  
**Persona:** Rafał S. (Wykonawca — moduł `Schedule`)  

### Kontekst
Rafał musi zaplanować etapy budowy w harmonogramie. Ma dostęp tylko do modułu Schedule.

### Kroki

| # | Akcja | Oczekiwany rezultat |
|---|-------|---------------------|
| 1 | Rafał loguje się, otwiera projekt | Widzi TYLKO zakładkę „Harmonogram" |
| 2 | Przechodzi do Harmonogram → „Nowy harmonogram" | Formularz tworzenia |
| 3 | Wpisuje nazwę „Harmonogram główny — Hala A3" | Harmonogram tworzony |
| 4 | Dodaje etap „Faza 1 — Prace ziemne" | Etap pojawia się w drzewie |
| 5 | W etapie dodaje pracę „Wykop fundamentów" z datami: 2026-06-01 → 2026-06-20 | Praca dodana z osią czasu |
| 6 | Dodaje drugą pracę „Szalowanie" 2026-06-15 → 2026-06-28 | Praca dodana |
| 7 | Ustawia zależność FS (Finish-to-Start): „Wykop" → „Szalowanie" | Strzałka zależności widoczna na wykresie Gantta |
| 8 | Przypisuje **siebie** do pracy „Wykop fundamentów" | Przypisanie widoczne |
| 9 | Rafał próbuje wejść w zakładkę „Kosztorysy" | Brak dostępu — zakładka niewidoczna lub komunikat 403 |

---

## TC-TEAM-005 — Tomasz przypisuje członków do prac i dodaje komentarze

**ID:** TC-TEAM-005  
**Priorytet:** Średni  
**Typ:** Pozytywny  
**Persona:** Tomasz W. (Kierownik — moduły `Estimates`, `Schedule`, `Files`)  

### Kontekst
Tomasz jako kierownik ma pełny wgląd. Uzupełnia harmonogram, przypisuje Rafała i dodaje instrukcje w komentarzach.

### Kroki

| # | Akcja | Oczekiwany rezultat |
|---|-------|---------------------|
| 1 | Tomasz otwiera harmonogram Rafała | Widzi istniejącą strukturę (scope `All` — jako member z uprawnieniem Schedule) |
| 2 | Przypisuje **Rafał S.** do etapu „Faza 1" | Przypisanie zapisane |
| 3 | Dodaje komentarz do pracy „Wykop fundamentów": „Sprawdź głębokość wód gruntowych przed startem" | Komentarz widoczny |
| 4 | Dodaje etap „Faza 2 — Konstrukcja stalowa" z pracami: „Montaż słupów" (2026-07-01→2026-07-31) | Etap i praca dodane |
| 5 | Ustawia zależność FS: „Szalowanie" → „Montaż słupów" | Łańcuch zależności widoczny |
| 6 | Sprawdza wykres Gantta — czy daty się nie nakładają niezgodnie z zależnościami | Gantt renderuje prawidłowy układ |

---

## TC-TEAM-006 — Marta zatwierdza kosztorys i śledzi budżet

**ID:** TC-TEAM-006  
**Priorytet:** Wysoki  
**Typ:** Pozytywny  
**Persona:** Marta K. (Admin)  

### Kontekst
Marta przegląda wszystkie kosztorysy projektu (scope `All`) i porównuje z budżetem.

### Kroki

| # | Akcja | Oczekiwany rezultat |
|---|-------|---------------------|
| 1 | Marta otwiera Kosztorysy → filtruje scope „Wszystkie" | Widzi kosztorys Agnieszki „Robocizna Q3 2026" |
| 2 | Otwiera kosztorys — sprawdza sumę | Suma: 30 000 PLN, poziom dostępu `Full` |
| 3 | Marta edytuje pole ceny w pozycji (jako admin ma `Full`) | Edycja działa, wartości przeliczają się automatycznie |
| 4 | Przechodzi do Dashboard → Śledzenie budżetu | Widzi: Budżet: 500 000 PLN, Zaplanowane: 30 000 PLN (6% budżetu) |
| 5 | Dodaje wydatek w Cost Tracker: „Zaliczka dla podwykonawcy" — 15 000 PLN | Wydatek zapisany |
| 6 | Dashboard aktualizuje: Wydano: 15 000 PLN, Pozostało: 485 000 PLN | Liczby prawidłowe |

---

## TC-TEAM-007 — Rafał próbuje wyjść poza swoje uprawnienia

**ID:** TC-TEAM-007  
**Priorytet:** Wysoki  
**Typ:** Negatywny (kontrola dostępu)  
**Persona:** Rafał S. (Wykonawca — tylko `Schedule`)  

### Kontekst
Test weryfikujący, że ograniczenia uprawnień działają realnie — nie tylko w UI, ale też na poziomie API.

### Kroki

| # | Akcja | Oczekiwany rezultat |
|---|-------|---------------------|
| 1 | Rafał próbuje otworzyć URL kosztorysów bezpośrednio (deeplink) | Redirect do 403 lub strony „Brak dostępu" |
| 2 | Rafał próbuje wywołać API `GET /cost-estimate/all` z własnym tokenem | HTTP 403 Forbidden |
| 3 | Rafał próbuje wywołać API `POST /cost-estimate` (tworzenie kosztorysu) | HTTP 403 Forbidden |
| 4 | Rafał próbuje otworzyć zakładkę „Pliki" | Zakładka niewidoczna lub 403 |
| 5 | Rafał próbuje wywołać API `GET /files` z własnym tokenem | HTTP 403 Forbidden |
| 6 | Rafał próbuje wejść w Ustawienia projektu | 403 — nie jest adminem |
| 7 | Rafał otwiera zakładkę „Harmonogram" | Działa normalnie — to jego uprawnienie |

### Kryterium zaliczenia
Każda próba dostępu do niedozwolonego zasobu kończy się 403. UI NIE wyświetla danych z nieautoryzowanych modułów nawet przez chwilę (brak „flash of unauthorized content").

---

## TC-TEAM-008 — Zespół komunikuje się przez czat projektowy

**ID:** TC-TEAM-008  
**Priorytet:** Średni  
**Typ:** Pozytywny  
**Persona:** Marta K. (inicjuje), Tomasz W., Rafał S.  

### Kontekst
Marta tworzy grupowy czat projektowy dla core zespołu. Testujemy komunikację w czasie rzeczywistym i ograniczenia edycji.

### Kroki

| # | Akcja | Oczekiwany rezultat |
|---|-------|---------------------|
| 1 | Marta otwiera Chat → „Nowy czat grupowy" | Modal tworzenia grupy |
| 2 | Dodaje: Tomasz W., Rafał S. → nazwa grupy „Hala A3 — Ekipa" | Czat grupowy tworzony (3 osoby = min. wymaganie) |
| 3 | Marta wysyła: „Zaczynamy projekt od 1 czerwca. Rafał — potwierdź gotowość." | Wiadomość widoczna dla wszystkich |
| 4 | Rafał widzi wiadomość w czasie rzeczywistym (SignalR) | Powiadomienie + wiadomość bez odświeżania |
| 5 | Rafał odpowiada: „Potwierdzam, sprzęt gotowy." | Wiadomość widoczna |
| 6 | Tomasz edytuje swoją wiadomość (w ciągu 15 minut) | Edycja możliwa, widoczny znacznik „Edytowano" |
| 7 | Tomasz próbuje edytować wiadomość Rafała | Brak przycisku „Edytuj" przy cudzej wiadomości |
| 8 | Marta usuwa wiadomość Rafała jako admin czatu | Wiadomość zastąpiona „[wiadomość usunięta]" |
| 9 | Rafał próbuje wysłać wiadomość po 16 minutach i ją edytować | Edycja ZABLOKOWANA (przekroczono 15 min), wysyłanie nowej działa |
| 10 | Bartosz L. próbuje napisać do „Hala A3 — Ekipa" | Bartosz NIE jest w tym czacie — nie widzi go na liście |

---

## TC-TEAM-009 — Tomasz wgrywa dokumenty techniczne

**ID:** TC-TEAM-009  
**Priorytet:** Średni  
**Typ:** Pozytywny  
**Persona:** Tomasz W. (Kierownik — ma moduł `Files`)  

### Kontekst
Tomasz wgrywa dokumentację projektową — rysunki techniczne i specyfikację. Agnieszka i Rafał NIE mają dostępu do plików (Agnieszka: tylko Estimates; Rafał: tylko Schedule).

### Kroki

| # | Akcja | Oczekiwany rezultat |
|---|-------|---------------------|
| 1 | Tomasz otwiera zakładkę „Pliki" | Lista paczek plików (pustá) |
| 2 | Tworzy paczkę „Dokumentacja techniczna v1" | Paczka tworzona |
| 3 | Wgrywa plik „rysunki_konstrukcyjne.pdf" (5 MB) | Upload zakończony, plik widoczny w paczce |
| 4 | Wgrywa plik „specyfikacja_materiałowa.xlsx" | Upload zakończony |
| 5 | Tomasz aktualizuje plik „rysunki_konstrukcyjne.pdf" → nowa wersja v2 | Nowa wersja widoczna, stara dostępna w historii |
| 6 | Marta (admin) pobiera plik v1 z historii wersji | Pobieranie działa — admin ma dostęp do wszystkich wersji |
| 7 | Agnieszka próbuje otworzyć zakładkę „Pliki" | Brak dostępu — zakładka niewidoczna |
| 8 | Rafał próbuje otworzyć zakładkę „Pliki" | Brak dostępu — zakładka niewidoczna |

---

## TC-TEAM-010 — Obserwator przegląda dashboard, admin usuwa nieaktywnego członka

**ID:** TC-TEAM-010  
**Priorytet:** Średni  
**Typ:** Pozytywny + Negatywny (graniczny)  
**Persona:** Bartosz L. (Obserwator), Marta K. (Admin)  

### Kontekst
Bartosz monitoruje postęp finansowy projektu. Po zakończeniu swojej roli Marta usuwa go z projektu. Weryfikujemy, że po usunięciu Bartosz traci dostęp natychmiastowo.

### Kroki

| # | Akcja | Oczekiwany rezultat |
|---|-------|---------------------|
| 1 | Bartosz loguje się, otwiera projekt | Widzi TYLKO zakładkę „Dashboard" |
| 2 | Dashboard wyświetla: budżet, wydatki, % realizacji | Dane widoczne (30 000 PLN zaplanowane, 15 000 PLN wydane) |
| 3 | Bartosz próbuje kliknąć „Kosztorysy" (URL bezpośredni) | 403 — brak modułu Estimates |
| 4 | Bartosz przegląda wykresy — filtruje po miesiącu (czerwiec 2026) | Wykres aktualizuje się |
| 5 | **Marta** otwiera Ustawienia → Członkowie → usuwa **Bartosz L.** | Potwierdzenie usunięcia, Bartosz znika z listy |
| 6 | Bartosz (zalogowany w tej samej chwili) odświeża stronę projektu | Projekt znika z jego listy LUB przekierowanie z komunikatem „Brak dostępu" |
| 7 | Bartosz próbuje wywołać API `GET /dashboard` z posiadanym tokenem | HTTP 403 — uprawnienie cofnięte |
| 8 | Marta sprawdza, że pozostałe 4 osoby mają niezmieniony dostęp | Lista członków: 4 osoby z prawidłowymi modułami |

### Kryterium zaliczenia
Usunięcie członka musi mieć efekt natychmiastowy — żaden request z jego tokenem nie może być autoryzowany po usunięciu (bez konieczności wylogowania/ponownego logowania).

---

## Podsumowanie

| TC | Persona | Typ | Moduły | Priorytet |
|----|---------|-----|--------|-----------|
| TC-TEAM-001 | Marta (Admin) | Pozytywny | Settings, Members | Wysoki |
| TC-TEAM-002 | Agnieszka (Kosztorysant) | Pozytywny | Estimates | Wysoki |
| TC-TEAM-003 | Agnieszka → Tomasz | Pozytywny | Estimates (Share) | Wysoki |
| TC-TEAM-004 | Rafał (Wykonawca) | Pozytywny | Schedule, Gantt | Wysoki |
| TC-TEAM-005 | Tomasz (Kierownik) | Pozytywny | Schedule | Średni |
| TC-TEAM-006 | Marta (Admin) | Pozytywny | Estimates, Dashboard, Costs | Wysoki |
| TC-TEAM-007 | Rafał (Wykonawca) | Negatywny | Wszystkie (kontrola 403) | Wysoki |
| TC-TEAM-008 | Marta, Tomasz, Rafał | Pozytywny + Brzegowy | Chat, SignalR | Średni |
| TC-TEAM-009 | Tomasz, Marta | Pozytywny | Files, Wersjonowanie | Średni |
| TC-TEAM-010 | Bartosz (Obserwator) + Marta | Pozytywny + Negatywny | Dashboard, Members | Średni |

**Pokryte moduły:** Settings, Members, Estimates (tworzenie + sharing + AccessLevel), Schedule (Gantt + zależności), Files (wersjonowanie), Costs/Dashboard, Chat (SignalR + limity czasowe), uprawnienia (403 enforcement)
