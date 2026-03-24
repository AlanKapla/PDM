# Backend Changes — `ItemSystemCategory` field + `CostEstimateTemplateCategory`

## Kontekst

Dodano nowy systemowy typ pola `ItemSystemCategory` oraz słownik kategorii (`Categories`) w szablonie kosztorysu.
**Pole zachowuje się identycznie jak `ItemSystemUnit`** — użytkownik może wybrać kategorię z listy zaszytej w szablonie lub wpisać własną wartość z palca (combobox z free-text).

---

## 1. Nowa wartość enuma `FieldType`

| Wartość | Int | Zakres | Typ danych |
|---|---|---|---|
| `ItemSystemCategory` | `106` | `ItemSystem` | `string` |

---

## 2. Nowa encja `CostEstimateTemplateCategory`

Słownik kategorii dostępnych w szablonie — analogiczny do `CostEstimateTemplateUnit`.

```ts
// kształt danych (analogia do UnitWeb)
interface CategoryWeb {
  id: string;            // Guid
  name: string;          // np. "Robocizna", "Materiały", "Sprzęt"
  symbol: string | null; // np. "R", "M", "S"
  order: number;
}
```

---

## 3. Zmiana w `CostEstimateTemplateStructureWeb`

`categories` dodane **obok** `units` — na tym samym poziomie struktury szablonu.

```ts
// PRZED
interface CostEstimateTemplateStructureWeb {
  templateId: string;
  maxGroupLevel: number | null;
  currencies: CurrencyWeb[];
  units: UnitWeb[];
  groupHeaderFields: FieldDefinitionWeb[];
  systemFields: FieldDefinitionWeb[];
  calculatedFields: FieldDefinitionWeb[];
  genericFields: FieldDefinitionWeb[];
  uiConfiguration: UiConfigurationWeb | null;
}

// PO
interface CostEstimateTemplateStructureWeb {
  templateId: string;
  maxGroupLevel: number | null;
  currencies: CurrencyWeb[];
  units: UnitWeb[];
  categories: CategoryWeb[];           // ← NOWE
  groupHeaderFields: FieldDefinitionWeb[];
  systemFields: FieldDefinitionWeb[];
  calculatedFields: FieldDefinitionWeb[];
  genericFields: FieldDefinitionWeb[];
  uiConfiguration: UiConfigurationWeb | null;
}
```

---

## 4. Zmiana w żądaniu aktualizacji szablonu (Update Template)

`categories` dodane **obok** `units` w body requestu.

```ts
// PRZED
interface UpdateCostEstimateTemplateRequest {
  // ...
  currencies: CurrencyDto[] | null;
  units: UnitDto[] | null;
  groupHeaderFields: FieldDefinitionDto[] | null;
  // ...
}

// PO
interface UpdateCostEstimateTemplateRequest {
  // ...
  currencies: CurrencyDto[] | null;
  units: UnitDto[] | null;
  categories: CategoryDto[] | null;    // ← NOWE
  groupHeaderFields: FieldDefinitionDto[] | null;
  // ...
}

interface CategoryDto {
  name: string;
  symbol: string | null;
  order: number;
}
```

---

## 5. Zachowanie pola — identyczne jak `ItemSystemUnit`

| Aspekt | `ItemSystemUnit` (wzorzec) | `ItemSystemCategory` (nowe) |
|---|---|---|
| FieldType int | `102` | `106` |
| Typ wartości | `string` | `string` |
| Źródło opcji | `structure.units` | `structure.categories` |
| Klucz matchowania | `unit.code` | `category.name` |
| Free-text | ✅ tak | ✅ tak |
| Komponent UI | Combobox | Combobox (taki sam) |
| Wyświetlana etykieta opcji | `unit.symbol ?? unit.code` | `category.symbol ?? category.name` |
| Wartość zapisywana | `string` | `string` |

### Logika comboboxa

```
1. Pole w szablonie ma FieldType === 106 (ItemSystemCategory)
2. Opcje dropdown = structure.categories posortowane po `order`
3. Wyświetlana etykieta opcji = category.symbol ?? category.name
4. Wartość zapisywana = category.name (string)
5. Użytkownik może wpisać własną wartość nieobecną na liście (free-text)
6. Jeśli structure.categories jest puste → zwykłe pole tekstowe (fallback)
```

---

## 6. Migracja bazy danych

Wymagana nowa migracja EF Core — tabela `CostEstimateTemplateCategories`:

```bash
dotnet ef migrations add add-cost-estimate-template-categories --project src/Entities --startup-project src/WebApi
dotnet ef database update --project src/Entities --startup-project src/WebApi
```

---

## 7. Pliki zmienione na backendzie

| Plik | Zmiana |
|---|---|
| `Entities/Models/CostEstimates/CostEstimateEnums.cs` | `ItemSystemCategory = 106` dodane do `FieldType` |
| `Entities/Models/CostEstimateTemplates/CostEstimateTemplateCategory.cs` | Nowa encja (Id, TemplateId, Name, Symbol, Order) |
| `Entities/Models/CostEstimateTemplates/CostEstimateTemplate.cs` | Kolekcja `Categories` w navigation properties |
| `Entities/Configurations/CostEstimateTemplateCategoryConfiguration.cs` | Nowa konfiguracja EF Core (cascade delete, unique index) |
| `Entities/Context/AppDbContext.cs` | `DbSet<CostEstimateTemplateCategory>` |
| `Business/Interfaces/WebModels/.../CostEstimateTemplateDtos.cs` | `CategoryDto(Name, Symbol, Order)` |
| `Business/Interfaces/WebModels/.../CostEstimateTemplateStructureWeb.cs` | `CategoryWeb` record + `Categories` w `CostEstimateTemplateStructureWeb` |
| `Business/Interfaces/Services/ICostEstimateTemplateService.cs` | `List<CategoryDto>? categories` w `UpdateTemplateAsync` |
| `Business/Implementation/Services/CostEstimateTemplateService.cs` | Pełna obsługa: `categoryRepository`, `UpdateCategoriesAsync`, build + duplicate |
| `Business/Implementation/Helpers/CostEstimateFieldTypeHelper.cs` | Konfiguracja `ItemSystemCategory` w sekcji ItemSystem |
| `Business/Implementation/Helpers/DefaultTemplateJsonModels.cs` | `List<CategoryDto> Categories` w modelu JSON domyślnych szablonów |
