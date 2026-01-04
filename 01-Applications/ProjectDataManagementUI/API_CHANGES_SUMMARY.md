# API Changes Summary - Backend to Frontend

## ✅ FINALNA WERYFIKACJA ROUTINGU - PEŁNA SPÓJNOŚĆ (liczba pojedyncza)!

### **🎯 Konwencja: WSZYSTKIE zasoby w liczbie pojedynczej**

| Controller | Backend Route | UI Route | Status |
|-----------|---------------|----------|---------|
| **TenantController** | `/api/tenant` | `/tenant` | ✅ OK |
| **UserController** | `/api/user` | `/user` | ✅ OK |
| **RoleController** | `/api/role` | `/role` | ✅ OK |
| **NotificationController** | `/api/notification` | `/notification` | ✅ **POPRAWIONO** |
| **ProjectController** | `/api/tenants/{id}/project` | `/tenants/{id}/project` | ✅ **POPRAWIONO** |
| **FileController** | `/api/.../project/{id}/file` | `/.../project/{id}/file` | ✅ **POPRAWIONO** |
| **ChatController** | `/api/.../project/{id}/chat` | `/.../project/{id}/chat` | ✅ **POPRAWIONO** |
| **ProjectCostController** | `/api/.../project/{id}/cost` | `/.../project/{id}/cost` | ✅ **ROZSZERZONO** |
| **WorkScheduleController** | `/api/.../project/{id}/work-schedule` | `/.../project/{id}/work-schedule` | ✅ **POPRAWIONO** |
| **CostEstimateController** | `/api/.../project/{id}/cost-estimate` | `/.../project/{id}/cost-estimate` | ✅ **POPRAWIONO** |
| **CostEstimateTemplateController** | `/api/cost-estimate-template` | `/cost-estimate-template` | ✅ OK |

---

## 🔧 Ostatnie Poprawki

### **NotificationController**
**Problem:** UI używało `/Notification` (wielka litera) i `/notifications` (liczba mnoga)
**Rozwiązanie:** 
```typescript
// PRZED (błędne):
GET /notifications/unread
GET /notifications?limit=50
PUT /notifications/{id}/mark-as-read

// PO (poprawne):
GET /notification/unread
GET /notification?limit=50
PUT /notification/{id}/mark-as-read
```

### **ProjectCostController**
**Problem:** Duplikaty kodu i błędy składniowe
**Rozwiązanie:** Wyczyszczono controller - usunięto duplikaty i naprawiono błędy

---

## ✨ Nowe funkcje - Grupowe udostępnianie kosztów

### **🎯 Inspiracja: FileController**

Dodano funkcjonalność analogiczną do `FileController`:

#### **1. Grupowe udostępnianie (POST /share)**
Udostępnij wiele kosztów wielu użytkownikom na raz:
```typescript
POST /api/tenants/{tenantId}/project/{projectId}/cost/share
{
  "projectCostIds": ["guid1", "guid2", "guid3"],
  "sharedWithUserIds": ["userId1", "userId2"]
}
```

#### **2. Update udostępnienia pojedynczego kosztu (PUT /{costId}/share)**
Zarządzaj dostępem do konkretnego kosztu (dodaj/usuń użytkowników):
```typescript
PUT /api/tenants/{tenantId}/project/{projectId}/cost/{costId}/share
{
  "sharedWithUserIds": ["userId1", "userId3"]  // userId2 zostanie usunięty
}
```

---

## Zmiany wykonane - Project Costs

### ✅ Backend (CQRS Commands)

**Nowe:**
1. ✅ **ShareProjectCostsCommand** - grupowe udostępnianie wielu kosztów
   - Command
   - CommandHandler
   - CommandValidator
   
2. ✅ **UpdateCostShareCommand** - update pojedynczego kosztu
   - Command (był ShareProjectCostCommand)
   - CommandHandler
   - CommandValidator

**Usunięte:**
- ❌ ShareProjectCostCommand → **przemianowany na** UpdateCostShareCommand

---

### ✅ Backend (Controller)

**ProjectCostController - nowe endpointy:**
```csharp
// Grupowe udostępnianie
[HttpPost("share")]
ShareProjectCosts(...)

// Update pojedynczego kosztu
[HttpPut("{costId}/share")]  // było POST
UpdateCostShare(...)
```

**Routing:**
- `POST .../cost/share` → grupowe udostępnianie
- `PUT .../cost/{costId}/share` → update pojedynczego (było POST)

---

### ✅ Frontend (projectApi.ts)

**Nowe metody:**
```typescript
// Grupowe udostępnianie
shareProjectCosts(tenantId, projectId, costIds[], userIds[])

// Update pojedynczego
updateCostShare(tenantId, projectId, costId, userIds[])
```

**Zmienione:**
- ~~`shareProjectCost`~~ → **`updateCostShare`** (PUT zamiast POST)
- Dodano **`shareProjectCosts`** (grupowe - POST)

---

### ✅ Frontend (Komponenty UI)

**Nowe komponenty:**
1. ✅ **ShareCostsModal.tsx** - grupowe udostępnianie kosztów
   - Wybór wielu kosztów
   - Wybór wielu użytkowników
   - Analogiczny do ShareFilesModal

2. ✅ **ManageCostShareModal.tsx** - zarządzanie pojedynczym kosztem
   - Dodawanie/usuwanie użytkowników
   - Analogiczny do ManageFileShareModal

**Zaktualizowane komponenty:**
1. ✅ **ShareCostModal.tsx** - zmienione z `shareProjectCost` na `updateCostShare`
2. ✅ **notificationApi.ts** - zmienione z `/notifications` na `/notification`

---

## Podsumowanie - Project Costs vs Files

| Funkcja | FileController | ProjectCostController | Status |
|---------|---------------|----------------------|---------|
| **Grupowe share** | POST `/file/share` | POST `/cost/share` | ✅ Dodano |
| **Update share** | PUT `/file/{id}/share` | PUT `/cost/{id}/share` | ✅ Dodano |
| **UI - grupowe** | `ShareFilesModal` | `ShareCostsModal` | ✅ Dodano |
| **UI - update** | `ManageFileShareModal` | `ManageCostShareModal` | ✅ Dodano |
| **API - grupowe** | `shareFiles(...)` | `shareProjectCosts(...)` | ✅ Dodano |
| **API - update** | `updateFileShare(...)` | `updateCostShare(...)` | ✅ Dodano |

---

## Jak używać (UI)

### **Grupowe udostępnianie kosztów:**
```typescript
import ShareCostsModal from "../components/ShareCostsModal";

// W komponencie:
<ShareCostsModal
  isOpen={isShareCostsModalOpen}
  onClose={onShareCostsModalClose}
  tenantId={tenantId}
  projectId={projectId}
  onCostsShared={fetchCosts}
/>
```

### **Zarządzanie pojedynczym kosztem:**
```typescript
import { ManageCostShareModal } from "../components/ManageCostShareModal";

// W komponencie:
<ManageCostShareModal
  isOpen={isManageCostShareModalOpen}
  onClose={onManageCostShareModalClose}
  tenantId={tenantId}
  projectId={projectId}
  costId={cost.id}
  costName={cost.name}
  sharedWithUserIds={cost.sharedWithUserIds}
  members={members}
  currentUserId={user.id}
  onShareUpdated={fetchCosts}
  />
```

---

## Podsumowanie ogólne

### ✅ Ukończone:
- [x] **Wszystkie kontrolery** - zmienione na liczbę pojedynczą
- [x] **Wszystkie API w UI** - zsynchronizowane z backend
- [x] **100% spójność** - jedna konwencja w całym projekcie
- [x] **Brak `[controller]`** - wszystkie routing explicit
- [x] **Backend ↔ Frontend** - pełna synchronizacja
- [x] **Project Costs - Backend** - dodano grupowe udostępnianie (jak Files)
- [x] **Project Costs - Frontend API** - dodano shareProjectCosts i updateCostShare
- [x] **Project Costs - Frontend UI** - dodano ShareCostsModal i ManageCostShareModal
- [x] **Notifications** - naprawiono routing (notification zamiast Notification/notifications)
- [x] **ProjectCostController** - naprawiono błędy składniowe i duplikaty

### 🎯 Osiągnięcia:
- ✅ **Pełna spójność** - wszystkie zasoby w liczbie pojedynczej
- ✅ **Przewidywalność** - łatwe zgadywanie URL
- ✅ **Łatwiejsze utrzymanie** - jedna jasna zasada
- ✅ **REST best practices** - konsekwentna konwencja
- ✅ **Analogiczna funkcjonalność** - Files i Costs działają tak samo
- ✅ **Gotowe komponenty UI** - ShareCostsModal i ManageCostShareModal
- ✅ **Naprawione błędy** - Notifications i ProjectCostController

### 🔍 Przykłady URL:
```
# Powiadomienia (poprawione)
GET    /api/notification/unread
GET    /api/notification?limit=50
PUT    /api/notification/{id}/mark-as-read

# Koszty - grupowe udostępnianie
POST   /api/tenants/{tid}/project/{pid}/cost/share
Body: { projectCostIds: [...], sharedWithUserIds: [...] }

# Koszty - update pojedynczego
PUT    /api/tenants/{tid}/project/{pid}/cost/{cid}/share
Body: { sharedWithUserIds: [...] }

# Pliki - analogicznie
POST   /api/tenants/{tid}/project/{pid}/file/share
PUT    /api/tenants/{tid}/project/{pid}/file/{fid}/share
```

---

**Routing jest teraz w 100% spójny - wszystkie zasoby używają liczby pojedynczej + Project Costs mają pełną funkcjonalność grupowego udostępniania z gotowymi komponentami UI + Naprawiono błędy w notyfikacjach i ProjectCostController!** 🎉
