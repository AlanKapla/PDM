# Files — Fix 07: DRY mapowania + drobiazgi

Cel: usunąć duplikację mapowań w handlerach Get* i sprzątnąć drobne pozostałości.

## Wymagania wstępne
- fix-03 zakończony (web modele są `sealed record` z `required init`).

## Zakres

### 1) IFileVersionWebMapper (N5)
Lokalizacja: `02-ApplicationServices/.../Business/Implementation/Services/Files/` (lub `CQRS/Files/_Shared/Mappers/` — wybierz spójnie z innymi domenami, sprawdź w `#codebase`).

Aktualnie mapowanie `ProjectFileVersion → ProjectFileVersionWeb` jest powtarzane:
- `GetFileVersionsQueryHandler.MapToVersionWeb`
- `GetPackageFilesQueryHandler.MapToProjectFileWeb` (część budująca currentVersion)

Wprowadź:
```csharp
public interface IFileVersionWebMapper
{
    ProjectFileVersionWeb Map(ProjectFileVersion version, IReadOnlyDictionary<Guid, User> userDict, string sasUri);
}
```
Sealed implementacja w tym samym katalogu. Zarejestruj w DI.

Wymień obie kopie mapowania w handlerach na wywołanie mappera.

### 2) Pomocnik ResolveUserName (N6)
W tym samym miejscu co mapper (lub jako static helper w `_Shared/`):
```csharp
public static string ResolveUserName(IReadOnlyDictionary<Guid, User> userDict, Guid userId)
    => userDict.TryGetValue(userId, out User? user) ? user.FullName : string.Empty;
```
Wymień powtórzony kod w 4 Get* handlerach na wywołanie pomocnika.

### 3) Wydziel inline mapowania (N7)
- `GetProjectFilePackagesQueryHandler` — wydziel prywatną `MapToPackageWeb(ProjectFilePackage, ...)`.
- `GetVersionCommentsQueryHandler` — wydziel prywatną `MapToCommentWeb(ProjectFileVersionComment, ...)`.

### 4) Drobiazgi (N1, N2, N3, N8, N9, N10, N11, N12)
Większość powinna być załatwiona w fix-03. Zweryfikuj i dokończ jeśli coś zostało:
- Pusty plik `Files/ProjectFileDto.cs` → usunięty.
- Nadmiarowe `}` w `UploadProjectFilesCommand.cs`, `CreatePackageAndUploadFilesCommand.cs` → poprawione.
- Wszystkie `== null` / `!= null` → `is null` / `is not null`.
- Nieużywane usingi (Entities.Models.{Chats,Costs,Notifications,Roles,Tenants,Users,WorkSchedules}) → usunięte.
- Komentarze z błędną numeracją `// 4.`, `// 5.` (powtórzone) w `DeleteProjectFile`, `AddFileVersionComment` → poprawione.
- `Files.Any()` → `Files.Count > 0`.
- `BeValidExtension`/`BeValidContentType` → już zastąpione extensions z fix-02 (zweryfikuj że żadna kopia nie została).
- Niepotrzebny `using CQRS.Files.GetProjectFilePackages;` w `GetProjectFilePackagesQueryValidator.cs` → usunąć.

### 5) Komentarze EN/PL (N4)
Ujednolić: komunikaty walidacji EN, komentarze w kodzie zostaw zgodnie z dominującym stylem domeny (sprawdź w `#codebase` — dominuje EN czy PL?).

## Reguły jakości
- Zakaz `var` — explicit types.
- Sealed mapper i wszystkie nowe klasy.
- Mapper bez I/O — przyjmuje już załadowane słowniki users/sas jako parametry.

## Po wykonaniu
Zbuduj solution. Zwróć raport: status buildu, lista zmodyfikowanych/utworzonych/usuniętych plików, lista drobiazgów rzeczywiście zaaplikowanych (które już były zrobione wcześniej), blokery.
