# Pole kalkulowane: Discount (Rabat) — dokumentacja dla UI

---

## 📋 Podsumowanie

Nowe pole kalkulowane `ItemCalculatedDiscount` (enum value `207`) umożliwia zastosowanie **rabatu procentowego** na pozycji kosztorysu. Rabat wpływa na obliczone wartości netto, brutto i VAT.

---

## ⚙️ Konfiguracja pola

| Właściwość | Wartość |
|---|---|
| **FieldType** | `ItemCalculatedDiscount` (`207`) |
| **FieldScope** | `ItemCalculated` |
| **Typ wartości** | `decimal` (ułamek: 0 = 0%, 1 = 100%) |
| **Zakres wartości** | `0` – `1` (0 = brak rabatu, 1 = pełny rabat) |
| **Wymagane** | Nie — pole opcjonalne w szablonie |
| **Domyślna wartość** | `null` lub `0` (brak rabatu) |

---

## 🧮 Wpływ na obliczenia

Rabat jest stosowany **po** obliczeniu bazowych wartości pozycji (netto, VAT, brutto) i **przed** zapisaniem wyników.

### Formuły

```
discountMultiplier = 1 - Discount

ValueNet       = ValueNet(bazowe) × discountMultiplier
TotalVat       = TotalVat(bazowe) × discountMultiplier
ValueGross     = ValueNet(po rabacie) + TotalVat(po rabacie)
```

### Przykład

| Pole | Przed rabatem | Rabat 0.1 (10%) | Po rabacie |
|---|---|---|---|
| UnitPriceNet | 100.00 | — | 100.00 (bez zmian) |
| Quantity | 5 | — | 5 (bez zmian) |
| VatRate | 23% | — | 23% (bez zmian) |
| **Discount** | — | **0.1** | — |
| ValueNet | 500.00 | × 0.90 | **450.00** |
| TotalVat | 115.00 | × 0.90 | **103.50** |
| ValueGross | 615.00 | przeliczone | **553.50** |

### Warunki zastosowania

- Rabat jest stosowany **tylko gdy** pole `ItemCalculatedDiscount` jest zdefiniowane w szablonie **i** ma wartość różną od `0`
- Jeśli pole nie istnieje w szablonie lub wartość = `null` / `0` — obliczenia przebiegają bez zmian
- Backend waliduje że wartość mieści się w zakresie `0–1`. Wartości poza zakresem powodują błąd walidacji
- VatRate stosuje tę samą konwencję `0–1` (np. `0.23` = 23%)
- Rabat wpływa na wartości **tylko jeśli** zostały wcześniej obliczone (np. jeśli `ValueNet` = `null` z powodu braku danych wejściowych, rabat nie jest stosowany)
- Pola jednostkowe (`UnitPriceNet`, `UnitPriceGross`, `UnitVat`) **nie są** modyfikowane przez rabat — rabat działa na wartości łączne

---

## 🖥️ Wskazówki dla UI

### Wyświetlanie pola

- Pole powinno być wyświetlane jako **kolumna numeryczna z sufiksem `%`** (wartość × 100 do wyświetlenia)
- Input: pole numeryczne z walidacją zakresu `0–1` (UI może przyjmować procenty 0–100 i dzielić przez 100 przed wysłaniem)
- Formatowanie: wartość przechowywana jako ułamek (`0.1`), wyświetlana jako procent (`10%`)
- Placeholder: `0%` lub `Brak rabatu`

### Pozycja w tabeli

- Kolumna `Discount` powinna znajdować się **przed** kolumnami wartości (`ValueNet`, `ValueGross`, `TotalVat`)
- Sugerowana kolejność kolumn: `Name → Quantity → Unit → UnitPriceNet → VatRate → Discount → ValueNet → TotalVat → ValueGross`

### Zachowanie w edycji

```
1. Użytkownik wpisuje wartość rabatu (np. 10)
2. UI wysyła PATCH/PUT z zaktualizowanym polem Discount
3. Backend przelicza wartości pozycji z rabatem
4. Backend przelicza sumy grupy i totale kosztorysu
5. Response zawiera zaktualizowane wartości (ValueNet, TotalVat, ValueGross z rabatem)
6. UI odświeża wiersz pozycji + sumy
```

### Walidacja po stronie UI

| Reguła | Komunikat |
|---|---|
| Wartość < 0 | "Rabat nie może być ujemny" |
| Wartość > 1 | "Rabat nie może przekraczać 1 (100%)" |
| Nie-liczbowa | "Rabat musi być liczbą" |

### Wizualne wskazówki

- Jeśli `Discount > 0` — można wyświetlić przekreśloną wartość bazową obok wartości po rabacie
- Przykład: ~~500.00~~ → **450.00 zł** (rabat 10%, wartość `0.1`)
- Opcjonalnie: kolorowe oznaczenie komórki rabatu (np. zielone tło gdy > 0)

---

## 🏗️ Konfiguracja w szablonie

Pole `ItemCalculatedDiscount` dodaje się do szablonu jak każde inne pole kalkulowane:

```json
{
  "fieldType": 207,
  "fieldScope": 2,
  "fieldName": "<generated-guid>",
  "label": "Rabat",
  "isRequired": false,
  "isReadOnly": false,
  "sumInGroup": false,
  "sumInTotal": false,
  "sortOrder": 6,
  "defaultDecimalValue": 0,
  "minValue": 0,
  "maxValue": 1,
  "childFields": []
}
```

| Właściwość | Uwagi |
|---|---|
| `sumInGroup` | Zazwyczaj `false` — sumowanie rabatów procentowych nie ma sensu |
| `sumInTotal` | Zazwyczaj `false` — j.w. |
| `isReadOnly` | `false` — użytkownik wprowadza wartość ręcznie |
| `isRequired` | `false` — brak rabatu = pełna cena |

---

## 🔗 Powiązanie z komponentami

Jeśli pozycja ma **komponenty** (`RelationType.Component`), wartości pozycji są sumowane z komponentów. W tym przypadku:
- Rabat stosowany jest **na poziomie każdego komponentu osobno**
- Pozycja nadrzędna sumuje już **zrabatowane** wartości komponentów
- Pole Discount na pozycji nadrzędnej z komponentami **nie jest stosowane** (wartości brane z komponentów)

---

## 📊 Wpływ na sumy

| Poziom | Zachowanie |
|---|---|
| **Pozycja** | Rabat pomniejsza `NetValue`, `GrossValue`, `VatValue` |
| **Grupa** | Suma grupowa zawiera już zrabatowane wartości pozycji |
| **Kosztorys (total)** | Total zawiera sumy z grup z uwzględnionym rabatem |
