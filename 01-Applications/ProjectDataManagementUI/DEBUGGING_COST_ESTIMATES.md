# Debugging Guide - Cost Estimate Creation

## Problem
Dodawanie kosztorysów nie działa zgodnie z wymaganiami.

## Krok po kroku - Co powinno się dziać:

### 1. Użytkownik otwiera modal
```
ProjectCosts -> Przycisk "Nowy kosztorys" -> onCreateModalOpen()
```
**Co się dzieje:**
- Modal `CreateCostEstimateModal` otwiera się z `isOpen={true}`
- useEffect wykrywa `isOpen=true` i wywołuje `costEstimateTemplateApi.getTemplates()`
- Request: `GET /api/cost-estimate-templates`
- Odpowiedź: Lista szablonów użytkownika

**Co sprawdzić w console:**
```
Modal opened, loading templates...
Fetching templates from API...
Templates received: [...]
```

### 2. Wybór szablonu
```
User wybiera szablon z dropdown -> handleTemplateChange(templateId)
```
**Co się dzieje:**
- `setSelectedTemplateId(templateId)`
- Wywołanie `costEstimateTemplateApi.getTemplateDetails(templateId)`
- Request: `GET /api/cost-estimate-templates/${templateId}`
- Odpowiedź: Szczegóły szablonu z `templateStructure`

**Co sprawdzić w console:**
```
Template changed to: <id>
Fetching template details for: <id>
Template details received: {...}
```

### 3. Wypełnienie formularza
- Nazwa kosztorysu (wymagane)
- Opis (opcjonalnie)
- Wybrany szablon (już wybrany)

### 4. Kliknięcie "Utwórz kosztorys"
```
handleSubmit() -> createInitialDataFromTemplate() -> costEstimateApi.createCostEstimate()
```

**Co się dzieje:**
- Walidacja: czy `name` i `selectedTemplateId` są wypełnione
- `createInitialDataFromTemplate(selectedTemplateDetails)` tworzy strukturę:
  ```typescript
  {
    groups: [
      {
        id: "group-<timestamp>",
        level: 0,
        order: 0,
        headerValues: {},
        workScopes: []
      }
    ],
    metadata: {
      lastModified: "<ISO date>",
      schemaVersion: 1
    }
  }
  ```
- Request: `POST /api/tenants/${tenantId}/projects/${projectId}/cost-estimates`
  Body:
  ```json
  {
    "templateId": "...",
    "name": "...",
    "description": "...",
    "data": {
      "groups": [...],
      "metadata": {...}
    }
  }
  ```

**Co sprawdzić w console:**
```
Creating cost estimate with data: {...}
```

### 5. Po sukcesie
- Toast: "Kosztorys został utworzony"
- Wywołanie `onCostEstimateCreated()` -> `fetchData()` w ProjectCosts
- Modal się zamyka

---

## Możliwe problemy:

### A. Szablony się nie ładują
**Symptom:** Dropdown jest pusty lub pokazuje "Ładowanie szablonów..."

**Przyczyny:**
1. Endpoint `/api/cost-estimate-templates` nie istnieje
2. Użytkownik nie ma żadnych szablonów
3. Błąd 401/403 - brak autoryzacji
4. Backend zwraca błędny format danych

**Rozwiązanie:**
1. Sprawdź Network tab w DevTools
2. Sprawdź console - powinno być `Templates received: [...]`
3. Jeśli pusta tablica `[]` - użytkownik musi najpierw utworzyć szablon w `/cost-estimate-templates`

### B. Szczegóły szablonu się nie ładują
**Symptom:** Po wyborze szablonu nie pokazuje się podgląd struktury

**Przyczyny:**
1. Endpoint `/api/cost-estimate-templates/${id}` nie działa
2. Szablon nie ma `templateStructure`

**Rozwiązanie:**
1. Sprawdź Network tab
2. Sprawdź console - `Template details received`

### C. Request tworzenia kosztorysu kończy się błędem
**Symptom:** Toast "Nie udało się utworzyć kosztorysu"

**Przyczyny:**
1. Endpoint nie istnieje: `POST /api/tenants/${tenantId}/projects/${projectId}/cost-estimates`
2. Błędna struktura body
3. Walidacja na backendzie odrzuca request
4. `tenantId` lub `projectId` są undefined

**Rozwiązanie:**
1. Sprawdź console: `Creating cost estimate with data:`
2. Sprawdź Network tab - kod odpowiedzi i błąd
3. Sprawdź czy `user.activeTenantId` istnieje
4. Sprawdź czy `projectId` jest prawidłowe

### D. Kosztorys się tworzy, ale nie pojawia na liście
**Symptom:** Toast sukcesu, ale lista pusta

**Przyczyny:**
1. `fetchData()` w ProjectCosts nie wywołuje się
2. Filtrowanie po `ownerId` wyklucza nowy kosztorys
3. Backend zwraca pusty status

**Rozwiązanie:**
1. Sprawdź czy `onCostEstimateCreated()` jest wywołane
2. Sprawdź console w `ProjectCosts.fetchData()`

---

## Debugging checklist:

1. [ ] Otwórz DevTools (F12)
2. [ ] Przejdź do zakładki Console
3. [ ] Przejdź do zakładki Network
4. [ ] Otwórz stronę `/projects/${projectId}/costs`
5. [ ] Kliknij "Nowy kosztorys"
6. [ ] Sprawdź console - czy widzisz "Modal opened, loading templates..."
7. [ ] Sprawdź Network - czy jest request do `/api/cost-estimate-templates`
8. [ ] Sprawdź odpowiedź - czy zwraca szablony?
9. [ ] Wybierz szablon z dropdown
10. [ ] Sprawdź console - czy widzisz "Template changed to..."
11. [ ] Sprawdź Network - czy jest request do `/api/cost-estimate-templates/${id}`
12. [ ] Wypełnij formularz
13. [ ] Kliknij "Utwórz kosztorys"
14. [ ] Sprawdź console - czy widzisz "Creating cost estimate with data:"
15. [ ] Sprawdź Network - czy jest POST request
16. [ ] Sprawdź odpowiedź - jaki status? 200/201/400/500?
17. [ ] Jeśli błąd - sprawdź treść błędu w Response

---

## Szybki test bez backendu:

Jeśli backend nie jest gotowy, możesz przetestować frontend:

1. Utwórz mock dane w `CreateCostEstimateModal`:
```typescript
// Temporary mock for testing
useEffect(() => {
  if (isOpen) {
    setTemplates([
      {
        id: 'mock-1',
        name: 'Szablon testowy',
        description: 'To jest testowy szablon',
        createdAt: new Date().toISOString(),
        ownerId: 'user-1',
        ownerName: 'Test User'
      }
    ]);
  }
}, [isOpen]);
```

2. Zakomentuj wywołanie API w `handleSubmit`
3. Sprawdź czy struktura danych jest generowana poprawnie
