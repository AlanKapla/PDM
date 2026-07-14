# Przypadki testowe — Moduł Plików

**Data:** 2026-05-29  
**Liczba przypadków:** 10  
**Prefiks:** TC-FILE-###

---

## Konwencje

| Rola | Opis |
|------|------|
| **Tenant Admin** | Administrator tenanta — widzi wszystko (scope: All) |
| **Project Admin** | Administrator projektu — widzi wszystko (scope: All) |
| **Owner** | Właściciel pliku — może Read, Write, Share, Delete |
| **Shared User** | Użytkownik z dostępem — może Read i Write na udostępnionych plikach |
| **Member** | Członek projektu bez dostępu do konkretnego pliku — widzi tylko swoje (scope: Mine) |

---

## TC-FILE-001 — Upload pliku (happy path)

**Moduł:** Pliki → Upload  
**Typ:** Pozytywny  
**Priorytet:** Wysoki  
**Rola:** Member (dowolny członek projektu)

### Warunki wstępne
- Użytkownik jest członkiem projektu z uprawnieniem `PROJECT.FILES`
- Przygotowany plik `.pdf` o rozmiarze < 10 MB

### Kroki

| # | Akcja | Oczekiwany wynik |
|---|-------|-----------------|
| 1 | Przejdź do projektu → zakładka **Pliki** | Widoczna lista paczek + przycisk „Dodaj pliki" |
| 2 | Kliknij „Dodaj pliki" | Otwiera się modal `UploadFilesModal` |
| 3 | Wpisz nazwę nowej paczki (np. „Dokumentacja techniczna") | Pole przyjmuje tekst |
| 4 | Dodaj opcjonalną wyświetlaną nazwę pliku | Pole displayName wypełnione |
| 5 | Przeciągnij plik `.pdf` do strefy dropzone lub kliknij i wybierz plik | Plik pojawia się na liście z nazwą i rozmiarem |
| 6 | Kliknij „Zapisz" / „Prześlij" | Loader widoczny podczas uploadu |
| 7 | Poczekaj na zakończenie uploadu | Modal zamknięty, nowa paczka widoczna na liście, plik widoczny wewnątrz paczki |

### Oczekiwany wynik końcowy
Paczka „Dokumentacja techniczna" widoczna w zakładce **Mine**. Plik posiada wersję `v1`.

---

## TC-FILE-002 — Upload pliku z niedozwolonym typem

**Moduł:** Pliki → Upload  
**Typ:** Negatywny  
**Priorytet:** Wysoki  
**Rola:** Member

### Warunki wstępne
- Przygotowane pliki: `dokument.docx`, `foto.png`, `skrypt.exe`

### Kroki

| # | Akcja | Oczekiwany wynik |
|---|-------|-----------------|
| 1 | Otwórz modal „Dodaj pliki" | Modal widoczny |
| 2 | Próbuj dodać plik `dokument.docx` | Komunikat błędu: niedozwolony typ pliku. Plik NIE pojawia się na liście |
| 3 | Próbuj dodać `foto.png` | Komunikat błędu: niedozwolony typ pliku |
| 4 | Próbuj dodać `skrypt.exe` | Komunikat błędu: niedozwolony typ pliku |
| 5 | Dodaj poprawny plik `.pdf` lub `.jpg` | Plik akceptowany |

### Oczekiwany wynik końcowy
Tylko pliki `.pdf`, `.jpg`, `.jpeg` są akceptowane. Przycisk „Prześlij" jest nieaktywny dopóki na liście nie ma żadnego poprawnego pliku.

---

## TC-FILE-003 — Upload pliku przekraczającego limit 10 MB

**Moduł:** Pliki → Upload  
**Typ:** Brzegowy  
**Priorytet:** Wysoki  
**Rola:** Member

### Warunki wstępne
- Przygotowany plik `.pdf` o rozmiarze **11 MB**
- Przygotowany plik `.pdf` o rozmiarze dokładnie **10 MB** (limit brzegowy)

### Kroki

| # | Akcja | Oczekiwany wynik |
|---|-------|-----------------|
| 1 | Otwórz modal „Dodaj pliki" | Modal widoczny |
| 2 | Dodaj plik 11 MB | Komunikat błędu: plik przekracza limit 10 MB. Plik NIE dodany |
| 3 | Dodaj plik dokładnie 10 MB | Plik **zaakceptowany** (limit włącznie) |
| 4 | Kliknij „Prześlij" | Upload przechodzi pomyślnie dla pliku 10 MB |

### Oczekiwany wynik końcowy
Pliki do 10 MB włącznie są akceptowane. Komunikat błędu pojawia się natychmiast po próbie dodania zbyt dużego pliku (walidacja frontendowa).

---

## TC-FILE-004 — Upload nowej wersji pliku przez właściciela

**Moduł:** Pliki → Wersjonowanie  
**Typ:** Pozytywny  
**Priorytet:** Wysoki  
**Rola:** Owner

### Warunki wstępne
- Istniejący plik w wersji `v1`
- Użytkownik jest właścicielem pliku (`isOwner: true`)

### Kroki

| # | Akcja | Oczekiwany wynik |
|---|-------|-----------------|
| 1 | Rozwiń paczkę i znajdź plik | Widoczna aktualna wersja `v1` |
| 2 | Kliknij „Dodaj wersję" przy pliku | Otwiera się modal `UploadNewVersionModal` |
| 3 | Dodaj nowy plik `.pdf` | Plik widoczny w modalu |
| 4 | Wpisz opcjonalny komentarz do wersji (np. „Poprawiono rozdział 3") | Pole przyjmuje tekst |
| 5 | Kliknij „Prześlij" | Loader podczas uploadu |
| 6 | Poczekaj na zakończenie | Modal zamknięty |

### Oczekiwany wynik końcowy
Plik posiada teraz `v2`. Licznik `totalVersions` wzrósł do 2. Wersja `v1` nadal dostępna w historii wersji. Komentarz widoczny przy wersji `v2`.

---

## TC-FILE-005 — Próba dodania nowej wersji przez Shared User

**Moduł:** Pliki → Wersjonowanie  
**Typ:** Pozytywny (uprawnienie Write)  
**Priorytet:** Średni  
**Rola:** Shared User

### Warunki wstępne
- Plik udostępniony użytkownikowi B przez użytkownika A (właściciela)
- Użytkownik B zalogowany

### Kroki

| # | Akcja | Oczekiwany wynik |
|---|-------|-----------------|
| 1 | Przejdź do zakładki **Pliki** → podzakładka **Shared** | Widoczny udostępniony plik |
| 2 | Rozwiń plik | Widoczna aktualna wersja |
| 3 | Sprawdź czy przycisk „Dodaj wersję" jest widoczny | Przycisk **widoczny** (Shared User ma prawo Write) |
| 4 | Kliknij „Dodaj wersję" i prześlij nowy plik | Upload przebiega pomyślnie |
| 5 | Sprawdź czy przycisk „Usuń plik" / „Udostępnij" jest dostępny | Przyciski **niewidoczne** (Shared User nie ma Delete ani Share) |

### Oczekiwany wynik końcowy
Shared User może dodawać wersje, ale nie może usuwać ani zarządzać udostępnieniem pliku.

---

## TC-FILE-006 — Dodanie komentarza do wersji pliku

**Moduł:** Pliki → Komentarze  
**Typ:** Pozytywny  
**Priorytet:** Średni  
**Rola:** Owner lub Shared User

### Warunki wstępne
- Istniejący plik z co najmniej jedną wersją

### Kroki

| # | Akcja | Oczekiwany wynik |
|---|-------|-----------------|
| 1 | Rozwiń plik i wybierz wersję | Widoczna sekcja komentarzy |
| 2 | Wpisz komentarz (np. „Wersja zatwierdzona przez klienta") | Pole przyjmuje tekst |
| 3 | Kliknij „Dodaj komentarz" | Komentarz pojawia się na liście natychmiast (optymistyczna aktualizacja lub refresh) |
| 4 | Sprawdź wyświetlane dane | Widoczne: treść, `userName`, `createdAt`, flaga `isEdited: false` |
| 5 | Wpisz komentarz o długości 2001 znaków | Pole blokuje input lub pokazuje błąd walidacji (max 2000) |

### Oczekiwany wynik końcowy
Komentarz dodany poprawnie. Komentarz przekraczający 2000 znaków jest odrzucany.

---

## TC-FILE-007 — Edycja i usunięcie komentarza przez autora

**Moduł:** Pliki → Komentarze  
**Typ:** Pozytywny + Negatywny  
**Priorytet:** Średni  
**Rola:** Autor komentarza vs. inny użytkownik

### Warunki wstępne
- Istniejący komentarz dodany przez Użytkownika A
- Użytkownik B (niebędący autorem) zalogowany w drugiej sesji

### Kroki

| # | Akcja | Oczekiwany wynik |
|---|-------|-----------------|
| 1 | Użytkownik A: kliknij „Edytuj" przy swoim komentarzu | Pole edycji aktywne |
| 2 | Zmień treść i zatwierdź | Komentarz zaktualizowany, flaga `isEdited: true`, widoczne `editedAt` |
| 3 | Użytkownik A: kliknij „Usuń" przy swoim komentarzu | Potwierdzenie usunięcia, komentarz znika z listy |
| 4 | Użytkownik B: sprawdź komentarz innego użytkownika | Przyciski „Edytuj" i „Usuń" są **niewidoczne** (`canEdit: false`, `canDelete: false`) |

### Oczekiwany wynik końcowy
Tylko autor komentarza widzi i może użyć opcji edycji/usunięcia. Admin projektu może usuwać komentarze innych.

---

## TC-FILE-008 — Udostępnienie pliku wybranemu użytkownikowi (Share)

**Moduł:** Pliki → Udostępnianie  
**Typ:** Pozytywny  
**Priorytet:** Wysoki  
**Rola:** Owner

### Warunki wstępne
- Istniejący plik (`isOwner: true`)
- W projekcie istnieje co najmniej jeden inny członek (Użytkownik B)

### Kroki

| # | Akcja | Oczekiwany wynik |
|---|-------|-----------------|
| 1 | Kliknij ikonę udostępnienia przy pliku | Otwiera się modal `ManageFileShareModal` |
| 2 | Sprawdź listę dostępnych użytkowników | Lista członków projektu z checkboxami |
| 3 | Zaznacz checkbox przy Użytkowniku B | Checkbox zaznaczony |
| 4 | Kliknij „Zapisz" | Modal zamknięty, wysłany `PUT /file/{fileId}/share` z `sharedWithUserIds: [B]` |
| 5 | Zaloguj się jako Użytkownik B | W zakładce **Shared** widoczny udostępniony plik |
| 6 | Właściciel: cofnij dostęp (odznacz B i zapisz) | Plik znika z zakładki Shared u Użytkownika B |

### Oczekiwany wynik końcowy
Udostępnianie działa dwukierunkowo — dodanie i cofnięcie dostępu. Powiadomienie wysłane do Użytkownika B przy udostępnieniu.

---

## TC-FILE-009 — Próba usunięcia pliku przez Shared User (brak uprawnień)

**Moduł:** Pliki → Usuwanie  
**Typ:** Negatywny  
**Priorytet:** Wysoki  
**Rola:** Shared User

### Warunki wstępne
- Plik udostępniony Użytkownikowi B
- Użytkownik B nie jest ani właścicielem, ani adminem projektu

### Kroki

| # | Akcja | Oczekiwany wynik |
|---|-------|-----------------|
| 1 | Zaloguj się jako Użytkownik B | Widoczna zakładka Shared z udostępnionym plikiem |
| 2 | Sprawdź dostępne akcje przy pliku | Brak przycisków „Usuń" i „Udostępnij" w UI |
| 3 | Spróbuj wysłać `DELETE /file/{fileId}` bezpośrednio (np. przez narzędzie deweloperskie) | API zwraca `403 Forbidden` |

### Oczekiwany wynik końcowy
UI nie eksponuje niedozwolonych akcji. Backend dodatkowo zwraca `403 Forbidden` przy próbie ominięcia UI.

---

## TC-FILE-010 — Widoczność plików według roli (zakresy All / Mine / Shared)

**Moduł:** Pliki → Lista plików  
**Typ:** Pozytywny + Brzegowy  
**Priorytet:** Wysoki  
**Role:** Tenant Admin, Owner, Member (bez dostępu)

### Warunki wstępne
- Projekt zawiera:
  - Plik A (właściciel: Użytkownik X, udostępniony: Użytkownik Y)
  - Plik B (właściciel: Użytkownik Y, nieudostępniony)
- Zalogowani kolejno: Tenant Admin, Użytkownik X, Użytkownik Y, Użytkownik Z (bez żadnego pliku)

### Kroki i oczekiwane wyniki

| # | Rola | Zakładka | Widoczne pliki |
|---|------|----------|----------------|
| 1 | Tenant Admin | **All** | Plik A + Plik B (wszystkie w projekcie) |
| 2 | Tenant Admin | **Mine** | Tylko pliki, których adminisrt. jest właścicielem |
| 3 | Użytkownik X (Owner Pliku A) | **Mine** | Plik A |
| 4 | Użytkownik X | **Shared** | Brak (X nie ma udostępnionych jemu plików) |
| 5 | Użytkownik Y (Shared Pliku A, Owner Pliku B) | **Mine** | Plik B |
| 6 | Użytkownik Y | **Shared** | Plik A |
| 7 | Użytkownik Z | **Mine** | Pusta lista |
| 8 | Użytkownik Z | **Shared** | Pusta lista |

### Oczekiwany wynik końcowy
Zakresy `All`, `Mine`, `Shared` poprawnie filtrują widoczność. Użytkownik nie widzi plików, do których nie ma dostępu. Tenant Admin zawsze widzi zakres `All`.

---

## Podsumowanie

| TC | Obszar | Typ | Priorytet |
|----|--------|-----|-----------|
| TC-FILE-001 | Upload pliku (happy path) | Pozytywny | Wysoki |
| TC-FILE-002 | Niedozwolony typ pliku | Negatywny | Wysoki |
| TC-FILE-003 | Limit 10 MB | Brzegowy | Wysoki |
| TC-FILE-004 | Nowa wersja — właściciel | Pozytywny | Wysoki |
| TC-FILE-005 | Nowa wersja — Shared User | Pozytywny | Średni |
| TC-FILE-006 | Dodanie komentarza | Pozytywny | Średni |
| TC-FILE-007 | Edycja/usunięcie komentarza | Pozytywny + Negatywny | Średni |
| TC-FILE-008 | Udostępnienie pliku | Pozytywny | Wysoki |
| TC-FILE-009 | Usunięcie przez Shared User | Negatywny | Wysoki |
| TC-FILE-010 | Widoczność wg roli (scope) | Brzegowy | Wysoki |
