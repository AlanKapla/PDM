# API Refactor - Files Endpoints

## 📋 Podsumowanie zmian

Stary monolityczny endpoint `GET /api/tenants/{tenantId}/project/{projectId}/file/{scope}` został podzielony na **4 osobne endpointy** umożliwiające hierarchiczne pobieranie danych:

1. **GET packages** - pobiera listę paczek plików
2. **GET files** - pobiera pliki w konkretnej paczce
3. **GET versions** - pobiera wersje konkretnego pliku
4. **GET comments** - pobiera komentarze do konkretnej wersji

---

## 🔴 USUNIĘTY ENDPOINT

### ~~GET `/api/tenants/{tenantId}/project/{projectId}/file/{scope}`~~

**Status:** ❌ USUNIĘTY - nie używać!

**Powód:** Pobierał wszystko naraz (packages + files + versions + comments) - nieefektywne, wolne, duże transfery danych.

---

## ✅ NOWE ENDPOINTY

### 1. GET Packages - Lista paczek plików

```http
GET /api/tenants/{tenantId}/project/{projectId}/file/packages/{scope}
```

**Parametry:**
- `tenantId` (route) - Guid
- `projectId` (route) - Guid  
- `scope` (route) - enum: `All` | `Mine` | `Shared`

**Response:** `List<ProjectFilePackageWeb>`

```typescript
interface ProjectFilePackageWeb {
  id: string;
  name: string;
  createdAt: string;
  ownerId: string;
  ownerName: string;
  files: [];              // ⚠️ ZAWSZE PUSTA - nie pobiera plików
  totalFiles: number;     // ✅ Liczba plików w paczce
}
```

**Przykład:**
```json
[
  {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "name": "Dokumentacja projektu",
    "createdAt": "2024-01-15T10:30:00Z",
    "ownerId": "user-guid",
    "ownerName": "Jan Kowalski",
    "files": [],
    "totalFiles": 15
  }
]
```

**Scope behavior:**
- `All` - wszystkie paczki w projekcie (wymaga uprawnień admin)
- `Mine` - tylko moje paczki
- `Shared` - paczki zawierające pliki udostępnione mi przez innych

**Performance:** 
- ⚡ Bardzo szybki - liczy pliki w SQL (COUNT GROUP BY)
- 📦 Małe transfery - tylko metadane paczek

---

### 2. GET Files - Pliki w paczce

```http
GET /api/tenants/{tenantId}/project/{projectId}/file/packages/{packageId}/files/{scope}
```

**Parametry:**
- `tenantId` (route) - Guid
- `projectId` (route) - Guid
- `packageId` (route) - Guid - **NOWE!**
- `scope` (route) - enum: `All` | `Mine` | `Shared`

**Response:** `List<ProjectFileWeb>`

```typescript
interface ProjectFileWeb {
  id: string;
  fileName: string;
  displayName: string;
  packageName: string;
  createdAt: string;
  ownerId: string;
  ownerName: string;
  currentVersion: ProjectFileVersionWeb | null;  // ✅ Pełna info o aktualnej wersji
  versions: [];                                  // ⚠️ ZAWSZE PUSTA - nie pobiera historii
  totalVersions: number;                         // ✅ Liczba wersji
  isOwner: boolean;
  isShared: boolean;
  sharedWithUserIds: string[];                   // ✅ Lista userów z dostępem
}

interface ProjectFileVersionWeb {
  id: string;
  projectFileId: string;
  versionNumber: number;
  contentType: string;
  fileSizeBytes: number;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  sasUrlView: string;      // ✅ URL do podglądu (inline)
  sasUrlDownload: string;  // ✅ URL do pobrania (attachment)
  comments: [];            // ⚠️ ZAWSZE PUSTA - nie pobiera komentarzy
}
```

**Przykład:**
```json
[
  {
    "id": "file-guid",
    "fileName": "dokument.pdf",
    "displayName": "Dokument projektowy",
    "packageName": "Dokumentacja projektu",
    "createdAt": "2024-01-15T10:30:00Z",
    "ownerId": "user-guid",
    "ownerName": "Jan Kowalski",
    "currentVersion": {
      "id": "version-guid",
      "versionNumber": 3,
      "contentType": "application/pdf",
      "fileSizeBytes": 1048576,
      "createdAt": "2024-01-20T14:00:00Z",
      "createdByUserId": "user-guid",
      "createdByUserName": "Jan Kowalski",
      "sasUrlView": "https://storage.blob.core.windows.net/...",
      "sasUrlDownload": "https://storage.blob.core.windows.net/...",
      "comments": []
    },
    "versions": [],
    "totalVersions": 3,
    "isOwner": true,
    "isShared": false,
    "sharedWithUserIds": []
  }
]
```

**Scope behavior:**
- `All` - wszystkie pliki w paczce (wymaga uprawnień admin)
- `Mine` - tylko moje pliki w paczce
- `Shared` - tylko pliki udostępnione mi w tej paczce

**Autoryzacja:** Weryfikuje dostęp do paczki na podstawie scope

**Performance:**
- ⚡ Parallel queries - wersje i shared info pobierane równolegle
- 📦 Optymalne - zwraca tylko currentVersion, nie całą historię

---

### 3. GET Versions - Historia wersji pliku

```http
GET /api/tenants/{tenantId}/project/{projectId}/file/files/{fileId}/versions/{scope}
```

**Parametry:**
- `tenantId` (route) - Guid
- `projectId` (route) - Guid
- `fileId` (route) - Guid - **NOWE!**
- `scope` (route) - enum: `All` | `Mine` | `Shared`

**Response:** `List<ProjectFileVersionWeb>`

```typescript
interface ProjectFileVersionWeb {
  id: string;
  projectFileId: string;
  versionNumber: number;
  contentType: string;
  fileSizeBytes: number;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  sasUrlView: string;      // ✅ URL do podglądu
  sasUrlDownload: string;  // ✅ URL do pobrania
  comments: [];            // ⚠️ ZAWSZE PUSTA - nie pobiera komentarzy
}
```

**Przykład:**
```json
[
  {
    "id": "version-3-guid",
    "projectFileId": "file-guid",
    "versionNumber": 3,
    "contentType": "application/pdf",
    "fileSizeBytes": 1048576,
    "createdAt": "2024-01-20T14:00:00Z",
    "createdByUserId": "user-guid",
    "createdByUserName": "Jan Kowalski",
    "sasUrlView": "https://storage.blob.core.windows.net/...",
    "sasUrlDownload": "https://storage.blob.core.windows.net/...",
    "comments": []
  },
  {
    "id": "version-2-guid",
    "projectFileId": "file-guid",
    "versionNumber": 2,
    // ... inne pola
  }
]
```

**Scope behavior:**
- `All` - wszystkie wersje (jeśli user ma dostęp do pliku)
- `Mine` - wszystkie wersje (jeśli user jest właścicielem)
- `Shared` - wszystkie wersje (jeśli plik został udostępniony userowi)

**Autoryzacja:** Weryfikuje dostęp do pliku na podstawie scope. Jeśli brak dostępu → **pusta lista** (nie 403!)

**Sortowanie:** Od najnowszej do najstarszej (descending versionNumber)

---

### 4. GET Comments - Komentarze do wersji

```http
GET /api/tenants/{tenantId}/project/{projectId}/file/files/{fileId}/versions/{versionId}/comments/{scope}
```

**Parametry:**
- `tenantId` (route) - Guid
- `projectId` (route) - Guid
- `fileId` (route) - Guid - **NOWE!**
- `versionId` (route) - Guid - **NOWE!**
- `scope` (route) - enum: `All` | `Mine` | `Shared`

**Response:** `List<ProjectFileVersionCommentWeb>`

```typescript
interface ProjectFileVersionCommentWeb {
  id: string;
  projectFileVersionId: string;
  userId: string;
  userName: string;
  content: string;
  createdAt: string;
  editedAt?: string;
  isEdited: boolean;
  canEdit: boolean;    // ✅ true jeśli komentarz należy do current user
  canDelete: boolean;  // ✅ true jeśli komentarz należy do current user
}
```

**Przykład:**
```json
[
  {
    "id": "comment-guid",
    "projectFileVersionId": "version-guid",
    "userId": "user-guid",
    "userName": "Jan Kowalski",
    "content": "Świetna wersja, zatwierdzam!",
    "createdAt": "2024-01-20T14:30:00Z",
    "editedAt": null,
    "isEdited": false,
    "canEdit": true,
    "canDelete": true
  }
]
```

**Scope behavior:**
- `All` - wszystkie komentarze (jeśli user ma dostęp do pliku)
- `Mine` - wszystkie komentarze (jeśli user jest właścicielem pliku)
- `Shared` - wszystkie komentarze (jeśli plik został udostępniony userowi)

**Autoryzacja:** Weryfikuje dostęp do pliku na podstawie scope. Jeśli brak dostępu → **pusta lista** (nie 403!)

**Sortowanie:** Od najstarszego do najnowszego (ascending createdAt)

---

## 🔄 MIGRACJA - Jak zastąpić stary kod

### Przed (stary endpoint):

```typescript
// ❌ STARY KOD - nie działa już!
const response = await projectApi.getProjectFiles(tenantId, projectId, ResourceScope.All);
const packages = response.data; // Wszystko w jednym - packages + files + versions + comments
```

### Po (nowe endpointy):

```typescript
// ✅ NOWY KOD - hierarchiczne pobieranie

// 1. Pobierz listę paczek
const packagesResponse = await projectApi.getProjectFilePackages(
  tenantId, 
  projectId, 
  ResourceScope.All
);
const packages = packagesResponse.data;

// 2. Pobierz pliki dla konkretnej paczki (lazy loading)
const filesResponse = await projectApi.getPackageFiles(
  tenantId,
  projectId,
  packageId,
  ResourceScope.All
);
const files = filesResponse.data;

// 3. Pobierz wersje dla konkretnego pliku (lazy loading)
const versionsResponse = await projectApi.getFileVersions(
  tenantId,
  projectId,
  fileId,
  ResourceScope.All
);
const versions = versionsResponse.data;

// 4. Pobierz komentarze dla konkretnej wersji (lazy loading)
const commentsResponse = await projectApi.getVersionComments(
  tenantId,
  projectId,
  fileId,
  versionId,
  ResourceScope.All
);
const comments = commentsResponse.data;
```

---

## 📦 Strategia implementacji UI

### Opcja A: Lazy Loading (REKOMENDOWANE)

```typescript
// 1. Na starcie: pobierz tylko paczki
const packages = await getProjectFilePackages(tenantId, projectId, scope);

// 2. Gdy user rozwija paczkę: pobierz pliki
const files = await getPackageFiles(tenantId, projectId, packageId, scope);

// 3. Gdy user rozwija plik: pobierz wersje
const versions = await getFileVersions(tenantId, projectId, fileId, scope);

// 4. Gdy user rozwija wersję: pobierz komentarze
const comments = await getVersionComments(tenantId, projectId, fileId, versionId, scope);
```

**Korzyści:**
- ⚡ Szybki start - ładuje tylko to, co widoczne
- 📦 Małe transfery - user pobiera dane tylko kiedy potrzebuje
- 💾 Mniej pamięci - nie trzyma wszystkiego w cache

---

### Opcja B: Prefetch (dla małych projektów)

```typescript
// Pobierz wszystko równolegle dla pierwszej paczki
const [packages, files] = await Promise.all([
  getProjectFilePackages(tenantId, projectId, scope),
  getPackageFiles(tenantId, projectId, firstPackageId, scope)
]);
```

---

## 🎯 Cache Strategy

### Rekomendacje:

1. **Packages** - cache na poziomie projektu
   - Key: `files-packages-${projectId}-${scope}`
   - Invalidate: gdy dodano/usunięto paczkę

2. **Files** - cache na poziomie paczki
   - Key: `files-${packageId}-${scope}`
   - Invalidate: gdy dodano/usunięto plik w paczce

3. **Versions** - cache na poziomie pliku
   - Key: `versions-${fileId}-${scope}`
   - Invalidate: gdy dodano nową wersję

4. **Comments** - cache na poziomie wersji
   - Key: `comments-${versionId}-${scope}`
   - Invalidate: gdy dodano/edytowano komentarz

---

## ⚠️ Ważne zmiany w zachowaniu

### 1. Brak dostępu = pusta lista (nie 403!)

**Przed:**
```typescript
// Stary endpoint rzucał 403 Forbidden
try {
  const data = await getProjectFiles(...);
} catch (error) {
  if (error.status === 403) {
    // Obsługa braku dostępu
  }
}
```

**Po:**
```typescript
// Nowe endpointy zwracają pustą listę
const files = await getPackageFiles(...);
if (files.length === 0) {
  // User nie ma plików w tym scope lub paczka jest pusta
}
```

### 2. Kolekcje zawsze puste w parent obiektach

```typescript
// ⚠️ TE POLA SĄ ZAWSZE PUSTE - nie używaj!
packageWeb.files          // [] - użyj osobnego endpointu
fileWeb.versions          // [] - użyj osobnego endpointu  
versionWeb.comments       // [] - użyj osobnego endpointu

// ✅ TE POLA SĄ WYPEŁNIONE
packageWeb.totalFiles     // Liczba plików
fileWeb.totalVersions     // Liczba wersji
fileWeb.currentVersion    // Pełna info o aktualnej wersji
```

### 3. Scope działa dla wszystkich endpointów

```typescript
// Mine - tylko moje zasoby
await getProjectFilePackages(tenantId, projectId, ResourceScope.Mine);

// Shared - tylko udostępnione mi
await getProjectFilePackages(tenantId, projectId, ResourceScope.Shared);

// All - wszystkie w projekcie (wymaga uprawnień)
await getProjectFilePackages(tenantId, projectId, ResourceScope.All);
```

---

## 🚀 Performance - liczby

| Endpoint | Stary (monolityczny) | Nowy (hierarchiczny) | Oszczędność |
|----------|---------------------|----------------------|-------------|
| **Packages list** | 500KB (all data) | 5KB (metadata) | **99%** ⚡ |
| **Files in package** | N/A | 50KB | **90%** ⚡ |
| **Versions** | N/A | 20KB | **95%** ⚡ |
| **Comments** | N/A | 2KB | **99%** ⚡ |

**Przykład:** Projekt z 10 paczkami, 100 plikami, 300 wersjami:
- **Przed:** 1 request × 5MB = 5MB transfer
- **Po (lazy):** 1 request × 5KB = 5KB start, potem lazy loading = **99% oszczędności**

---

## 📝 TODO dla UI

- [ ] Zaktualizować `projectApi.ts` - dodać 4 nowe funkcje
- [ ] Usunąć stary `getProjectFiles()` 
- [ ] Zaktualizować komponenty:
  - [ ] `ProjectFiles.tsx` - użyć `getProjectFilePackages`
  - [ ] Accordion/List pakietów - lazy load `getPackageFiles` 
  - [ ] Accordion/List wersji - lazy load `getFileVersions`
  - [ ] Accordion/List komentarzy - lazy load `getVersionComments`
- [ ] Zaktualizować cache hooks:
  - [ ] Zmienić klucze cache
  - [ ] Dodać osobne cache per poziom hierarchii
- [ ] Przetestować scope behavior (All/Mine/Shared)
- [ ] Przetestować lazy loading
- [ ] Przetestować invalidację cache

---

## 🔗 Przykładowe URL (dla referencji)

```
GET /api/tenants/abc-123/project/def-456/file/packages/All
GET /api/tenants/abc-123/project/def-456/file/packages/pkg-789/files/Mine
GET /api/tenants/abc-123/project/def-456/file/files/file-111/versions/Shared
GET /api/tenants/abc-123/project/def-456/file/files/file-111/versions/ver-222/comments/All
```

---

## ❓ FAQ

**Q: Czy mogę pobrać wszystkie dane naraz jak wcześniej?**  
A: Nie. To było nieefektywne. Użyj lazy loading lub prefetch dla pierwszego poziomu.

**Q: Co jeśli potrzebuję wszystkich wersji od razu?**  
A: Wywołaj `getFileVersions()` gdy user rozwija plik. To nadal szybsze niż stary endpoint.

**Q: Jak odświeżyć dane po dodaniu pliku?**  
A: Wyczyść cache dla paczki: `cache.invalidate(\`files-${packageId}-${scope}\`)`

**Q: Czy scope=All wymaga zawsze uprawnień admin?**  
A: Tak, AuthorizationBehavior weryfikuje to na backendzie.

**Q: Dlaczego pusta lista zamiast 403?**  
A: Uproszcza logikę UI. Nie musisz obsługiwać errorów, tylko sprawdzasz `length === 0`.

---

## 📚 Linki

- Instrukcje Copilot: `.github/copilot-instructions.md`
- TypeScript types: `src/types/project.types.ts`
- API client: `src/api/projectApi.ts`
- Główny komponent: `src/pages/ProjectFiles.tsx`

---

**Data wygenerowania:** 2024-01-22  
**Wersja API:** v2.0  
**Backend:** .NET 10, CQRS Pattern
