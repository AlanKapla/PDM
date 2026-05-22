# Feature: Ujednolicenie modala dodawania kosztów i wydatków

## Cel

Ujednolicić wygląd, strukturę i zachowanie modali służących do
dodawania i edycji kosztów (TrackedCost) oraz wydatków (ProjectCost).

Obie encje dziedziczą po wspólnej klasie bazowej `BaseCost`
co oznacza że mają wspólne pola — UI powinno to odzwierciedlać.

## Kontekst domenowy

```csharp
// Klasa bazowa — pola wspólne dla obu typów kosztów
public abstract class BaseCost : DeletableEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public decimal? Net { get; set; }
    public decimal? Gross { get; set; }
    public string? Contractor { get; set; }
    public DateTime? Date { get; set; }
}

// Koszt śledzony — powiązany z trackerem projektu
public class TrackedCost : BaseCost
{
    public Guid TrackerId { get; set; }
    public Guid? CostEstimateItemId { get; set; }      // opcjonalne powiązanie z kosztorysem
    public Guid? WorkScheduleStageWorkId { get; set; }  // opcjonalne powiązanie z harmonogramem
}

// Wydatek projektowy — dodawany przez członków projektu
public class ProjectCost : BaseCost
{
    public Guid UserId { get; set; }
    public bool IsAccepted { get; set; }
}
```

## Zadanie dla agentów

### Audyt API
Zbadaj istniejące endpointy i web modele (DTO) dla TrackedCost i ProjectCost:
- Czy struktury request/response są spójne dla wspólnych pól?
- Czy Commands mają tę samą strukturę dla pól z BaseCost?
- Czy walidatory są spójne?
- Co trzeba ujednolicić po stronie API żeby UI mogło używać wspólnej logiki?

### Audyt UI
Znajdź przez #codebase wszystkie komponenty związane z dodawaniem
i edycją TrackedCost i ProjectCost:
- Gdzie są obecne modale/formularze?
- Jak wyglądają i jak działają?
- Co jest zduplikowane między nimi?
- Jakie pola są wspólne a jakie specyficzne?
- Czy jest już jakaś współdzielona logika?

### Na podstawie audytu zaproponuj
1. Czy lepszy jest jeden modal z trybem (type: tracked | project)
   czy dwa modale dziedziczące ze wspólnego komponentu?
2. Jak obsłużyć pola specyficzne (CostEstimateItemId, WorkScheduleStageWorkId)?
3. Jaki jest minimalny zakres zmian API jeśli w ogóle są potrzebne?

## Kryteria akceptacji

- Oba modale wyglądają i działają identycznie dla wspólnych pól
- Kod wspólnych pól jest zaimplementowany w jednym miejscu
- Pola specyficzne są obsługiwane czysto (bez if/else w środku komponentu bazowego)
- TypeScript kompiluje bez błędów (`tsc --noEmit`)
- Build API bez błędów
- Istniejąca funkcjonalność nie jest naruszona

## Ograniczenia

- Nie zmieniamy kontraktu API (route, HTTP method) — tylko jeśli niezbędne
- Nie migrujemy danych w DB
- Zachowujemy istniejące uprawnienia (kto może dodawać TrackedCost vs ProjectCost)
