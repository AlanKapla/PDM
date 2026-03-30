/**
 * Mapowanie URL-i API na dane mock.
 * Każdy handler to obiekt { method, pattern, resolve }.
 * `resolve` otrzymuje nazwy grup przechwycone z URL + opcjonalne body requestu
 * i zwraca dane do odesłania jako odpowiedź.
 */

import { DEMO_CURRENT_USER } from "./data/users";
import {
  DEMO_USER_TENANT,
  DEMO_TENANT_DETAILS,
  DEMO_TENANT_BUDPROJEKT,
  DEMO_TENANT_INFRA,
  DEMO_TENANT_STUDIO,
  ALL_TENANT_DETAILS,
  DEMO_ANNA_RECEIVED_INVITATIONS,
} from "./data/organization";
import {
  DEMO_PROJECTS,
  DEMO_PROJECT_MEMBERS,
  DEMO_FILE_PACKAGES,
  DEMO_PROJECT_IDS,
} from "./data/projects";
import {
  DEMO_WORK_SCHEDULE_SUMMARIES,
  DEMO_WORK_SCHEDULE_DETAILS,
} from "./data/workSchedules";
import {
  DEMO_TEMPLATE_LIST,
  DEMO_TEMPLATE_DETAILS,
  DEMO_COST_ESTIMATE_LIST,
  DEMO_COST_ESTIMATE_DETAILS,
} from "./data/costEstimates";
import {
  DEMO_CHATS,
  DEMO_MESSAGES,
  DEMO_CHAT_CONTACTS,
} from "./data/chats";
import { DEMO_PROJECT_COSTS } from "./data/projectCosts";

export interface MockHandler {
  method: string;
  /** RegExp dopasowany do `config.url` (względem baseURL /api) */
  pattern: RegExp;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  resolve: (groups: Record<string, string>, body?: any) => unknown;
  status?: number;
}

// Helper: zwraca pustą listę dla nieznanych ID projektów
function filePackagesFor(projectId: string) {
  return DEMO_FILE_PACKAGES[projectId] ?? [];
}

function costEstimatesFor(projectId: string) {
  return DEMO_COST_ESTIMATE_LIST[projectId] ?? [];
}

function workScheduleSummariesFor(projectId: string) {
  return DEMO_WORK_SCHEDULE_SUMMARIES[projectId] ?? [];
}

export const MOCK_HANDLERS: MockHandler[] = [
  // ── Auth / User ──────────────────────────────────────────────────────────
  {
    method: "POST",
    pattern: /\/user\/sync-b2c/,
    resolve: () => null,
    status: 200,
  },
  {
    method: "GET",
    pattern: /\/user\/me$/,
    resolve: () => DEMO_CURRENT_USER,
  },

  // ── Tenant ───────────────────────────────────────────────────────────────
  {
    method: "GET",
    pattern: /\/tenant\/my-tenants/,
    // Anna jest adminem w ArchPlan i Studio Rewitalizacji, członkiem w pozostałych
    resolve: () => [DEMO_USER_TENANT, DEMO_TENANT_BUDPROJEKT, DEMO_TENANT_INFRA, DEMO_TENANT_STUDIO],
  },
  {
    method: "GET",
    pattern: /\/tenant\/admin-tenants/,
    // Anna jest adminem w ArchPlan i Studio Rewitalizacji
    resolve: () => [DEMO_USER_TENANT, DEMO_TENANT_STUDIO],
  },
  {
    method: "GET",
    pattern: /\/tenant\/invitations/,
    // Zaproszenia odebrane przez Annę od innych organizacji (status Pending)
    resolve: () => DEMO_ANNA_RECEIVED_INVITATIONS,
  },
  {
    method: "GET",
    pattern: /\/tenant\/(?<tenantId>[^/]+)\/details/,
    resolve: ({ tenantId }) => ALL_TENANT_DETAILS[tenantId] ?? DEMO_TENANT_DETAILS,
  },
  {
    method: "GET",
    pattern: /\/tenant\/(?<tenantId>[^/]+)\/members/,
    resolve: ({ tenantId }) => ALL_TENANT_DETAILS[tenantId]?.members ?? DEMO_TENANT_DETAILS.members,
  },
  {
    method: "PUT",
    pattern: /\/tenant\/active/,
    resolve: () => null,
    status: 200,
  },

  // ── Projekty ─────────────────────────────────────────────────────────────
  {
    method: "GET",
    pattern: /\/tenants\/(?<tenantId>[^/]+)\/project$/,
    resolve: () => DEMO_PROJECTS,
  },
  {
    method: "GET",
    pattern: /\/tenants\/(?<tenantId>[^/]+)\/project\/dictionary/,
    resolve: () =>
      Object.fromEntries(DEMO_PROJECTS.map((p) => [p.id, p.name])),
  },
  {
    method: "GET",
    pattern: /\/tenants\/(?<tenantId>[^/]+)\/project\/(?<projectId>[^/]+)$/,
    resolve: ({ projectId }) =>
      DEMO_PROJECTS.find((p) => p.id === projectId) ?? null,
  },
  {
    method: "GET",
    pattern: /\/tenants\/(?<tenantId>[^/]+)\/project\/(?<projectId>[^/]+)\/members/,
    resolve: ({ projectId }) => DEMO_PROJECT_MEMBERS[projectId] ?? [],
  },

  // ── Pliki ────────────────────────────────────────────────────────────────
  {
    method: "GET",
    pattern:
      /\/tenants\/[^/]+\/project\/(?<projectId>[^/]+)\/file\/packages\/(mine|shared|all)/,
    resolve: ({ projectId }) => filePackagesFor(projectId),
  },
  {
    method: "GET",
    pattern:
      /\/tenants\/[^/]+\/project\/(?<projectId>[^/]+)\/file\/packages\/(?<packageId>[^/]+)\/files\/(mine|shared|all)/,
    resolve: ({ projectId, packageId }) => {
      const pkgs = filePackagesFor(projectId);
      return pkgs.find((p) => p.id === packageId)?.files ?? [];
    },
  },
  {
    method: "GET",
    pattern:
      /\/tenants\/[^/]+\/project\/(?<projectId>[^/]+)\/file\/files\/(?<fileId>[^/]+)\/versions\/(mine|shared|all)/,
    resolve: ({ projectId, fileId }) => {
      const allFiles = filePackagesFor(projectId).flatMap((p) => p.files);
      return allFiles.find((f) => f.id === fileId)?.versions ?? [];
    },
  },

  // ── Harmonogramy prac ────────────────────────────────────────────────────
  {
    method: "GET",
    pattern:
      /\/tenants\/[^/]+\/project\/(?<projectId>[^/]+)\/work-schedule\/(mine|shared|all)$/,
    resolve: ({ projectId }) => workScheduleSummariesFor(projectId),
  },
  {
    method: "GET",
    pattern:
      /\/tenants\/[^/]+\/project\/(?<projectId>[^/]+)\/work-schedule\/details\/(?<scheduleId>[^/]+)$/,
    resolve: ({ scheduleId }) =>
      DEMO_WORK_SCHEDULE_DETAILS[scheduleId] ?? null,
  },

  // ── Szablony kosztorysów ──────────────────────────────────────────────────
  {
    method: "GET",
    pattern: /\/cost-estimate-template\/field-type-configurations/,
    resolve: () => ({}),
  },
  {
    method: "GET",
    pattern: /\/cost-estimate-template\/default-templates/,
    resolve: () => [],
  },
  {
    method: "GET",
    pattern: /\/cost-estimate-template\/(?<templateId>[^/]+)$/,
    resolve: ({ templateId }) => DEMO_TEMPLATE_DETAILS[templateId] ?? null,
  },
  {
    method: "GET",
    pattern: /\/cost-estimate-template$/,
    resolve: () => DEMO_TEMPLATE_LIST,
  },

  // ── Wydatki projektowe (SimpleCosts) ────────────────────────────────────
  {
    method: "GET",
    pattern: /\/tenants\/[^/]+\/project\/(?<projectId>[^/]+)\/cost\/(?<scope>mine|shared|all)$/,
    resolve: ({ projectId, scope }) => {
      const all = DEMO_PROJECT_COSTS[projectId] ?? [];
      if (scope === "mine")   return all.filter((c) => c.userId === DEMO_CURRENT_USER.id);
      if (scope === "shared") return all.filter((c) => c.sharedWithUserIds.includes(DEMO_CURRENT_USER.id!));
      return all; // "all"
    },
  },
  // Zaślepki mutujące na wydatkach
  {
    method: "POST",
    pattern: /\/tenants\/[^/]+\/project\/[^/]+\/cost$/,
    resolve: () => ({ id: `cost-demo-${Date.now()}` }),
    status: 201,
  },
  {
    method: "PUT",
    pattern: /\/tenants\/[^/]+\/project\/[^/]+\/cost\/[^/]+$/,
    resolve: () => null,
    status: 204,
  },
  {
    method: "DELETE",
    pattern: /\/tenants\/[^/]+\/project\/[^/]+\/cost\/[^/]+$/,
    resolve: () => null,
    status: 204,
  },

  // ── Kosztorysy ────────────────────────────────────────────────────────────
  {
    method: "GET",
    pattern:
      /\/tenants\/[^/]+\/projects\/(?<projectId>[^/]+)\/cost-estimate\/(?<scope>mine|shared|all)$/,
    resolve: ({ projectId, scope }) => {
      const all = costEstimatesFor(projectId);
      if (scope === "mine")   return all.filter((ce) => ce.ownerId === DEMO_CURRENT_USER.id);
      if (scope === "shared") return all.filter((ce) => ce.isSharedWithMe);
      return all; // "all"
    },
  },
  {
    method: "GET",
    pattern:
      /\/tenants\/[^/]+\/projects\/(?<projectId>[^/]+)\/cost-estimate\/details\/(?<estimateId>[^/]+)/,
    resolve: ({ estimateId }) =>
      DEMO_COST_ESTIMATE_DETAILS[estimateId] ?? null,
  },
  // Zaślepki mutujące (zawsze 200/OK bez efektu ubocznego)
  {
    method: "POST",
    pattern:
      /\/tenants\/[^/]+\/projects\/[^/]+\/cost-estimate$/,
    resolve: () => "ce-demo-new",
    status: 201,
  },
  {
    method: "PUT",
    pattern:
      /\/tenants\/[^/]+\/projects\/[^/]+\/cost-estimate\/[^/]+$/,
    resolve: () => null,
    status: 204,
  },

  // ── Powiadomienia ─────────────────────────────────────────────────────────
  {
    method: "GET",
    pattern: /\/notification/,
    resolve: () => [],
  },

  // ── Czat ─────────────────────────────────────────────────────────────────
  {
    // GET /chats — lista wszystkich czatów użytkownika
    method: "GET",
    pattern: /^\/chats$/,
    resolve: () => DEMO_CHATS,
  },
  {
    // GET /chats/contacts — kontakty pogrupowane wg projektów
    method: "GET",
    pattern: /^\/chats\/contacts$/,
    resolve: () => DEMO_CHAT_CONTACTS,
  },
  {
    // GET /chats/search?q=... — wyszukiwanie czatów
    method: "GET",
    pattern: /^\/chats\/search/,
    resolve: () => [],
  },
  {
    // GET /chats/by-members — czaty z konkretnymi członkami
    method: "GET",
    pattern: /^\/chats\/by-members/,
    resolve: () => DEMO_CHATS,
  },
  {
    // GET /chats/:chatId/messages — wiadomości czatu (obsługuje ?pageSize i ?before)
    method: "GET",
    pattern: /^\/chats\/(?<chatId>[^/]+)\/messages$/,
    resolve: ({ chatId }) => DEMO_MESSAGES[chatId] ?? [],
  },
  {
    // GET /chats/:chatId/members — członkowie czatu
    method: "GET",
    pattern: /^\/chats\/(?<chatId>[^/]+)\/members$/,
    resolve: ({ chatId }) => DEMO_CHATS.find((c) => c.id === chatId)?.members ?? [],
  },
  {
    // GET /chats/:chatId/available-members — dostępni do dodania
    method: "GET",
    pattern: /^\/chats\/(?<chatId>[^/]+)\/available-members$/,
    resolve: ({ chatId }) => {
      const chat = DEMO_CHATS.find((c) => c.id === chatId);
      const memberIds = new Set(chat?.members.map((m) => m.userId) ?? []);
      return DEMO_CHAT_CONTACTS.flatMap((g) =>
        g.members.filter((m) => !memberIds.has(m.userId))
      );
    },
  },
  {
    // POST /chats — utwórz nowy czat (zaślepka)
    method: "POST",
    pattern: /^\/chats$/,
    resolve: () => ({ id: `chat-demo-${Date.now()}`, isGroupChat: false }),
    status: 201,
  },
  {
    // POST /chats/:chatId/messages — wyślij wiadomość (zaślepka)
    method: "POST",
    pattern: /^\/chats\/(?<chatId>[^/]+)\/messages$/,
    resolve: () => ({ id: `msg-demo-${Date.now()}` }),
    status: 201,
  },
  {
    // PATCH /chats/:chatId — zmień nazwę czatu (zaślepka)
    method: "PATCH",
    pattern: /^\/chats\/(?<chatId>[^/]+)$/,
    resolve: () => null,
    status: 204,
  },
  {
    // PATCH /chats/:chatId/messages/:messageId — edytuj wiadomość (zaślepka)
    method: "PATCH",
    pattern: /^\/chats\/(?<chatId>[^/]+)\/messages\/(?<messageId>[^/]+)$/,
    resolve: () => null,
    status: 204,
  },
  {
    // POST /chats/:chatId/members — dodaj członka (zaślepka)
    method: "POST",
    pattern: /^\/chats\/(?<chatId>[^/]+)\/members$/,
    resolve: () => null,
    status: 201,
  },
  {
    // DELETE /chats/:chatId/members/:userId — usuń członka (zaślepka)
    method: "DELETE",
    pattern: /^\/chats\/(?<chatId>[^/]+)\/members\/(?<userId>[^/]+)$/,
    resolve: () => null,
    status: 204,
  },
  {
    // POST /chats/:chatId/leave — opuść czat (zaślepka)
    method: "POST",
    pattern: /^\/chats\/(?<chatId>[^/]+)\/leave$/,
    resolve: () => null,
    status: 204,
  },
  {
    // DELETE /chats/:chatId — usuń czat (zaślepka)
    method: "DELETE",
    pattern: /^\/chats\/(?<chatId>[^/]+)$/,
    resolve: () => null,
    status: 204,
  },
  {
    // DELETE /chats/:chatId/messages/:messageId — usuń wiadomość (zaślepka)
    method: "DELETE",
    pattern: /^\/chats\/(?<chatId>[^/]+)\/messages\/(?<messageId>[^/]+)$/,
    resolve: () => null,
    status: 204,
  },

  // ── Role ──────────────────────────────────────────────────────────────────
  {
    method: "GET",
    pattern: /\/role/,
    resolve: () => [
      { id: "0", code: "Admin", name: "Administrator" },
      { id: "1", code: "Member", name: "Członek" },
      { id: "2", code: "Editor", name: "Edytor" },
      { id: "3", code: "Viewer", name: "Przeglądający" },
    ],
  },

  // ── Moje prace (cross-tenant, widok osobisty) ────────────────────────────
  {
    method: "GET",
    pattern: /\/user\/assigned-works/,
    resolve: () => {
      // Zbieramy wszystkie harmonogramy ze wszystkich projektów i filtrujemy prace Anny
      const allProjectIds = Object.values(DEMO_PROJECT_IDS);
      const result: {
        tenantId: string;
        tenantName: string;
        projectId: string;
        projectName: string;
        workSchedules: unknown[];
      }[] = [];

      for (const projectId of allProjectIds) {
        const project = DEMO_PROJECTS.find((p) => p.id === projectId);
        if (!project) continue;

        const scheduleIds = Object.keys(DEMO_WORK_SCHEDULE_SUMMARIES).includes(projectId)
          ? DEMO_WORK_SCHEDULE_SUMMARIES[projectId].map((s) => s.id)
          : [];

        const schedulesWithWorks = scheduleIds.flatMap((schedId) => {
          const details = DEMO_WORK_SCHEDULE_DETAILS[schedId];
          if (!details) return [];

          const stages = (details.stages ?? []).flatMap((stage) => {
            const annaWorks = stage.works.filter((w) =>
              w.assignees.some((a) => a.userId === DEMO_CURRENT_USER.id)
            );
            if (annaWorks.length === 0) return [];
            return [
              {
                stageId: stage.id,
                stageName: stage.name,
                stageOrder: stage.order,
                works: annaWorks.map((w) => ({
                  workId: w.id,
                  workName: w.name,
                  workOrder: w.order,
                  colorRgb: w.colorRgb,
                  isClosed: w.isClosed,
                  periods: w.periods,
                })),
              },
            ];
          });

          if (stages.length === 0) return [];
          return [
            {
              workScheduleId: schedId,
              workScheduleName: details.name,
              workScheduleCreatedAt: details.createdAt,
              stages,
            },
          ];
        });

        if (schedulesWithWorks.length === 0) continue;

        result.push({
          tenantId: project.tenantId,
          tenantName: "ArchPlan Sp. z o.o.",
          projectId: project.id,
          projectName: project.name,
          workSchedules: schedulesWithWorks,
        });
      }

      return result;
    },
  },

  // ── Assigned works (moje prace w konkretnym projekcie) ───────────────────
  {
    method: "GET",
    pattern: /\/tenants\/[^/]+\/project\/(?<projectId>[^/]+)\/assigned-works/,
    resolve: ({ projectId }) => {
      const schedules = workScheduleSummariesFor(projectId);
      return schedules.flatMap((s) => {
        const details = DEMO_WORK_SCHEDULE_DETAILS[s.id];
        return (details?.stages ?? []).flatMap((stage) =>
          stage.works
            .filter((w) =>
              w.assignees.some((a) => a.userId === DEMO_CURRENT_USER.id!)
            )
            .map((w) => ({
              workId: w.id,
              workName: w.name,
              scheduleId: s.id,
              scheduleName: s.name,
              projectId,
              stageName: stage.name,
              colorRgb: w.colorRgb,
              isClosed: w.isClosed,
              periods: w.periods,
            }))
        );
      });
    },
  },

  // ── Prosty kosztorys (SimpleCosts) ────────────────────────────────────────
  {
    method: "GET",
    pattern:
      /\/tenants\/[^/]+\/projects\/(?<projectId>[^/]+)\/cost-estimate\/calendar/,
    resolve: ({ projectId }) =>
      costEstimatesFor(projectId).map((ce) => ({
        id: ce.id,
        name: ce.name,
        status: ce.status,
        totalNet: ce.totalNet,
        totalGross: ce.totalGross,
        createdAt: ce.createdAt,
      })),
  },
];

/**
 * Dopasowuje URL i metodę do handlera.
 * Zwraca { data, status } lub null gdy brak dopasowania.
 */
export function resolveHandler(
  method: string,
  url: string,
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  body?: any
): { data: unknown; status: number } | null {
  const normalizedMethod = method.toUpperCase();

  for (const handler of MOCK_HANDLERS) {
    if (handler.method !== normalizedMethod) continue;

    // Próba dopasowania regex z grupami nazwanymi
    const match = handler.pattern.exec(url);
    if (!match) continue;

    const groups: Record<string, string> = { ...match.groups };
    const data = handler.resolve(groups, body);
    return { data, status: handler.status ?? 200 };
  }

  // Nieznane endpointy – zwróć puste dane, nie rzucaj błędu
  console.warn(`[DEMO] Brak handlera dla ${method} ${url}`);
  return { data: null, status: 200 };
}
