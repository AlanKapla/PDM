# Typy zależności i działanie LagDays

Dokumentacja dla `WorkDependencyType` i pola `LagDays` encji `WorkScheduleStageWorkDependency`.

---

## Zasada ogólna

```
Successor.AnchorDate = Predecessor.AnchorDate + LagDays
```

| Wartość LagDays | Znaczenie |
|----------------:|:----------|
| `> 0` | Opóźnienie — następnik czeka N dni po kotw. poprzednika |
| `= 0` | Bez przerwy — następnik zaczyna/kończy natychmiast po kotwicy |
| `< 0` | Wyprzedzenie (lead) — następnik zaczyna się przed kotwicą poprzednika |

---

## Założenia bazowe dla wszystkich przykładów

| | Data |
|:-|:----:|
| **Poprzednik Start** | 01.05 |
| **Poprzednik Koniec** | 10.05 |

---

## FinishToStart (FS) — domyślny, najczęstszy

> Następnik **zaczyna się** po zakończeniu poprzednika.

**Kotwica:** `Predecessor.End`  
**Wzór:** `Successor.Start = Predecessor.End + LagDays`

| LagDays | Successor.Start | Przykład zastosowania |
|--------:|:---------------:|:----------------------|
| `0` | **10.05** | Montaż zaczyna się gdy dostawa się kończy |
| `+3` | **13.05** | 3 dni schnięcia betonu przed kolejnym etapem |
| `+7` | **17.05** | Tydzień przerwy technologicznej |
| `−2` | **08.05** | Przygotowanie placu zaczyna się 2 dni przed końcem dostawy |

---

## StartToStart (SS)

> Następnik **zaczyna się** po rozpoczęciu poprzednika.

**Kotwica:** `Predecessor.Start`  
**Wzór:** `Successor.Start = Predecessor.Start + LagDays`

| LagDays | Successor.Start | Przykład zastosowania |
|--------:|:---------------:|:----------------------|
| `0` | **01.05** | Dwa zespoły zaczynają równocześnie |
| `+2` | **03.05** | Drugi zespół wchodzi 2 dni po pierwszym |
| `+5` | **06.05** | Malowanie zaczyna 5 dni po tynkowaniu |
| `−1` | **30.04** | Następnik musi ruszyć dzień przed poprzednikiem |

---

## FinishToFinish (FF)

> Następnik **kończy się** po zakończeniu poprzednika.

**Kotwica:** `Predecessor.End`  
**Wzór:** `Successor.End = Predecessor.End + LagDays`

| LagDays | Successor.End | Przykład zastosowania |
|--------:|:-------------:|:----------------------|
| `0` | **10.05** | Oba zadania kończą się tego samego dnia |
| `+3` | **13.05** | Dokumentacja musi być gotowa 3 dni po zakończeniu robót |
| `+7` | **17.05** | Odbiór techniczny tydzień po zakończeniu prac |
| `−2` | **08.05** | Następnik musi zakończyć 2 dni przed poprzednikiem |

---

## StartToFinish (SF) — rzadki

> Następnik **kończy się** po rozpoczęciu poprzednika.

**Kotwica:** `Predecessor.Start`  
**Wzór:** `Successor.End = Predecessor.Start + LagDays`

| LagDays | Successor.End | Przykład zastosowania |
|--------:|:-------------:|:----------------------|
| `0` | **01.05** | Następnik kończy się dokładnie gdy poprzednik startuje |
| `+5` | **06.05** | Stare zadanie kończy się 5 dni po uruchomieniu nowego |
| `−3` | **28.04** | Następnik musi zakończyć 3 dni przed startem poprzednika |

> **Uwaga:** SF jest używany niemal wyłącznie w harmonogramach „just-in-time", gdzie nowe zadanie zastępuje stare. W praktyce budowlanej i produkcyjnej FS pokrywa >90% przypadków.

---

## Zestawienie wszystkich typów

| Typ | Kotwica poprzednika | Wpływa na | Wzór |
|:----|:-------------------:|:---------:|:-----|
| `FinishToStart` | `End` | `Successor.Start` | `S.Start = P.End + Lag` |
| `StartToStart` | `Start` | `Successor.Start` | `S.Start = P.Start + Lag` |
| `FinishToFinish` | `End` | `Successor.End` | `S.End = P.End + Lag` |
| `StartToFinish` | `Start` | `Successor.End` | `S.End = P.Start + Lag` |

---

## Wartości graniczne LagDays

Zdefiniowane w walidatorze `WorkScheduleWorkDependencyDtoValidator`:

| Ograniczenie | Wartość |
|:-------------|:-------:|
| Minimum | `-365` |
| Maksimum | `+365` |
| Domyślna | `0` |

---

## Model encji

```csharp
public class WorkScheduleStageWorkDependency : BaseEntity
{
    public Guid TenantId       { get; set; }
    public Guid ProjectId      { get; set; }
    public Guid WorkScheduleId { get; set; }

    public Guid PredecessorWorkId { get; set; }
    public Guid SuccessorWorkId   { get; set; }

    /// <summary>Typ powiązania — określa które daty są kotwicą.</summary>
    public WorkDependencyType DependencyType { get; set; } = WorkDependencyType.FinishToStart;

    /// <summary>
    /// Opóźnienie (wartość dodatnia) lub wyprzedzenie (wartość ujemna) w dniach
    /// stosowane między kotwicą poprzednika a kotwicą następnika.
    /// </summary>
    public int LagDays { get; set; } = 0;
}
```

```csharp
public enum WorkDependencyType
{
    FinishToStart  = 0,   // domyślny
    StartToStart   = 1,
    FinishToFinish = 2,
    StartToFinish  = 3
}
```
