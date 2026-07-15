// ============================================
//   Mock handlers — URL pattern → response
//   Symuluje opóźnienie sieciowe 200-500ms
// ============================================

import { MOCK_DATA, getCostEstimateDetailsById, getProjectFileData, getProjectCosts, getWorkSchedules, getWorkScheduleDetails, getDashboard } from "./mockData";

function delay(): Promise<void> {
  return new Promise((r) => setTimeout(r, 25));
}

type MockResponse = [number, unknown];

function ok<T>(data: T): MockResponse {
  return [200, data];
}

function noContent(): MockResponse {
  return [204, null];
}

/** Wyciągnij parametr z URL po indeksie segmentu */
function urlSegment(url: string, index: number): string {
  const parts = url.split("/").filter(Boolean);
  return parts[index] || "";
}

/** Wyciągnij projectId z URL /api/tenants/:tId/projects/:pId/... */
function extractProjectId(url: string): string {
  const parts = url.split("/").filter(Boolean);
  const projIdx = parts.indexOf("projects");
  if (projIdx >= 0 && projIdx + 1 < parts.length) {
    return parts[projIdx + 1];
  }
  return "";
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export async function handleMockRequest(method: string, url: string, data?: any): Promise<MockResponse> {
  const urlPath = url.replace(/^https?:\/\/[^/]+/, "");
  const pathOnly = urlPath.split("?")[0];

  await delay();

  // ============================================
  //  USER
  // ============================================
  if (pathOnly === "/api/user/me" && method === "get") {
    return ok(MOCK_DATA.userProfile);
  }
  if (pathOnly === "/api/user/sync-b2c" && method === "post") {
    return ok({ userId: MOCK_DATA.userProfile.id, message: "User synced successfully (mock)" });
  }
  if (pathOnly === "/api/user/assigned-works" && method === "get") {
    return ok([]);
  }
  if (pathOnly === "/api/user/auth-status" && method === "get") {
    return ok({ isAuthenticated: true, userId: MOCK_DATA.userProfile.id });
  }

  // ============================================
  //  TENANTS
  // ============================================
  if (pathOnly === "/api/tenants/my-tenants" && method === "get") {
    return ok(MOCK_DATA.tenants);
  }
  if (pathOnly === "/api/tenants/admin-tenants" && method === "get") {
    return ok(MOCK_DATA.tenants);
  }
  if (pathOnly === "/api/tenants/invitations" && method === "get") {
    return ok([]);
  }
  if (pathOnly === "/api/tenants/active" && method === "put") {
    return ok({ activeTenantId: MOCK_DATA.tenants[0].id });
  }
  // /tenants/:tenantId/details
  if (/^\/api\/tenants\/[^/]+\/details$/.test(pathOnly) && method === "get") {
    const tid = urlSegment(pathOnly, 2);
    const t = MOCK_DATA.tenants.find(x => x.id === tid) || MOCK_DATA.tenants[0];
    return ok({ id: t.id, name: t.name, createdAt: t.createdAt, isAdmin: true, isActive: true, members: [], invitations: [] });
  }
  // /tenants/:tenantId/members
  if (/^\/api\/tenants\/[^/]+\/members$/.test(pathOnly) && method === "get") {
    return ok([]);
  }

  // ============================================
  //  PROJECTS
  // ============================================
  if (/^\/api\/tenants\/[^/]+\/projects$/.test(pathOnly) && method === "get") {
    const tid = urlSegment(pathOnly, 2);
    return ok(MOCK_DATA.projects.filter(p => p.tenantId === tid));
  }
  if (/^\/api\/tenants\/[^/]+\/projects\/dictionary$/.test(pathOnly) && method === "get") {
    return ok(MOCK_DATA.projectDictionary);
  }
  // /tenants/:tId/projects/:pId
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+$/.test(pathOnly) && method === "get") {
    const pid = extractProjectId(pathOnly);
    const proj = MOCK_DATA.projects.find(p => p.id === pid);
    return ok(proj || MOCK_DATA.projects[0]);
  }

  // ============================================
  //  COST ESTIMATES
  // ============================================
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/cost-estimate\/\w+$/.test(pathOnly) && method === "get") {
    const pid = extractProjectId(pathOnly);
    return ok(MOCK_DATA.costEstimates.filter(ce => ce.projectId === pid));
  }
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/cost-estimate\/details\/[^/]+$/.test(pathOnly) && method === "get") {
    const ceId = pathOnly.split("/").pop() || "ce-001";
    return ok(getCostEstimateDetailsById(ceId));
  }
  // AI preview
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/cost-estimate\/generate-ai-preview$/.test(pathOnly) && method === "post") {
    return ok(MOCK_DATA.aiEstimateGenerate);
  }

  // ============================================
  //  WORK SCHEDULES
  // ============================================
  // GET /work-schedule/details/{wsId}
  const wsDetailMatch = pathOnly.match(/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/work-schedule\/details\/([^/]+)$/);
  if (wsDetailMatch && method === "get") {
    const wsId = wsDetailMatch[1];
    return ok(getWorkScheduleDetails(wsId));
  }
  // GET /work-schedule/{scope} — all|mine|shared|PendingApproval
  const wsListMatch = pathOnly.match(/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/work-schedule\/(all|mine|shared|PendingApproval)$/);
  if (wsListMatch && method === "get") {
    const pid = extractProjectId(pathOnly);
    const scope = wsListMatch[1].toLowerCase();
    return ok(getWorkSchedules(pid, scope));
  }
  // POST /work-schedule — create
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/work-schedule$/.test(pathOnly) && method === "post") {
    return [201, "ws-new-001"];
  }
  // PUT /work-schedule/{wsId}/dependencies — zwraca zaktualizowane szczegóły
  const wsDepsMatch = pathOnly.match(/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/work-schedule\/([^/]+)\/dependencies$/);
  if (wsDepsMatch && method === "put") {
    return ok(getWorkScheduleDetails(wsDepsMatch[1]));
  }
  // PUT/DELETE /work-schedule/{wsId} — rename / delete
  const wsIdMatch = pathOnly.match(/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/work-schedule\/([^/]+)$/);
  if (wsIdMatch && (method === "put" || method === "delete")) {
    return noContent();
  }
  // POST /work-schedule/{wsId}/sync-with-estimate
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/work-schedule\/[^/]+\/sync-with-estimate$/.test(pathOnly) && method === "post") {
    return noContent();
  }
  // POST /work-schedule/{wsId}/generate-from-ai
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/work-schedule\/[^/]+\/generate-from-ai$/.test(pathOnly) && method === "post") {
    return ok(getWorkScheduleDetails("ws-001"));
  }
  // Pozostałe mutacje harmonogramu (etapy, prace, okresy, komentarze) — 204
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/work-schedule\/[^/]+/.test(pathOnly) && method !== "get") {
    return noContent();
  }

  // ============================================
  //  PROJECT COSTS
  // ============================================
  const costMatch = pathOnly.match(/^\/api\/tenants\/[^/]+\/projects\/([^/]+)\/cost\/(all|mine|PendingApproval)$/);
  if (costMatch && method === "get") {
    const projectId = costMatch[1];
    const scope = costMatch[2];
    return ok(getProjectCosts(projectId, scope));
  }

  // ============================================
  //  DASHBOARD
  // ============================================
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/dashboard$/.test(pathOnly) && method === "get") {
    const pid = extractProjectId(pathOnly);
    return ok(getDashboard(pid));
  }

  // ============================================
  //  COST TRACKERS
  // ============================================
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/cost-trackers\/by-project$/.test(pathOnly) && method === "get") {
    return ok([{
      id: "ct-001", projectId: "p-001",
      budgetNet: 12450000, budgetGross: 15313500,
      trackedNet: 1587100, trackedGross: 1952133,
      costs: MOCK_DATA.trackedCosts,
    }]);
  }
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/cost-trackers\/by-estimate\/[^/]+$/.test(pathOnly) && method === "get") {
    return ok([{
      id: "ct-001", projectId: "p-001", costEstimateId: "ce-001",
      budgetNet: 12450000, budgetGross: 15313500,
      trackedNet: 1407000, trackedGross: 1730610,
      costs: MOCK_DATA.trackedCosts.filter(tc => tc.sourceType === "EstimateItem" || tc.sourceType === "ProjectAdditional"),
    }]);
  }
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/cost-trackers\/costs$/.test(pathOnly) && method === "get") {
    return ok(MOCK_DATA.trackedCosts);
  }

  // ============================================
  //  CHATS
  // ============================================
  if (/^\/api\/tenants\/[^/]+\/chats$/.test(pathOnly) && method === "get") {
    const tid = urlSegment(pathOnly, 2);
    return ok(MOCK_DATA.chats.filter(c => c.tenantId === tid));
  }
  if (pathOnly === "/api/chats/direct" && method === "get") {
    return ok(MOCK_DATA.chats.filter(c => !c.isGroupChat));
  }
  if (/^\/api\/tenants\/[^/]+\/chats\/[^/]+$/.test(pathOnly) && method === "get") {
    const chatId = pathOnly.split("/").pop() || "";
    const chat = MOCK_DATA.chats.find(c => c.id === chatId);
    return ok(chat || MOCK_DATA.chats[0]);
  }
  if (/^\/api\/tenants\/[^/]+\/chats\/[^/]+\/messages$/.test(pathOnly) && method === "get") {
    const parts = pathOnly.split("/");
    const chatId = parts[parts.indexOf("chats") + 1];
    const msgs = (MOCK_DATA.messages as Record<string, unknown[]>)[chatId] || [];
    return ok(msgs);
  }

  // ============================================
  //  CONTRACTORS
  // ============================================
  if (/^\/api\/tenants\/[^/]+\/contractors$/.test(pathOnly) && method === "get") {
    return ok(MOCK_DATA.contractors);
  }
  if (/^\/api\/tenants\/[^/]+\/contractors\/[^/]+$/.test(pathOnly) && method === "get") {
    const cid = pathOnly.split("/").pop() || "";
    const c = MOCK_DATA.contractors.find(x => x.id === cid);
    return ok(c || MOCK_DATA.contractors[0]);
  }

  // ============================================
  //  PROJECT FILES
  // ============================================
  // 1. Pobierz paczki plików: GET ...file/packages/{scope}
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/file\/packages\/(all|mine|shared|PendingApproval)$/.test(pathOnly) && method === "get") {
    const projectId = extractProjectId(pathOnly);
    const fileData = getProjectFileData(projectId);
    return ok(fileData.packages);
  }
  // 2. Pobierz pliki w paczce: GET ...file/packages/{pkgId}/files/{scope}
  const pkgFilesMatch = pathOnly.match(/\/file\/packages\/([^/]+)\/files\/(all|mine|shared|PendingApproval)$/);
  if (pkgFilesMatch && method === "get") {
    const pkgId = pkgFilesMatch[1];
    const projectId = extractProjectId(pathOnly);
    const fileData = getProjectFileData(projectId);
    // Szukaj paczki we wszystkich zagnieżdżeniach
    const findPkg = (pkgs: any[]): any | undefined => {
      for (const p of pkgs) {
        if (p.id === pkgId) return p;
        if (p.subCatalogs?.length) {
          const found = findPkg(p.subCatalogs);
          if (found) return found;
        }
      }
      return undefined;
    };
    const pkg = findPkg(fileData.packages);
    return ok(pkg?.files || []);
  }
  // 3. Pobierz wersje pliku: GET ...file/files/{fileId}/versions/{scope}
  const fileVersionsMatch = pathOnly.match(/\/file\/files\/([^/]+)\/versions\/(all|mine|shared|PendingApproval)$/);
  if (fileVersionsMatch && method === "get") {
    const fileId = fileVersionsMatch[1];
    const projectId = extractProjectId(pathOnly);
    const fileData = getProjectFileData(projectId);
    // Szukaj pliku we wszystkich paczkach
    const findFile = (pkgs: any[]): any | undefined => {
      for (const p of pkgs) {
        const f = p.files?.find((x: any) => x.id === fileId);
        if (f) return f;
        if (p.subCatalogs?.length) {
          const found = findFile(p.subCatalogs);
          if (found) return found;
        }
      }
      return undefined;
    };
    const file = findFile(fileData.packages);
    return ok(file?.versions || []);
  }
  // 4. Pobierz komentarze do wersji: GET ...file/files/{fileId}/versions/{versionId}/comments/{scope}
  const commentsMatch = pathOnly.match(/\/file\/files\/([^/]+)\/versions\/([^/]+)\/comments\/(all|mine|shared|PendingApproval)$/);
  if (commentsMatch && method === "get") {
    const versionId = commentsMatch[2];
    const projectId = extractProjectId(pathOnly);
    const fileData = getProjectFileData(projectId);
    // Szukaj wersji we wszystkich plikach
    const findVersion = (pkgs: any[]): any | undefined => {
      for (const p of pkgs) {
        for (const f of (p.files || [])) {
          const v = f.versions?.find((x: any) => x.id === versionId);
          if (v) return v;
        }
        if (p.subCatalogs?.length) {
          const found = findVersion(p.subCatalogs);
          if (found) return found;
        }
      }
      return undefined;
    };
    const version = findVersion(fileData.packages);
    return ok(version?.comments || []);
  }

  // ============================================
  //  AI COST IMPORT
  // ============================================
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/ai\/cost\/parse\/\w+$/.test(pathOnly) && method === "post") {
    return ok(MOCK_DATA.aiCostImport);
  }
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/ai\/cost\/import\/pending\/count$/.test(pathOnly) && method === "get") {
    return ok({ pendingCount: 0, errorCount: 0, duplicateCount: 0 });
  }
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/ai\/cost\/import\/pending\/accept-all$/.test(pathOnly) && method === "post") {
    return noContent();
  }
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/ai\/cost\/import\/pending\/[^/]+\/accept$/.test(pathOnly) && method === "post") {
    return noContent();
  }
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/ai\/cost\/import\/pending\/[^/]+$/.test(pathOnly) && method === "get") {
    return ok([]);
  }
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/ai\/cost\/import\/pending$/.test(pathOnly) && method === "get") {
    return ok([]);
  }
  if (/^\/api\/tenants\/[^/]+\/projects\/[^/]+\/ai\/cost\/import\/batch$/.test(pathOnly) && method === "post") {
    return ok({ batchId: "mock-batch", itemCount: 0, status: "Queued" });
  }

  // ============================================
  //  NOTIFICATIONS
  // ============================================
  if (pathOnly === "/api/notification" && method === "get") {
    return ok([]);
  }
  if (pathOnly === "/api/notification/unread" && method === "get") {
    return ok([]);
  }
  if (pathOnly === "/api/notification/unread-counter" && method === "get") {
    return ok({ count: 0 });
  }

  // ============================================
  //  DICTIONARY
  // ============================================
  if (pathOnly === "/api/dictionary/currencies" && method === "get") {
    return ok([{ code: "PLN", symbol: "zł", name: "Złoty polski" }, { code: "EUR", symbol: "€", name: "Euro" }]);
  }

  // ============================================
  //  FALLBACK
  // ============================================
  console.warn(`[PDMDemo] Unhandled mock request: ${method} ${pathOnly}`);
  return [200, []];
}

// Pomocnicza — data (duplikat z mockData dla lokalnego użycia)
function date(d: string): string { return d + "T08:00:00Z"; }
