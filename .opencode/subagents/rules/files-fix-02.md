# Files — Fix 02: Klasy bazowe + CommonValidationExtensions

Cel: stworzyć fundamenty dla kolejnych refaktorów — klasy bazowe Commands/Queries domeny Files i wspólne reguły walidacji rozszerzeń, MIME, rozmiaru.

## Założenia
- Decyzja człowieka: klasy bazowe — TAK, pełna hierarchia.
- Najpierw sprawdź `#codebase` czy istnieje już globalny `ProjectScopedRequestBase` lub podobny (np. w `Business.Interfaces.Model` lub w innych domenach CQRS). Jeśli istnieje — użyj go i nie duplikuj.
- Zakaz `var` — explicit types.

## Zmiany

### 1) Klasy bazowe domeny Files

Lokalizacja: `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/Files/_Shared/` (nowy katalog).

Utwórz:

**a) ProjectScopedFilesRequestBase.cs** (jeśli globalny `ProjectScopedRequestBase` nie istnieje — w przeciwnym razie pomiń ten plik i dziedzicz po globalnym)
```csharp
public abstract record ProjectScopedFilesRequestBase : IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public abstract string PermissionCode { get; }
    public virtual ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

**b) FileScopedRequestBase.cs**
```csharp
public abstract record FileScopedRequestBase : ProjectScopedFilesRequestBase
{
    public required Guid FileId { get; init; }
}
```

**c) PackageScopedRequestBase.cs**
```csharp
public abstract record PackageScopedRequestBase : ProjectScopedFilesRequestBase
{
    public required Guid PackageId { get; init; }
}
```

UWAGA: w tym promptcie TYLKO twórz klasy bazowe. NIE migruj jeszcze Commands/Queries — to jest w fix-03.

### 2) CommonValidationExtensions — rozszerzenia dla plików

Sprawdź w `#codebase` lokalizację istniejącego `CommonValidationExtensions` (klasa z `RequiredId`, `UniqueIds`, `NonNegativeOrder`).

Jeśli istnieje jako jeden plik — dodaj tam metody. Jeśli jest podzielony per kategoria — utwórz `FileValidationExtensions.cs` w tej samej lokalizacji.

Dodaj metody (sygnatury — implementację dopasuj do istniejącego stylu):
```csharp
public static IRuleBuilderOptions<T, string> AllowedFileExtension<T>(
    this IRuleBuilder<T, string> rb, IReadOnlyCollection<string> allowedExtensions);

public static IRuleBuilderOptions<T, string> AllowedContentType<T>(
    this IRuleBuilder<T, string> rb, IReadOnlyCollection<string> allowedContentTypes);

public static IRuleBuilderOptions<T, long> MaxFileSize<T>(
    this IRuleBuilder<T, long> rb, long maxBytes);

public static IRuleBuilderOptions<T, ResourceScope> ValidScope<T>(
    this IRuleBuilder<T, ResourceScope> rb);
```

Przed implementacją sprawdź:
- czy `BeValidExtension`/`BeValidContentType` w istniejących validatorach Files (`UploadProjectFilesCommandValidator`, `CreatePackageAndUploadFilesCommandValidator`, `UploadProjectFileVersionCommandValidator`) mają jednakową logikę — przenieś najlepszy wariant do extension.
- gdzie zdefiniowane są listy dozwolonych rozszerzeń/MIME (constants?). Jeśli są stałe rozsiane w validatorach — wynieś do `FileConstants` (lub podobnego) w `Business.Interfaces.Constants` lub w `_Shared/`.

UWAGA: w tym promptcie TYLKO twórz extensions + constants. NIE wymieniaj jeszcze wywołań w istniejących validatorach — to fix-03.

## Po wykonaniu
Zbuduj solution. Zwróć raport: status buildu, lista nowych plików, czy globalny `ProjectScopedRequestBase` istniał (jeśli tak — gdzie), lokalizacja `CommonValidationExtensions`, blokery.
