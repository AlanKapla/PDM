/**
 * Dane demonstracyjne harmonogramów prac.
 */

import type {
  WorkScheduleSummaryWeb,
  WorkScheduleDetailsWeb,
} from "../../types/workSchedule.types";
import { DEMO_TENANT_ID, DEMO_USERS } from "./users";
import { DEMO_PROJECT_IDS } from "./projects";

/** Mapuje userId na wyświetlaną nazwę użytkownika */
const USER_NAMES: Record<string, string> = {
  [DEMO_USERS.anna.userId]:      "Anna Kowalska",
  [DEMO_USERS.piotr.userId]:     "Piotr Wiśniewski",
  [DEMO_USERS.marta.userId]:     "Marta Nowak",
  [DEMO_USERS.tomasz.userId]:    "Tomasz Zając",
  [DEMO_USERS.katarzyna.userId]: "Katarzyna Wójcik",
};

const assignees = (...ids: string[]) =>
  ids.map((uid) => ({ userId: uid, userName: USER_NAMES[uid] ?? uid }));

/**
 * Zwraca datę w formacie YYYY-MM-DD przesuniętą o `days` dni od dziś.
 * Dzięki temu harmonogram zawsze wygląda aktualnie.
 */
function d(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toISOString().slice(0, 10);
}

// ===== HARMONOGRAM: Centrum Biurowe Nowa Brama =====

const SCHED_BIUROWE_ID = "sched-biurowe-001";
const SCHED_DROGOWA_ID = "sched-drogowa-001";
const SCHED_HOTEL_ID = "sched-hotel-001";

export const DEMO_WORK_SCHEDULE_SUMMARIES: Record<string, WorkScheduleSummaryWeb[]> = {
  [DEMO_PROJECT_IDS.biurowe]: [
    {
      id: SCHED_BIUROWE_ID,
      name: "Harmonogram główny 2024",
      createdAt: "2023-02-15T08:00:00Z",
      createdByUserId: DEMO_USERS.anna.userId,
      createdByUserName: "Anna Kowalska",
    },
  ],
  [DEMO_PROJECT_IDS.drogowa]: [
    {
      id: SCHED_DROGOWA_ID,
      name: "Harmonogram robót drogowych",
      createdAt: "2023-05-20T09:00:00Z",
      createdByUserId: DEMO_USERS.piotr.userId,
      createdByUserName: "Piotr Wiśniewski",
    },
  ],
  [DEMO_PROJECT_IDS.hotel]: [
    {
      id: SCHED_HOTEL_ID,
      name: "Harmonogram rozbudowy – edycja 2",
      createdAt: "2023-08-25T10:00:00Z",
      createdByUserId: DEMO_USERS.piotr.userId,
      createdByUserName: "Piotr Wiśniewski",
    },
  ],
};

export const DEMO_WORK_SCHEDULE_DETAILS: Record<string, WorkScheduleDetailsWeb> = {
  [SCHED_BIUROWE_ID]: {
    id: SCHED_BIUROWE_ID,
    tenantId: DEMO_TENANT_ID,
    projectId: DEMO_PROJECT_IDS.biurowe,
    name: "Harmonogram główny 2024",
    createdAt: "2023-02-15T08:00:00Z",
    createdByUserId: DEMO_USERS.anna.userId,
    createdByUserName: "Anna Kowalska",
    stages: [
      {
        id: "stage-biu-001",
        name: "Etap I – Prace przygotowawcze i fundamenty",
        order: 1,
        works: [
          {
            id: "work-biu-001",
            name: "Wytyczenie geodezyjne",
            order: 1,
            colorRgb: "#4CAF50",
            isClosed: true,
            periods: [
              { id: "per-biu-001", startDate: d(-180), endDate: d(-163), isClosed: true },
            ],
            assignees: assignees(DEMO_USERS.tomasz.userId),
            comments: [],
          },
          {
            id: "work-biu-002",
            name: "Roboty ziemne i wykop",
            order: 2,
            colorRgb: "#FF9800",
            isClosed: true,
            periods: [
              { id: "per-biu-002", startDate: d(-158), endDate: d(-130), isClosed: true },
            ],
            assignees: assignees(DEMO_USERS.tomasz.userId, DEMO_USERS.marta.userId),
            comments: [
              {
                id: "cmt-biu-001",
                content: "Napotkano warstwę gruntu nienośnego na głębokości 3,2m – zmiana projektu fundamentów.",
                createdAt: "2024-01-30T14:00:00Z",
                createdByUserId: DEMO_USERS.tomasz.userId,
                createdByUserName: "Tomasz Zając",
              },
            ],
          },
          {
            id: "work-biu-003",
            name: "Ławy i płyta fundamentowa",
            order: 3,
            colorRgb: "#F44336",
            isClosed: true,
            periods: [
              { id: "per-biu-003", startDate: d(-125), endDate: d(-90), isClosed: true },
            ],
            assignees: assignees(DEMO_USERS.marta.userId),
            comments: [],
          },
        ],
      },
      {
        id: "stage-biu-002",
        name: "Etap II – Konstrukcja i stropy",
        order: 2,
        works: [
          {
            id: "work-biu-004",
            name: "Słupy i ściany żelbetowe – parter",
            order: 1,
            colorRgb: "#2196F3",
            isClosed: true,
            periods: [
              { id: "per-biu-004", startDate: d(-85), endDate: d(-60), isClosed: true },
            ],
            assignees: assignees(DEMO_USERS.marta.userId, DEMO_USERS.tomasz.userId),
            comments: [],
          },
          {
            id: "work-biu-005",
            name: "Slaby stropowe – kondygnacje 1–6",
            order: 2,
            colorRgb: "#9C27B0",
            isClosed: false,
            periods: [
              { id: "per-biu-005", startDate: d(-55), endDate: d(+60), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.marta.userId),
            comments: [],
          },
          {
            id: "work-biu-006",
            name: "Klatki schodowe i szyb windy",
            order: 3,
            colorRgb: "#00BCD4",
            isClosed: false,
            periods: [
              { id: "per-biu-006", startDate: d(-45), endDate: d(+75), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.tomasz.userId),
            comments: [],
          },
        ],
      },
      {
        id: "stage-biu-003",
        name: "Etap III – Elewacje i wykończenie",
        order: 3,
        works: [
          {
            id: "work-biu-007",
            name: "Montaż fasady aluminiowo-szklanej",
            order: 1,
            colorRgb: "#607D8B",
            isClosed: false,
            periods: [
              { id: "per-biu-007", startDate: d(+80), endDate: d(+170), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.piotr.userId),
            comments: [],
          },
          {
            id: "work-biu-008",
            name: "Wykończenie wnętrz – lobby i biura",
            order: 2,
            colorRgb: "#FF5722",
            isClosed: false,
            periods: [
              { id: "per-biu-008", startDate: d(+100), endDate: d(+200), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.marta.userId, DEMO_USERS.katarzyna.userId),
            comments: [],
          },
        ],
      },
    ],
  },

  [SCHED_DROGOWA_ID]: {
    id: SCHED_DROGOWA_ID,
    tenantId: DEMO_TENANT_ID,
    projectId: DEMO_PROJECT_IDS.drogowa,
    name: "Harmonogram robót drogowych",
    createdAt: "2023-05-20T09:00:00Z",
    createdByUserId: DEMO_USERS.piotr.userId,
    createdByUserName: "Piotr Wiśniewski",
    stages: [
      {
        id: "stage-drg-001",
        name: "Faza A – Przygotowanie pasa drogowego",
        order: 1,
        works: [
          {
            id: "work-drg-001",
            name: "Wycinka drzew i krzewów",
            order: 1,
            colorRgb: "#8BC34A",
            isClosed: true,
            periods: [
              { id: "per-drg-001", startDate: d(-200), endDate: d(-170), isClosed: true },
            ],
            assignees: assignees(DEMO_USERS.tomasz.userId),
            comments: [],
          },
          {
            id: "work-drg-002",
            name: "Rozbiórka nawierzchni istniejącej",
            order: 2,
            colorRgb: "#795548",
            isClosed: true,
            periods: [
              { id: "per-drg-002", startDate: d(-165), endDate: d(-130), isClosed: true },
            ],
            assignees: assignees(DEMO_USERS.tomasz.userId, DEMO_USERS.anna.userId),
            comments: [],
          },
        ],
      },
      {
        id: "stage-drg-002",
        name: "Faza B – Podbudowa i nawierzchnia",
        order: 2,
        works: [
          {
            id: "work-drg-003",
            name: "Podbudowa z kruszywa łamanego",
            order: 1,
            colorRgb: "#FF9800",
            isClosed: false,
            periods: [
              { id: "per-drg-003", startDate: d(-30), endDate: d(+40), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.anna.userId),
            comments: [],
          },
          {
            id: "work-drg-004",
            name: "Podbudowa z betonu cementowego (klasa C25/30)",
            order: 2,
            colorRgb: "#607D8B",
            isClosed: false,
            periods: [
              { id: "per-drg-004", startDate: d(+45), endDate: d(+120), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.anna.userId, DEMO_USERS.tomasz.userId),
            comments: [],
          },
          {
            id: "work-drg-005",
            name: "Ułożenie warstwy ścieralnej z SMA 11",
            order: 3,
            colorRgb: "#212121",
            isClosed: false,
            periods: [
              { id: "per-drg-005", startDate: d(+125), endDate: d(+175), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.tomasz.userId),
            comments: [],
          },
        ],
      },
      {
        id: "stage-drg-003",
        name: "Faza C – Oznakowanie i bezpieczeństwo",
        order: 3,
        works: [
          {
            id: "work-drg-006",
            name: "Montaż barier energochłonnych",
            order: 1,
            colorRgb: "#F44336",
            isClosed: false,
            periods: [
              { id: "per-drg-006", startDate: d(+180), endDate: d(+215), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.piotr.userId),
            comments: [],
          },
          {
            id: "work-drg-007",
            name: "Oznakowanie poziome i pionowe",
            order: 2,
            colorRgb: "#FFEB3B",
            isClosed: false,
            periods: [
              { id: "per-drg-007", startDate: d(+220), endDate: d(+245), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.piotr.userId),
            comments: [],
          },
        ],
      },
    ],
  },

  [SCHED_HOTEL_ID]: {
    id: SCHED_HOTEL_ID,
    tenantId: DEMO_TENANT_ID,
    projectId: DEMO_PROJECT_IDS.hotel,
    name: "Harmonogram rozbudowy – edycja 2",
    createdAt: "2023-08-25T10:00:00Z",
    createdByUserId: DEMO_USERS.piotr.userId,
    createdByUserName: "Piotr Wiśniewski",
    stages: [
      {
        id: "stage-htl-001",
        name: "Prace rozbiórkowe i przygotowawcze",
        order: 1,
        works: [
          {
            id: "work-htl-001",
            name: "Rozbudowa w kierunku północnym – wykop",
            order: 1,
            colorRgb: "#FF9800",
            isClosed: true,
            periods: [
              { id: "per-htl-001", startDate: d(-400), endDate: d(-360), isClosed: true },
            ],
            assignees: assignees(DEMO_USERS.marta.userId),
            comments: [],
          },
          {
            id: "work-htl-002",
            name: "Fundamenty – ławy żelbetowe",
            order: 2,
            colorRgb: "#9C27B0",
            isClosed: true,
            periods: [
              { id: "per-htl-002", startDate: d(-355), endDate: d(-310), isClosed: true },
            ],
            assignees: assignees(DEMO_USERS.marta.userId, DEMO_USERS.tomasz.userId),
            comments: [],
          },
        ],
      },
      {
        id: "stage-htl-002",
        name: "Konstrukcja Skrzydła B",
        order: 2,
        works: [
          {
            id: "work-htl-003",
            name: "Szkielet stalowy – wszystkie kondygnacje",
            order: 1,
            colorRgb: "#2196F3",
            isClosed: false,
            periods: [
              { id: "per-htl-003", startDate: d(-60), endDate: d(+90), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.piotr.userId, DEMO_USERS.marta.userId),
            comments: [
              {
                id: "cmt-htl-001",
                content: "Opóźnienie dostawy stalowych elementów modułowych – nowy termin 4 tygodnie.",
                createdAt: "2024-02-14T09:00:00Z",
                createdByUserId: DEMO_USERS.piotr.userId,
                createdByUserName: "Piotr Wiśniewski",
              },
            ],
          },
          {
            id: "work-htl-004",
            name: "Stropy i klatki schodowe",
            order: 2,
            colorRgb: "#00BCD4",
            isClosed: false,
            periods: [
              { id: "per-htl-004", startDate: d(-30), endDate: d(+120), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.tomasz.userId),
            comments: [],
          },
        ],
      },
      {
        id: "stage-htl-003",
        name: "Wykończenie i wyposażenie wnętrz",
        order: 3,
        works: [
          {
            id: "work-htl-005",
            name: "Instalacje HVAC, elektryka, BMS",
            order: 1,
            colorRgb: "#E91E63",
            isClosed: false,
            periods: [
              { id: "per-htl-005", startDate: d(+130), endDate: d(+215), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.anna.userId),
            comments: [],
          },
          {
            id: "work-htl-006",
            name: "Wykończenie pokoi 101–180",
            order: 2,
            colorRgb: "#FF5722",
            isClosed: false,
            periods: [
              { id: "per-htl-006", startDate: d(+220), endDate: d(+310), isClosed: false },
            ],
            assignees: assignees(DEMO_USERS.marta.userId, DEMO_USERS.katarzyna.userId),
            comments: [],
          },
        ],
      },
    ],
  },
};
