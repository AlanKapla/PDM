# Files — Fix 05: Atomowość uploadów (CRITICAL)

Cel: zlikwidować ryzyko częściowego stanu (orphan ProjectFile bez Version + osierocone bloby Azure) przy błędzie w trakcie uploadu.

## Wymagania wstępne
- fix-03 i fix-04 zakończone.

## Decyzja człowieka
- All-or-nothing — operacja jest atomowa lub w pełni cofnięta.

## Zakres

### 1) CreatePackageAndUploadFilesCommandHandler (K6, ~210 linii, 3× SaveChangesAsync)
Plik: `02-ApplicationServices/.../CQRS/Files/CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommandHandler.cs`

Refaktor:
- Usuń wszystkie ręczne `SaveChangesAsync` z handlera. `TransactionBehavior` w pipeline robi jeden `SaveChangesAsync` na końcu (komentarz w obecnym kodzie „handler is self-contained" jest nieprawdziwy — usuń go).
- Buduj graf encji w pamięci (Package + ProjectFile[] + ProjectFileVersion[] + komentarze) i `Insert` ich razem; `SaveChangesAsync` zostawia pipeline.
- Blob upload zostaje przed `SaveChangesAsync`, ale gdy później cokolwiek pójdzie źle — wprowadź **mechanizm kompensacji blobów**:
  - Zbieraj listę zuploadowanych ścieżek blobów.
  - Otaczaj logikę `try { ... } catch { compensate; throw; }` — w `catch` usuń wszystkie zuploadowane bloby z Azure.
  - DB rollback załatwia transakcja MediatR (rzucenie wyjątku przed `SaveChangesAsync`).
- Wydziel atomowe metody (max ~20 linii):
  - `BuildPackage(...)`
  - `BuildFilesAndVersions(...)`
  - `UploadBlobsAsync(...)` — zwraca listę uploadowanych ścieżek do kompensacji
  - `CompensateBlobsAsync(IReadOnlyCollection<string> uploadedPaths, CancellationToken ct)`
  - `BuildBlobPath(...)`

### 2) UploadProjectFilesCommandHandler (K7, SaveChangesAsync w pętli)
Plik: `02-ApplicationServices/.../CQRS/Files/UploadProjectFiles/UploadProjectFilesCommandHandler.cs`

Refaktor analogiczny:
- Usuń `SaveChangesAsync` z pętli — jeden zapis na końcu (przez `TransactionBehavior`).
- Buduj wszystkie ProjectFile + Version + komentarz w pamięci, dodaj do repo, jedno `Insert`/`InsertRange`.
- Mechanizm kompensacji blobów jak wyżej.
- Usuń `try/catch` który tylko loguje błąd pojedynczego pliku — błąd ma propagować, transakcja robi rollback, kompensacja czyści bloby.
- Wydziel `UploadSingleFileAsync` (zwraca utworzone encje + ścieżkę bloba), `BuildBlobPath`, `BuildVersion`, `BuildComment`.

### 3) UploadProjectFileVersionCommandHandler (W11)
Plik: `02-ApplicationServices/.../CQRS/Files/UploadProjectFileVersion/UploadProjectFileVersionCommandHandler.cs`

- Walidację „nowe rozszerzenie == oryginalne rozszerzenie" przenieś do validatora albo (jeśli wymaga załadowania starego pliku) zostaw w handlerze ale jako pierwszą rzecz po `GetAndValidateFileAsync`, opakowaną w prywatną metodę `EnsureSameExtension(...)`.
- Optymalizuj `Include(pf => pf.Versions)` używany tylko do `MAX(VersionNumber)` — zamień na osobne zapytanie projekcyjne `MaxAsync(v => v.VersionNumber)` na repo wersji (jeśli dostępne).
- Mechanizm kompensacji bloba (jak wyżej) — jeśli upload się powiedzie, ale `SaveChangesAsync` zawiedzie → usuń blob.
- Wydziel `GetAndValidateFileAsync`, `BuildNewVersion`, `UploadBlobAsync` (zwraca ścieżkę), `CompensateBlobAsync`.

## Reguły jakości
- Zakaz `var` — explicit types.
- `is null` / `is not null`.
- Wszystkie nowe metody prywatne — zwięzłe, jednoznaczne nazwy intencyjne.
- Sealed handlery (powinno już być z fix-03, ale upewnij się).
- Sprawdź czy `TransactionBehavior` faktycznie obejmuje `IRequestCommand` (zgodnie z `MediatRPipeline.md` w `docs/`) i czy te 3 handlery są nim objęte.

## Po wykonaniu
Zbuduj solution. Zwróć raport: status buildu, lista zmodyfikowanych plików, opisz krótko jak działa kompensacja blobów (1 linia), blokery.
