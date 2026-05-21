# Feature: Ujednolicenie modala dodawania kosztów

## Opis

Ujednolicić wygląd i zachowanie modala dodawania kosztów (TrackedCost)
i wydatków (ProjectCost) — obie encje mają wspólną klasę bazową `BaseCost`.

## Problem

Obecnie istnieją dwa osobne modale:
- Modal dodawania TrackedCost (koszt śledzony w trackerze)
- Modal dodawania ProjectCost (wydatek projektowy)

Wyglądają i działają podobnie ale są zaimplementowane osobno,
co powoduje duplikację kodu i niespójny UX.

## Cel

Stworzyć jeden wspólny komponent bazowy dla obu modali
lub ujednolicić ich wygląd i zachowanie tak żeby były spójne.

## Wymagania

### UI
- Oba modale powinny mieć identyczny layout i wygląd
- Pola wspólne (Name, Description, Net, Gross, Date, Contractor)
  powinny być zaimplementowane raz jako shared component
- Pola specyficzne (np. CostEstimateItemId dla TrackedCost)
  powinny być dodawane przez extension/slot

### API
- Sprawdzić czy endpointy Create/Update dla obu encji
  mają spójną strukturę request/response
- Sprawdzić czy web modele (DTO) są spójne

## Klasa bazowa domenowa

```csharp
// BaseCost jest klasą bazową dla TrackedCost i ProjectCost
public abstract class BaseCost : DeletableEntity
{
    public Guid TrackerId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public decimal? Net { get; set; }
    public decimal? Gross { get; set; }
    public string? Contractor { get; set; }
    public DateTime? Date { get; set; }
}
```

## Kryteria akceptacji

1. Oba modale wyglądają identycznie dla wspólnych pól
2. Kod wspólnych pól jest w jednym miejscu (nie duplikowany)
3. TypeScript kompiluje bez błędów
4. Build API bez błędów
5. Istniejące testy (jeśli są) przechodzą

## Pytania do rozstrzygnięcia przez użytkownika

1. Czy preferujesz jeden modal z trybem (TrackedCost/ProjectCost)
   czy dwa osobne modale dziedziczące ze wspólnego komponentu?
2. Czy pola specyficzne (CostEstimateItemId) mają być widoczne zawsze
   czy tylko gdy kosztorys jest podłączony?
