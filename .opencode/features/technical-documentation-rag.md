# Feature: Dokumentacja techniczna (RAG / ekstrakcja z PDF/JPG)

## Kontekst

System umożliwia przetwarzanie dokumentacji technicznej projektów budowlanych (pliki PDF i JPG) przy użyciu modelu wizyjnego GPT-4o. Model wyciąga z rysunków technicznych ustrukturyzowane dane (wymiary, materiały, instalacje, podział na pomieszczenia), zapisuje je w bazie jako obiekt JSON powiązany z projektem, a użytkownik może przeglądać i zarządzać dokumentacją z poziomu interfejsu projektowego. Przetwarzanie plików odbywa się asynchronicznie jako background service z powiadomieniami przez SignalR.

---

## Uprawnienia

Moduł **Dokumentacja techniczna** podlega temu samemu mechanizmowi uprawnień co pozostałe moduły projektowe:

- Administrator projektu ma dostęp do modułu domyślnie, bez konieczności ręcznej konfiguracji.
- Pozostałe role (np. członek projektu, obserwator) wymagają jawnego nadania uprawnienia przez administratora.
- Uprawnienia są granularne: **odczyt** (przeglądanie listy i szczegółów) oraz **zapis** (dodawanie nowej dokumentacji).
- Brak uprawnienia do modułu powoduje ukrycie kafelka **Dokumentacja techniczna** w widoku projektu.

---

## API

### Scenariusz przetwarzania dokumentacji

#### 1. Przyjęcie plików i natychmiastowa odpowiedź

- Użytkownik przesyła jeden lub wiele plików — PDF lub JPG.
- Maksymalny rozmiar pojedynczego pliku: **50 MB**.
- Dozwolone formaty: `application/pdf`, `image/jpeg`.
- API przyjmuje żądanie, tworzy rekord `ProjectTechnicalDocumentation` ze statusem `Pending` i natychmiast zwraca odpowiedź `202 Accepted` z identyfikatorem dokumentacji.
- Przetwarzanie odbywa się asynchronicznie jako **background service** — użytkownik nie czeka na wynik w ramach tego samego żądania HTTP.

#### 2. Background service — pipeline przetwarzania

Background service odbiera zadanie z kolejki i wykonuje kolejne kroki poprzez architekturę agentową:

**Krok 1 — Konwersja PDF → JPG**
- Pliki PDF są renderowane strona po stronie do osobnych plików JPG.
- Pliki JPG przechodzą bezpośrednio bez konwersji.
- Status dokumentacji aktualizowany do `Processing`.

**Krok 2 — Ekstrakcja danych przez subagentów (GPT-4o)**
- Agent orkiestrator rozdziela obrazy pomiędzy subagentów ekstrakcji.
- Każdy subagent przetwarza przydzielony obraz i zwraca częściowy wynik do orkiestratora.
- Subagenci wyciągają z rysunków: metadane dokumentu, podział na kondygnacje i pomieszczenia, wymiary pomieszczeń i ścian, otwory okienne i drzwiowe, dach i więźbę, izolację, instalacje.

**Krok 3 — Budowa zbiorczego modelu JSON**
- Dedykowany subagent agregacji odbiera wyniki wszystkich subagentów ekstrakcji.
- Scala dane w jeden obiekt `ProjectTechnicalDocumentationDetails`, oblicza sumaryczne zestawienia materiałów i powierzchni.

**Krok 4 — Zapis do bazy danych**
- Rekord `ProjectTechnicalDocumentation` jest aktualizowany: pole `Details` uzupełniane JSON-em, status zmieniany na `Completed`.
- W przypadku błędu na dowolnym etapie status ustawiany jest na `Failed` z zapisem przyczyny błędu.

**Krok 5 — Powiadomienie przez SignalR**
- Po zakończeniu przetwarzania (sukces lub błąd) background service wysyła powiadomienie do użytkownika przez **SignalR**.
- Powiadomienie zawiera: identyfikator dokumentacji, końcowy status (`Completed` / `Failed`), nazwę dokumentacji.
- UI po odebraniu powiadomienia odświeża listę dokumentacji i wyświetla stosowny komunikat (toast / alert).

#### 3. Encja `ProjectTechnicalDocumentation`

| Pole | Typ | Opis |
|------|-----|------|
| `Id` | `Guid` | Identyfikator dokumentacji |
| `ProjectId` | `Guid` | Powiązanie z projektem |
| `Name` | `string` | Nazwa dokumentacji podana przez użytkownika |
| `Description` | `string` | Opis podany przez użytkownika |
| `Status` | `enum` | `Pending` / `Processing` / `Completed` / `Failed` |
| `ErrorMessage` | `string?` | Przyczyna błędu (wypełniane tylko przy `Failed`) |
| `Details` | `jsonb` / `nvarchar(max)` | Zserializowany `ProjectTechnicalDocumentationDetails` (null do czasu zakończenia) |
| `CreatedAt` | `DateTime` | Data utworzenia |
| `CompletedAt` | `DateTime?` | Data zakończenia przetwarzania |
| `Files` | kolekcja | Powiązane pliki źródłowe (PDF / JPG) |

---

## UI

### Widok projektu — nowy kafelek

- Na stronie projektu, obok istniejącego kafelka **Pliki**, dodawany jest nowy kafelek **Dokumentacja techniczna**.
- Kafelek wyświetla liczbę istniejących dokumentacji dla danego projektu.
- Kafelek jest widoczny wyłącznie dla użytkowników z uprawnieniem do modułu.
- Kliknięcie otwiera widok listy dokumentacji.

### Lista dokumentacji technicznej

- Wyświetla wszystkie dokumentacje powiązane z projektem.
- Każdy element listy zawiera: nazwę, opis, datę utworzenia, liczbę plików oraz aktualny status przetwarzania.
- Dokumentacje w trakcie przetwarzania wyświetlają wskaźnik postępu (`Pending` / `Processing`).
- Przycisk **Dodaj dokumentację** widoczny wyłącznie dla użytkowników z uprawnieniem zapisu.

### Dodawanie dokumentacji — formularz

| Pole | Typ | Walidacja |
|------|-----|-----------|
| Nazwa | tekst | wymagane |
| Opis | tekst wieloliniowy | opcjonalne |
| Pliki | upload (PDF / JPG) | wymagane, maks. 50 MB/plik, dozwolone: pdf, jpg |

- Po zatwierdzeniu formularz wysyła żądanie i natychmiast wyświetla dokumentację na liście ze statusem `Pending`.
- UI nasłuchuje powiadomień SignalR — po odebraniu zdarzenia zakończenia odświeża rekord na liście i wyświetla komunikat o sukcesie lub błędzie.

### Szczegóły dokumentacji

- Po kliknięciu w dokumentację użytkownik widzi:
  - nazwę, opis i aktualny status,
  - listę powiązanych plików z możliwością podglądu,
  - pełny opis projektu wygenerowany z obiektu JSON — czytelnie sformatowany widok zawierający kondygnacje, pomieszczenia, wymiary, materiały, instalacje.
- Jeśli status to `Pending` lub `Processing`, zamiast szczegółów wyświetlany jest komunikat o trwającym przetwarzaniu.
- Jeśli status to `Failed`, wyświetlany jest komunikat o błędzie z możliwością ponowienia przetwarzania.

---

## Model danych

### Klasa C# — `ProjectTechnicalDocumentationDetails`

```csharp
public class ProjectTechnicalDocumentationDetails
{
    public ProjectInfo Project { get; set; } = new();
    public List<Drawing> Drawings { get; set; } = new();
    public RoofDetails? Roof { get; set; }
    public List<InstallationInfo> Installations { get; set; } = new();
    public List<MaterialSummary> MaterialsSummary { get; set; } = new();
    public double TotalAreaM2 { get; set; }
}

public class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Author { get; set; }
    public string? Client { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class Drawing
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DrawingType { get; set; } = string.Empty;
    public string? Scale { get; set; }
    public DrawingSource Source { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
    public List<StockItem>? JoinerySchedule { get; set; }
}

public class DrawingSource
{
    public string FileName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
}

public class Room
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public Dimensions Dimensions { get; set; } = new();
    public List<Wall> Walls { get; set; } = new();
    public List<Opening> Openings { get; set; } = new();
    public InsulationInfo? Insulation { get; set; }
    public Finishing? Finishing { get; set; }
}

public class Dimensions
{
    public double WidthM { get; set; }
    public double LengthM { get; set; }
    public double HeightM { get; set; }
    public double AreaM2 { get; set; }
}

public class Wall
{
    public string Type { get; set; } = string.Empty;
    public double ThicknessCm { get; set; }
    public string Material { get; set; } = string.Empty;
    public double LengthM { get; set; }
}

public class Opening
{
    public string Type { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public double WidthCm { get; set; }
    public double HeightCm { get; set; }
    public int Count { get; set; }
    public string? Material { get; set; }
}

public class InsulationInfo
{
    public string Type { get; set; } = string.Empty;
    public double ThicknessCm { get; set; }
    public string? Location { get; set; }
}

public class Finishing
{
    public string? Floor { get; set; }
    public string? Walls { get; set; }
    public string? Ceiling { get; set; }
}

public class RoofDetails
{
    public string Type { get; set; } = string.Empty;
    public double PitchDegrees { get; set; }
    public double RidgeHeightM { get; set; }
    public double SpanM { get; set; }
    public string TrussType { get; set; } = string.Empty;
    public string CoveringMaterial { get; set; } = string.Empty;
    public InsulationInfo? Insulation { get; set; }
}

public class InstallationInfo
{
    public string Type { get; set; } = string.Empty;
    public bool IsPresent { get; set; }
    public string? Notes { get; set; }
}

public class StockItem
{
    public string Symbol { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double WidthCm { get; set; }
    public double HeightCm { get; set; }
    public int Count { get; set; }
    public string? Material { get; set; }
}

public class MaterialSummary
{
    public string Type { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
}
```

---

## Otwarte kwestie do ustalenia

- Zakres danych wyciąganych przez model wizyjny (co jest wymagane, co opcjonalne).
- Czy model JSON wymaga wersjonowania (wiele rewizji tej samej dokumentacji).
- Czy dokumentacja techniczna ma wspierać RAG / wyszukiwanie semantyczne (integracja z Azure AI Search).
- Obsługa błędów przetwarzania — polityka ponowień (retry), czy użytkownik może ręcznie wyzwolić ponowne przetwarzanie.
- Technologia kolejkowania zadań dla background service (np. Azure Service Bus, Hangfire, IHostedService z kanałem in-memory).

---

## Architektura agentowa

### Przegląd

```
Background Service
       │
       ▼
 Orkiestrator
 ├── Subagent: Klasyfikacja rysunku     (każdy obraz)
 ├── Subagent: Ekstrakcja danych        (każdy obraz, równolegle)
 ├── Subagent: Ekstrakcja instalacji    (każdy obraz, równolegle)
 └── Subagent: Agregacja i JSON         (raz, po zebraniu wszystkich wyników)
```

### Agenci

1. **DocumentationOrchestratorAgent** — koordynacja pipeline'u
2. **DrawingClassificationAgent** — typ rysunku, skala, zakres danych
3. **ArchitecturalExtractionAgent** — pomieszczenia, ściany, otwory, dach
4. **InstallationsExtractionAgent** — instalacje branżowe
5. **AggregationAgent** — scalenie w `ProjectTechnicalDocumentationDetails`

Szczegóły promptów systemowych i schematów wejścia/wyjścia — w wymaganiach biznesowych (pełna specyfikacja agentów).

---

## Powiązane feature'y w repo

- `.opencode/features/project-module-permissions.md` — wzorzec uprawnień modułowych
- `.opencode/features/file-directories.md` — wzorzec uploadu plików projektowych
- `.opencode/features/ai-cost-document-import.md` — wzorzec importu dokumentów przez AI
