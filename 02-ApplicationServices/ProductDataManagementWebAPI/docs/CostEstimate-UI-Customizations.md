# 🎨 Cost Estimate UI Customizations Guide

## Przegląd

System kosztorysów obsługuje **customizację UI** dla grup i zakresów robót (work scopes) podczas wypełniania kosztorysu. Customizacje są przechowywane w `metadata` i **nie wpływają na dane biznesowe ani walidację schematu**.

---

## 📊 Architektura

### Separacja Danych

```
CostEstimateTemplate (Szablon)
├── TemplateStructure
│   ├── GroupDefinition
│   │   └── HeaderFields[] → color, icon (DOMYŚLNE)
│   └── WorkScopeFieldsDefinition
│       └── CalculatedFields[] → color, icon (DOMYŚLNE)

CostEstimate (Wypełniony)
├── Data
│   ├── Groups[] → wartości biznesowe (TYLKO DANE)
│   └── WorkScopes[] → wartości biznesowe (TYLKO DANE)
└── Metadata
    ├── GroupCustomizations{} → kolory, ikony (UI OVERRIDE)
    └── WorkScopeCustomizations{} → kolory, tagi (UI OVERRIDE)
```

**Zasada:** Szablon definiuje domyślny wygląd, metadata pozwala na override per instancja.

---

## 🎨 Customizacja Grup

### Model

```csharp
public class GroupUiCustomization
{
    public string? HeaderColor { get; set; }              // Kolor nagłówka
    public string? HeaderBackgroundColor { get; set; }    // Kolor tła nagłówka
    public string? Icon { get; set; }                     // Ikona (override)
    public bool? Collapsed { get; set; }                  // Czy zwinięta
    public bool? Highlighted { get; set; }                // Wyróżnienie
    public string? Notes { get; set; }                    // Notatki użytkownika
}
```

### Przykład JSON

```json
{
  "data": {
    "groups": [
      {
        "id": "group-1",
        "headerValues": {
          "GroupName": "Roboty ziemne",
          "GroupDescription": "Wykopy fundamentowe"
        }
      },
      {
        "id": "group-2",
        "headerValues": {
          "GroupName": "Roboty wykończeniowe"
        }
      }
    ]
  },
  "metadata": {
    "groupCustomizations": {
      "group-1": {
        "headerColor": "#FF5733",
        "headerBackgroundColor": "#FFE5E5",
        "highlighted": true,
        "notes": "Grupa wymagająca dodatkowej uwagi - problemy z gruntem"
      },
      "group-2": {
        "headerColor": "#28a745",
        "icon": "paint-brush",
        "collapsed": false
      }
    }
  }
}
```

### Frontend - Renderowanie z Override

```typescript
function renderGroupHeader(group: CostEstimateGroup, template: CostEstimateTemplate) {
  // 1. Domyślny kolor z szablonu
  const defaultColor = template.templateStructure
    .groupDefinition.headerFields
    .find(f => f.type === 'GroupName')?.color || '#000000';
  
  // 2. Override z metadata (jeśli istnieje)
  const customization = costEstimate.data.metadata
    ?.groupCustomizations?.[group.id];
  
  // 3. Final values (custom ma pierwszeństwo)
  const finalColor = customization?.headerColor || defaultColor;
  const finalBgColor = customization?.headerBackgroundColor || 'transparent';
  const isHighlighted = customization?.highlighted || false;
  
  return (
    <div 
      className={`group-header ${isHighlighted ? 'highlighted' : ''}`}
      style={{
        borderLeft: `4px solid ${finalColor}`,
        backgroundColor: finalBgColor
      }}
    >
      <h3>{group.headerValues.GroupName}</h3>
      {customization?.notes && (
        <Tooltip content={customization.notes}>
          <InfoIcon />
        </Tooltip>
      )}
    </div>
  );
}
```

---

## 🎨 Customizacja Work Scopes

### Model

```csharp
public class WorkScopeUiCustomization
{
    public string? RowColor { get; set; }        // Kolor tła wiersza
    public string? TextColor { get; set; }       // Kolor tekstu
    public bool? Highlighted { get; set; }       // Wyróżnienie
    public List<string>? Tags { get; set; }      // Tagi (np. "ważne", "problem")
    public string? Notes { get; set; }           // Notatki użytkownika
}
```

### Przykład JSON

```json
{
  "data": {
    "groups": [
      {
        "id": "group-1",
        "workScopes": [
          {
            "id": "ws-1",
            "calculatedFieldValues": {
              "unitPrice": 50.00,
              "quantity": 100
            }
          },
          {
            "id": "ws-2",
            "calculatedFieldValues": {
              "unitPrice": 75.00,
              "quantity": 50
            }
          }
        ]
      }
    ]
  },
  "metadata": {
    "workScopeCustomizations": {
      "ws-1": {
        "rowColor": "#FFFFCC",
        "highlighted": true,
        "tags": ["ważne", "do weryfikacji"],
        "notes": "Sprawdzić z dostawcą - cena może być niższa"
      },
      "ws-2": {
        "rowColor": "#FFE5E5",
        "textColor": "#CC0000",
        "highlighted": true,
        "tags": ["problem", "do rozwiązania"],
        "notes": "Brak materiału na magazynie - zamówić"
      }
    }
  }
}
```

### Frontend - Renderowanie Work Scope

```typescript
function renderWorkScopeRow(workScope: CostEstimateWorkScope) {
  const customization = costEstimate.data.metadata
    ?.workScopeCustomizations?.[workScope.id];
  
  return (
    <tr 
      className={customization?.highlighted ? 'highlighted' : ''}
      style={{
        backgroundColor: customization?.rowColor || 'transparent',
        color: customization?.textColor || 'inherit'
      }}
    >
      <td>{workScope.calculatedFieldValues.unitPrice}</td>
      <td>{workScope.calculatedFieldValues.quantity}</td>
      <td>
        {customization?.tags?.map(tag => (
          <Badge key={tag} variant="warning">{tag}</Badge>
        ))}
      </td>
      <td>
        {customization?.notes && (
          <Popover content={customization.notes}>
            <CommentIcon />
          </Popover>
        )}
      </td>
    </tr>
  );
}
```

---

## 🔄 API - Ustawienie Customizacji

### Endpoint

```
PUT /api/tenants/{tenantId}/projects/{projectId}/cost-estimates/{id}
```

### Request Body (Update Command)

```json
{
  "costEstimateId": "guid",
  "name": "Kosztorys budowy",
  "status": "InProgress",
  "data": {
    "groups": [...],
    "metadata": {
      "lastModified": "2024-01-15T10:30:00Z",
      "lastModifiedBy": "user-guid",
      "schemaVersion": 1,
      "groupCustomizations": {
        "group-1": {
          "headerColor": "#FF5733",
          "highlighted": true
        }
      },
      "workScopeCustomizations": {
        "ws-1": {
          "rowColor": "#FFFFCC",
          "tags": ["ważne"]
        }
      }
    }
  },
  "totalNet": 150000.50,
  "totalGross": 184500.62
}
```

---

## 💡 Use Cases

### 1. Wyróżnienie Grupy Wymagającej Uwagi

```typescript
async function highlightGroup(groupId: string, reason: string) {
  await updateCostEstimate({
    ...costEstimate,
    data: {
      ...costEstimate.data,
      metadata: {
        ...costEstimate.data.metadata,
        groupCustomizations: {
          ...costEstimate.data.metadata?.groupCustomizations,
          [groupId]: {
            headerColor: "#FF5733",
            headerBackgroundColor: "#FFE5E5",
            highlighted: true,
            notes: reason
          }
        }
      }
    }
  });
}

// Użycie
await highlightGroup("group-1", "Problemy z gruntem - wymaga konsultacji");
```

### 2. Oznaczenie Wiersza do Weryfikacji

```typescript
async function markWorkScopeForReview(workScopeId: string, tags: string[]) {
  await updateCostEstimate({
    ...costEstimate,
    data: {
      ...costEstimate.data,
      metadata: {
        ...costEstimate.data.metadata,
        workScopeCustomizations: {
          ...costEstimate.data.metadata?.workScopeCustomizations,
          [workScopeId]: {
            rowColor: "#FFFFCC",
            highlighted: true,
            tags: tags
          }
        }
      }
    }
  });
}

// Użycie
await markWorkScopeForReview("ws-1", ["do weryfikacji", "sprawdzić cenę"]);
```

### 3. Dodanie Notatki do Work Scope

```typescript
async function addWorkScopeNote(workScopeId: string, note: string) {
  const existing = costEstimate.data.metadata
    ?.workScopeCustomizations?.[workScopeId];
  
  await updateCostEstimate({
    ...costEstimate,
    data: {
      ...costEstimate.data,
      metadata: {
        ...costEstimate.data.metadata,
        workScopeCustomizations: {
          ...costEstimate.data.metadata?.workScopeCustomizations,
          [workScopeId]: {
            ...existing,
            notes: note
          }
        }
      }
    }
  });
}

// Użycie
await addWorkScopeNote("ws-2", "Brak materiału - zamówić do piątku");
```

### 4. Czyszczenie Wszystkich Customizacji

```typescript
async function clearAllCustomizations() {
  await updateCostEstimate({
    ...costEstimate,
    data: {
      ...costEstimate.data,
      metadata: {
        ...costEstimate.data.metadata,
        groupCustomizations: undefined,
        workScopeCustomizations: undefined
      }
    }
  });
}
```

---

## ✅ Zalety Rozwiązania

1. ✅ **Separacja Danych vs UI**
   - Dane biznesowe w `groups` i `workScopes`
   - Customizacje UI w `metadata`

2. ✅ **Nie Wpływa na Walidację**
   - `CostEstimateSchemaValidator` ignoruje `metadata`
   - Customizacje nie muszą być zgodne z szablonem

3. ✅ **Backward Compatible**
   - Stare kosztorysy bez customizacji działają normalnie
   - Domyślne kolory z szablonu

4. ✅ **Łatwe Czyszczenie**
   - Można wyczyścić wszystkie customizacje jednym ruchem
   - Powrót do domyślnego wyglądu

5. ✅ **Elastyczne**
   - Override per grupa
   - Override per work scope
   - Możliwość rozbudowy (tagi, notatki, collapsed)

---

## 🎨 Rekomendowane Kolory

### Paleta Statusów

```typescript
const STATUS_COLORS = {
  success: "#28a745",      // Zielony - OK
  warning: "#ffc107",      // Żółty - Uwaga
  danger: "#dc3545",       // Czerwony - Problem
  info: "#17a2b8",         // Niebieski - Informacja
  primary: "#007bff",      // Niebieski - Domyślny
  secondary: "#6c757d"     // Szary - Drugorzędny
};
```

### Paleta Wyróżnień

```typescript
const HIGHLIGHT_COLORS = {
  important: "#FFFFCC",    // Żółte tło - Ważne
  problem: "#FFE5E5",      // Czerwone tło - Problem
  verified: "#E5FFE5",     // Zielone tło - Zweryfikowane
  pending: "#E5F3FF"       // Niebieskie tło - Oczekujące
};
```

---

## 🔐 Bezpieczeństwo

- ✅ Customizacje są per kosztorys (OwnerId)
- ✅ Nie wpływają na inne kosztorysy
- ✅ Walidowane przez `UpdateCostEstimateCommandValidator`
- ✅ Tenant isolation (TenantId)
- ✅ Project membership required

---

## 📝 Przykład Kompletnego Flow

```typescript
// 1. Użytkownik otwiera kosztorys
const costEstimate = await api.getCostEstimate(id);
const template = await api.getTemplate(costEstimate.templateId);

// 2. Renderuje z merge kolorów (szablon + customizacje)
renderCostEstimate(costEstimate, template);

// 3. Użytkownik klika "Highlight group"
await highlightGroup("group-1", "Wymaga uwagi");

// 4. Użytkownik dodaje tag do wiersza
await markWorkScopeForReview("ws-1", ["do weryfikacji"]);

// 5. System zapisuje customizacje w metadata
// (dane biznesowe pozostają nietknięte)

// 6. Inni użytkownicy widzą customizacje
// (jeśli mają dostęp do tego kosztorysu)
```
