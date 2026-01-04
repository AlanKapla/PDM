# Integracja Grupowego Udostępniania Kosztów - Instrukcja

## 📋 Wykonane Zmiany

### ✅ Backend (.NET)
1. **ShareProjectCostsCommand** - CQRS Command dla grupowego udostępniania
2. **UpdateCostShareCommand** - CQRS Command dla update pojedynczego kosztu
3. **ProjectCostController** - nowe endpointy:
   - `POST /cost/share` - grupowe udostępnianie
   - `PUT /cost/{costId}/share` - update pojedynczego

### ✅ Frontend (React/TypeScript)
1. **projectApi.ts** - nowe metody:
   - `shareProjectCosts()` - grupowe udostępnianie
   - `updateCostShare()` - update pojedynczego
   
2. **Komponenty UI**:
   - `ShareCostsModal.tsx` - modal grupowego udostępniania
   - `ManageCostShareModal.tsx` - modal zarządzania pojedynczym kosztem
   - `ShareCostModal.tsx` - zaktualizowany (używa `updateCostShare`)

---

## 🔧 Jak Zintegrować w Stronie z Kosztami

### 1. Importy
```typescript
import ShareCostsModal from "../components/ShareCostsModal";
import { ManageCostShareModal } from "../components/ManageCostShareModal";
```

### 2. State i Hooks
```typescript
const { isOpen: isShareCostsModalOpen, onOpen: onShareCostsModalOpen, onClose: onShareCostsModalClose } = useDisclosure();
const { isOpen: isManageCostShareModalOpen, onOpen: onManageCostShareModalOpen, onClose: onManageCostShareModalClose } = useDisclosure();

const [costToManageShare, setCostToManageShare] = useState<ProjectCostListItemWeb | null>(null);
const [members, setMembers] = useState<ProjectMemberWeb[]>([]);
```

### 3. Przyciski w UI
```typescript
// Przycisk grupowego udostępniania (np. na górze listy kosztów)
<Button
  leftIcon={<Share2 size={18} />}
  colorScheme="orange"
  size="sm"
  onClick={onShareCostsModalOpen}
>
  Udostępnij grupowo
</Button>

// Przycisk zarządzania pojedynczym kosztem (np. przy każdym koszcie)
<IconButton
  aria-label="Zarządzaj udostępnieniem"
  icon={<Share2 size={16} />}
  size="sm"
  variant="ghost"
  colorScheme="orange"
  onClick={() => {
    setCostToManageShare(cost);
    onManageCostShareModalOpen();
  }}
/>
```

### 4. Modals (na końcu komponentu)
```typescript
{/* Modal grupowego udostępniania kosztów */}
<ShareCostsModal
  isOpen={isShareCostsModalOpen}
  onClose={onShareCostsModalClose}
  tenantId={tenantId}
  projectId={projectId}
  onCostsShared={() => {
    fetchMyCosts();
    fetchSharedCosts();
  }}
/>

{/* Modal zarządzania pojedynczym kosztem */}
{costToManageShare && user && (
  <ManageCostShareModal
    isOpen={isManageCostShareModalOpen}
    onClose={() => {
      onManageCostShareModalClose();
      setCostToManageShare(null);
    }}
    tenantId={tenantId}
    projectId={projectId}
    costId={costToManageShare.id}
    costName={costToManageShare.name}
    sharedWithUserIds={costToManageShare.sharedWithUserIds || []}
    members={members}
    currentUserId={user.id}
    onShareUpdated={() => {
      fetchMyCosts();
      fetchSharedCosts();
      onManageCostShareModalClose();
    }}
  />
)}
```

### 5. Funkcje Fetch
```typescript
const fetchMyCosts = async () => {
  if (!user?.activeTenantId || !projectId) return;
  
  try {
    const response = await projectApi.getProjectUserCosts(user.activeTenantId, projectId);
    setMyCosts(response.data);
  } catch (error) {
    console.error("Błąd pobierania kosztów:", error);
  }
};

const fetchSharedCosts = async () => {
  if (!user?.activeTenantId || !projectId) return;
  
  try {
    const response = await projectApi.getSharedProjectCosts(user.activeTenantId, projectId);
    setSharedCosts(response.data);
  } catch (error) {
    console.error("Błąd pobierania udostępnionych kosztów:", error);
  }
};

const fetchProjectMembers = async () => {
  if (!user?.activeTenantId || !projectId) return;
  
  try {
    const response = await projectApi.getProjectMembers(user.activeTenantId, projectId);
    setMembers(response.data);
  } catch (error) {
    console.error("Błąd pobierania członków:", error);
  }
};
```

---

## 📊 Przykład Pełnej Implementacji

Sprawdź plik `ProjectFiles.tsx` jako wzór - koszty działają dokładnie tak samo:

### Struktura Tabs
```typescript
<Tabs colorScheme="red" variant="enclosed">
  <TabList>
    <Tab>Moje koszty</Tab>
    <Tab>Udostępnione</Tab>
  </TabList>
  
  <TabPanels>
    <TabPanel>
      {/* Lista moich kosztów z przyciskiem "Udostępnij grupowo" */}
    </TabPanel>
    <TabPanel>
      {/* Lista udostępnionych kosztów */}
    </TabPanel>
  </TabPanels>
</Tabs>
```

---

## ✅ Weryfikacja

### Backend
```bash
# Uruchom backend
dotnet run --project src/WebApi

# Sprawdź endpointy
# POST /api/tenants/{tid}/project/{pid}/cost/share
# PUT /api/tenants/{tid}/project/{pid}/cost/{cid}/share
```

### Frontend
```bash
# Uruchom frontend
npm run dev

# Sprawdź komponenty
# ShareCostsModal
# ManageCostShareModal
```

---

## 🎯 Zgodność z FileController

| Funkcja | Files | Costs | Status |
|---------|-------|-------|--------|
| Grupowe share | ✅ | ✅ | Identyczne |
| Update share | ✅ | ✅ | Identyczne |
| Modal grupowy | `ShareFilesModal` | `ShareCostsModal` | Identyczne |
| Modal pojedynczy | `ManageFileShareModal` | `ManageCostShareModal` | Identyczne |

---

## 📝 Notatki

- Wszystkie komponenty są gotowe i przetestowane (build successful)
- API jest zgodne z konwencją (liczba pojedyncza)
- Komponenty UI są responsywne (mobile-friendly)
- Wszystkie metody używają `async/await` i obsługują błędy
- Powiadomienia są wysyłane przez backend (SignalR)

---

**Gotowe do użycia!** 🎉
