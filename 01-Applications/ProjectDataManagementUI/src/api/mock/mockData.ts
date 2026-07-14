// ============================================
//   PDM Demo Mode — Kompletne mockowane dane
// ============================================

import type {
  WorkScheduleSummaryWeb,
  WorkScheduleStageWorkCommentWeb,
} from "../../types/workSchedule.types";
import {
  buildingGroups,
  installationGroups,
  landDevelopmentGroups,
  garageGroups,
  preliminaryGroups,
  customizeGroups,
  buildMockFieldSchemas,
  buildMockAdditionalFields,
  getCategoryFieldId,
  remapGroupsForEstimate,
  type EstimateMeta,
} from "./costEstimateMockData";

const uid = "demo-user-001";
const date = (d: string) => d + "T08:00:00Z";
const now = new Date().toISOString();

// ---- TENANTS ----
const T1 = "t-001"; const T2 = "t-002";
export const mockTenants = [
  { id: T1, name: "Dom Development Sp. z o.o.", createdAt: date("2024-11-15"), isActive: true, isAdmin: true, isActiveTenant: true },
  { id: T2, name: "Budinvest Polska S.A.", createdAt: date("2025-02-01"), isActive: true, isAdmin: true, isActiveTenant: false },
];

// ---- USER ----
export const mockUserProfile = {
  id: uid, email: "m.kowalski@brickly.pro", firstName: "Michał", lastName: "Kowalski",
  activeTenantId: T1, isActiveTenantAdmin: true, isSuperAdmin: true,
  phoneNumber: "+48 601 234 567", companyName: null, taxId: null, street: null, city: null, postalCode: null, country: null,
};

// ---- PROJECTS ----
const P1 = "p-001"; const P2 = "p-002"; const P3 = "p-003"; const P4 = "p-004"; const P5 = "p-005";

const allProjects = [
  { id: P1, tenantId: T1, name: "Osiedle Zielone Wzgórza — Etap I", isActive: true, createdAt: date("2025-06-01"), createdByUserId: uid, createdByUserName: "Michał Kowalski", isAdmin: true, canViewAllResources: true, membersCount: 12, userPermissions: ["PROJECT.VIEW","PROJECT.SETTINGS","PROJECT.FILES","PROJECT.ESTIMATES","PROJECT.COSTS","PROJECT.SCHEDULE","PROJECT.DASHBOARD_TRACKER"], currency: { code: "PLN", name: "Złoty polski", symbol: "zł" } },
  { id: P2, tenantId: T1, name: "Osiedle Zielone Wzgórza — Etap II", isActive: true, createdAt: date("2025-09-15"), createdByUserId: uid, createdByUserName: "Michał Kowalski", isAdmin: true, canViewAllResources: true, membersCount: 8, userPermissions: ["PROJECT.VIEW","PROJECT.SETTINGS","PROJECT.FILES","PROJECT.ESTIMATES","PROJECT.COSTS","PROJECT.SCHEDULE","PROJECT.DASHBOARD_TRACKER"], currency: { code: "PLN", name: "Złoty polski", symbol: "zł" } },
  { id: P3, tenantId: T1, name: "Osiedle Parkowe — Bud. A", isActive: true, createdAt: date("2025-11-01"), createdByUserId: uid, createdByUserName: "Michał Kowalski", isAdmin: true, canViewAllResources: true, membersCount: 6, userPermissions: ["PROJECT.VIEW","PROJECT.SETTINGS","PROJECT.FILES","PROJECT.ESTIMATES","PROJECT.COSTS","PROJECT.SCHEDULE","PROJECT.DASHBOARD_TRACKER"], currency: { code: "PLN", name: "Złoty polski", symbol: "zł" } },
  { id: P4, tenantId: T2, name: "Apartamenty Centrum — Faza I", isActive: true, createdAt: date("2025-07-01"), createdByUserId: uid, createdByUserName: "Michał Kowalski", isAdmin: true, canViewAllResources: true, membersCount: 10, userPermissions: ["PROJECT.VIEW","PROJECT.SETTINGS","PROJECT.FILES","PROJECT.ESTIMATES","PROJECT.COSTS","PROJECT.SCHEDULE","PROJECT.DASHBOARD_TRACKER"], currency: { code: "PLN", name: "Złoty polski", symbol: "zł" } },
  { id: P5, tenantId: T2, name: "Rezydencja Jeziorki", isActive: false, createdAt: date("2026-01-10"), createdByUserId: uid, createdByUserName: "Michał Kowalski", isAdmin: true, canViewAllResources: true, membersCount: 4, userPermissions: ["PROJECT.VIEW","PROJECT.SETTINGS","PROJECT.FILES","PROJECT.ESTIMATES","PROJECT.COSTS","PROJECT.SCHEDULE","PROJECT.DASHBOARD_TRACKER"], currency: { code: "EUR", name: "Euro", symbol: "€" } },
];

export const mockProjects = allProjects;
export const mockProjectDictionary = allProjects.filter(p => p.isActive).map(p => ({ id: p.id, name: p.name }));

// ---- CONTRACTORS ----
export const mockContractors = [
  { id: "ctr-001", name: "Budimex S.A.", nip: "5261003148", email: "kontakt@budimex.pl", phone: "+48 22 623 3000", street: "ul. Stawki 40", city: "Warszawa", postalCode: "01-040" },
  { id: "ctr-002", name: "Strabag Sp. z o.o.", nip: "5210090567", email: "biuro@strabag.pl", phone: "+48 22 375 8000", street: "ul. Cybernetyki 9", city: "Warszawa", postalCode: "02-677" },
  { id: "ctr-003", name: "Dębickie Przedsiębiorstwo Budowlane", nip: "8720001425", email: "dpb@dpb.pl", phone: "+48 14 670 2400", street: "ul. Metalowców 12", city: "Dębica", postalCode: "39-200" },
  { id: "ctr-004", name: "Erbet Sp. z o.o.", nip: "8130308515", email: "erbet@erbet.pl", phone: "+48 17 853 4400", street: "ul. Przemysłowa 20", city: "Rzeszów", postalCode: "35-105" },
  { id: "ctr-005", name: "Cemex Polska Sp. z o.o.", nip: "1180016063", email: "cemex@cemex.pl", phone: "+48 22 212 2000", street: "ul. Wołoska 22", city: "Warszawa", postalCode: "02-675" },
  { id: "ctr-006", name: "Saint-Gobain Construction Products", nip: "5260201394", email: "biuro@saint-gobain.pl", phone: "+48 22 457 2800", street: "ul. Domaniewska 50A", city: "Warszawa", postalCode: "02-672" },
  { id: "ctr-007", name: "Elektromontaż Rzeszów S.A.", nip: "8130003082", email: "rzeszow@elektromontaz.pl", phone: "+48 17 862 1100", street: "ul. Przemysłowa 8", city: "Rzeszów", postalCode: "35-105" },
  { id: "ctr-008", name: "Wienerberger Ceramika Budowlana", nip: "5210115902", email: "biuro@wienerberger.pl", phone: "+48 22 514 2100", street: "ul. Postępu 18A", city: "Warszawa", postalCode: "02-676" },
  { id: "ctr-009", name: "Wodoinstal Kraków Sp. z o.o.", nip: "9450012345", email: "biuro@wodoinstal.pl", phone: "+48 12 656 7800", street: "ul. Wodociągowa 15", city: "Kraków", postalCode: "30-001" },
  { id: "ctr-010", name: "GreenScape Architektura Krajobrazu", nip: "6793087654", email: "biuro@greenscape.pl", phone: "+48 12 412 3400", street: "ul. Zielona 7", city: "Kraków", postalCode: "31-001" },
  { id: "ctr-011", name: "WentSystemy Sp. z o.o.", nip: "5272819273", email: "biuro@wentsystemy.pl", phone: "+48 22 845 6700", street: "ul. Klimatyzacyjna 3", city: "Warszawa", postalCode: "01-234" },
  { id: "ctr-012", name: "InsBud — Izolacje Budowlane", nip: "9452198765", email: "biuro@insbud.pl", phone: "+48 12 345 6789", street: "ul. Izolacyjna 10", city: "Kraków", postalCode: "30-150" },
  { id: "ctr-013", name: "Porotherm Polska Sp. z o.o.", nip: "5262543109", email: "biuro@porotherm.pl", phone: "+48 22 765 4321", street: "ul. Ceramiczna 22", city: "Warszawa", postalCode: "01-345" },
  { id: "ctr-014", name: "Hydrobudowa Kraków S.A.", nip: "9451234567", email: "biuro@hydrobudowa.pl", phone: "+48 12 222 3333", street: "ul. Wodna 5", city: "Kraków", postalCode: "30-222" },
  { id: "ctr-015", name: "Technika Grzewcza Rzeszów", nip: "8139876543", email: "biuro@tg-rzeszow.pl", phone: "+48 17 111 2222", street: "ul. Ciepła 18", city: "Rzeszów", postalCode: "35-222" },
];

// ---- COST ESTIMATES (Kosztorysy) ----
// CostEstimateStatus: Draft=0, InProgress=1, ReadyForReview=2, Approved=3, Rejected=4, Archived=5

export const mockCostEstimates = [
  // Projekt P1 — Zielone Wzgórza I
  { id: "ce-001", tenantId: T1, projectId: P1, name: "Kosztorys budowlany — Etap I", description: "Kosztorys główny dla pierwszego etapu inwestycji", status: 3, totalNet: 12450000, totalGross: 15313500, totalVat: 2863500, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), ownerId: uid, ownerName: "Michał Kowalski", isSharedWithMe: false, isSharedByMe: true, sharedWithUsers: [{ userId: "u-006", fullName: "Anna Nowak", email: "a.nowak@brickly.pro", sharedAt: date("2025-07-01") }], currencyCode: "PLN", currencySymbol: "zł" },
  { id: "ce-002", tenantId: T1, projectId: P1, name: "Kosztorys instalacji sanitarnych", description: "Kosztorys branżowy — instalacje wod-kan i grzewcze", status: 3, totalNet: 2850000, totalGross: 3505500, totalVat: 655500, createdAt: date("2025-07-10"), updatedAt: date("2026-02-15"), ownerId: "u-008", ownerName: "Piotr Zieliński", isSharedWithMe: true, isSharedByMe: false, sharedWithUsers: [], currencyCode: "PLN", currencySymbol: "zł" },
  { id: "ce-003", tenantId: T1, projectId: P1, name: "Kosztorys elektryki i teletechniki", description: null, status: 1, totalNet: 1920000, totalGross: 2361600, totalVat: 441600, createdAt: date("2025-08-01"), updatedAt: date("2026-03-01"), ownerId: uid, ownerName: "Michał Kowalski", isSharedWithMe: false, isSharedByMe: true, sharedWithUsers: [{ userId: "u-009", fullName: "Krzysztof Baran", email: "k.baran@brickly.pro", sharedAt: date("2025-08-15") }], currencyCode: "PLN", currencySymbol: "zł" },

  // Projekt P2 — Zielone Wzgórza II
  { id: "ce-004", tenantId: T1, projectId: P2, name: "Kosztorys budowlany — Etap II", description: "Kosztorys główny drugiego etapu", status: 2, totalNet: 9870000, totalGross: 12140100, totalVat: 2270100, createdAt: date("2025-10-01"), updatedAt: date("2026-04-10"), ownerId: uid, ownerName: "Michał Kowalski", isSharedWithMe: false, isSharedByMe: false, sharedWithUsers: [], currencyCode: "PLN", currencySymbol: "zł" },
  { id: "ce-005", tenantId: T1, projectId: P2, name: "Kosztorys zagospodarowania terenu", description: null, status: 0, totalNet: 1450000, totalGross: 1783500, totalVat: 333500, createdAt: date("2025-11-15"), updatedAt: null, ownerId: uid, ownerName: "Michał Kowalski", isSharedWithMe: false, isSharedByMe: false, sharedWithUsers: [], currencyCode: "PLN", currencySymbol: "zł" },

  // Projekt P3 — Parkowe A
  { id: "ce-006", tenantId: T1, projectId: P3, name: "Kosztorys budowlany — Bud. A", description: null, status: 1, totalNet: 6450000, totalGross: 7933500, totalVat: 1483500, createdAt: date("2025-12-01"), updatedAt: date("2026-05-01"), ownerId: uid, ownerName: "Michał Kowalski", isSharedWithMe: false, isSharedByMe: false, sharedWithUsers: [], currencyCode: "PLN", currencySymbol: "zł" },

  // Projekt P4 — Apartamenty Centrum
  { id: "ce-007", tenantId: T2, projectId: P4, name: "Kosztorys główny — Faza I", description: "Kosztorys dla apartamentów w centrum miasta", status: 3, totalNet: 18750000, totalGross: 23062500, totalVat: 4312500, createdAt: date("2025-07-15"), updatedAt: date("2026-02-28"), ownerId: uid, ownerName: "Michał Kowalski", isSharedWithMe: false, isSharedByMe: true, sharedWithUsers: [{ userId: "u-010", fullName: "Ewa Majewska", email: "e.majewska@budinvest.pl", sharedAt: date("2025-08-01") }], currencyCode: "PLN", currencySymbol: "zł" },
  { id: "ce-008", tenantId: T2, projectId: P4, name: "Kosztorys garażu podziemnego", description: null, status: 3, totalNet: 4250000, totalGross: 5227500, totalVat: 977500, createdAt: date("2025-08-20"), updatedAt: date("2026-01-15"), ownerId: "u-010", ownerName: "Ewa Majewska", isSharedWithMe: true, isSharedByMe: false, sharedWithUsers: [], currencyCode: "PLN", currencySymbol: "zł" },

  // Projekt P5 — Rezydencja Jeziorki
  { id: "ce-009", tenantId: T2, projectId: P5, name: "Kosztorys wstępny — Rezydencja", description: "Kosztorys koncepcyjny, waluta EUR", status: 0, totalNet: 3200000, totalGross: 3936000, totalVat: 736000, createdAt: date("2026-01-15"), updatedAt: null, ownerId: uid, ownerName: "Michał Kowalski", isSharedWithMe: false, isSharedByMe: false, sharedWithUsers: [], currencyCode: "EUR", currencySymbol: "€" },
];

// ============================================================================
//   COST ESTIMATE DETAILS — generator unikalnych danych per kosztorys
// ============================================================================

const estimateMetaMap: Record<string, EstimateMeta> = {
  "ce-001": {
    tenantId: T1, projectId: P1, groups: buildingGroups,
    name: "Kosztorys budowlany — Etap I", description: "Kosztorys główny dla pierwszego etapu inwestycji",
    status: 3, totalNet: 12450000, ownerName: "Michał Kowalski", workScheduleId: "ws-001",
    sharedWith: [{ userId: "u-006", fullName: "Anna Nowak", email: "a.nowak@brickly.pro", sharedAt: date("2025-07-01") }],
    currency: { code: "PLN", symbol: "zł" },
  },
  "ce-002": {
    tenantId: T1, projectId: P1, groups: installationGroups,
    name: "Kosztorys instalacji sanitarnych", description: "Kosztorys branżowy — instalacje wod-kan i grzewcze",
    status: 3, totalNet: 2850000, ownerName: "Piotr Zieliński", workScheduleId: "ws-002",
    sharedWith: [],
    currency: { code: "PLN", symbol: "zł" },
  },
  "ce-003": {
    tenantId: T1, projectId: P1, groups: installationGroups,
    name: "Kosztorys elektryki i teletechniki", description: null,
    status: 1, totalNet: 1920000, ownerName: "Michał Kowalski", workScheduleId: null,
    sharedWith: [{ userId: "u-009", fullName: "Krzysztof Baran", email: "k.baran@brickly.pro", sharedAt: date("2025-08-15") }],
    currency: { code: "PLN", symbol: "zł" },
    customize: (g) => customizeGroups(g,
      ["1. Instalacje elektryczne wewnętrzne", "2. Teletechnika i okablowanie strukturalne"],
      { "Rurociągi wodociągowe PP-R": "Kable i przewody elektryczne YKY 5×10",
        "Kanalizacja sanitarna PCV": "Rozdzielnice i osprzęt elektryczny",
        "Pompy i zestawy hydroforowe": "Oprawy oświetleniowe LED",
        "Kotłownia gazowa z instalacją CO": "Instalacja teletechniczna (RJ45, światłowód)",
        "Grzejniki i instalacja rozdzielcza": "System przyzywowo-alarmowy",
        "Wentylacja mechaniczna z rekuperacją": "Systemy zabezpieczeń i monitoringu" }
    ),
  },
  "ce-004": {
    tenantId: T1, projectId: P2, groups: buildingGroups,
    name: "Kosztorys budowlany — Etap II", description: "Kosztorys główny drugiego etapu",
    status: 2, totalNet: 9870000, ownerName: "Michał Kowalski", workScheduleId: "ws-003",
    sharedWith: [],
    currency: { code: "PLN", symbol: "zł" },
    customize: (g) => customizeGroups(g,
      ["1. Roboty ziemne i konstrukcja", "2. Ściany konstrukcyjne i stropy", "3. Izolacje i elewacja", "4. Instalacje wbudowane"],
      { "Wykopy pod fundamenty": "Wykopy pod ławy i stopy fundamentowe",
        "Ławy fundamentowe żelbetowe": "Ściany fundamentowe żelbetowe",
        "Izolacja przeciwwilgociowa fundamentów": "Izolacja pozioma i pionowa fundamentów",
        "Zasypka i zagęszczenie": "Zasypka piaskiem i zagęszczenie mechaniczne",
        "Słupy żelbetowe 40×40 cm": "Słupy żelbetowe 30×50 cm",
        "Stropy żelbetowe monolityczne": "Stropy gęstożebrowe Teriva",
        "Schody żelbetowe": "Schody żelbetowe płytowe",
        "Ściany nośne z bloczków silikatowych": "Ściany działowe z gazobetonu",
        "Elewacja — tynk silikonowy": "Elewacja — tynk mineralny",
        "Stolarka okienna PCV 3-szybowa": "Stolarka drzwiowa zewnętrzna",
        "Dach i pokrycie dachowe": "Stropodach wentylowany",
        "Instalacje sanitarne (wod-kan, CO)": "Instalacje sanitarne podposadzkowe",
        "Instalacja elektryczna i teletechnika": "Instalacja elektryczna wbudowana",
        "Koszty pośrednie i organizacja placu budowy": "Zagospodarowanie placu budowy" }
    ),
  },
  "ce-005": {
    tenantId: T1, projectId: P2, groups: landDevelopmentGroups,
    name: "Kosztorys zagospodarowania terenu", description: null,
    status: 0, totalNet: 1450000, ownerName: "Michał Kowalski", workScheduleId: null,
    sharedWith: [],
    currency: { code: "PLN", symbol: "zł" },
  },
  "ce-006": {
    tenantId: T1, projectId: P3, groups: buildingGroups,
    name: "Kosztorys budowlany — Bud. A", description: null,
    status: 1, totalNet: 6450000, ownerName: "Michał Kowalski", workScheduleId: "ws-006",
    sharedWith: [],
    currency: { code: "PLN", symbol: "zł" },
    customize: (g) => customizeGroups(g,
      ["1. Fundamenty i izolacje", "2. Konstrukcja nośna", "3. Ściany zewnętrzne i elewacja", "4. Prace wykończeniowe"],
      { "Wykopy pod fundamenty": "Wykopy wąskoprzestrzenne pod ławy",
        "Ławy fundamentowe żelbetowe": "Ławy fundamentowe zbrojone ciągłe",
        "Izolacja przeciwwilgociowa fundamentów": "Hydroizolacja ścian fundamentowych",
        "Zasypka i zagęszczenie": "Zasypka i ubijanie gruntu",
        "Słupy żelbetowe 40×40 cm": "Słupy żelbetowe prefabrykowane",
        "Stropy żelbetowe monolityczne": "Płyta żelbetowa stropodachu",
        "Schody żelbetowe": "Schody prefabrykowane",
        "Ściany nośne z bloczków silikatowych": "Ściany osłonowe z płyt warstwowych",
        "Elewacja — tynk silikonowy": "Elewacja z płyt kompozytowych",
        "Stolarka okienna PCV 3-szybowa": "Stolarka aluminiowa",
        "Dach i pokrycie dachowe": "Pokrycie dachu papą termozgrzewalną",
        "Instalacje sanitarne (wod-kan, CO)": "Instalacje sanitarne i klimatyzacja",
        "Instalacja elektryczna i teletechnika": "Instalacje niskoprądowe",
        "Koszty pośrednie i organizacja placu budowy": "Zaplecze budowy" }
    ),
  },
  "ce-007": {
    tenantId: T2, projectId: P4, groups: buildingGroups,
    name: "Kosztorys główny — Faza I", description: "Kosztorys dla apartamentów w centrum miasta",
    status: 3, totalNet: 18750000, ownerName: "Michał Kowalski", workScheduleId: "ws-005",
    sharedWith: [{ userId: "u-010", fullName: "Ewa Majewska", email: "e.majewska@budinvest.pl", sharedAt: date("2025-08-01") }],
    currency: { code: "PLN", symbol: "zł" },
    customize: (g) => customizeGroups(g,
      ["1. Roboty rozbiórkowe i przygotowawcze", "2. Konstrukcja żelbetowa apartamentowca", "3. Elewacja i stolarka", "4. Instalacje i wykończenie"],
      { "Wykopy pod fundamenty": "Wykopy głębokie pod budynek",
        "Ławy fundamentowe żelbetowe": "Płyta fundamentowa żelbetowa",
        "Izolacja przeciwwilgociowa fundamentów": "Izolacja przeciwwodna białej wanny",
        "Zasypka i zagęszczenie": "Zasypka wykopów",
        "Słupy żelbetowe 40×40 cm": "Słupy żelbetowe 60×60 cm zbrojone",
        "Stropy żelbetowe monolityczne": "Stropy monolityczne gr. 25 cm",
        "Schody żelbetowe": "Schody ewakuacyjne żelbetowe",
        "Ściany nośne z bloczków silikatowych": "Ściany nośne z betonu architektonicznego",
        "Elewacja — tynk silikonowy": "Elewacja szklana z panelami aluminiowymi",
        "Stolarka okienna PCV 3-szybowa": "Stolarka okienna aluminiowa 3-szybowa",
        "Dach i pokrycie dachowe": "Dach zielony z tarasami",
        "Instalacje sanitarne (wod-kan, CO)": "Instalacje sanitarne apartamentów",
        "Instalacja elektryczna i teletechnika": "Instalacje elektryczne i inteligentny budynek",
        "Koszty pośrednie i organizacja placu budowy": "Zabezpieczenie i organizacja placu budowy w centrum" }
    ),
  },
  "ce-008": {
    tenantId: T2, projectId: P4, groups: garageGroups,
    name: "Kosztorys garażu podziemnego", description: null,
    status: 3, totalNet: 4250000, ownerName: "Ewa Majewska", workScheduleId: null,
    sharedWith: [],
    currency: { code: "PLN", symbol: "zł" },
  },
  "ce-009": {
    tenantId: T2, projectId: P5, groups: preliminaryGroups,
    name: "Kosztorys wstępny — Rezydencja", description: "Kosztorys koncepcyjny, waluta EUR",
    status: 0, totalNet: 3200000, ownerName: "Michał Kowalski", workScheduleId: "ws-007",
    sharedWith: [],
    currency: { code: "EUR", symbol: "€" },
  },
};

export function getCostEstimateDetailsById(id: string): unknown {
  const meta = estimateMetaMap[id];
  const createdAt = date("2025-06-15");
  const updatedAt = date("2026-01-20");

  if (!meta) {
    const fallbackDetails = getCostEstimateDetailsById("ce-001") as Record<string, unknown>;
    return {
      ...fallbackDetails,
      id,
      name: `Kosztorys (${id})`,
      description: null,
      totalNet: 100000,
      totalGross: 123000,
      totalVat: 23000,
      status: 0,
      ownerName: "Michał Kowalski",
      workScheduleId: null,
      sharedWithUsers: [],
    };
  }

  const totalGross = Math.round(meta.totalNet * 1.23);
  const totalVat = totalGross - meta.totalNet;
  const categoryFieldId = getCategoryFieldId(id);
  const sourceGroups = meta.customize ? meta.customize(meta.groups) : meta.groups;
  const rootGroups = remapGroupsForEstimate(sourceGroups, id, categoryFieldId);

  return {
    id,
    tenantId: meta.tenantId,
    projectId: meta.projectId,
    selectedCurrencyCode: meta.currency.code,
    selectedCurrencySymbol: meta.currency.symbol,
    name: meta.name,
    description: meta.description,
    status: meta.status,
    totalNet: meta.totalNet,
    totalGross,
    totalVat,
    createdAt,
    updatedAt,
    lastCalculatedAt: updatedAt,
    ownerId: uid,
    ownerName: meta.ownerName,
    workScheduleId: meta.workScheduleId,
    accessLevel: 3,
    sharedWithUsers: meta.sharedWith,
    fieldSchemas: buildMockFieldSchemas(id, createdAt),
    additionalFields: buildMockAdditionalFields(id, createdAt),
    rootGroups,
  };
}

/** @deprecated Używaj getCostEstimateDetailsById(id) */
export const mockCostEstimateDetails = getCostEstimateDetailsById("ce-001");

// ---- WORK SCHEDULES (Harmonogramy) ----

/** Zwraca listę harmonogramów dla projektu — zgodne z WorkScheduleSummaryWeb[] */
export function getWorkSchedules(projectId: string, scope?: string): WorkScheduleSummaryWeb[] {
  const all = scheduleListData
    .filter(s => s.projectId === projectId)
    .map(s => ({
      id: s.id,
      costEstimateId: s.costEstimateId ?? null,
      name: s.name,
      createdAt: s.createdAt,
      createdByUserId: s.createdByUserId,
      createdByUserName: s.createdByUserName,
    }));
  if (scope === "mine") {
    return all.filter(s => s.createdByUserId === uid);
  }
  if (scope === "shared" || scope === "pendingapproval") {
    return [];
  }
  return all;
}

/** Dane pomocnicze dla listy harmonogramów — mapuje wsId na metadane projektu */
const scheduleListData: Array<{
  id: string; projectId: string; name: string; createdAt: string;
  createdByUserId: string; createdByUserName: string; costEstimateId?: string;
}> = [
  // P1
  { id: "ws-001", projectId: P1, name: "Harmonogram — Etap I", createdAt: date("2025-06-20"), createdByUserId: uid, createdByUserName: "Michał Kowalski", costEstimateId: "ce-001" },
  { id: "ws-002", projectId: P1, name: "Harmonogram instalacji sanitarnych", createdAt: date("2026-02-10"), createdByUserId: "u-008", createdByUserName: "Piotr Zieliński" },
  // P2
  { id: "ws-003", projectId: P2, name: "Harmonogram — Etap II", createdAt: date("2026-05-15"), createdByUserId: uid, createdByUserName: "Michał Kowalski", costEstimateId: "ce-004" },
  // P3
  { id: "ws-006", projectId: P3, name: "Harmonogram — Bud. A", createdAt: date("2025-12-15"), createdByUserId: uid, createdByUserName: "Michał Kowalski", costEstimateId: "ce-006" },
  // P4
  { id: "ws-004", projectId: P4, name: "Harmonogram — Bud. A", createdAt: date("2026-01-05"), createdByUserId: "u-010", createdByUserName: "Ewa Majewska", costEstimateId: "ce-005" },
  { id: "ws-005", projectId: P4, name: "Harmonogram — Apartamenty Centrum", createdAt: date("2025-07-20"), createdByUserId: uid, createdByUserName: "Michał Kowalski", costEstimateId: "ce-007" },
  // P5
  { id: "ws-007", projectId: P5, name: "Harmonogram — Rezydencja Jeziorki", createdAt: date("2026-02-01"), createdByUserId: uid, createdByUserName: "Michał Kowalski", costEstimateId: "ce-009" },
];

/** Zwraca szczegółowe dane harmonogramu dla danego wsId — zgodne z WorkScheduleDetailsWeb */
export function getWorkScheduleDetails(wsId: string): object {
  const entry = scheduleDetailsMap[wsId];
  if (!entry) {
    // Fallback — zwróć pierwszy harmonogram dla P1
    return buildWs001Details();
  }
  return entry();
}

/** Helper do tworzenia assigneów */
function assignee(...users: { userId: string; userName: string }[]): { userId: string; userName: string }[] {
  return users;
}

/** Helper do tworzenia komentarzy */
function comment(id: string, content: string, daysAgo: number, userId: string, userName: string): WorkScheduleStageWorkCommentWeb {
  const d = new Date();
  d.setDate(d.getDate() - daysAgo);
  return { id, content, createdAt: d.toISOString(), createdByUserId: userId, createdByUserName: userName };
}

// ---- SZCZEGÓŁOWE DANE 5 HARMONOGRAMÓW ----

function buildWs001Details(): object {
  return {
    id: "ws-001", tenantId: T1, projectId: P1, costEstimateId: "ce-001",
    name: "Harmonogram — Etap I",
    createdAt: date("2025-06-20"), createdByUserId: uid, createdByUserName: "Michał Kowalski",
    stages: [
      {
        id: "stg-ws1-1", name: "1. Stan surowy otwarty", order: 0, parentStageId: null, costEstimateGroupId: "g-001",
        works: [
          { id: "w-ws1-001", name: "Wykopy fundamentowe", order: 0, colorRgb: "#4A7FEF", isClosed: true,
            periods: [
              { id: "p-ws1-001", startDate: "2025-07-01", endDate: "2025-07-20", isClosed: true },
              { id: "p-ws1-002", startDate: "2025-07-21", endDate: "2025-08-15", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [
              comment("c-ws1-001", "Zakończono wykopy — zgodnie z planem.", 330, "u-002", "Tomasz Wójcik"),
              comment("c-ws1-002", "Poziom wód gruntowych wyższy niż zakładano — konieczne igłofiltry.", 340, uid, "Michał Kowalski"),
            ],
          },
          { id: "w-ws1-002", name: "Fundamenty i izolacje", order: 1, colorRgb: "#E07B39", isClosed: true,
            periods: [
              { id: "p-ws1-003", startDate: "2025-08-01", endDate: "2025-09-10", isClosed: true },
              { id: "p-ws1-004", startDate: "2025-09-11", endDate: "2025-10-15", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }, { userId: "u-006", userName: "Anna Nowak" }),
            comments: [
              comment("c-ws1-003", "Betonowanie ław zakończone. Izolacja przeciwwilgociowa wykonana.", 280, "u-006", "Anna Nowak"),
            ],
          },
          { id: "w-ws1-003", name: "Ściany nośne parteru", order: 2, colorRgb: "#38A169", isClosed: true,
            periods: [
              { id: "p-ws1-005", startDate: "2025-09-15", endDate: "2025-10-31", isClosed: true },
              { id: "p-ws1-006", startDate: "2025-11-01", endDate: "2025-12-15", isClosed: true },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [
              comment("c-ws1-004", "Murowanie ścian parteru zakończone. Nadproża zamontowane.", 190, "u-006", "Anna Nowak"),
            ],
          },
          { id: "w-ws1-004", name: "Strop nad parterem", order: 3, colorRgb: "#805AD5", isClosed: true,
            periods: [
              { id: "p-ws1-007", startDate: "2025-11-01", endDate: "2025-12-20", isClosed: true },
              { id: "p-ws1-008", startDate: "2025-12-21", endDate: "2026-01-30", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [
              comment("c-ws1-005", "Strop wylany. Czas wiązania betonu wydłużony z powodu niskich temperatur.", 150, "u-002", "Tomasz Wójcik"),
            ],
          },
          { id: "w-ws1-005", name: "Ściany I piętra", order: 4, colorRgb: "#DD6B20", isClosed: false,
            periods: [
              { id: "p-ws1-009", startDate: "2025-12-15", endDate: "2026-02-10", isClosed: false },
              { id: "p-ws1-010", startDate: "2026-02-11", endDate: "2026-03-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [
              comment("c-ws1-006", "Ściany I piętra w trakcie — opóźnienie 2 tyg. przez mrozy.", 100, "u-006", "Anna Nowak"),
            ],
          },
          { id: "w-ws1-006", name: "Stropodach", order: 5, colorRgb: "#3182CE", isClosed: false,
            periods: [
              { id: "p-ws1-011", startDate: "2026-02-01", endDate: "2026-03-31", isClosed: false },
              { id: "p-ws1-012", startDate: "2026-04-01", endDate: "2026-05-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws1-2", name: "2. Instalacje wewnętrzne", order: 1, parentStageId: null, costEstimateGroupId: "g-004",
        works: [
          { id: "w-ws1-007", name: "Instalacja elektryczna", order: 0, colorRgb: "#D69E2E", isClosed: false,
            periods: [
              { id: "p-ws1-013", startDate: "2026-03-01", endDate: "2026-04-30", isClosed: true },
              { id: "p-ws1-014", startDate: "2026-05-01", endDate: "2026-06-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-008", userName: "Piotr Zieliński" }),
            comments: [
              comment("c-ws1-007", "Okablowanie piętra zakończone. Rozpoczęto montaż rozdzielnic.", 30, "u-008", "Piotr Zieliński"),
            ],
          },
          { id: "w-ws1-008", name: "Instalacja wodno-kanalizacyjna", order: 1, colorRgb: "#319795", isClosed: false,
            periods: [
              { id: "p-ws1-015", startDate: "2026-03-15", endDate: "2026-05-15", isClosed: true },
              { id: "p-ws1-016", startDate: "2026-05-16", endDate: "2026-07-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-009", userName: "Krzysztof Baran" }),
            comments: [
              comment("c-ws1-008", "Piony kanalizacyjne zamontowane. Podejścia pod przybory w trakcie.", 45, "u-009", "Krzysztof Baran"),
            ],
          },
          { id: "w-ws1-009", name: "Instalacja grzewcza", order: 2, colorRgb: "#E53E3E", isClosed: false,
            periods: [
              { id: "p-ws1-017", startDate: "2026-04-01", endDate: "2026-06-15", isClosed: false },
              { id: "p-ws1-018", startDate: "2026-06-16", endDate: "2026-08-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-008", userName: "Piotr Zieliński" }),
            comments: [],
          },
          { id: "w-ws1-010", name: "Wentylacja mechaniczna", order: 3, colorRgb: "#6B46C1", isClosed: false,
            periods: [
              { id: "p-ws1-019", startDate: "2026-05-01", endDate: "2026-07-15", isClosed: false },
              { id: "p-ws1-020", startDate: "2026-07-16", endDate: "2026-09-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-009", userName: "Krzysztof Baran" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws1-3", name: "3. Wykończenia wewnętrzne", order: 2, parentStageId: null, costEstimateGroupId: "g-005",
        works: [
          { id: "w-ws1-011", name: "Tynki wewnętrzne", order: 0, colorRgb: "#2B6CB0", isClosed: false,
            periods: [{ id: "p-ws1-021", startDate: "2026-06-01", endDate: "2026-09-30", isClosed: false }],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws1-012", name: "Wylewki i posadzki", order: 1, colorRgb: "#C05621", isClosed: false,
            periods: [{ id: "p-ws1-022", startDate: "2026-07-15", endDate: "2026-10-30", isClosed: false }],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws1-013", name: "Płytki i okładziny", order: 2, colorRgb: "#276749", isClosed: false,
            periods: [{ id: "p-ws1-023", startDate: "2026-09-01", endDate: "2026-12-15", isClosed: false }],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws1-4", name: "4. Elewacja i teren", order: 3, parentStageId: null, costEstimateGroupId: "g-003",
        works: [
          { id: "w-ws1-014", name: "Tynki zewnętrzne", order: 0, colorRgb: "#B83280", isClosed: false,
            periods: [
              { id: "p-ws1-024", startDate: "2026-05-01", endDate: "2026-07-15", isClosed: false },
              { id: "p-ws1-025", startDate: "2026-07-16", endDate: "2026-08-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws1-015", name: "Zagospodarowanie terenu", order: 1, colorRgb: "#2C7A7B", isClosed: false,
            periods: [
              { id: "p-ws1-026", startDate: "2026-08-01", endDate: "2026-10-15", isClosed: false },
              { id: "p-ws1-027", startDate: "2026-10-16", endDate: "2026-12-31", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws1-5", name: "5. Odbiory i przekazanie", order: 4, parentStageId: null,
        works: [
          { id: "w-ws1-016", name: "Odbiory techniczne", order: 0, colorRgb: "#9B2C2C", isClosed: false,
            periods: [
              { id: "p-ws1-028", startDate: "2026-11-01", endDate: "2026-11-30", isClosed: false },
              { id: "p-ws1-029", startDate: "2026-12-01", endDate: "2026-12-15", isClosed: false },
            ],
            assignees: assignee({ userId: uid, userName: "Michał Kowalski" }),
            comments: [],
          },
          { id: "w-ws1-017", name: "Przekazanie obiektu", order: 1, colorRgb: "#285E61", isClosed: false,
            periods: [{ id: "p-ws1-030", startDate: "2026-12-15", endDate: "2026-12-31", isClosed: false }],
            assignees: assignee({ userId: uid, userName: "Michał Kowalski" }),
            comments: [],
          },
        ],
      },
    ],
    dependencies: [
      { id: "dep-ws1-01", predecessorWorkId: "w-ws1-001", successorWorkId: "w-ws1-002", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws1-02", predecessorWorkId: "w-ws1-002", successorWorkId: "w-ws1-003", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws1-03", predecessorWorkId: "w-ws1-003", successorWorkId: "w-ws1-004", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws1-04", predecessorWorkId: "w-ws1-004", successorWorkId: "w-ws1-005", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws1-05", predecessorWorkId: "w-ws1-005", successorWorkId: "w-ws1-006", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws1-06", predecessorWorkId: "w-ws1-006", successorWorkId: "w-ws1-007", dependencyType: 0, lagDays: 7 },
      { id: "dep-ws1-07", predecessorWorkId: "w-ws1-007", successorWorkId: "w-ws1-008", dependencyType: 1, lagDays: 5 },
      { id: "dep-ws1-08", predecessorWorkId: "w-ws1-006", successorWorkId: "w-ws1-014", dependencyType: 0, lagDays: 5 },
      { id: "dep-ws1-09", predecessorWorkId: "w-ws1-011", successorWorkId: "w-ws1-012", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws1-10", predecessorWorkId: "w-ws1-012", successorWorkId: "w-ws1-013", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws1-11", predecessorWorkId: "w-ws1-016", successorWorkId: "w-ws1-017", dependencyType: 0, lagDays: 0 },
    ],
  };
}

function buildWs002Details(): object {
  return {
    id: "ws-002", tenantId: T1, projectId: P1, costEstimateId: null,
    name: "Harmonogram instalacji sanitarnych",
    createdAt: date("2026-02-10"), createdByUserId: "u-008", createdByUserName: "Piotr Zieliński",
    stages: [
      {
        id: "stg-ws2-1", name: "1. Przyłącza zewnętrzne", order: 0, parentStageId: null,
        works: [
          { id: "w-ws2-001", name: "Przyłącze wodociągowe", order: 0, colorRgb: "#3182CE", isClosed: true,
            periods: [
              { id: "p-ws2-001", startDate: "2026-03-01", endDate: "2026-03-31", isClosed: true },
            ],
            assignees: assignee({ userId: "u-009", userName: "Krzysztof Baran" }),
            comments: [
              comment("c-ws2-001", "Przyłącze wodne wykonane. Próba ciśnieniowa pozytywna.", 80, "u-009", "Krzysztof Baran"),
            ],
          },
          { id: "w-ws2-002", name: "Przyłącze kanalizacji sanitarnej", order: 1, colorRgb: "#319795", isClosed: true,
            periods: [
              { id: "p-ws2-002", startDate: "2026-03-05", endDate: "2026-04-10", isClosed: true },
            ],
            assignees: assignee({ userId: "u-009", userName: "Krzysztof Baran" }),
            comments: [
              comment("c-ws2-002", "Kanalizacja podłączona. Studzienki rewizyjne zamontowane.", 70, "u-009", "Krzysztof Baran"),
            ],
          },
          { id: "w-ws2-003", name: "Przyłącze ciepłownicze", order: 2, colorRgb: "#E53E3E", isClosed: false,
            periods: [
              { id: "p-ws2-003", startDate: "2026-04-01", endDate: "2026-05-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-008", userName: "Piotr Zieliński" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws2-2", name: "2. Instalacje wewnętrzne budynku", order: 1, parentStageId: null,
        works: [
          { id: "w-ws2-004", name: "Rozprowadzenie wody w budynku", order: 0, colorRgb: "#3182CE", isClosed: false,
            periods: [
              { id: "p-ws2-004", startDate: "2026-04-15", endDate: "2026-06-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-009", userName: "Krzysztof Baran" }),
            comments: [
              comment("c-ws2-003", "Piony wodne zamontowane do poziomu 2. piętra.", 20, "u-009", "Krzysztof Baran"),
            ],
          },
          { id: "w-ws2-005", name: "Kanalizacja wewnętrzna", order: 1, colorRgb: "#319795", isClosed: false,
            periods: [
              { id: "p-ws2-005", startDate: "2026-05-01", endDate: "2026-07-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-009", userName: "Krzysztof Baran" }),
            comments: [],
          },
          { id: "w-ws2-006", name: "Sieć ciepłownicza wewnętrzna", order: 2, colorRgb: "#E53E3E", isClosed: false,
            periods: [
              { id: "p-ws2-006", startDate: "2026-05-15", endDate: "2026-08-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-008", userName: "Piotr Zieliński" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws2-3", name: "3. Rozruch i regulacja", order: 2, parentStageId: null,
        works: [
          { id: "w-ws2-007", name: "Próby i regulacja instalacji", order: 0, colorRgb: "#805AD5", isClosed: false,
            periods: [
              { id: "p-ws2-007", startDate: "2026-08-01", endDate: "2026-09-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-008", userName: "Piotr Zieliński" }, { userId: "u-009", userName: "Krzysztof Baran" }),
            comments: [],
          },
          { id: "w-ws2-008", name: "Odbiór instalacji sanitarnych", order: 1, colorRgb: "#2B6CB0", isClosed: false,
            periods: [
              { id: "p-ws2-008", startDate: "2026-09-15", endDate: "2026-09-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-008", userName: "Piotr Zieliński" }),
            comments: [],
          },
        ],
      },
    ],
    dependencies: [
      { id: "dep-ws2-01", predecessorWorkId: "w-ws2-001", successorWorkId: "w-ws2-002", dependencyType: 1, lagDays: 2 },
      { id: "dep-ws2-02", predecessorWorkId: "w-ws2-001", successorWorkId: "w-ws2-003", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws2-03", predecessorWorkId: "w-ws2-002", successorWorkId: "w-ws2-004", dependencyType: 0, lagDays: 5 },
      { id: "dep-ws2-04", predecessorWorkId: "w-ws2-004", successorWorkId: "w-ws2-005", dependencyType: 1, lagDays: 3 },
      { id: "dep-ws2-05", predecessorWorkId: "w-ws2-003", successorWorkId: "w-ws2-006", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws2-06", predecessorWorkId: "w-ws2-005", successorWorkId: "w-ws2-007", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws2-07", predecessorWorkId: "w-ws2-006", successorWorkId: "w-ws2-007", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws2-08", predecessorWorkId: "w-ws2-007", successorWorkId: "w-ws2-008", dependencyType: 0, lagDays: 0 },
    ],
  };
}

function buildWs003Details(): object {
  return {
    id: "ws-003", tenantId: T1, projectId: P2, costEstimateId: "ce-004",
    name: "Harmonogram — Etap II",
    createdAt: date("2026-05-15"), createdByUserId: uid, createdByUserName: "Michał Kowalski",
    stages: [
      {
        id: "stg-ws3-1", name: "1. Roboty ziemne i fundamenty", order: 0, parentStageId: null,
        works: [
          { id: "w-ws3-001", name: "Niwelacja terenu i wykopy", order: 0, colorRgb: "#4A7FEF", isClosed: true,
            periods: [
              { id: "p-ws3-001", startDate: "2026-06-01", endDate: "2026-06-20", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [
              comment("c-ws3-001", "Teren wyrównany. Wykopy pod ławy rozpoczęte.", 5, "u-002", "Tomasz Wójcik"),
            ],
          },
          { id: "w-ws3-002", name: "Ławy fundamentowe żelbetowe", order: 1, colorRgb: "#E07B39", isClosed: false,
            periods: [
              { id: "p-ws3-002", startDate: "2026-06-20", endDate: "2026-07-20", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws3-003", name: "Ściany fundamentowe i izolacje", order: 2, colorRgb: "#DD6B20", isClosed: false,
            periods: [
              { id: "p-ws3-003", startDate: "2026-07-15", endDate: "2026-08-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws3-004", name: "Zasypanie wykopów", order: 3, colorRgb: "#38A169", isClosed: false,
            periods: [
              { id: "p-ws3-004", startDate: "2026-08-20", endDate: "2026-09-10", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws3-2", name: "2. Konstrukcja budynku", order: 1, parentStageId: null,
        works: [
          { id: "w-ws3-005", name: "Konstrukcja stalowa szkieletu", order: 0, colorRgb: "#805AD5", isClosed: false,
            periods: [
              { id: "p-ws3-005", startDate: "2026-09-01", endDate: "2026-10-31", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws3-006", name: "Stropy międzykondygnacyjne", order: 1, colorRgb: "#3182CE", isClosed: false,
            periods: [
              { id: "p-ws3-006", startDate: "2026-10-15", endDate: "2026-12-20", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws3-007", name: "Ściany osłonowe i działowe", order: 2, colorRgb: "#38A169", isClosed: false,
            periods: [
              { id: "p-ws3-007", startDate: "2026-11-01", endDate: "2027-01-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws3-008", name: "Dach i pokrycie", order: 3, colorRgb: "#2C7A7B", isClosed: false,
            periods: [
              { id: "p-ws3-008", startDate: "2027-01-01", endDate: "2027-02-28", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws3-3", name: "3. Instalacje i wykończenia", order: 2, parentStageId: null,
        works: [
          { id: "w-ws3-009", name: "Instalacje elektryczne", order: 0, colorRgb: "#D69E2E", isClosed: false,
            periods: [{ id: "p-ws3-009", startDate: "2027-02-01", endDate: "2027-04-15", isClosed: false }],
            assignees: assignee({ userId: "u-008", userName: "Piotr Zieliński" }),
            comments: [],
          },
          { id: "w-ws3-010", name: "Instalacje sanitarne", order: 1, colorRgb: "#319795", isClosed: false,
            periods: [{ id: "p-ws3-010", startDate: "2027-02-15", endDate: "2027-04-30", isClosed: false }],
            assignees: assignee({ userId: "u-009", userName: "Krzysztof Baran" }),
            comments: [],
          },
          { id: "w-ws3-011", name: "Tynki i posadzki", order: 2, colorRgb: "#2B6CB0", isClosed: false,
            periods: [{ id: "p-ws3-011", startDate: "2027-04-01", endDate: "2027-05-31", isClosed: false }],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws3-4", name: "4. Prace wykończeniowe i odbiory", order: 3, parentStageId: null,
        works: [
          { id: "w-ws3-012", name: "Elewacja i docieplenie", order: 0, colorRgb: "#B83280", isClosed: false,
            periods: [{ id: "p-ws3-012", startDate: "2027-04-15", endDate: "2027-05-31", isClosed: false }],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws3-013", name: "Zagospodarowanie terenu", order: 1, colorRgb: "#2C7A7B", isClosed: false,
            periods: [{ id: "p-ws3-013", startDate: "2027-05-15", endDate: "2027-06-15", isClosed: false }],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws3-014", name: "Odbiory końcowe", order: 2, colorRgb: "#9B2C2C", isClosed: false,
            periods: [{ id: "p-ws3-014", startDate: "2027-06-10", endDate: "2027-06-30", isClosed: false }],
            assignees: assignee({ userId: uid, userName: "Michał Kowalski" }),
            comments: [],
          },
        ],
      },
    ],
    dependencies: [
      { id: "dep-ws3-01", predecessorWorkId: "w-ws3-001", successorWorkId: "w-ws3-002", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws3-02", predecessorWorkId: "w-ws3-002", successorWorkId: "w-ws3-003", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws3-03", predecessorWorkId: "w-ws3-003", successorWorkId: "w-ws3-004", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws3-04", predecessorWorkId: "w-ws3-004", successorWorkId: "w-ws3-005", dependencyType: 0, lagDays: 5 },
      { id: "dep-ws3-05", predecessorWorkId: "w-ws3-005", successorWorkId: "w-ws3-006", dependencyType: 0, lagDays: 3 },
      { id: "dep-ws3-06", predecessorWorkId: "w-ws3-005", successorWorkId: "w-ws3-007", dependencyType: 1, lagDays: 5 },
      { id: "dep-ws3-07", predecessorWorkId: "w-ws3-006", successorWorkId: "w-ws3-008", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws3-08", predecessorWorkId: "w-ws3-008", successorWorkId: "w-ws3-009", dependencyType: 0, lagDays: 3 },
      { id: "dep-ws3-09", predecessorWorkId: "w-ws3-008", successorWorkId: "w-ws3-010", dependencyType: 1, lagDays: 0 },
      { id: "dep-ws3-10", predecessorWorkId: "w-ws3-011", successorWorkId: "w-ws3-012", dependencyType: 0, lagDays: 0 },
    ],
  };
}

function buildWs004Details(): object {
  return {
    id: "ws-004", tenantId: T2, projectId: P4, costEstimateId: "ce-005",
    name: "Harmonogram — Bud. A",
    createdAt: date("2026-01-05"), createdByUserId: "u-010", createdByUserName: "Ewa Majewska",
    stages: [
      {
        id: "stg-ws4-1", name: "1. Przygotowanie terenu", order: 0, parentStageId: null,
        works: [
          { id: "w-ws4-001", name: "Rozbiórki istniejących obiektów", order: 0, colorRgb: "#E53E3E", isClosed: true,
            periods: [
              { id: "p-ws4-001", startDate: "2026-01-15", endDate: "2026-02-15", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [
              comment("c-ws4-001", "Rozbiórki zakończone. Gruz wywieziony.", 120, "u-002", "Tomasz Wójcik"),
            ],
          },
          { id: "w-ws4-002", name: "Wykopy i przygotowanie pod fundamenty", order: 1, colorRgb: "#4A7FEF", isClosed: true,
            periods: [
              { id: "p-ws4-002", startDate: "2026-02-10", endDate: "2026-03-20", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [
              comment("c-ws4-002", "Wykopy gotowe. Poziom posadowienia zatwierdzony przez geodetę.", 90, "u-002", "Tomasz Wójcik"),
            ],
          },
          { id: "w-ws4-003", name: "Przyłącza tymczasowe (woda, energia)", order: 2, colorRgb: "#805AD5", isClosed: true,
            periods: [
              { id: "p-ws4-003", startDate: "2026-01-20", endDate: "2026-02-28", isClosed: true },
            ],
            assignees: assignee({ userId: "u-008", userName: "Piotr Zieliński" }),
            comments: [
              comment("c-ws4-003", "Przyłącza tymczasowe wykonane. Licznik energii zamontowany.", 110, "u-008", "Piotr Zieliński"),
            ],
          },
        ],
      },
      {
        id: "stg-ws4-2", name: "2. Fundamenty i konstrukcja", order: 1, parentStageId: null,
        works: [
          { id: "w-ws4-004", name: "Ławy i stopy fundamentowe", order: 0, colorRgb: "#E07B39", isClosed: false,
            periods: [
              { id: "p-ws4-004", startDate: "2026-03-15", endDate: "2026-04-30", isClosed: true },
              { id: "p-ws4-005", startDate: "2026-05-01", endDate: "2026-05-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [
              comment("c-ws4-004", "Ławy wylane w 80%. Oczekiwanie na dostawę stali zbrojeniowej.", 30, "u-002", "Tomasz Wójcik"),
            ],
          },
          { id: "w-ws4-005", name: "Ściany fundamentowe i izolacje", order: 1, colorRgb: "#DD6B20", isClosed: false,
            periods: [
              { id: "p-ws4-006", startDate: "2026-05-01", endDate: "2026-06-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws4-006", name: "Płyta fundamentowa", order: 2, colorRgb: "#3182CE", isClosed: false,
            periods: [
              { id: "p-ws4-007", startDate: "2026-06-01", endDate: "2026-07-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws4-3", name: "3. Stan surowy nadziemia", order: 2, parentStageId: null,
        works: [
          { id: "w-ws4-007", name: "Konstrukcja żelbetowa parteru", order: 0, colorRgb: "#805AD5", isClosed: false,
            periods: [
              { id: "p-ws4-008", startDate: "2026-07-01", endDate: "2026-09-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws4-008", name: "Stropy i wieńce", order: 1, colorRgb: "#38A169", isClosed: false,
            periods: [
              { id: "p-ws4-009", startDate: "2026-09-01", endDate: "2026-11-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws4-009", name: "Ściany działowe", order: 2, colorRgb: "#2B6CB0", isClosed: false,
            periods: [
              { id: "p-ws4-010", startDate: "2026-10-15", endDate: "2026-12-31", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws4-4", name: "4. Dach i pokrycie", order: 3, parentStageId: null,
        works: [
          { id: "w-ws4-010", name: "Więźba dachowa", order: 0, colorRgb: "#2C7A7B", isClosed: false,
            periods: [{ id: "p-ws4-011", startDate: "2026-11-01", endDate: "2027-01-15", isClosed: false }],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws4-011", name: "Pokrycie dachu i orynnowanie", order: 1, colorRgb: "#C05621", isClosed: false,
            periods: [{ id: "p-ws4-012", startDate: "2027-01-01", endDate: "2027-02-28", isClosed: false }],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws4-012", name: "Obróbki blacharskie i izolacje", order: 2, colorRgb: "#B83280", isClosed: false,
            periods: [{ id: "p-ws4-013", startDate: "2027-02-15", endDate: "2027-03-31", isClosed: false }],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
        ],
      },
    ],
    dependencies: [
      { id: "dep-ws4-01", predecessorWorkId: "w-ws4-001", successorWorkId: "w-ws4-002", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws4-02", predecessorWorkId: "w-ws4-001", successorWorkId: "w-ws4-003", dependencyType: 1, lagDays: 0 },
      { id: "dep-ws4-03", predecessorWorkId: "w-ws4-002", successorWorkId: "w-ws4-004", dependencyType: 0, lagDays: 3 },
      { id: "dep-ws4-04", predecessorWorkId: "w-ws4-004", successorWorkId: "w-ws4-005", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws4-05", predecessorWorkId: "w-ws4-005", successorWorkId: "w-ws4-006", dependencyType: 1, lagDays: 5 },
      { id: "dep-ws4-06", predecessorWorkId: "w-ws4-006", successorWorkId: "w-ws4-007", dependencyType: 0, lagDays: 7 },
      { id: "dep-ws4-07", predecessorWorkId: "w-ws4-007", successorWorkId: "w-ws4-008", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws4-08", predecessorWorkId: "w-ws4-008", successorWorkId: "w-ws4-009", dependencyType: 0, lagDays: 3 },
      { id: "dep-ws4-09", predecessorWorkId: "w-ws4-008", successorWorkId: "w-ws4-010", dependencyType: 0, lagDays: 0 },
    ],
  };
}

function buildWs005Details(): object {
  return {
    id: "ws-005", tenantId: T2, projectId: P4, costEstimateId: "ce-007",
    name: "Harmonogram — Apartamenty Centrum",
    createdAt: date("2025-07-20"), createdByUserId: uid, createdByUserName: "Michał Kowalski",
    stages: [
      {
        id: "stg-ws5-1", name: "1. Roboty przygotowawcze i ziemne", order: 0, parentStageId: null,
        works: [
          { id: "w-ws5-001", name: "Wyburzenia i przygotowanie placu", order: 0, colorRgb: "#E53E3E", isClosed: true,
            periods: [
              { id: "p-ws5-001", startDate: "2025-08-01", endDate: "2025-09-15", isClosed: true },
              { id: "p-ws5-002", startDate: "2025-09-16", endDate: "2025-09-30", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [
              comment("c-ws5-001", "Teren przygotowany. Wyburzenia zakończone zgodnie z planem.", 270, "u-002", "Tomasz Wójcik"),
            ],
          },
          { id: "w-ws5-002", name: "Wykopy głębokie pod garaż podziemny", order: 1, colorRgb: "#4A7FEF", isClosed: true,
            periods: [
              { id: "p-ws5-003", startDate: "2025-09-15", endDate: "2025-11-15", isClosed: true },
              { id: "p-ws5-004", startDate: "2025-11-16", endDate: "2025-12-15", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [
              comment("c-ws5-002", "Wykopy zrealizowane. Wzmocnienie ścian wykopu wykonane.", 185, "u-002", "Tomasz Wójcik"),
            ],
          },
          { id: "w-ws5-003", name: "Odwodnienie wykopu", order: 2, colorRgb: "#319795", isClosed: true,
            periods: [
              { id: "p-ws5-005", startDate: "2025-09-20", endDate: "2025-11-30", isClosed: true },
            ],
            assignees: assignee({ userId: "u-009", userName: "Krzysztof Baran" }),
            comments: [
              comment("c-ws5-003", "System odwodnienia działa. Poziom wód stabilny.", 190, "u-009", "Krzysztof Baran"),
            ],
          },
        ],
      },
      {
        id: "stg-ws5-2", name: "2. Garaż podziemny", order: 1, parentStageId: null,
        works: [
          { id: "w-ws5-004", name: "Płyta denna garażu", order: 0, colorRgb: "#805AD5", isClosed: true,
            periods: [
              { id: "p-ws5-006", startDate: "2025-11-15", endDate: "2025-12-31", isClosed: true },
              { id: "p-ws5-007", startDate: "2026-01-01", endDate: "2026-01-30", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [
              comment("c-ws5-004", "Płyta denna wylana. Izolacja przeciwwodna wykonana.", 140, "u-002", "Tomasz Wójcik"),
            ],
          },
          { id: "w-ws5-005", name: "Ściany garażu i słupy", order: 1, colorRgb: "#DD6B20", isClosed: true,
            periods: [
              { id: "p-ws5-008", startDate: "2026-01-01", endDate: "2026-03-15", isClosed: true },
              { id: "p-ws5-009", startDate: "2026-03-16", endDate: "2026-04-30", isClosed: true },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [
              comment("c-ws5-005", "Ściany żelbetowe garażu gotowe. Słupy w trakcie betonowania.", 80, "u-006", "Anna Nowak"),
            ],
          },
          { id: "w-ws5-006", name: "Strop nad garażem", order: 2, colorRgb: "#3182CE", isClosed: false,
            periods: [
              { id: "p-ws5-010", startDate: "2026-04-01", endDate: "2026-05-31", isClosed: true },
              { id: "p-ws5-011", startDate: "2026-06-01", endDate: "2026-06-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [
              comment("c-ws5-006", "Strop wylany w 70%. Przerwa technologiczna z powodu opóźnień w dostawie stali.", 15, "u-006", "Anna Nowak"),
            ],
          },
          { id: "w-ws5-007", name: "Wentylacja garażu", order: 3, colorRgb: "#6B46C1", isClosed: false,
            periods: [
              { id: "p-ws5-012", startDate: "2026-05-01", endDate: "2026-07-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-009", userName: "Krzysztof Baran" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws5-3", name: "3. Stan surowy nadziemia", order: 2, parentStageId: null,
        works: [
          { id: "w-ws5-008", name: "Ściany nośne parteru", order: 0, colorRgb: "#38A169", isClosed: false,
            periods: [
              { id: "p-ws5-013", startDate: "2026-05-15", endDate: "2026-07-31", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [
              comment("c-ws5-007", "Ściany parteru w trakcie. Opóźnienie ~2 tygodnie.", 25, "u-006", "Anna Nowak"),
            ],
          },
          { id: "w-ws5-009", name: "Stropy nad parterem", order: 1, colorRgb: "#805AD5", isClosed: false,
            periods: [
              { id: "p-ws5-014", startDate: "2026-07-01", endDate: "2026-08-31", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws5-010", name: "Ściany I piętra", order: 2, colorRgb: "#38A169", isClosed: false,
            periods: [
              { id: "p-ws5-015", startDate: "2026-08-15", endDate: "2026-10-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws5-011", name: "Strop nad I piętrem", order: 3, colorRgb: "#805AD5", isClosed: false,
            periods: [
              { id: "p-ws5-016", startDate: "2026-10-01", endDate: "2026-11-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws5-012", name: "Ściany II piętra", order: 4, colorRgb: "#38A169", isClosed: false,
            periods: [
              { id: "p-ws5-017", startDate: "2026-11-15", endDate: "2027-01-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws5-4", name: "4. Dach i elewacja", order: 3, parentStageId: null,
        works: [
          { id: "w-ws5-013", name: "Konstrukcja dachu", order: 0, colorRgb: "#2C7A7B", isClosed: false,
            periods: [
              { id: "p-ws5-018", startDate: "2026-12-01", endDate: "2027-01-31", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws5-014", name: "Pokrycie dachu", order: 1, colorRgb: "#C05621", isClosed: false,
            periods: [
              { id: "p-ws5-019", startDate: "2027-01-15", endDate: "2027-03-15", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws5-015", name: "Elewacja — docieplenie i tynki", order: 2, colorRgb: "#B83280", isClosed: false,
            periods: [
              { id: "p-ws5-020", startDate: "2026-10-01", endDate: "2026-12-31", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws5-016", name: "Stolarka okienna", order: 3, colorRgb: "#D69E2E", isClosed: false,
            periods: [
              { id: "p-ws5-021", startDate: "2026-11-01", endDate: "2027-01-31", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws5-5", name: "5. Instalacje wewnętrzne", order: 4, parentStageId: null,
        works: [
          { id: "w-ws5-017", name: "Elektryka — piony i rozprowadzenie", order: 0, colorRgb: "#D69E2E", isClosed: false,
            periods: [{ id: "p-ws5-022", startDate: "2026-08-01", endDate: "2026-12-31", isClosed: false }],
            assignees: assignee({ userId: "u-008", userName: "Piotr Zieliński" }),
            comments: [],
          },
          { id: "w-ws5-018", name: "Woda i kanalizacja — piony", order: 1, colorRgb: "#319795", isClosed: false,
            periods: [{ id: "p-ws5-023", startDate: "2026-08-15", endDate: "2026-12-31", isClosed: false }],
            assignees: assignee({ userId: "u-009", userName: "Krzysztof Baran" }),
            comments: [],
          },
          { id: "w-ws5-019", name: "C.O. i wentylacja", order: 2, colorRgb: "#E53E3E", isClosed: false,
            periods: [{ id: "p-ws5-024", startDate: "2026-09-01", endDate: "2027-01-31", isClosed: false }],
            assignees: assignee({ userId: "u-008", userName: "Piotr Zieliński" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws5-6", name: "6. Wykończenia i odbiory", order: 5, parentStageId: null,
        works: [
          { id: "w-ws5-020", name: "Tynki i gładzie", order: 0, colorRgb: "#2B6CB0", isClosed: false,
            periods: [{ id: "p-ws5-025", startDate: "2027-01-01", endDate: "2027-03-31", isClosed: false }],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws5-021", name: "Posadzki i płytki", order: 1, colorRgb: "#C05621", isClosed: false,
            periods: [{ id: "p-ws5-026", startDate: "2027-02-15", endDate: "2027-04-30", isClosed: false }],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws5-022", name: "Malowanie i wykończenia", order: 2, colorRgb: "#276749", isClosed: false,
            periods: [{ id: "p-ws5-027", startDate: "2027-04-01", endDate: "2027-05-31", isClosed: false }],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws5-023", name: "Zagospodarowanie terenu", order: 3, colorRgb: "#2C7A7B", isClosed: false,
            periods: [{ id: "p-ws5-028", startDate: "2027-04-01", endDate: "2027-05-31", isClosed: false }],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws5-024", name: "Odbiory końcowe i przekazanie", order: 4, colorRgb: "#9B2C2C", isClosed: false,
            periods: [{ id: "p-ws5-029", startDate: "2027-05-15", endDate: "2027-06-30", isClosed: false }],
            assignees: assignee({ userId: uid, userName: "Michał Kowalski" }, { userId: "u-010", userName: "Ewa Majewska" }),
            comments: [],
          },
        ],
      },
    ],
    dependencies: [
      { id: "dep-ws5-01", predecessorWorkId: "w-ws5-001", successorWorkId: "w-ws5-002", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws5-02", predecessorWorkId: "w-ws5-001", successorWorkId: "w-ws5-003", dependencyType: 1, lagDays: 3 },
      { id: "dep-ws5-03", predecessorWorkId: "w-ws5-002", successorWorkId: "w-ws5-004", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws5-04", predecessorWorkId: "w-ws5-003", successorWorkId: "w-ws5-004", dependencyType: 2, lagDays: 0 },
      { id: "dep-ws5-05", predecessorWorkId: "w-ws5-004", successorWorkId: "w-ws5-005", dependencyType: 0, lagDays: 3 },
      { id: "dep-ws5-06", predecessorWorkId: "w-ws5-005", successorWorkId: "w-ws5-006", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws5-07", predecessorWorkId: "w-ws5-005", successorWorkId: "w-ws5-007", dependencyType: 1, lagDays: 5 },
      { id: "dep-ws5-08", predecessorWorkId: "w-ws5-006", successorWorkId: "w-ws5-008", dependencyType: 0, lagDays: 7 },
      { id: "dep-ws5-09", predecessorWorkId: "w-ws5-008", successorWorkId: "w-ws5-009", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws5-10", predecessorWorkId: "w-ws5-008", successorWorkId: "w-ws5-017", dependencyType: 0, lagDays: 5 },
      { id: "dep-ws5-11", predecessorWorkId: "w-ws5-009", successorWorkId: "w-ws5-010", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws5-12", predecessorWorkId: "w-ws5-010", successorWorkId: "w-ws5-011", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws5-13", predecessorWorkId: "w-ws5-011", successorWorkId: "w-ws5-012", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws5-14", predecessorWorkId: "w-ws5-009", successorWorkId: "w-ws5-013", dependencyType: 0, lagDays: 5 },
      { id: "dep-ws5-15", predecessorWorkId: "w-ws5-014", successorWorkId: "w-ws5-016", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws5-16", predecessorWorkId: "w-ws5-015", successorWorkId: "w-ws5-022", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws5-17", predecessorWorkId: "w-ws5-023", successorWorkId: "w-ws5-024", dependencyType: 0, lagDays: 0 },
    ],
  };
}

function buildWs006Details(): object {
  return {
    id: "ws-006", tenantId: T1, projectId: P3, costEstimateId: "ce-006",
    name: "Harmonogram — Bud. A",
    createdAt: date("2025-12-15"), createdByUserId: uid, createdByUserName: "Michał Kowalski",
    stages: [
      {
        id: "stg-ws6-1", name: "1. Fundamenty i konstrukcja", order: 0, parentStageId: null, costEstimateGroupId: "ce-006-g-0",
        works: [
          { id: "w-ws6-001", name: "Wykopy wąskoprzestrzenne", order: 0, colorRgb: "#4A7FEF", isClosed: true,
            periods: [
              { id: "p-ws6-001", startDate: "2025-06-01", endDate: "2025-06-25", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [comment("c-ws6-001", "Wykopy zakończone zgodnie z planem.", 280, "u-002", "Tomasz Wójcik")],
          },
          { id: "w-ws6-002", name: "Ławy fundamentowe zbrojone", order: 1, colorRgb: "#E07B39", isClosed: true,
            periods: [
              { id: "p-ws6-002", startDate: "2025-07-01", endDate: "2025-08-15", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [comment("c-ws6-002", "Betonowanie ław zakończone.", 250, uid, "Michał Kowalski")],
          },
          { id: "w-ws6-003", name: "Słupy żelbetowe prefabrykowane", order: 2, colorRgb: "#805AD5", isClosed: false,
            periods: [
              { id: "p-ws6-003", startDate: "2025-10-01", endDate: "2025-12-20", isClosed: true },
              { id: "p-ws6-004", startDate: "2025-12-21", endDate: "2026-02-28", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [comment("c-ws6-003", "Opóźnienie dostaw prefabrykatów — 3 tygodnie.", 120, "u-006", "Anna Nowak")],
          },
          { id: "w-ws6-004", name: "Płyta żelbetowa stropodachu", order: 3, colorRgb: "#3182CE", isClosed: false,
            periods: [
              { id: "p-ws6-005", startDate: "2026-02-01", endDate: "2026-04-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
        ],
      },
      {
        id: "stg-ws6-2", name: "2. Ściany i wykończenia", order: 1, parentStageId: null, costEstimateGroupId: "ce-006-g-2",
        works: [
          { id: "w-ws6-005", name: "Ściany osłonowe z płyt warstwowych", order: 0, colorRgb: "#38A169", isClosed: false,
            periods: [
              { id: "p-ws6-006", startDate: "2026-03-01", endDate: "2026-05-31", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
          { id: "w-ws6-006", name: "Stolarka aluminiowa", order: 1, colorRgb: "#DD6B20", isClosed: false,
            periods: [
              { id: "p-ws6-007", startDate: "2026-05-01", endDate: "2026-07-31", isClosed: false },
            ],
            assignees: assignee({ userId: uid, userName: "Michał Kowalski" }),
            comments: [],
          },
          { id: "w-ws6-007", name: "Prace wykończeniowe wewnętrzne", order: 2, colorRgb: "#2B6CB0", isClosed: false,
            periods: [
              { id: "p-ws6-008", startDate: "2026-06-01", endDate: "2026-09-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-006", userName: "Anna Nowak" }),
            comments: [],
          },
        ],
      },
    ],
    dependencies: [
      { id: "dep-ws6-01", predecessorWorkId: "w-ws6-001", successorWorkId: "w-ws6-002", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws6-02", predecessorWorkId: "w-ws6-002", successorWorkId: "w-ws6-003", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws6-03", predecessorWorkId: "w-ws6-003", successorWorkId: "w-ws6-004", dependencyType: 0, lagDays: 5 },
      { id: "dep-ws6-04", predecessorWorkId: "w-ws6-004", successorWorkId: "w-ws6-005", dependencyType: 0, lagDays: 7 },
      { id: "dep-ws6-05", predecessorWorkId: "w-ws6-005", successorWorkId: "w-ws6-006", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws6-06", predecessorWorkId: "w-ws6-006", successorWorkId: "w-ws6-007", dependencyType: 0, lagDays: 5 },
    ],
  };
}

function buildWs007Details(): object {
  return {
    id: "ws-007", tenantId: T2, projectId: P5, costEstimateId: "ce-009",
    name: "Harmonogram — Rezydencja Jeziorki",
    createdAt: date("2026-02-01"), createdByUserId: uid, createdByUserName: "Michał Kowalski",
    stages: [
      {
        id: "stg-ws7-1", name: "1. Prace projektowe i przygotowawcze", order: 0, parentStageId: null,
        works: [
          { id: "w-ws7-001", name: "Projekt koncepcyjny", order: 0, colorRgb: "#805AD5", isClosed: true,
            periods: [
              { id: "p-ws7-001", startDate: "2025-07-01", endDate: "2025-08-31", isClosed: true },
            ],
            assignees: assignee({ userId: uid, userName: "Michał Kowalski" }),
            comments: [comment("c-ws7-001", "Koncepcja zatwierdzona przez inwestora.", 300, uid, "Michał Kowalski")],
          },
          { id: "w-ws7-002", name: "Badania geotechniczne", order: 1, colorRgb: "#4A7FEF", isClosed: true,
            periods: [
              { id: "p-ws7-002", startDate: "2026-02-01", endDate: "2026-03-15", isClosed: true },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws7-003", name: "Uzyskanie pozwoleń budowlanych", order: 2, colorRgb: "#D69E2E", isClosed: false,
            periods: [
              { id: "p-ws7-003", startDate: "2026-03-01", endDate: "2026-05-31", isClosed: false },
            ],
            assignees: assignee({ userId: uid, userName: "Michał Kowalski" }),
            comments: [comment("c-ws7-002", "Wniosek złożony — oczekiwanie na decyzję.", 60, uid, "Michał Kowalski")],
          },
        ],
      },
      {
        id: "stg-ws7-2", name: "2. Roboty budowlane (planowane)", order: 1, parentStageId: null,
        works: [
          { id: "w-ws7-004", name: "Przygotowanie placu budowy", order: 0, colorRgb: "#38A169", isClosed: false,
            periods: [
              { id: "p-ws7-004", startDate: "2026-07-01", endDate: "2026-08-31", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
          { id: "w-ws7-005", name: "Fundamenty — etap I", order: 1, colorRgb: "#E07B39", isClosed: false,
            periods: [
              { id: "p-ws7-005", startDate: "2026-09-01", endDate: "2026-11-30", isClosed: false },
            ],
            assignees: assignee({ userId: "u-002", userName: "Tomasz Wójcik" }),
            comments: [],
          },
        ],
      },
    ],
    dependencies: [
      { id: "dep-ws7-01", predecessorWorkId: "w-ws7-001", successorWorkId: "w-ws7-002", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws7-02", predecessorWorkId: "w-ws7-002", successorWorkId: "w-ws7-003", dependencyType: 0, lagDays: 0 },
      { id: "dep-ws7-03", predecessorWorkId: "w-ws7-003", successorWorkId: "w-ws7-004", dependencyType: 0, lagDays: 14 },
      { id: "dep-ws7-04", predecessorWorkId: "w-ws7-004", successorWorkId: "w-ws7-005", dependencyType: 0, lagDays: 0 },
    ],
  };
}

/** Mapa wsId → builder funkcji */
const scheduleDetailsMap: Record<string, () => object> = {
  "ws-001": buildWs001Details,
  "ws-002": buildWs002Details,
  "ws-003": buildWs003Details,
  "ws-004": buildWs004Details,
  "ws-005": buildWs005Details,
  "ws-006": buildWs006Details,
  "ws-007": buildWs007Details,
};

/** @deprecated Używaj getWorkSchedules(projectId, scope) */
export const mockWorkSchedules = getWorkSchedules(P1);

/** @deprecated Używaj getWorkScheduleDetails(wsId) */
export const mockWorkScheduleDetails = getWorkScheduleDetails("ws-001");

// ---- PROJECT COSTS (Dokumentacja kosztowa) ----
// CostApprovalStatus: 'Draft' | 'PendingApproval' | 'Approved'

// ---- PER-PROJECT COSTS ----

/** Build a cost item with proper defaults */
function cost(
  id: string, userId: string, userName: string, name: string, number: string,
  contractorId: string | null, contractorName: string | null, dateStr: string,
  net: number, gross: number, approvalStatus: string,
  approvedByUserId: string | null, approvedAt: string | null,
  hasDocument: boolean = true, fileName?: string
) {
  return {
    id, userId, userName, name, number,
    contractorId, contractorName,
    date: date(dateStr),
    net, gross, approvalStatus,
    approvedByUserId, approvedAt: approvedAt ? date(approvedAt) : null,
    hasDocument,
    documentFileName: fileName || (hasDocument ? `${id}.pdf` : undefined),
    createdAt: date(dateStr),
  };
}

const projectCostsMap: Record<string, any[]> = {
  "p-001": [
    cost("pc-001", uid, "Michał Kowalski", "Faktura nr FV/08/1245 — Materiały budowlane", "FV/2025/08/1245", "ctr-003", "Dębickie Przedsiębiorstwo Budowlane", "2025-08-15", 187500, 230625, "Approved", "u-006", "2025-08-20"),
    cost("pc-002", "u-002", "Tomasz Wójcik", "Faktura 321/09/2025 — Wynajem szalunków", "321/09/2025", "ctr-004", "Erbet Sp. z o.o.", "2025-09-10", 45600, 56088, "Approved", "u-006", "2025-09-15"),
    cost("pc-003", uid, "Michał Kowalski", "FA/10/089 — Beton towarowy B25", "FA/10/2025/089", "ctr-005", "Cemex Polska Sp. z o.o.", "2025-10-05", 324000, 398520, "Approved", "u-006", "2025-10-12"),
    cost("pc-004", "u-002", "Tomasz Wójcik", "Faktura 11/567 — Prace zbrojarskie", "FV/11/2025/567", "ctr-001", "Budimex S.A.", "2025-11-20", 215000, 264450, "Approved", "u-006", "2025-11-28"),
    cost("pc-005", uid, "Michał Kowalski", "FV 0012/25 — Bloczki silikatowe 24cm", "FV 0012/25", "ctr-008", "Wienerberger Ceramika Budowlana", "2025-12-05", 178900, 220047, "Approved", "u-006", "2025-12-10"),
    cost("pc-006", uid, "Michał Kowalski", "Faktura 45/01/2026 — Stolarka okienna PCV", "45/01/2026", "ctr-006", "Saint-Gobain Construction Products", "2026-01-15", 412000, 506760, "PendingApproval", null, null),
    cost("pc-007", "u-009", "Krzysztof Baran", "Rachunek 02/2026 — Kable i osprzęt elektryczny", "R/02/2026/01", "ctr-007", "Elektromontaż Rzeszów S.A.", "2026-02-10", 156300, 192249, "PendingApproval", null, null),
    cost("pc-008", uid, "Michał Kowalski", "FV/03/045 — Transport i wynajem dźwigu", "FV/03/2026/045", "ctr-002", "Strabag Sp. z o.o.", "2026-03-01", 67800, 83394, "Approved", "u-006", "2026-03-05", false),
    cost("pc-009", "u-002", "Tomasz Wójcik", "Faktura 67/03 — Izolacje termiczne", "67/03/2026", "ctr-012", "InsBud — Izolacje Budowlane", "2026-03-20", 98400, 121032, "Approved", uid, "2026-03-25"),
    cost("pc-010", uid, "Michał Kowalski", "FV/04/112 — Wentylacja — projekt", "FV/04/2026/112", "ctr-011", "WentSystemy Sp. z o.o.", "2026-04-05", 134500, 165435, "Draft", null, null, false),
  ],
  "p-002": [
    cost("pc-020", uid, "Michał Kowalski", "Faktura 15/06/2025 — Roboty ziemne Etap II", "15/06/2025", "ctr-002", "Strabag Sp. z o.o.", "2025-06-20", 245000, 301350, "Approved", "u-006", "2025-06-28"),
    cost("pc-021", uid, "Michał Kowalski", "FA/08/2025/234 — Beton B20 fundamenty", "FA/08/2025/234", "ctr-005", "Cemex Polska Sp. z o.o.", "2025-08-10", 186000, 228780, "Approved", "u-006", "2025-08-18"),
    cost("pc-022", "u-008", "Piotr Zieliński", "FV/09/2025/89 — Stal zbrojeniowa", "FV/09/2025/89", "ctr-001", "Budimex S.A.", "2025-09-05", 312000, 383760, "PendingApproval", null, null),
    cost("pc-023", uid, "Michał Kowalski", "Rachunek 11/2025 — Wynajem koparki", "R/11/2025/03", "ctr-004", "Erbet Sp. z o.o.", "2025-11-15", 42500, 52275, "Approved", "u-008", "2025-11-20"),
    cost("pc-024", "u-008", "Piotr Zieliński", "FV/01/2026/007 — Prace murarskie", "FV/01/2026/007", "ctr-008", "Wienerberger Ceramika Budowlana", "2026-01-10", 154000, 189420, "Approved", uid, "2026-01-18"),
    cost("pc-025", uid, "Michał Kowalski", "FV/03/2026/118 — Izolacje dachu", "FV/03/2026/118", "ctr-012", "InsBud — Izolacje Budowlane", "2026-03-15", 87200, 107256, "PendingApproval", null, null),
    cost("pc-026", uid, "Michał Kowalski", "FV/04/2026/201 — Ogrodzenie placu budowy", "FV/04/2026/201", "ctr-002", "Strabag Sp. z o.o.", "2026-04-02", 31200, 38376, "Draft", null, null, false),
  ],
  "p-003": [
    cost("pc-030", uid, "Michał Kowalski", "Faktura 10/06/2025 — Fundamenty Bud. A", "10/06/2025", "ctr-001", "Budimex S.A.", "2025-06-25", 195000, 239850, "Approved", "u-006", "2025-07-02"),
    cost("pc-031", "u-002", "Tomasz Wójcik", "FA/09/2025/56 — Beton B25", "FA/09/2025/56", "ctr-005", "Cemex Polska Sp. z o.o.", "2025-09-20", 142000, 174660, "Approved", uid, "2025-09-28"),
    cost("pc-032", uid, "Michał Kowalski", "FV/12/2025/34 — Stropy gęstożebrowe", "FV/12/2025/34", "ctr-001", "Budimex S.A.", "2025-12-10", 278000, 341940, "PendingApproval", null, null),
    cost("pc-033", "u-002", "Tomasz Wójcik", "Rachunek 02/2026 — Prace wykończeniowe", "R/02/2026/07", "ctr-006", "Saint-Gobain Construction Products", "2026-02-20", 63500, 78105, "Approved", uid, "2026-02-25"),
    cost("pc-034", uid, "Michał Kowalski", "FV/04/2026/55 — Drzwi wewnętrzne", "FV/04/2026/55", "ctr-006", "Saint-Gobain Construction Products", "2026-04-10", 89100, 109593, "Draft", null, null, false),
  ],
  "p-004": [
    cost("pc-011", uid, "Michał Kowalski", "Faktura 22/07/2025 — Stropy żelbetowe", "22/07/2025", "ctr-001", "Budimex S.A.", "2025-07-25", 845000, 1039350, "Approved", "u-010", "2025-08-01"),
    cost("pc-012", "u-010", "Ewa Majewska", "Faktura 89/09/2025 — Prace ziemne", "89/09/2025", "ctr-002", "Strabag Sp. z o.o.", "2025-09-15", 320000, 393600, "Approved", uid, "2025-09-20"),
    cost("pc-013", "u-010", "Ewa Majewska", "FV 156/11/2025 — Instalacje hydrauliczne", "156/11/2025", "ctr-014", "Hydrobudowa Kraków S.A.", "2025-11-20", 275000, 338250, "PendingApproval", null, null),
    cost("pc-014", uid, "Michał Kowalski", "FV/01/234 — Beton B30 dostawa", "FV/01/2026/234", "ctr-005", "Cemex Polska Sp. z o.o.", "2026-01-18", 198000, 243540, "Approved", "u-010", "2026-01-22"),
    cost("pc-015", "u-010", "Ewa Majewska", "FV/03/078 — Kotłownia i rurociągi", "FV/03/2026/078", "ctr-015", "Technika Grzewcza Rzeszów", "2026-03-12", 167000, 205410, "Draft", null, null, false),
    cost("pc-035", "u-010", "Ewa Majewska", "Faktura 45/04/2026 — Elewacja szklana", "45/04/2026", "ctr-006", "Saint-Gobain Construction Products", "2026-04-05", 562000, 691260, "PendingApproval", null, null),
    cost("pc-036", uid, "Michał Kowalski", "FV/05/2026/88 — Klimatyzacja apartamentów", "FV/05/2026/88", "ctr-011", "WentSystemy Sp. z o.o.", "2026-05-10", 234000, 287820, "Approved", "u-010", "2026-05-18"),
    cost("pc-037", "u-010", "Ewa Majewska", "FV/06/2026/12 — Windy osobowe", "FV/06/2026/12", "ctr-004", "Erbet Sp. z o.o.", "2026-06-01", 890000, 1094700, "Draft", null, null, false),
  ],
  "p-005": [
    cost("pc-040", uid, "Michał Kowalski", "Faktura 08/2025 — Projekt koncepcyjny", "08/2025/PROJ", "ctr-003", "Dębickie Przedsiębiorstwo Budowlane", "2025-08-10", 45000, 55350, "Approved", "u-010", "2025-08-15"),
    cost("pc-041", uid, "Michał Kowalski", "FV/01/2026/03 — Wizualizacje 3D", "FV/01/2026/03", null, null, "2026-01-20", 18500, 22755, "PendingApproval", null, null),
    cost("pc-042", "u-002", "Tomasz Wójcik", "Rachunek 03/2026 — Badania geotechniczne", "R/03/2026/01", "ctr-003", "Dębickie Przedsiębiorstwo Budowlane", "2026-03-05", 22000, 27060, "Draft", null, null, false),
  ],
};

/**
 * Zwraca koszty dla konkretnego projektu, opcjonalnie filtrowane po scope.
 */
export function getProjectCosts(projectId: string, scope?: string): any[] {
  const allCosts = projectCostsMap[projectId] || projectCostsMap["p-001"] || [];
  if (!scope || scope === "all") return allCosts;

  if (scope === "mine") {
    return allCosts.filter((c: any) => c.userId === uid);
  }

  if (scope === "PendingApproval") {
    return allCosts.filter((c: any) => c.approvalStatus === "PendingApproval");
  }

  return allCosts;
}

/** Backwards compatibility: wszystkie koszty projektu P1 */
export const mockProjectCosts = getProjectCosts("p-001");

// ---- TRACKED COSTS (dla dashboard — TrackedCostWeb) ----
// workScheduleStageWorkId używa ID z harmonogramów (w-wsX-xxx)
// costEstimateItemId używa pełnych ID pozycji kosztorysowych (ce-XXX-i-bXXX)

const mockProjectCostCategories: Record<string, Array<{ id: string; name: string; color: string; order: number }>> = {
  [P1]: [
    { id: "cat-p1-001", name: "Materiały", color: "#3182CE", order: 0 },
    { id: "cat-p1-002", name: "Robocizna", color: "#38A169", order: 1 },
    { id: "cat-p1-003", name: "Sprzęt", color: "#DD6B20", order: 2 },
    { id: "cat-p1-004", name: "Usługi", color: "#805AD5", order: 3 },
    { id: "cat-p1-005", name: "Inne", color: "#718096", order: 4 },
  ],
  [P2]: [
    { id: "cat-p2-001", name: "Materiały", color: "#3182CE", order: 0 },
    { id: "cat-p2-002", name: "Robocizna", color: "#38A169", order: 1 },
    { id: "cat-p2-003", name: "Sprzęt", color: "#DD6B20", order: 2 },
    { id: "cat-p2-004", name: "Usługi", color: "#805AD5", order: 3 },
    { id: "cat-p2-005", name: "Inne", color: "#718096", order: 4 },
  ],
  [P3]: [
    { id: "cat-p3-001", name: "Materiały", color: "#3182CE", order: 0 },
    { id: "cat-p3-002", name: "Robocizna", color: "#38A169", order: 1 },
    { id: "cat-p3-003", name: "Sprzęt", color: "#DD6B20", order: 2 },
    { id: "cat-p3-004", name: "Usługi", color: "#805AD5", order: 3 },
    { id: "cat-p3-005", name: "Inne", color: "#718096", order: 4 },
  ],
  [P4]: [
    { id: "cat-p4-001", name: "Materiały", color: "#3182CE", order: 0 },
    { id: "cat-p4-002", name: "Robocizna", color: "#38A169", order: 1 },
    { id: "cat-p4-003", name: "Sprzęt", color: "#DD6B20", order: 2 },
    { id: "cat-p4-004", name: "Usługi", color: "#805AD5", order: 3 },
    { id: "cat-p4-005", name: "Inne", color: "#718096", order: 4 },
  ],
  [P5]: [
    { id: "cat-p5-001", name: "Materiały", color: "#3182CE", order: 0 },
    { id: "cat-p5-002", name: "Robocizna", color: "#38A169", order: 1 },
    { id: "cat-p5-003", name: "Sprzęt", color: "#DD6B20", order: 2 },
    { id: "cat-p5-004", name: "Usługi", color: "#805AD5", order: 3 },
    { id: "cat-p5-005", name: "Inne", color: "#718096", order: 4 },
  ],
};

function cat(projectId: string, categoryId: string): { categoryId: string; categoryName: string; categoryColor: string } {
  const categories = mockProjectCostCategories[projectId] ?? mockProjectCostCategories[P1];
  const found = categories.find(c => c.id === categoryId);
  return {
    categoryId,
    categoryName: found?.name ?? "Inne",
    categoryColor: found?.color ?? "#718096",
  };
}

const trackedCostsP1 = [
  { id: "tc-001", costEstimateItemId: "ce-001-i-b001", workScheduleStageWorkId: "w-ws1-001", isAdditional: false, name: "Wykopy fundamentowe — realizacja", description: null, net: 187500, gross: 230625, vatRate: 23, contractorId: "ctr-003", contractorName: "Dębickie Przedsiębiorstwo Budowlane", ...cat(P1, "cat-p1-002"), date: date("2025-08-15"), number: "FV/2025/08/1245", attachments: [], createdAt: date("2025-08-16"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap I", estimateGroupName: "1. Roboty ziemne i fundamentowe", estimateItemName: "Wykopy pod fundamenty", costEstimateItemPath: "Kosztorys budowlany — Etap I > 1. Roboty ziemne i fundamentowe > Wykopy pod fundamenty", workScheduleWorkPath: null },
  { id: "tc-002", costEstimateItemId: "ce-001-i-b002", workScheduleStageWorkId: "w-ws1-002", isAdditional: false, name: "Fundamenty — szalunki i beton", description: null, net: 324000, gross: 398520, vatRate: 23, contractorId: "ctr-005", contractorName: "Cemex Polska Sp. z o.o.", ...cat(P1, "cat-p1-001"), date: date("2025-10-05"), number: "FA/10/2025/089", attachments: [], createdAt: date("2025-10-06"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap I", estimateGroupName: "1. Roboty ziemne i fundamentowe", estimateItemName: "Ławy fundamentowe żelbetowe", costEstimateItemPath: "Kosztorys budowlany — Etap I > 1. Roboty ziemne i fundamentowe > Ławy fundamentowe żelbetowe", workScheduleWorkPath: null },
  { id: "tc-003", costEstimateItemId: "ce-001-i-b005", workScheduleStageWorkId: "w-ws1-004", isAdditional: false, name: "Zbrojenie słupów i stropów", description: "Prace zbrojarskie — słupy i stropy", net: 215000, gross: 264450, vatRate: 23, contractorId: "ctr-001", contractorName: "Budimex S.A.", ...cat(P1, "cat-p1-002"), date: date("2025-11-20"), number: "FV/11/2025/567", attachments: [], createdAt: date("2025-11-21"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap I", estimateGroupName: "2. Konstrukcja żelbetowa", estimateItemName: "Słupy żelbetowe 40×40 cm", costEstimateItemPath: "Kosztorys budowlany — Etap I > 2. Konstrukcja żelbetowa > Słupy żelbetowe 40×40 cm", workScheduleWorkPath: null },
  { id: "tc-004", costEstimateItemId: "ce-001-i-b008", workScheduleStageWorkId: "w-ws1-005", isAdditional: false, name: "Bloczki silikatowe — dostawa i murowanie", description: null, net: 178900, gross: 220047, vatRate: 23, contractorId: "ctr-008", contractorName: "Wienerberger Ceramika Budowlana", ...cat(P1, "cat-p1-001"), date: date("2025-12-05"), number: "FV 0012/25", attachments: [], createdAt: date("2025-12-06"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap I", estimateGroupName: "3. Ściany i elewacja", estimateItemName: "Ściany nośne z bloczków silikatowych", costEstimateItemPath: "Kosztorys budowlany — Etap I > 3. Ściany i elewacja > Ściany nośne z bloczków silikatowych", workScheduleWorkPath: null },
  { id: "tc-005", costEstimateItemId: "ce-001-i-b010", workScheduleStageWorkId: null, isAdditional: false, name: "Stolarka okienna PCV — zamówienie", description: "Zamówienie na produkcję okien", net: 412000, gross: 506760, vatRate: 23, contractorId: "ctr-006", contractorName: "Saint-Gobain Construction Products", ...cat(P1, "cat-p1-001"), date: date("2026-01-15"), number: "45/01/2026", attachments: [], createdAt: date("2026-01-16"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap I", estimateGroupName: "3. Ściany i elewacja", estimateItemName: "Stolarka okienna PCV 3-szybowa", costEstimateItemPath: "Kosztorys budowlany — Etap I > 3. Ściany i elewacja > Stolarka okienna PCV 3-szybowa", workScheduleWorkPath: null },
  { id: "tc-006", costEstimateItemId: null, workScheduleStageWorkId: "w-ws1-007", isAdditional: false, name: "Kable i osprzęt elektryczny", description: null, net: 156300, gross: 192249, vatRate: 23, contractorId: "ctr-007", contractorName: "Elektromontaż Rzeszów S.A.", ...cat(P1, "cat-p1-001"), date: date("2026-02-10"), number: "R/02/2026/01", attachments: [], createdAt: date("2026-02-11"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Etap I", stageName: "2. Instalacje wewnętrzne", workItemName: "Instalacja elektryczna", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Etap I > 2. Instalacje wewnętrzne > Instalacja elektryczna" },
  { id: "tc-007", costEstimateItemId: null, workScheduleStageWorkId: "w-ws1-003", isAdditional: false, name: "Transport i wynajem dźwigu", description: null, net: 67800, gross: 83394, vatRate: 23, contractorId: "ctr-002", contractorName: "Strabag Sp. z o.o.", ...cat(P1, "cat-p1-003"), date: date("2026-03-01"), number: "FV/03/2026/045", attachments: [], createdAt: date("2026-03-02"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Etap I", stageName: "1. Stan surowy otwarty", workItemName: "Ściany nośne parteru", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Etap I > 1. Stan surowy otwarty > Ściany nośne parteru" },
  { id: "tc-008", costEstimateItemId: null, workScheduleStageWorkId: "w-ws1-009", isAdditional: false, name: "Izolacje termiczne — wełna i styropian", description: null, net: 98400, gross: 121032, vatRate: 23, contractorId: "ctr-012", contractorName: "InsBud — Izolacje Budowlane", ...cat(P1, "cat-p1-001"), date: date("2026-03-20"), number: "67/03/2026", attachments: [], createdAt: date("2026-03-21"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Etap I", stageName: "2. Instalacje wewnętrzne", workItemName: "Instalacja grzewcza", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Etap I > 2. Instalacje wewnętrzne > Instalacja grzewcza" },
  { id: "tc-009", costEstimateItemId: null, workScheduleStageWorkId: null, isAdditional: true, name: "Projekt wentylacji — honorarium", description: "Koszty projektowe dodatkowe", net: 134500, gross: 165435, vatRate: 23, contractorId: "ctr-011", contractorName: "WentSystemy Sp. z o.o.", ...cat(P1, "cat-p1-004"), date: date("2026-04-05"), number: "FV/04/2026/112", attachments: [], createdAt: date("2026-04-06"), updatedAt: null, sourceType: "ProjectAdditional" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: null },
  { id: "tc-010", costEstimateItemId: null, workScheduleStageWorkId: null, isAdditional: true, name: "Szalunki — wynajem", description: "Wynajem szalunków systemowych", net: 45600, gross: 56088, vatRate: 23, contractorId: "ctr-004", contractorName: "Erbet Sp. z o.o.", ...cat(P1, "cat-p1-003"), date: date("2025-09-10"), number: "321/09/2025", attachments: [], createdAt: date("2025-09-11"), updatedAt: null, sourceType: "ProjectAdditional" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: null },
  { id: "tc-011", costEstimateItemId: "ce-002-i-s001", workScheduleStageWorkId: "w-ws2-002", isAdditional: false, name: "Rurociągi wodociągowe PP-R — montaż", description: null, net: 245000, gross: 301350, vatRate: 23, contractorId: "ctr-009", contractorName: "Wodoinstal Kraków Sp. z o.o.", ...cat(P1, "cat-p1-001"), date: date("2025-11-01"), number: "FV/11/2025/301", attachments: [], createdAt: date("2025-11-02"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys instalacji sanitarnych", estimateGroupName: "1. Instalacje wodociągowe", estimateItemName: "Rurociągi wodociągowe PP-R", costEstimateItemPath: "Kosztorys instalacji sanitarnych > 1. Instalacje wodociągowe > Rurociągi wodociągowe PP-R", workScheduleWorkPath: null },
  { id: "tc-012", costEstimateItemId: "ce-002-i-s004", workScheduleStageWorkId: "w-ws2-004", isAdditional: false, name: "Kotłownia gazowa — dostawa i montaż", description: null, net: 520000, gross: 639600, vatRate: 23, contractorId: "ctr-015", contractorName: "Technika Grzewcza Rzeszów", ...cat(P1, "cat-p1-002"), date: date("2026-01-20"), number: "FV/01/2026/445", attachments: [], createdAt: date("2026-01-21"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys instalacji sanitarnych", estimateGroupName: "2. Instalacje grzewcze i wentylacja", estimateItemName: "Kotłownia gazowa z instalacją CO", costEstimateItemPath: "Kosztorys instalacji sanitarnych > 2. Instalacje grzewcze i wentylacja > Kotłownia gazowa z instalacją CO", workScheduleWorkPath: null },
  { id: "tc-013", costEstimateItemId: "ce-003-i-s001", workScheduleStageWorkId: null, isAdditional: false, name: "Kable i przewody elektryczne YKY 5×10", description: null, net: 385000, gross: 473550, vatRate: 23, contractorId: "ctr-007", contractorName: "Elektromontaż Rzeszów S.A.", ...cat(P1, "cat-p1-001"), date: date("2026-02-28"), number: "FV/02/2026/178", attachments: [], createdAt: date("2026-03-01"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys elektryki i teletechniki", estimateGroupName: "1. Instalacje elektryczne wewnętrzne", estimateItemName: "Kable i przewody elektryczne YKY 5×10", costEstimateItemPath: "Kosztorys elektryki i teletechniki > 1. Instalacje elektryczne wewnętrzne > Kable i przewody elektryczne YKY 5×10", workScheduleWorkPath: null },
  { id: "tc-014", costEstimateItemId: "ce-003-i-s003", workScheduleStageWorkId: null, isAdditional: false, name: "Oprawy oświetleniowe LED", description: null, net: 198000, gross: 243540, vatRate: 23, contractorId: "ctr-007", contractorName: "Elektromontaż Rzeszów S.A.", ...cat(P1, "cat-p1-001"), date: date("2026-03-15"), number: "FV/03/2026/289", attachments: [], createdAt: date("2026-03-16"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys elektryki i teletechniki", estimateGroupName: "1. Instalacje elektryczne wewnętrzne", estimateItemName: "Oprawy oświetleniowe LED", costEstimateItemPath: "Kosztorys elektryki i teletechniki > 1. Instalacje elektryczne wewnętrzne > Oprawy oświetleniowe LED", workScheduleWorkPath: null },
];

// ---- PER-PROJECT TRACKED COSTS ----

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const trackedCostsP2: any[] = [
  { id: "tc-p2-001", costEstimateItemId: "ce-004-i-b001", workScheduleStageWorkId: "w-ws3-001", isAdditional: false, name: "Roboty ziemne Etap II", description: null, net: 245000, gross: 301350, vatRate: 23, contractorId: "ctr-002", contractorName: "Strabag Sp. z o.o.", ...cat(P2, "cat-p2-002"), date: date("2025-06-20"), number: "15/06/2025", attachments: [], createdAt: date("2025-06-21"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap II", estimateGroupName: "1. Roboty ziemne i konstrukcja", estimateItemName: "Wykopy pod ławy i stopy fundamentowe", costEstimateItemPath: "Kosztorys budowlany — Etap II > 1. Roboty ziemne i konstrukcja > Wykopy pod ławy i stopy fundamentowe", workScheduleWorkPath: null },
  { id: "tc-p2-002", costEstimateItemId: "ce-004-i-b002", workScheduleStageWorkId: "w-ws3-003", isAdditional: false, name: "Beton B20 fundamenty Etap II", description: null, net: 186000, gross: 228780, vatRate: 23, contractorId: "ctr-005", contractorName: "Cemex Polska Sp. z o.o.", ...cat(P2, "cat-p2-001"), date: date("2025-08-10"), number: "FA/08/2025/234", attachments: [], createdAt: date("2025-08-11"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap II", estimateGroupName: "1. Roboty ziemne i konstrukcja", estimateItemName: "Ściany fundamentowe żelbetowe", costEstimateItemPath: "Kosztorys budowlany — Etap II > 1. Roboty ziemne i konstrukcja > Ściany fundamentowe żelbetowe", workScheduleWorkPath: null },
  { id: "tc-p2-003", costEstimateItemId: "ce-004-i-b005", workScheduleStageWorkId: "w-ws3-005", isAdditional: false, name: "Stal zbrojeniowa — konstrukcja", description: null, net: 312000, gross: 383760, vatRate: 23, contractorId: "ctr-001", contractorName: "Budimex S.A.", ...cat(P2, "cat-p2-001"), date: date("2025-09-05"), number: "FV/09/2025/89", attachments: [], createdAt: date("2025-09-06"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap II", estimateGroupName: "2. Ściany konstrukcyjne i stropy", estimateItemName: "Słupy żelbetowe 30×50 cm", costEstimateItemPath: "Kosztorys budowlany — Etap II > 2. Ściany konstrukcyjne i stropy > Słupy żelbetowe 30×50 cm", workScheduleWorkPath: null },
  { id: "tc-p2-004", costEstimateItemId: null, workScheduleStageWorkId: null, isAdditional: true, name: "Wynajem koparki — Etap II", description: null, net: 42500, gross: 52275, vatRate: 23, contractorId: "ctr-004", contractorName: "Erbet Sp. z o.o.", ...cat(P2, "cat-p2-003"), date: date("2025-11-15"), number: "R/11/2025/03", attachments: [], createdAt: date("2025-11-16"), updatedAt: null, sourceType: "ProjectAdditional" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: null },
  { id: "tc-p2-005", costEstimateItemId: "ce-004-i-b008", workScheduleStageWorkId: "w-ws3-007", isAdditional: false, name: "Prace murarskie — ściany", description: null, net: 154000, gross: 189420, vatRate: 23, contractorId: "ctr-008", contractorName: "Wienerberger Ceramika Budowlana", ...cat(P2, "cat-p2-002"), date: date("2026-01-10"), number: "FV/01/2026/007", attachments: [], createdAt: date("2026-01-11"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap II", estimateGroupName: "3. Izolacje i elewacja", estimateItemName: "Elewacja — tynk mineralny", costEstimateItemPath: "Kosztorys budowlany — Etap II > 3. Izolacje i elewacja > Elewacja — tynk mineralny", workScheduleWorkPath: null },
  { id: "tc-p2-006", costEstimateItemId: "ce-005-i-l001", workScheduleStageWorkId: null, isAdditional: false, name: "Nawierzchnia z kostki brukowej", description: null, net: 87200, gross: 107256, vatRate: 23, contractorId: "ctr-010", contractorName: "GreenScape Architektura Krajobrazu", ...cat(P2, "cat-p2-001"), date: date("2026-03-15"), number: "FV/03/2026/118", attachments: [], createdAt: date("2026-03-16"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys zagospodarowania terenu", estimateGroupName: "1. Nawierzchnie i drogi", estimateItemName: "Nawierzchnia z kostki brukowej", costEstimateItemPath: "Kosztorys zagospodarowania terenu > 1. Nawierzchnie i drogi > Nawierzchnia z kostki brukowej", workScheduleWorkPath: null },
  { id: "tc-p2-007", costEstimateItemId: "ce-005-i-l003", workScheduleStageWorkId: null, isAdditional: false, name: "Nasadzenia drzew i krzewów", description: null, net: 64500, gross: 79335, vatRate: 23, contractorId: "ctr-010", contractorName: "GreenScape Architektura Krajobrazu", ...cat(P2, "cat-p2-002"), date: date("2026-04-02"), number: "FV/04/2026/201", attachments: [], createdAt: date("2026-04-03"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys zagospodarowania terenu", estimateGroupName: "2. Zieleń i mała architektura", estimateItemName: "Nasadzenia drzew i krzewów", costEstimateItemPath: "Kosztorys zagospodarowania terenu > 2. Zieleń i mała architektura > Nasadzenia drzew i krzewów", workScheduleWorkPath: null },
];

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const trackedCostsP3: any[] = [
  { id: "tc-p3-001", costEstimateItemId: "ce-006-i-b001", workScheduleStageWorkId: "w-ws6-001", isAdditional: false, name: "Fundamenty Bud. A — wykopy", description: null, net: 195000, gross: 239850, vatRate: 23, contractorId: "ctr-001", contractorName: "Budimex S.A.", ...cat(P3, "cat-p3-002"), date: date("2025-06-25"), number: "10/06/2025", attachments: [], createdAt: date("2025-06-26"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Bud. A", estimateGroupName: "1. Fundamenty i izolacje", estimateItemName: "Wykopy wąskoprzestrzenne pod ławy", costEstimateItemPath: "Kosztorys budowlany — Bud. A > 1. Fundamenty i izolacje > Wykopy wąskoprzestrzenne pod ławy", workScheduleWorkPath: null },
  { id: "tc-p3-002", costEstimateItemId: "ce-006-i-b002", workScheduleStageWorkId: "w-ws6-002", isAdditional: false, name: "Beton B25 — ławy fundamentowe", description: null, net: 142000, gross: 174660, vatRate: 23, contractorId: "ctr-005", contractorName: "Cemex Polska Sp. z o.o.", ...cat(P3, "cat-p3-001"), date: date("2025-09-20"), number: "FA/09/2025/56", attachments: [], createdAt: date("2025-09-21"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Bud. A", estimateGroupName: "1. Fundamenty i izolacje", estimateItemName: "Ławy fundamentowe zbrojone ciągłe", costEstimateItemPath: "Kosztorys budowlany — Bud. A > 1. Fundamenty i izolacje > Ławy fundamentowe zbrojone ciągłe", workScheduleWorkPath: null },
  { id: "tc-p3-003", costEstimateItemId: "ce-006-i-b005", workScheduleStageWorkId: "w-ws6-003", isAdditional: false, name: "Stropy gęstożebrowe — dostawa", description: null, net: 278000, gross: 341940, vatRate: 23, contractorId: "ctr-001", contractorName: "Budimex S.A.", ...cat(P3, "cat-p3-001"), date: date("2025-12-10"), number: "FV/12/2025/34", attachments: [], createdAt: date("2025-12-11"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Bud. A", estimateGroupName: "2. Konstrukcja nośna", estimateItemName: "Słupy żelbetowe prefabrykowane", costEstimateItemPath: "Kosztorys budowlany — Bud. A > 2. Konstrukcja nośna > Słupy żelbetowe prefabrykowane", workScheduleWorkPath: null },
  { id: "tc-p3-004", costEstimateItemId: null, workScheduleStageWorkId: "w-ws6-005", isAdditional: false, name: "Prace wykończeniowe — tynki", description: null, net: 63500, gross: 78105, vatRate: 23, contractorId: "ctr-006", contractorName: "Saint-Gobain Construction Products", ...cat(P3, "cat-p3-002"), date: date("2026-02-20"), number: "R/02/2026/07", attachments: [], createdAt: date("2026-02-21"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Bud. A", stageName: "2. Ściany i wykończenia", workItemName: "Ściany osłonowe z płyt warstwowych", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Bud. A > 2. Ściany i wykończenia > Ściany osłonowe z płyt warstwowych" },
  { id: "tc-p3-005", costEstimateItemId: null, workScheduleStageWorkId: null, isAdditional: true, name: "Drzwi wewnętrzne — zamówienie", description: null, net: 89100, gross: 109593, vatRate: 23, contractorId: "ctr-006", contractorName: "Saint-Gobain Construction Products", ...cat(P3, "cat-p3-001"), date: date("2026-04-10"), number: "FV/04/2026/55", attachments: [], createdAt: date("2026-04-11"), updatedAt: null, sourceType: "ProjectAdditional" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: null },
];

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const trackedCostsP4: any[] = [
  { id: "tc-p4-001", costEstimateItemId: "ce-007-i-b001", workScheduleStageWorkId: "w-ws4-001", isAdditional: false, name: "Przygotowanie terenu — Bud. A", description: null, net: 312000, gross: 383760, vatRate: 23, contractorId: "ctr-002", contractorName: "Strabag Sp. z o.o.", ...cat(P4, "cat-p4-002"), date: date("2026-02-01"), number: "FV/02/2026/201", attachments: [], createdAt: date("2026-02-02"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys główny — Faza I", estimateGroupName: "1. Roboty rozbiórkowe i przygotowawcze", estimateItemName: "Wykopy głębokie pod budynek", costEstimateItemPath: "Kosztorys główny — Faza I > 1. Roboty rozbiórkowe i przygotowawcze > Wykopy głębokie pod budynek", workScheduleWorkPath: null },
  { id: "tc-p4-002", costEstimateItemId: "ce-007-i-b002", workScheduleStageWorkId: "w-ws4-003", isAdditional: false, name: "Pale fundamentowe — Bud. A", description: null, net: 198000, gross: 243540, vatRate: 23, contractorId: "ctr-003", contractorName: "Dębickie Przedsiębiorstwo Budowlane", ...cat(P4, "cat-p4-001"), date: date("2026-03-10"), number: "FV/03/2026/089", attachments: [], createdAt: date("2026-03-11"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys główny — Faza I", estimateGroupName: "1. Roboty rozbiórkowe i przygotowawcze", estimateItemName: "Płyta fundamentowa żelbetowa", costEstimateItemPath: "Kosztorys główny — Faza I > 1. Roboty rozbiórkowe i przygotowawcze > Płyta fundamentowa żelbetowa", workScheduleWorkPath: null },
  { id: "tc-p4-003", costEstimateItemId: "ce-007-i-b005", workScheduleStageWorkId: "w-ws4-006", isAdditional: false, name: "Konstrukcja żelbetowa — apartamenty", description: null, net: 567000, gross: 697410, vatRate: 23, contractorId: "ctr-001", contractorName: "Budimex S.A.", ...cat(P4, "cat-p4-002"), date: date("2026-05-15"), number: "FV/05/2026/456", attachments: [], createdAt: date("2026-05-16"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys główny — Faza I", estimateGroupName: "2. Konstrukcja żelbetowa apartamentowca", estimateItemName: "Słupy żelbetowe 60×60 cm zbrojone", costEstimateItemPath: "Kosztorys główny — Faza I > 2. Konstrukcja żelbetowa apartamentowca > Słupy żelbetowe 60×60 cm zbrojone", workScheduleWorkPath: null },
  { id: "tc-p4-004", costEstimateItemId: "ce-007-i-b008", workScheduleStageWorkId: "w-ws5-002", isAdditional: false, name: "Ściany osłonowe — elewacja", description: null, net: 445000, gross: 547350, vatRate: 23, contractorId: "ctr-006", contractorName: "Saint-Gobain Construction Products", ...cat(P4, "cat-p4-001"), date: date("2025-09-20"), number: "FV/09/2025/789", attachments: [], createdAt: date("2025-09-21"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys główny — Faza I", estimateGroupName: "3. Elewacja i stolarka", estimateItemName: "Elewacja szklana z panelami aluminiowymi", costEstimateItemPath: "Kosztorys główny — Faza I > 3. Elewacja i stolarka > Elewacja szklana z panelami aluminiowymi", workScheduleWorkPath: null },
  { id: "tc-p4-005", costEstimateItemId: null, workScheduleStageWorkId: null, isAdditional: true, name: "Projekt konstrukcji apartamentów", description: null, net: 98500, gross: 121155, vatRate: 23, contractorId: "ctr-011", contractorName: "WentSystemy Sp. z o.o.", ...cat(P4, "cat-p4-004"), date: date("2025-08-01"), number: "FV/08/2025/001", attachments: [], createdAt: date("2025-08-02"), updatedAt: null, sourceType: "ProjectAdditional" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: null },
  { id: "tc-p4-006", costEstimateItemId: "ce-007-i-b012", workScheduleStageWorkId: "w-ws5-005", isAdditional: false, name: "Instalacje sanitarne — apartamenty", description: null, net: 278000, gross: 341940, vatRate: 23, contractorId: "ctr-009", contractorName: "Wodoinstal Kraków Sp. z o.o.", ...cat(P4, "cat-p4-002"), date: date("2026-04-05"), number: "FV/04/2026/312", attachments: [], createdAt: date("2026-04-06"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys główny — Faza I", estimateGroupName: "4. Instalacje i wykończenie", estimateItemName: "Instalacje sanitarne apartamentów", costEstimateItemPath: "Kosztorys główny — Faza I > 4. Instalacje i wykończenie > Instalacje sanitarne apartamentów", workScheduleWorkPath: null },
  { id: "tc-p4-007", costEstimateItemId: "ce-008-i-r001", workScheduleStageWorkId: null, isAdditional: false, name: "Płyta denna garażu podziemnego", description: null, net: 425000, gross: 522750, vatRate: 23, contractorId: "ctr-001", contractorName: "Budimex S.A.", ...cat(P4, "cat-p4-001"), date: date("2025-11-15"), number: "FV/11/2025/890", attachments: [], createdAt: date("2025-11-16"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys garażu podziemnego", estimateGroupName: "1. Konstrukcja garażu", estimateItemName: "Płyta denna żelbetowa 30 cm", costEstimateItemPath: "Kosztorys garażu podziemnego > 1. Konstrukcja garażu > Płyta denna żelbetowa 30 cm", workScheduleWorkPath: null },
  { id: "tc-p4-008", costEstimateItemId: "ce-008-i-r004", workScheduleStageWorkId: null, isAdditional: false, name: "Posadzka epoksydowa garażu", description: null, net: 156000, gross: 191880, vatRate: 23, contractorId: "ctr-004", contractorName: "Erbet Sp. z o.o.", ...cat(P4, "cat-p4-001"), date: date("2026-01-22"), number: "FV/01/2026/567", attachments: [], createdAt: date("2026-01-23"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys garażu podziemnego", estimateGroupName: "2. Wykończenie garażu", estimateItemName: "Posadzka epoksydowa garażu", costEstimateItemPath: "Kosztorys garażu podziemnego > 2. Wykończenie garażu > Posadzka epoksydowa garażu", workScheduleWorkPath: null },
];

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const trackedCostsP5: any[] = [
  { id: "tc-p5-001", costEstimateItemId: "ce-009-i-p001", workScheduleStageWorkId: "w-ws7-001", isAdditional: false, name: "Projekt koncepcyjny — Rezydencja", description: null, net: 45000, gross: 55350, vatRate: 23, contractorId: "ctr-003", contractorName: "Dębickie Przedsiębiorstwo Budowlane", ...cat(P5, "cat-p5-004"), date: date("2025-08-10"), number: "08/2025/PROJ", attachments: [], createdAt: date("2025-08-11"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys wstępny — Rezydencja", estimateGroupName: "1. Prace projektowe", estimateItemName: "Projekt koncepcyjny i wstępne koszty", costEstimateItemPath: "Kosztorys wstępny — Rezydencja > 1. Prace projektowe > Projekt koncepcyjny i wstępne koszty", workScheduleWorkPath: null },
  { id: "tc-p5-002", costEstimateItemId: null, workScheduleStageWorkId: null, isAdditional: true, name: "Wizualizacje 3D", description: null, net: 18500, gross: 22755, vatRate: 23, contractorId: null, contractorName: null, ...cat(P5, "cat-p5-004"), date: date("2026-01-20"), number: "FV/01/2026/03", attachments: [], createdAt: date("2026-01-21"), updatedAt: null, sourceType: "ProjectAdditional" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: null },
  { id: "tc-p5-003", costEstimateItemId: "ce-009-i-p002", workScheduleStageWorkId: "w-ws7-002", isAdditional: false, name: "Badania geotechniczne", description: null, net: 22000, gross: 27060, vatRate: 23, contractorId: "ctr-003", contractorName: "Dębickie Przedsiębiorstwo Budowlane", ...cat(P5, "cat-p5-004"), date: date("2026-03-05"), number: "R/03/2026/01", attachments: [], createdAt: date("2026-03-06"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys wstępny — Rezydencja", estimateGroupName: "1. Prace projektowe", estimateItemName: "Badania geotechniczne i pomiary", costEstimateItemPath: "Kosztorys wstępny — Rezydencja > 1. Prace projektowe > Badania geotechniczne i pomiary", workScheduleWorkPath: null },
];

/** Mapa kosztów śledzonych per projekt */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const trackedCostsByProject: Record<string, any[]> = {
  [P1]: trackedCostsP1,
  [P2]: trackedCostsP2,
  [P3]: trackedCostsP3,
  [P4]: trackedCostsP4,
  [P5]: trackedCostsP5,
};

// ---- DASHBOARD — computed from source data ----

const DASHBOARD_TODAY = new Date();

function sumNet(items: any[]): number {
  return items.reduce((s: number, c: any) => s + (c.net ?? 0), 0);
}

function sumGross(items: any[]): number {
  return items.reduce((s: number, c: any) => s + (c.gross ?? 0), 0);
}

function getWorkLatestEnd(work: { periods?: Array<{ endDate: string }> }): string | null {
  const periods = work.periods ?? [];
  if (periods.length === 0) {
    return null;
  }
  return periods.reduce((max: string, p) => (!max || p.endDate > max ? p.endDate : max), "");
}

function isWorkDelayed(work: { isClosed: boolean; periods?: Array<{ endDate: string }> }): boolean {
  if (work.isClosed) {
    return false;
  }
  const latestEnd = getWorkLatestEnd(work);
  if (!latestEnd) {
    return false;
  }
  return new Date(latestEnd) < DASHBOARD_TODAY;
}

function isWorkInProgress(work: { isClosed: boolean; periods?: Array<{ startDate: string }> }): boolean {
  if (work.isClosed) {
    return false;
  }
  return (work.periods ?? []).some((p) => new Date(p.startDate) <= DASHBOARD_TODAY);
}

function computeWorkStats(allWorks: any[]) {
  let earliestStart: string | null = null;
  let latestEnd: string | null = null;
  let completedCount = 0;
  let inProgressCount = 0;
  let notStartedCount = 0;
  let delayedCount = 0;

  for (const w of allWorks) {
    for (const p of (w.periods ?? [])) {
      if (!earliestStart || p.startDate < earliestStart) {
        earliestStart = p.startDate;
      }
      if (!latestEnd || p.endDate > latestEnd) {
        latestEnd = p.endDate;
      }
    }
    if (w.isClosed) {
      completedCount++;
    } else if (isWorkDelayed(w)) {
      delayedCount++;
    } else if (isWorkInProgress(w)) {
      inProgressCount++;
    } else {
      notStartedCount++;
    }
  }

  const totalPlannedDays = earliestStart && latestEnd
    ? Math.round((new Date(latestEnd).getTime() - new Date(earliestStart).getTime()) / (1000 * 86400))
    : 0;
  const progressPercent = allWorks.length > 0 ? Math.round((completedCount / allWorks.length) * 100) : 0;
  const isDelayed = delayedCount > 0;
  const isCompleted = allWorks.length > 0 && completedCount === allWorks.length;
  let overallStatus = 1;
  if (allWorks.length === 0) {
    overallStatus = 6;
  } else if (isCompleted) {
    overallStatus = 4;
  } else if (isDelayed) {
    overallStatus = 3;
  } else if (inProgressCount > 0) {
    overallStatus = 2;
  }

  return {
    earliestStart,
    latestEnd,
    totalPlannedDays,
    completedCount,
    inProgressCount,
    notStartedCount,
    delayedCount,
    progressPercent,
    overallStatus,
    isDelayed,
    isCompleted,
  };
}

function buildTimelineStats(allWorks: any[]) {
  const stats = computeWorkStats(allWorks);
  if (!stats.earliestStart) {
    return null;
  }
  return {
    plannedStart: stats.earliestStart,
    plannedEnd: stats.latestEnd,
    totalPlannedDays: stats.totalPlannedDays,
    totalWorkCount: allWorks.length,
    completedCount: stats.completedCount,
    completedLateCount: 0,
    inProgressCount: stats.inProgressCount,
    notStartedCount: stats.notStartedCount,
    delayedCount: stats.delayedCount,
    progressPercent: stats.progressPercent,
    delayDays: null,
    overallStatus: stats.overallStatus,
    isDelayed: stats.isDelayed,
    isCompleted: stats.isCompleted,
  };
}

function computeFinancialStatus(budgetNet: number, costsNet: number, costCount: number): number {
  if (costCount === 0) {
    return 1;
  }
  if (budgetNet <= 0) {
    return 2;
  }
  const ratio = costsNet / budgetNet;
  if (ratio > 1) {
    return 4;
  }
  if (ratio >= 0.8) {
    return 3;
  }
  return 2;
}

function buildEstimateItemToEstimateMap(): Map<string, string> {
  const map = new Map<string, string>();
  for (const ceId of Object.keys(estimateMetaMap)) {
    const details = getCostEstimateDetailsById(ceId) as { rootGroups?: Array<{ items?: Array<{ id: string }> }> };
    for (const group of details.rootGroups ?? []) {
      for (const item of group.items ?? []) {
        map.set(item.id, ceId);
      }
    }
  }
  return map;
}

function getCostsForEstimate(
  estimateId: string,
  linkedCosts: any[],
  itemToEstimate: Map<string, string>,
): any[] {
  return linkedCosts.filter((c) => {
    if (!c.costEstimateItemId) {
      return false;
    }
    const itemId: string = c.costEstimateItemId;
    if (itemToEstimate.get(itemId) === estimateId) {
      return true;
    }
    return itemId.startsWith(`${estimateId}-`);
  });
}

function countEstimateItems(estimateId: string): number {
  const details = getCostEstimateDetailsById(estimateId) as { rootGroups?: Array<{ items?: Array<unknown> }> };
  return (details.rootGroups ?? []).reduce((sum, g) => sum + (g.items?.length ?? 0), 0);
}

function computeCostByCategory(costs: any[]) {
  const grouped = new Map<string | null, { categoryName: string; color: string | null; net: number; gross: number; costsCount: number }>();

  for (const c of costs) {
    const key = c.categoryId ?? null;
    const existing = grouped.get(key) ?? {
      categoryName: c.categoryName ?? "Bez kategorii",
      color: c.categoryColor ?? null,
      net: 0,
      gross: 0,
      costsCount: 0,
    };
    existing.net += c.net ?? 0;
    existing.gross += c.gross ?? 0;
    existing.costsCount += 1;
    grouped.set(key, existing);
  }

  return Array.from(grouped.entries())
    .map(([categoryId, data]) => ({
      categoryId,
      categoryName: data.categoryName,
      color: data.color,
      net: data.net,
      gross: data.gross > 0 ? data.gross : null,
      costsCount: data.costsCount,
    }))
    .sort((a, b) => b.net - a.net);
}

function getProjectReserveBudget(projectId: string): { net: number | null; gross: number | null } {
  if (projectId === P1) {
    return { net: 500000, gross: 615000 };
  }
  if (projectId === P4) {
    return { net: 750000, gross: 922500 };
  }
  return { net: null, gross: null };
}

/** Wylicza dashboard dla projektu na podstawie kosztów, kosztorysów i harmonogramów */
export function getDashboard(projectId: string): object {
  const allCosts = trackedCostsByProject[projectId] || [];
  const project = allProjects.find((p) => p.id === projectId);
  const currencyCode = project?.currency?.code ?? "PLN";
  const currencySymbol = project?.currency?.symbol ?? "zł";

  const additionalCosts = allCosts.filter((c) => c.isAdditional === true);
  const linkedCosts = allCosts.filter((c) => c.isAdditional !== true);

  const totalCostsNet = sumNet(allCosts);
  const totalCostsGross = sumGross(allCosts);
  const linkedCostsNet = sumNet(linkedCosts);
  const linkedCostsGross = sumGross(linkedCosts);
  const additionalCostsNet = sumNet(additionalCosts);
  const additionalCostsGross = sumGross(additionalCosts);

  const projectEstimates = mockCostEstimates.filter((e) => e.projectId === projectId);
  const totalBudgetNet = projectEstimates.reduce((s: number, ce: any) => s + (ce.totalNet ?? 0), 0);
  const totalBudgetGross = projectEstimates.reduce((s: number, ce: any) => s + (ce.totalGross ?? 0), 0);
  const reserve = getProjectReserveBudget(projectId);

  const deviationNet = totalBudgetNet - totalCostsNet;
  const deviationGross = totalBudgetGross - totalCostsGross;
  const coveredPercent = totalBudgetNet > 0 ? Math.round((linkedCostsNet / totalBudgetNet) * 10000) / 100 : 0;

  const itemToEstimate = buildEstimateItemToEstimateMap();
  const projectSchedules = scheduleListData.filter((s) => s.projectId === projectId);

  const costEstimateSummaries = projectEstimates.map((ce, index) => {
    const ceCosts = getCostsForEstimate(ce.id, linkedCosts, itemToEstimate);
    const ceCostsNet = sumNet(ceCosts);
    const ceCostsGross = sumGross(ceCosts);
    const totalItemsCount = countEstimateItems(ce.id);
    const itemsWithCostsCount = ceCosts.length;
    const meta = estimateMetaMap[ce.id];
    const linkedWorkScheduleId = meta?.workScheduleId ?? null;
    let scheduleWorks: any[] = [];
    if (linkedWorkScheduleId) {
      const wsDetail: any = getWorkScheduleDetails(linkedWorkScheduleId);
      scheduleWorks = (wsDetail?.stages ?? []).flatMap((s: any) => s.works ?? []);
    }
    const scheduleStats = scheduleWorks.length > 0 ? computeWorkStats(scheduleWorks) : null;
    const financialStatus = computeFinancialStatus(ce.totalNet ?? 0, ceCostsNet, ceCosts.length);

    return {
      costEstimateId: ce.id,
      costEstimateName: ce.name,
      order: index,
      budgetNet: ce.totalNet ?? 0,
      budgetGross: ce.totalGross ?? 0,
      costsNet: ceCostsNet,
      costsGross: ceCostsGross,
      deviationNet: (ce.totalNet ?? 0) - ceCostsNet,
      deviationGross: (ce.totalGross ?? 0) - ceCostsGross,
      deviationPercent: (ce.totalNet ?? 0) > 0 ? Math.round((ceCostsNet / (ce.totalNet ?? 0)) * 10000) / 100 : 0,
      isBudgetExceeded: ceCostsNet > (ce.totalNet ?? 0),
      costCount: ceCosts.length,
      coveredPercent: (ce.totalNet ?? 0) > 0 ? Math.round((ceCostsNet / (ce.totalNet ?? 0)) * 10000) / 100 : 0,
      totalItemsCount,
      itemsWithCostsCount,
      itemsWithoutCostsCount: totalItemsCount - itemsWithCostsCount,
      itemsOverBudgetCount: 0,
      itemsNearLimitCount: (ce.totalNet ?? 0) > 0 && ceCostsNet / (ce.totalNet ?? 0) >= 0.8 ? 1 : 0,
      financialStatus,
      timelineStatus: scheduleStats?.overallStatus ?? (linkedWorkScheduleId ? 1 : 0),
      hasLinkedSchedule: linkedWorkScheduleId !== null,
      linkedWorkScheduleId,
      timelinePlannedStart: scheduleStats?.earliestStart ?? null,
      timelinePlannedEnd: scheduleStats?.latestEnd ?? null,
      timelineTotalDays: scheduleStats?.totalPlannedDays ?? null,
      timeline: scheduleStats ? buildTimelineStats(scheduleWorks) : null,
      groups: [],
      additionalCosts: {
        totalNet: null,
        totalGross: null,
        costsCount: 0,
        costs: [],
      },
    };
  });

  const scheduleSummaries = projectSchedules.map((ws) => {
    const wsDetail: any = getWorkScheduleDetails(ws.id);
    const stages = wsDetail?.stages ?? [];
    const allWorks = stages.flatMap((s: any) => s.works ?? []);
    const wsCosts = linkedCosts.filter((c) =>
      c.workScheduleStageWorkId && allWorks.some((w: any) => w.id === c.workScheduleStageWorkId),
    );
    const wsCostsNet = sumNet(wsCosts);
    const wsCostsGross = sumGross(wsCosts);
    const wsStats = computeWorkStats(allWorks);
    const linkedEstimate = ws.costEstimateId ? projectEstimates.find((e) => e.id === ws.costEstimateId) : null;
    const budgetNet = linkedEstimate?.totalNet ?? null;
    const budgetGross = linkedEstimate?.totalGross ?? null;
    const delayedWorksCount = allWorks.filter((w: any) => isWorkDelayed(w)).length;

    const stageSummaries = stages.map((stg: any) => {
      const stgCosts = linkedCosts.filter((c: any) =>
        c.workScheduleStageWorkId && (stg.works ?? []).some((w: any) => w.id === c.workScheduleStageWorkId),
      );
      const stgCostsNet = sumNet(stgCosts);
      const stgCostsGross = sumGross(stgCosts);
      const stgWorks = stg.works ?? [];
      const stgStats = computeWorkStats(stgWorks);
      const stgDelayed = stgWorks.filter((w: any) => isWorkDelayed(w)).length;

      return {
        stageId: stg.id,
        stageName: stg.name,
        order: stg.order,
        totalWorkItemsCount: stgWorks.length,
        completedWorkItemsCount: stgStats.completedCount,
        delayedWorkItemsCount: stgDelayed,
        totalCostsNet: stgCostsNet > 0 ? stgCostsNet : null,
        totalCostsGross: stgCostsGross > 0 ? stgCostsGross : null,
        budgetNet: null,
        budgetGross: null,
        costsNet: stgCostsNet > 0 ? stgCostsNet : null,
        costsGross: stgCostsGross > 0 ? stgCostsGross : null,
        deviationNet: null,
        deviationGross: null,
        deviationPercent: null,
        coveredPercent: null,
        isBudgetExceeded: false,
        costCount: stgCosts.length,
        financialStatus: computeFinancialStatus(0, stgCostsNet, stgCosts.length),
        timelineStatus: stgStats.overallStatus,
        hasLinkedSchedule: true,
        timeline: buildTimelineStats(stgWorks),
        workItems: [],
        childStages: [],
      };
    });

    return {
      workScheduleId: ws.id,
      workScheduleName: ws.name,
      order: projectSchedules.indexOf(ws),
      hasLinkedEstimate: !!ws.costEstimateId,
      linkedCostEstimateId: ws.costEstimateId ?? null,
      totalWorkItemsCount: allWorks.length,
      workItemsWithCostsCount: wsCosts.length,
      workItemsOverBudgetCount: 0,
      workItemsNearLimitCount: 0,
      workItemsDelayedCount: delayedWorksCount,
      totalCostsNet: wsCostsNet > 0 ? wsCostsNet : null,
      totalCostsGross: wsCostsGross > 0 ? wsCostsGross : null,
      budgetNet,
      budgetGross,
      costsNet: wsCostsNet > 0 ? wsCostsNet : null,
      costsGross: wsCostsGross > 0 ? wsCostsGross : null,
      deviationNet: wsCostsNet > 0 && budgetNet !== null ? budgetNet - wsCostsNet : null,
      deviationGross: wsCostsGross > 0 && budgetGross !== null ? budgetGross - wsCostsGross : null,
      deviationPercent: budgetNet && budgetNet > 0
        ? Math.round((wsCostsNet / budgetNet) * 10000) / 100
        : null,
      coveredPercent: budgetNet && budgetNet > 0
        ? Math.round((wsCostsNet / budgetNet) * 10000) / 100
        : null,
      isBudgetExceeded: budgetNet !== null && wsCostsNet > budgetNet,
      costCount: wsCosts.length,
      financialStatus: computeFinancialStatus(budgetNet ?? 0, wsCostsNet, wsCosts.length),
      timelineStatus: wsStats.overallStatus,
      hasLinkedSchedule: !!ws.costEstimateId,
      timeline: buildTimelineStats(allWorks),
      stages: stageSummaries,
    };
  });

  const allProjectWorks: any[] = [];
  for (const ws of projectSchedules) {
    const detail: any = getWorkScheduleDetails(ws.id);
    allProjectWorks.push(...(detail?.stages ?? []).flatMap((s: any) => s.works ?? []));
  }
  const globalStats = computeWorkStats(allProjectWorks);

  const schedulesWithCosts = scheduleSummaries.filter((s) => (s.costCount ?? 0) > 0);
  const schedulesWithoutCosts = scheduleSummaries.filter((s) => (s.costCount ?? 0) === 0);
  const totalSchedulesCostsNet = sumNet(scheduleSummaries.map((s) => ({ net: s.totalCostsNet ?? 0 })));
  const totalSchedulesCostsGross = sumGross(scheduleSummaries.map((s) => ({ gross: s.totalCostsGross ?? 0 })));
  const costByCategory = computeCostByCategory(allCosts);

  return {
    projectId,
    selectedCurrencyCode: currencyCode,
    selectedCurrencySymbol: currencySymbol,
    referenceDate: now,
    generatedAt: now,
    financialSummary: {
      totalBudgetNet: totalBudgetNet + (reserve.net ?? 0),
      totalBudgetGross: totalBudgetGross + (reserve.gross ?? 0),
      estimateBudgetNet: totalBudgetNet,
      estimateBudgetGross: totalBudgetGross,
      projectReserveBudgetNet: reserve.net,
      projectReserveBudgetGross: reserve.gross,
      totalCostsNet,
      totalCostsGross,
      linkedCostsNet,
      linkedCostsGross,
      additionalCostsNet,
      additionalCostsGross,
      deviationNet,
      deviationGross,
      deviationPercent: Math.abs(coveredPercent),
      coveredPercent,
      isBudgetExceeded: totalCostsNet > totalBudgetNet,
      financialStatus: computeFinancialStatus(totalBudgetNet, totalCostsNet, allCosts.length),
      totalCostCount: allCosts.length,
      linkedCostCount: linkedCosts.length,
      additionalCostCount: additionalCosts.length,
      costEstimatesCount: projectEstimates.length,
      costEstimatesWithCostsCount: costEstimateSummaries.filter((s) => s.costCount > 0).length,
      costEstimatesOverBudgetCount: costEstimateSummaries.filter((s) => s.isBudgetExceeded).length,
      workSchedulesCount: projectSchedules.length,
      scheduleCostSummary: {
        totalSchedulesCostsNet,
        totalSchedulesCostsGross,
        schedulesWithCostsCount: schedulesWithCosts.length,
        schedulesWithoutCostsCount: schedulesWithoutCosts.length,
      },
    },
    timelineSummary: {
      earliestStart: globalStats.earliestStart,
      latestEnd: globalStats.latestEnd,
      totalPlannedDays: globalStats.totalPlannedDays,
      totalWorkCount: allProjectWorks.length,
      completedCount: globalStats.completedCount,
      completedLateCount: 0,
      inProgressCount: globalStats.inProgressCount,
      notStartedCount: globalStats.notStartedCount,
      delayedCount: globalStats.delayedCount,
      progressPercent: globalStats.progressPercent,
      delayDays: null,
      overallStatus: globalStats.overallStatus,
      isDelayed: globalStats.isDelayed,
      isCompleted: globalStats.isCompleted,
      workSchedulesCount: projectSchedules.length,
      activeSchedulesCount: projectSchedules.length,
      completedSchedulesCount: 0,
    },
    costEstimateSummaries,
    scheduleSummaries,
    projectAdditionalCosts: {
      totalNet: additionalCostsNet,
      totalGross: additionalCostsGross,
      costsCount: additionalCosts.length,
      costs: additionalCosts,
    },
    allCosts,
    costByCategory,
  };
}

/** @deprecated Używaj getDashboard(projectId) */
export const mockDashboard = getDashboard(P1);

// ---- CHATS (ChatWeb) ----
const chatMembers = [
  { userId: uid, firstName: "Michał", lastName: "Kowalski", joinedAt: date("2025-06-01"), isAdmin: true, lastReadAt: now },
  { userId: "u-002", firstName: "Tomasz", lastName: "Wójcik", joinedAt: date("2025-06-01"), isAdmin: false, lastReadAt: date("2026-01-28") },
  { userId: "u-006", firstName: "Anna", lastName: "Nowak", joinedAt: date("2025-07-01"), isAdmin: false, lastReadAt: date("2026-02-05") },
  { userId: "u-008", firstName: "Piotr", lastName: "Zieliński", joinedAt: date("2025-06-15"), isAdmin: false, lastReadAt: date("2026-03-01") },
  { userId: "u-009", firstName: "Krzysztof", lastName: "Baran", joinedAt: date("2025-08-01"), isAdmin: false, lastReadAt: date("2026-02-10") },
];

export const mockChats = [
  { id: "chat-001", name: "Zespół projektowy — Zielone Wzgórza", isGroupChat: true, projectId: P1, tenantId: T1, createdAt: date("2025-06-01"), createdByUserId: uid, unreadCount: 2, lastMessage: { id: "msg-012", chatId: "chat-001", senderId: "u-002", senderFirstName: "Tomasz", senderLastName: "Wójcik", content: "Czy fundamenty zostały już zalane? Jutro przyjeżdża ekipa od szalunków.", isDeleted: false, isEdited: false, sentAt: date("2026-01-28"), editedAt: null, replyToMessageId: null }, members: chatMembers.slice(0, 4) },
  { id: "chat-002", name: "Koszty i budżet", isGroupChat: true, projectId: P1, tenantId: T1, createdAt: date("2025-07-01"), createdByUserId: uid, unreadCount: 0, lastMessage: { id: "msg-008", chatId: "chat-002", senderId: "u-006", senderFirstName: "Anna", senderLastName: "Nowak", content: "Zaakceptowałem fakturę od Cemexu. Kwota 324 000 PLN netto za beton.", isDeleted: false, isEdited: false, sentAt: date("2026-02-05"), editedAt: null, replyToMessageId: null }, members: chatMembers.slice(0, 3) },
  { id: "chat-003", name: "Piotr Zieliński", isGroupChat: false, projectId: null, tenantId: T1, createdAt: date("2025-06-15"), createdByUserId: uid, unreadCount: 1, lastMessage: { id: "msg-012c", chatId: "chat-003", senderId: "u-008", senderFirstName: "Piotr", senderLastName: "Zieliński", content: "Przesyłam poprawiony projekt elewacji z uwzględnieniem uwag inwestora.", isDeleted: false, isEdited: false, sentAt: date("2026-03-01"), editedAt: null, replyToMessageId: null }, members: [chatMembers[0], chatMembers[3]] },
  { id: "chat-004", name: "Budinvest — Apartamenty Centrum", isGroupChat: true, projectId: P4, tenantId: T2, createdAt: date("2025-07-15"), createdByUserId: uid, unreadCount: 0, lastMessage: { id: "msg-020", chatId: "chat-004", senderId: "u-010", senderFirstName: "Ewa", senderLastName: "Majewska", content: "Zakończono prace ziemne etapu 2. Przechodzimy do fundamentów części B.", isDeleted: false, isEdited: false, sentAt: date("2026-03-15"), editedAt: null, replyToMessageId: null }, members: [chatMembers[0], { userId: "u-010", firstName: "Ewa", lastName: "Majewska", joinedAt: date("2025-07-15"), isAdmin: false, lastReadAt: now }] },
];

export const mockMessages: Record<string, unknown[]> = {
  "chat-001": [
    { id: "msg-001", chatId: "chat-001", senderId: uid, senderFirstName: "Michał", senderLastName: "Kowalski", content: "Dzień dobry zespołowi. Proszę o aktualizację postępów na koniec tygodnia.", isDeleted: false, isEdited: false, sentAt: date("2026-01-25"), editedAt: null, replyToMessageId: null },
    { id: "msg-002", chatId: "chat-001", senderId: "u-002", senderFirstName: "Tomasz", senderLastName: "Wójcik", content: "U nas fundamenty prawie gotowe. Została izolacja i zasypka. Będzie na poniedziałek.", isDeleted: false, isEdited: false, sentAt: date("2026-01-26"), editedAt: null, replyToMessageId: null },
    { id: "msg-003", chatId: "chat-001", senderId: "u-002", senderFirstName: "Tomasz", senderLastName: "Wójcik", content: "Czy fundamenty zostały już zalane? Jutro przyjeżdża ekipa od szalunków.", isDeleted: false, isEdited: false, sentAt: date("2026-01-28"), editedAt: null, replyToMessageId: null },
    { id: "msg-004", chatId: "chat-001", senderId: "u-009", senderFirstName: "Krzysztof", senderLastName: "Baran", content: "Tak, fundamenty zalane wczoraj. Beton wiąże. Szalunki można stawiać od środy.", isDeleted: false, isEdited: false, sentAt: date("2026-01-28"), editedAt: null, replyToMessageId: "msg-003" },
    { id: "msg-005", chatId: "chat-001", senderId: "u-006", senderFirstName: "Anna", senderLastName: "Nowak", content: "Dostawa bloczków silikatowych zaplanowana na 3 lutego. 12 palet.", isDeleted: false, isEdited: false, sentAt: date("2026-01-30"), editedAt: null, replyToMessageId: null },
  ],
  "chat-002": [
    { id: "msg-006", chatId: "chat-002", senderId: uid, senderFirstName: "Michał", senderLastName: "Kowalski", content: "Proszę o akceptację faktury od Cemexu — 324k netto za beton B25.", isDeleted: false, isEdited: false, sentAt: date("2026-02-03"), editedAt: null, replyToMessageId: null },
    { id: "msg-007", chatId: "chat-002", senderId: "u-006", senderFirstName: "Anna", senderLastName: "Nowak", content: "Sprawdziłem. Zgadza się z zamówieniem i dostawami. Akceptuję.", isDeleted: false, isEdited: false, sentAt: date("2026-02-04"), editedAt: null, replyToMessageId: "msg-006" },
    { id: "msg-008", chatId: "chat-002", senderId: "u-006", senderFirstName: "Anna", senderLastName: "Nowak", content: "Zaakceptowałem fakturę od Cemexu. Kwota 324 000 PLN netto za beton.", isDeleted: false, isEdited: false, sentAt: date("2026-02-05"), editedAt: null, replyToMessageId: null },
    { id: "msg-009", chatId: "chat-002", senderId: uid, senderFirstName: "Michał", senderLastName: "Kowalski", content: "Dzięki. Kolejna faktura od Wienerberger za bloczki — 178 900 PLN netto.", isDeleted: false, isEdited: false, sentAt: date("2026-02-08"), editedAt: null, replyToMessageId: null },
  ],
  "chat-003": [
    { id: "msg-010", chatId: "chat-003", senderId: uid, senderFirstName: "Michał", senderLastName: "Kowalski", content: "Piotrze, czy możesz przygotować wizualizację elewacji w kolorze grafitowym?", isDeleted: false, isEdited: false, sentAt: date("2026-02-28"), editedAt: null, replyToMessageId: null },
    { id: "msg-011", chatId: "chat-003", senderId: "u-008", senderFirstName: "Piotr", senderLastName: "Zieliński", content: "Oczywiście. Przesyłam poprawiony projekt elewacji z uwzględnieniem uwag inwestora.", isDeleted: false, isEdited: false, sentAt: date("2026-03-01"), editedAt: null, replyToMessageId: "msg-010" },
    { id: "msg-012c", chatId: "chat-003", senderId: "u-008", senderFirstName: "Piotr", senderLastName: "Zieliński", content: "Dodałem też wariant z jasną stolarką — wygląda lepiej przy grafcie.", isDeleted: false, isEdited: false, sentAt: date("2026-03-01"), editedAt: null, replyToMessageId: null },
  ],
  "chat-004": [
    { id: "msg-013", chatId: "chat-004", senderId: uid, senderFirstName: "Michał", senderLastName: "Kowalski", content: "Ewa, jak wygląda postęp na Apartamentach Centrum?", isDeleted: false, isEdited: false, sentAt: date("2026-03-10"), editedAt: null, replyToMessageId: null },
    { id: "msg-014", chatId: "chat-004", senderId: "u-010", senderFirstName: "Ewa", senderLastName: "Majewska", content: "Prace ziemne etapu 1 zakończone. Fundamental etapu 2 w trakcie.", isDeleted: false, isEdited: false, sentAt: date("2026-03-12"), editedAt: null, replyToMessageId: "msg-013" },
    { id: "msg-020", chatId: "chat-004", senderId: "u-010", senderFirstName: "Ewa", senderLastName: "Majewska", content: "Zakończono prace ziemne etapu 2. Przechodzimy do fundamentów części B.", isDeleted: false, isEdited: false, sentAt: date("2026-03-15"), editedAt: null, replyToMessageId: null },
  ],
};

// ---- FILES / DOKUMENTY ----

/**
 * Generuje wersje pliku z komentarzami.
 * @param fileId - ID pliku (prefix)
 * @param projectPrefix - prefix projektu (np. "p1")
 * @param configs - konfiguracje: [versionNumber, fileSizeBytes, contentType, ...comments]
 */
function generateVersions(
  fileId: string, projectPrefix: string,
  configs: Array<[number, number, string, string[]]>
): any[] {
  return configs.map(([vNum, size, contentType, comments], vi) => {
    const versionId = `${projectPrefix}-${fileId}-v${vi + 1}`;
    return {
      id: versionId,
      projectFileId: `${projectPrefix}-${fileId}`,
      versionNumber: vNum,
      contentType,
      fileSizeBytes: size,
      createdAt: date(`2025-0${6 + vi}-15`),
      createdByUserId: uid,
      createdByUserName: "Michał Kowalski",
      sasUrlView: "#",
      sasUrlDownload: "#",
      comments: comments.map((c, ci) => ({
        id: `${versionId}-c${ci + 1}`,
        projectFileVersionId: versionId,
        userId: uid,
        userName: "Michał Kowalski",
        content: c,
        createdAt: date("2025-07-01"),
        isEdited: false,
        canEdit: true,
        canDelete: true,
      })),
    };
  });
}

/**
 * Generuje strukturę pliku ProjectFileWeb z wersjami i komentarzami.
 */
function createFileObj(
  fileId: string, projectPrefix: string, pkgName: string,
  displayName: string, versionConfigs: Array<[number, number, string, string[]]>
): any {
  const versions: any[] = generateVersions(fileId, projectPrefix, versionConfigs);
  const lastV = versions[versions.length - 1];
  return {
    id: `${projectPrefix}-${fileId}`,
    fileName: displayName.replace(/\s+/g, "_").toLowerCase() + (versionConfigs[0][2] === "xlsx" ? ".xlsx" : ".pdf"),
    displayName,
    packageName: pkgName,
    createdAt: lastV.createdAt,
    ownerId: uid,
    ownerName: "Michał Kowalski",
    currentVersion: lastV,
    versions,
    totalVersions: versions.length,
    isOwner: true,
    isShared: false,
    sharedWithUserIds: [],
  };
}

/** Konfiguracja paczki (katalogu) z plikami i subkatalogami */
interface FilePkgConfig {
  pkgId: string;
  name: string;
  parentId?: string | null;
  files?: Array<{
    fileId: string;
    displayName: string;
    versions: Array<[number, number, string, string[]]>; // [vNum, sizeBytes, contentType, comments[]]
  }>;
  subCatalogs?: FilePkgConfig[];
}

const uid2 = "u-008"; const uid3 = "u-010";

function buildPackages(configs: FilePkgConfig[], projectPrefix: string, parentId: string | null): any[] {
  return configs.map((cfg) => {
    const pkgId = `${projectPrefix}-${cfg.pkgId}`;
    const pkgName = cfg.name;
    const files: any[] = (cfg.files || []).map((f) =>
      createFileObj(f.fileId, projectPrefix, pkgName, f.displayName, f.versions)
    );
    const subCatalogs = cfg.subCatalogs
      ? buildPackages(cfg.subCatalogs, projectPrefix, pkgId)
      : [];
    return {
      id: pkgId,
      name: pkgName,
      createdAt: date("2025-06-10"),
      ownerId: uid,
      ownerName: "Michał Kowalski",
      files,
      totalFiles: files.length + subCatalogs.reduce((s: number, sc: any) => s + sc.files.length, 0),
      parentId,
      subCatalogs,
    };
  });
}

/**
 * Zwraca kompletne dane plików dla konkretnego projektu.
 * Każdy projekt ma unikalne paczki, pliki, wersje i komentarze.
 */
export function getProjectFileData(projectId: string): { packages: any[] } {
  const prefix = projectId.replace(/-/g, "");

  const configs: Record<string, FilePkgConfig[]> = {
    "p-001": [
      {
        pkgId: "pkg-doc", name: "Dokumentacja projektowa",
        files: [
          { fileId: "f-ar", displayName: "Projekt budowlany — architektura", versions: [
            [3, 15800000, "pdf", ["Zatwierdzona wersja do zgłoszenia.", "Korekta elewacji po uwagach inwestora.", "Wersja pierwotna."]],
            [2, 14500000, "pdf", ["Poprawki rzutów po weryfikacji konstrukcji."]],
            [1, 12300000, "pdf", ["Wersja wstępna do konsultacji."]],
          ]},
          { fileId: "f-kon", displayName: "Projekt konstrukcyjny", versions: [
            [2, 12450000, "pdf", ["Zatwierdzony projekt konstrukcji żelbetowej.", "Wersja po zmianie obciążeń."]],
            [1, 11000000, "pdf", []],
          ]},
          { fileId: "f-el", displayName: "Projekt instalacji elektrycznych", versions: [
            [1, 8200000, "pdf", ["Projekt zgodny z warunkami przyłączenia."]],
          ]},
          { fileId: "f-san", displayName: "Projekt instalacji sanitarnych", versions: [
            [1, 9800000, "pdf", ["Aktualny projekt wod-kan i CO."]],
          ]},
          { fileId: "f-pozw", displayName: "Decyzja — pozwolenie na budowę", versions: [
            [1, 1200000, "pdf", ["Decyzja nr 145/2025 z uprawomocnieniem."]],
          ]},
        ],
        subCatalogs: [
          {
            pkgId: "pkg-doc-rzut", name: "Rzuty i przekroje",
            files: [
              { fileId: "f-rzut1", displayName: "Rzut parteru", versions: [
                [1, 3400000, "pdf", ["Rzut w skali 1:100."]],
              ]},
              { fileId: "f-rzut2", displayName: "Rzut I piętra", versions: [
                [1, 3200000, "pdf", []],
              ]},
              { fileId: "f-przekr", displayName: "Przekrój A-A", versions: [
                [2, 2800000, "pdf", ["Zaktualizowany przekrój z nowymi rzędnymi.", "Wersja pierwotna."]],
              ]},
            ],
          },
          {
            pkgId: "pkg-doc-det", name: "Detale i szczegóły",
            files: [
              { fileId: "f-det1", displayName: "Detal fundamentów", versions: [
                [1, 1900000, "pdf", ["Szczegół fundamentu w ścianą trójwarstwową."]],
              ]},
            ],
          },
        ],
      },
      {
        pkgId: "pkg-umowy", name: "Umowy i zlecenia",
        files: [
          { fileId: "f-um1", displayName: "Umowa — Budimex roboty żelbetowe", versions: [
            [1, 2100000, "pdf", ["Umowa podpisana elektronicznie."]],
          ]},
          { fileId: "f-um2", displayName: "Umowa — Cemex dostawa betonu", versions: [
            [2, 1800000, "pdf", ["Aneks nr 1 — zmiana ceny betonu B30.", "Wersja podstawowa."]],
            [1, 1500000, "pdf", []],
          ]},
          { fileId: "f-um3", displayName: "Umowa — Wienerberger bloczki", versions: [
            [1, 1500000, "pdf", ["Umowa ramowa na dostawy."]],
          ]},
          { fileId: "f-um4", displayName: "Zlecenie — Erbet szalunki", versions: [
            [1, 950000, "pdf", ["Zlecenie na wynajem szalunków."]],
          ]},
        ],
      },
      {
        pkgId: "pkg-koszt", name: "Kosztorysy i harmonogramy",
        files: [
          { fileId: "f-ks1", displayName: "Kosztorys — Etap I wersja 2", versions: [
            [2, 450000, "xlsx", ["Wersja po waloryzacji cen materiałów.", "Pierwotny kosztorys."]],
            [1, 380000, "xlsx", []],
          ]},
          { fileId: "f-har1", displayName: "Harmonogram — Etap I", versions: [
            [1, 680000, "pdf", ["Harmonogram zatwierdzony na naradzie."]],
          ]},
          { fileId: "f-ks2", displayName: "Kosztorys instalacji sanitarnych", versions: [
            [1, 320000, "xlsx", ["Kosztorys branżowy."]],
          ]},
        ],
        subCatalogs: [
          {
            pkgId: "pkg-koszt-zest", name: "Zestawienia",
            files: [
              { fileId: "f-zest1", displayName: "Zestawienie stali zbrojeniowej", versions: [
                [1, 220000, "xlsx", ["Zestawienie wg projektu konstrukcji."]],
              ]},
            ],
          },
        ],
      },
      {
        pkgId: "pkg-kor", name: "Korespondencja i protokoły",
        files: [
          { fileId: "f-prot1", displayName: "Protokół odbioru fundamentów", versions: [
            [1, 2400000, "pdf", ["Protokół podpisany przez inspektora nadzoru.", "Załączniki: dokumentacja fotograficzna."]],
          ]},
          { fileId: "f-pismo1", displayName: "Pismo do inwestora — styczeń 2026", versions: [
            [1, 850000, "pdf", ["Pismo dotyczące opóźnień w dostawie stali."]],
          ]},
        ],
      },
    ],

    "p-002": [
      {
        pkgId: "pkg-doc", name: "Dokumentacja projektowa — Etap II",
        files: [
          { fileId: "f-ar", displayName: "Projekt budowlany — Etap II", versions: [
            [2, 14200000, "pdf", ["Poprawiony po uwagach z narady koordynacyjnej.", "Wersja pierwotna."]],
            [1, 13000000, "pdf", []],
          ]},
          { fileId: "f-kon", displayName: "Projekt konstrukcyjny — Etap II", versions: [
            [1, 11200000, "pdf", ["Konstrukcja żelbetowa dla budynku B."]],
          ]},
          { fileId: "f-pozw", displayName: "Pozwolenie na budowę — Etap II", versions: [
            [1, 980000, "pdf", ["Decyzja nr 234/2025."]],
          ]},
        ],
        subCatalogs: [
          {
            pkgId: "pkg-doc-rys", name: "Rysunki techniczne",
            files: [
              { fileId: "f-rys1", displayName: "Rzut fundamentów — Etap II", versions: [
                [1, 2800000, "pdf", []],
              ]},
            ],
          },
        ],
      },
      {
        pkgId: "pkg-umowy", name: "Umowy — Etap II",
        files: [
          { fileId: "f-um1", displayName: "Umowa — Strabag roboty ziemne", versions: [
            [1, 3200000, "pdf", ["Umowa na roboty ziemne i fundamenty."]],
          ]},
          { fileId: "f-um2", displayName: "Umowa — Hydrobudowa instalacje", versions: [
            [1, 1800000, "pdf", ["Roboty instalacyjne wod-kan."]],
          ]},
        ],
      },
      {
        pkgId: "pkg-koszt", name: "Kosztorysy — Etap II",
        files: [
          { fileId: "f-ks1", displayName: "Kosztorys budowlany — Etap II", versions: [
            [1, 520000, "xlsx", ["Kosztorys główny drugiego etapu."]],
          ]},
          { fileId: "f-har1", displayName: "Harmonogram — Etap II", versions: [
            [1, 710000, "pdf", ["Harmonogram rzeczowo-finansowy."]],
          ]},
        ],
      },
    ],

    "p-003": [
      {
        pkgId: "pkg-doc", name: "Dokumentacja — Bud. A",
        files: [
          { fileId: "f-ar", displayName: "Projekt architektoniczny — Bud. A", versions: [
            [1, 9800000, "pdf", ["Projekt zatwierdzony."]],
          ]},
          { fileId: "f-kon", displayName: "Projekt konstrukcji — Bud. A", versions: [
            [1, 8700000, "pdf", []],
          ]},
          { fileId: "f-pozw", displayName: "Pozwolenie na budowę — Bud. A", versions: [
            [1, 650000, "pdf", ["Decyzja nr 89/2025."]],
          ]},
        ],
      },
      {
        pkgId: "pkg-koszt", name: "Kosztorysy — Bud. A",
        files: [
          { fileId: "f-ks1", displayName: "Kosztorys budowlany — Bud. A", versions: [
            [2, 410000, "xlsx", ["Korekta po weryfikacji.", "Wersja pierwotna."]],
            [1, 350000, "xlsx", []],
          ]},
          { fileId: "f-ks2", displayName: "Kosztorys instalacji — Bud. A", versions: [
            [1, 280000, "xlsx", []],
          ]},
        ],
      },
    ],

    "p-004": [
      {
        pkgId: "pkg-doc", name: "Dokumentacja — Apartamenty Centrum",
        files: [
          { fileId: "f-ar", displayName: "Projekt architektoniczny", versions: [
            [3, 18500000, "pdf", ["Wersja ostateczna po uzgodnieniach.", "Poprawki rzutów po zmianie układu mieszkań.", "Wersja koncepcyjna."]],
            [2, 17200000, "pdf", ["Aktualizacja po decyzji WZ.", "Pierwsza wersja robocza."]],
            [1, 15000000, "pdf", []],
          ]},
          { fileId: "f-kon", displayName: "Projekt konstrukcji żelbetowej", versions: [
            [2, 13500000, "pdf", ["Wzmocnienie stropów nad garażem.", "Projekt podstawowy."]],
            [1, 12000000, "pdf", []],
          ]},
          { fileId: "f-el", displayName: "Projekt instalacji elektrycznych", versions: [
            [1, 9200000, "pdf", ["Instalacje wg standardu deweloperskiego."]],
          ]},
        ],
        subCatalogs: [
          {
            pkgId: "pkg-doc-garaz", name: "Garaż podziemny",
            files: [
              { fileId: "f-garaz-kon", displayName: "Konstrukcja garażu podziemnego", versions: [
                [1, 7800000, "pdf", ["Projekt płyty dennej i ścian szczelinowych."]],
              ]},
              { fileId: "f-garaz-went", displayName: "Wentylacja garażu", versions: [
                [1, 3200000, "pdf", ["Projekt wentylacji mechanicznej."]],
              ]},
            ],
          },
        ],
      },
      {
        pkgId: "pkg-umowy", name: "Umowy — Apartamenty",
        files: [
          { fileId: "f-um1", displayName: "Umowa generalna — Budimex", versions: [
            [1, 5600000, "pdf", ["Umowa o generalne wykonawstwo."]],
          ]},
          { fileId: "f-um2", displayName: "Umowa — Saint-Gobain materiały", versions: [
            [1, 2100000, "pdf", ["Umowa na dostawę materiałów wykończeniowych."]],
          ]},
          { fileId: "f-um3", displayName: "Umowa — Elektromontaż", versions: [
            [1, 1400000, "pdf", ["Instalacje elektryczne."]],
          ]},
        ],
      },
      {
        pkgId: "pkg-koszt", name: "Kosztorysy — Apartamenty",
        files: [
          { fileId: "f-ks1", displayName: "Kosztorys główny", versions: [
            [1, 680000, "xlsx", ["Kosztorys inwestorski."]],
          ]},
          { fileId: "f-ks2", displayName: "Kosztorys garażu podziemnego", versions: [
            [1, 340000, "xlsx", []],
          ]},
          { fileId: "f-har1", displayName: "Harmonogram ogólny", versions: [
            [1, 890000, "pdf", ["Harmonogram całej inwestycji."]],
          ]},
        ],
        subCatalogs: [
          {
            pkgId: "pkg-koszt-przed", name: "Przedmiary",
            files: [
              { fileId: "f-przed1", displayName: "Przedmiar robót stanu surowego", versions: [
                [1, 520000, "xlsx", ["Przedmiar wg norm KNR."]],
              ]},
            ],
          },
        ],
      },
      {
        pkgId: "pkg-decyzje", name: "Decyzje i pozwolenia",
        files: [
          { fileId: "f-wz", displayName: "Warunki zabudowy", versions: [
            [1, 350000, "pdf", ["Decyzja WZ nr 45/2024."]],
          ]},
          { fileId: "f-pozw", displayName: "Pozwolenie na budowę", versions: [
            [1, 1800000, "pdf", ["Decyzja nr 312/2025."]],
          ]},
        ],
      },
      {
        pkgId: "pkg-kor", name: "Korespondencja",
        files: [
          { fileId: "f-pismo1", displayName: "Pismo — uzgodnienia branżowe", versions: [
            [1, 420000, "pdf", ["Uzgodnienia międzybranżowe."]],
          ]},
        ],
      },
    ],

    "p-005": [
      {
        pkgId: "pkg-doc", name: "Dokumentacja koncepcyjna",
        files: [
          { fileId: "f-konc", displayName: "Koncepcja architektoniczna", versions: [
            [1, 6500000, "pdf", ["Wstępna koncepcja rezydencji."]],
          ]},
          { fileId: "f-koszt", displayName: "Kosztorys wstępny", versions: [
            [1, 280000, "xlsx", ["Szacunek kosztów w EUR."]],
          ]},
          { fileId: "f-wiz", displayName: "Wizualizacje", versions: [
            [1, 8500000, "pdf", ["Wizualizacje 3D rezydencji."]],
          ]},
        ],
      },
    ],
  };

  const projectConfig = configs[projectId] || configs["p-001"];
  const packages = buildPackages(projectConfig, prefix, null);

  return { packages };
}

// Backwards compatibility: dane dla projektu P1
export const mockProjectFiles = getProjectFileData("p-001");

// ---- AI COST IMPORT ----
export const mockAiCostImport = {
  name: "Faktura VAT za dostawę bloczków betonowych",
  net: 78500, gross: 96555, number: "FV/2026/02/0891", date: "2026-02-15",
  contractor: { name: "Dębickie Przedsiębiorstwo Budowlane", nip: "8720001425", street: "ul. Metalowców 12", city: "Dębica", postalCode: "39-200" },
};

// ---- AI ESTIMATE GENERATE ----
export const mockAiEstimateGenerate = {
  previewId: "ai-preview-001",
  projectName: "Osiedle Słoneczne — Etap II",
  description: "Budynek mieszkalny wielorodzinny, 6 kondygnacji, 48 mieszkań, standard deweloperski, powierzchnia użytkowa 3200 m², garaż podziemny na 52 miejsca, lokalizacja Kraków, planowane zakończenie Q4 2027",
  totalNet: 18750000, totalGross: 23062500,
  groups: [
    { name: "Roboty ziemne i fundamentowe", itemCount: 6, groupNet: 1450000 },
    { name: "Konstrukcja żelbetowa", itemCount: 12, groupNet: 5200000 },
    { name: "Ściany i elewacja", itemCount: 8, groupNet: 3100000 },
    { name: "Instalacje wewnętrzne", itemCount: 15, groupNet: 2800000 },
    { name: "Wykończenia wewnętrzne", itemCount: 22, groupNet: 4200000 },
    { name: "Zagospodarowanie terenu", itemCount: 8, groupNet: 1200000 },
    { name: "Garaż podziemny", itemCount: 10, groupNet: 2500000 },
    { name: "Koszty pośrednie", itemCount: 5, groupNet: 800000 },
  ],
};

// ---- EXPORT ----
export const MOCK_DATA = {
  userProfile: mockUserProfile,
  tenants: mockTenants,
  projects: mockProjects,
  projectDictionary: mockProjectDictionary,
  projectDetails: allProjects[0],
  costEstimates: mockCostEstimates,
  costEstimateDetails: mockCostEstimateDetails,
  workSchedules: mockWorkSchedules,
  workScheduleDetails: mockWorkScheduleDetails,
  projectCosts: mockProjectCosts,
  dashboard: mockDashboard,
  chats: mockChats,
  messages: mockMessages,
  contractors: mockContractors,
  projectFiles: mockProjectFiles,
  aiCostImport: mockAiCostImport,
  aiEstimateGenerate: mockAiEstimateGenerate,
  trackedCosts: trackedCostsP1,
};
