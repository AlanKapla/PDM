# Notifications — Fix 02: CommonValidationExtensions (PageSize, NonNegativeOffset)

## Kontekst
Walidacja paginacji `Take`/`Skip` jest zduplikowana w `GetAllNotificationsQueryValidator` i `GetUnreadNotificationsQueryValidator`. Wydzielamy do `CommonValidationExtensions`.

## Zakres

### 1. Rozszerz `CommonValidationExtensions`
Plik: znajdź lokalizację `CommonValidationExtensions` przez `#codebase` (typowo `Business/Implementation/Validators/CommonValidationExtensions.cs`). W innych domenach jest używane z `RequiredId()`, `NonNegativeOrder()`, `UniqueIds()`.

Dodaj dwie ekstensje:

```csharp
public static IRuleBuilderOptions<T, int> PageSize<T>(
    this IRuleBuilder<T, int> ruleBuilder,
    int max = 100)
{
    return ruleBuilder
        .GreaterThan(0).WithMessage("Page size must be greater than 0")
        .LessThanOrEqualTo(max).WithMessage($"Page size cannot exceed {max}");
}

public static IRuleBuilderOptions<T, int> NonNegativeOffset<T>(
    this IRuleBuilder<T, int> ruleBuilder)
{
    return ruleBuilder
        .GreaterThanOrEqualTo(0).WithMessage("Offset must be non-negative");
}
```

Jeśli `RequiredId()` dla `Guid` istnieje — pozostaw bez zmian. Nie modyfikuj istniejących ekstensji.

### 2. Użyj ekstensji w validatorach Notifications

`GetAllNotificationsQueryValidator.cs` i `GetUnreadNotificationsQueryValidator.cs`:

```csharp
RuleFor(x => x.Take).PageSize();          // domyślnie 100
RuleFor(x => x.Skip).NonNegativeOffset();
```

`MarkNotificationAsReadCommandValidator.cs`:

```csharp
RuleFor(x => x.NotificationId).RequiredId();
```

(Zakładamy że `RequiredId()` istnieje — jeśli nie, sprawdź `#codebase` jak inne domeny robią walidację GUID-ów. W skrajnym wypadku zostaw `NotEmpty()`.)

## Kryteria akceptacji
- Validatory Notifications nie zawierają już ręcznych `GreaterThan(0)` / `GreaterThanOrEqualTo(0)` dla paginacji.
- Build: 0 błędów.
- Inne domeny używające `CommonValidationExtensions` nadal się kompilują (extensions tylko dodajesz, niczego nie zmieniaj).

## Raport końcowy
- Status build.
- Lista zmodyfikowanych plików.
- Potwierdzenie że ekstensje są dodane oraz użyte w 3 validatorach Notifications.
