# Prompt API-01: Web Models / DTOs dla generowania kosztorysu z AI

## Cel
Utwórz Web Models (DTO) potrzebne do komunikacji API w feature "Generuj kosztorys z AI".
Wszystkie nowe pliki trafiają do `src/Business/Interfaces/WebModels/AI/`.

---

## Pliki do utworzenia

### 1. `src/Business/Interfaces/WebModels/AI/AICostEstimateRequestWeb.cs`

Reprezentuje pytania wypełnione przez użytkownika w modalu.

```csharp
namespace Business.Interfaces.WebModels.AI
{
    /// <summary>
    /// Dane wejściowe od użytkownika do generowania kosztorysu przez AI.
    /// Wszystkie pola opcjonalne poza TemplateId i InvestmentType.
    /// </summary>
    public sealed record AICostEstimateRequestWeb
    {
        /// <summary>ID szablonu wybranego przez użytkownika.</summary>
        public Guid TemplateId { get; init; }

        /// <summary>Co budujesz? (wolny tekst, np. "dom jednorodzinny 150m²", "remont mieszkania")</summary>
        public string InvestmentType { get; init; } = string.Empty;

        /// <summary>Stan wykończenia: "surowy_otwarty", "surowy_zamkniety", "deweloperski", "pod_klucz"</summary>
        public string? FinishingStandard { get; init; }

        /// <summary>Szacowany budżet brutto w PLN.</summary>
        public decimal? Budget { get; init; }

        /// <summary>Powierzchnia/zakres inwestycji (np. 150 m², 500 mb)</summary>
        public decimal? Area { get; init; }

        /// <summary>Jednostka powierzchni (np. "m²", "mb", "szt")</summary>
        public string? AreaUnit { get; init; }

        /// <summary>Lokalizacja inwestycji — wpływa na koszty robocizny.</summary>
        public string? Location { get; init; }

        /// <summary>Orientacyjny rok ukończenia inwestycji.</summary>
        public int? CompletionYear { get; init; }

        /// <summary>Dodatkowe wymagania (np. "ogrzewanie podłogowe, fotowoltaika 10kW, winda")</summary>
        public string? AdditionalRequirements { get; init; }
    }
}
```

---

### 2. `src/Business/Interfaces/WebModels/AI/AICostEstimatePreviewWeb.cs`

Preview kosztorysu wygenerowanego przez AI — NIE jest zapisywany w bazie danych.
Klient UI wyświetla podgląd i decyduje czy zatwierdzić.

```csharp
namespace Business.Interfaces.WebModels.AI
{
    /// <summary>
    /// Podgląd kosztorysu wygenerowanego przez AI.
    /// Wyłącznie w pamięci — nie jest zapisywany do bazy danych.
    /// Użytkownik zatwierdza ten podgląd, po czym wysyła go do CreateCostEstimateFromAIPreview.
    /// </summary>
    public sealed record AICostEstimatePreviewWeb
    {
        /// <summary>ID szablonu (powtórzony dla walidacji po stronie klienta).</summary>
        public Guid TemplateId { get; init; }

        /// <summary>Sugerowana nazwa kosztorysu.</summary>
        public string SuggestedName { get; init; } = string.Empty;

        /// <summary>Sugerowany opis kosztorysu.</summary>
        public string? SuggestedDescription { get; init; }

        /// <summary>Grupy kosztorysu z zagnieżdżonymi pozycjami.</summary>
        public List<AIGroupPreviewWeb> Groups { get; init; } = [];

        /// <summary>Ostrzeżenia wygenerowane przez AI (np. pole pominięte bo brak danych).</summary>
        public List<string> Warnings { get; init; } = [];
    }

    public sealed record AIGroupPreviewWeb
    {
        /// <summary>Tymczasowe ID (np. "g1", "g2") — do mapowania relacji parent/child po stronie klienta.</summary>
        public string TempId { get; init; } = string.Empty;

        /// <summary>TempId grupy nadrzędnej lub null dla grup root.</summary>
        public string? ParentTempId { get; init; }

        /// <summary>Nazwa grupy.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Kolejność w ramach poziomu.</summary>
        public int Order { get; init; }

        /// <summary>Wartości pól grupy (GroupHeaderFields z szablonu).</summary>
        public List<AIFieldValueWeb> FieldValues { get; init; } = [];

        /// <summary>Pozycje kosztorysowe w tej grupie.</summary>
        public List<AIItemPreviewWeb> Items { get; init; } = [];
    }

    public sealed record AIItemPreviewWeb
    {
        /// <summary>Tymczasowe ID pozycji.</summary>
        public string TempId { get; init; } = string.Empty;

        /// <summary>Nazwa pozycji.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Kolejność w grupie.</summary>
        public int Order { get; init; }

        /// <summary>Wartości pól pozycji (SystemFields + CalculatedFields + GenericFields z szablonu).</summary>
        public List<AIFieldValueWeb> FieldValues { get; init; } = [];
    }

    /// <summary>
    /// Wartość pojedynczego pola wygenerowana przez AI.
    /// Tylko jedno z pól wartości powinno być wypełnione — zgodnie z FieldType.
    /// </summary>
    public sealed record AIFieldValueWeb
    {
        /// <summary>ID definicji pola z szablonu (CostEstimateTemplateFieldDefinitionBase.Id).</summary>
        public Guid FieldDefinitionId { get; init; }

        public decimal? DecimalValue { get; init; }
        public string? StringValue { get; init; }
        public bool? BoolValue { get; init; }
        public DateTime? DateTimeValue { get; init; }
    }
}
```

---

### 3. `src/Business/Interfaces/WebModels/AI/CreateCostEstimateFromAIPreviewWeb.cs`

Request do zapisu zatwierdzonego podglądu.

```csharp
namespace Business.Interfaces.WebModels.AI
{
    /// <summary>
    /// Żądanie zapisu kosztorysu zatwierdzonego przez użytkownika.
    /// Zawiera preview z AI + ostateczna nazwa/opis edytowane przez użytkownika.
    /// </summary>
    public sealed record CreateCostEstimateFromAIPreviewWeb
    {
        /// <summary>Ostateczna nazwa kosztorysu (użytkownik mógł ją zmodyfikować).</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Ostateczny opis kosztorysu (opcjonalny).</summary>
        public string? Description { get; init; }

        /// <summary>Preview zatwierdzony przez użytkownika (niezmieniony lub po edycji w UI).</summary>
        public AICostEstimatePreviewWeb Preview { get; init; } = default!;
    }
}
```

---

## Konwencje
- `record` zamiast `class` dla DTO
- `sealed` dla wszystkich rekordów
- Namespace: `Business.Interfaces.WebModels.AI`
- Brak adnotacji Data Annotations — walidacja przez FluentValidation w handlerze
- Żadnych referencji do encji EF Core w tym projekcie

## Weryfikacja
Po implementacji uruchom:
```
dotnet build src/Business/Business.csproj
```
Oczekiwany wynik: Build succeeded, 0 errors.
