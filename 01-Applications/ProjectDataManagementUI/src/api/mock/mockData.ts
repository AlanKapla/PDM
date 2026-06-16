// ============================================
//   PDM Demo Mode — Kompletne mockowane dane
// ============================================

import type {
  WorkScheduleSummaryWeb,
  WorkScheduleStageWorkCommentWeb,
} from "../../types/workSchedule.types";

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

/** Wspólna struktura pól dla mockowych kosztorysów */
const sharedFieldStructure = {
  maxGroupLevel: 2,
  currencies: [{ id: "curr-pln", code: "PLN", name: "Złoty polski", symbol: "zł", isDefault: true }],
  units: [{ id: "u-m3", code: "m³", name: "Metr sześcienny", symbol: "m³" }, { id: "u-m2", code: "m²", name: "Metr kwadratowy", symbol: "m²" }, { id: "u-ml", code: "m.b.", name: "Metr bieżący", symbol: "m.b." }, { id: "u-kpl", code: "kpl.", name: "Komplet", symbol: "kpl." }, { id: "u-szt", code: "szt.", name: "Sztuka", symbol: "szt." }, { id: "u-kg", code: "kg", name: "Kilogram", symbol: "kg" }],
  categories: [{ id: "cat-mat", name: "Materiały", symbol: "M" }, { id: "cat-rob", name: "Robocizna", symbol: "R" }, { id: "cat-sprz", name: "Sprzęt", symbol: "S" }, { id: "cat-trans", name: "Transport", symbol: "T" }],
  uiConfiguration: {
    groupColumns: [
      { fieldId: "fd-grp-name", fieldName: "grp-name-field", fieldType: 0, fieldLabel: "Nazwa etapu", fieldScope: 0, order: 0 },
    ],
    itemColumns: [
      { fieldId: "fd-name", fieldName: "sys-name-field", fieldType: 100, fieldLabel: "Nazwa", fieldScope: 1, order: 0 },
      { fieldId: "fd-qty", fieldName: "sys-qty-field", fieldType: 101, fieldLabel: "Ilość", fieldScope: 1, order: 1 },
      { fieldId: "fd-unit", fieldName: "sys-unit-field", fieldType: 102, fieldLabel: "J.m.", fieldScope: 1, order: 2 },
      { fieldId: "fd-price", fieldName: "calc-price-field", fieldType: 200, fieldLabel: "Cena jedn. netto", fieldScope: 2, order: 3 },
      { fieldId: "fd-value-net", fieldName: "calc-value-net-field", fieldType: 203, fieldLabel: "Wartość netto", fieldScope: 2, order: 4 },
      { fieldId: "fd-cat", fieldName: "sys-cat-field", fieldType: 106, fieldLabel: "Kategoria", fieldScope: 1, order: 5 },
    ],
  },
  groupHeaderFields: [
    {
      id: "fd-grp-name", fieldName: "grp-name-field", fieldType: 0,
      customLabel: "Nazwa etapu", isRequired: true, isVisible: true, order: 0, isReadonly: false,
      isSortable: true, isFilterable: true,
      fieldTypeConfig: { fieldType: 0, fieldScope: 0, namePl: "Nazwa etapu", valueTypeName: "String", isNumeric: false, isText: true, isDate: false, isBoolean: false, isCollection: false },
    },
  ],
  systemFields: [
    {
      id: "fd-name", fieldName: "sys-name-field", fieldType: 100, label: "Nazwa",
      isRequired: true, isVisible: true, order: 0, isSortable: true, isFilterable: true, isReadonly: false,
      fieldTypeConfig: { fieldType: 100, fieldScope: 1, namePl: "Nazwa", valueTypeName: "String", isNumeric: false, isText: true, isDate: false, isBoolean: false, isCollection: false },
    },
    {
      id: "fd-unit", fieldName: "sys-unit-field", fieldType: 102, label: "J.m.",
      isRequired: false, isVisible: true, order: 1, isSortable: true, isFilterable: true, isReadonly: false,
      fieldTypeConfig: { fieldType: 102, fieldScope: 1, namePl: "Jednostka miary", valueTypeName: "String", isNumeric: false, isText: true, isDate: false, isBoolean: false, isCollection: false },
    },
    {
      id: "fd-qty", fieldName: "sys-qty-field", fieldType: 101, label: "Ilość",
      isRequired: false, isVisible: true, order: 2, isSortable: true, isFilterable: true, isReadonly: false,
      fieldTypeConfig: { fieldType: 101, fieldScope: 1, namePl: "Ilość", valueTypeName: "Decimal", isNumeric: true, isText: false, isDate: false, isBoolean: false, isCollection: false },
    },
    {
      id: "fd-cat", fieldName: "sys-cat-field", fieldType: 106, label: "Kategoria",
      isRequired: false, isVisible: true, order: 3, isSortable: true, isFilterable: true, isReadonly: false,
      fieldTypeConfig: { fieldType: 106, fieldScope: 1, namePl: "Kategoria", valueTypeName: "String", isNumeric: false, isText: true, isDate: false, isBoolean: false, isCollection: false },
    },
  ],
  calculatedFields: [
    {
      id: "fd-price", fieldName: "calc-price-field", fieldType: 200, label: "Cena jedn. netto",
      isSortable: true, isFilterable: true, isSummable: false, isAutoCalculated: false, isReadonly: false, isRequired: false, isVisible: true, order: 0,
      fieldTypeConfig: { fieldType: 200, fieldScope: 2, namePl: "Cena jednostkowa netto", valueTypeName: "Decimal", isNumeric: true, isText: false, isDate: false, isBoolean: false, isCollection: false },
    },
    {
      id: "fd-value-net", fieldName: "calc-value-net-field", fieldType: 203, label: "Wartość netto",
      isSortable: true, isFilterable: true, isSummable: true, summaryScope: 0, sumInGroup: true, sumInTotal: true,
      isAutoCalculated: true, isReadonly: true, isRequired: false, isVisible: true, order: 1,
      fieldTypeConfig: { fieldType: 203, fieldScope: 2, namePl: "Wartość netto", valueTypeName: "Decimal", isNumeric: true, isText: false, isDate: false, isBoolean: false, isCollection: false },
    },
  ],
  genericFields: [],
  summaryConfiguration: {
    showGroupSummary: true,
    showTotalSummary: true,
    groupSummaryFields: [
      { fieldId: "fd-value-net", fieldName: "calc-value-net-field", fieldType: 203, fieldLabel: "Wartość netto", fieldSource: 2, order: 0 },
    ],
    totalSummaryFields: [
      { fieldId: "fd-value-net", fieldName: "calc-value-net-field", fieldType: 203, fieldLabel: "Wartość netto", fieldSource: 2, order: 0 },
    ],
  },
};

// ---- Wariant A: Budowlany (4 grupy, 14 pozycji) — ce-001, ce-004, ce-006, ce-007 ----
const buildingGroups = [
  {
    id: "g-b001", parentGroupId: undefined, level: 0, order: 0, totalNet: 707775, totalGross: 869563.5, totalVat: 161788.5, lastCalculatedAt: date("2026-01-20"), createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"),
    fieldValues: [{ id: "fv-gb1", fieldDefinitionId: "fd-grp-name", fieldType: 1, fieldScope: 0, stringValue: "1. Roboty ziemne i fundamentowe", fieldLabel: "Nazwa grupy" }],
    childGroups: [], items: [
      { id: "i-b001", groupId: "g-b001", order: 0, netValue: 272175, grossValue: 334775.25, vatValue: 62600.25, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi1n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Wykopy pod fundamenty", fieldLabel: "Nazwa" },
        { id: "fv-bi1u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m³", fieldLabel: "J.m." },
        { id: "fv-bi1q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 2850, fieldLabel: "Ilość" },
        { id: "fv-bi1p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 95.50, fieldLabel: "Cena jedn. netto" },
        { id: "fv-bi1c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-b002", groupId: "g-b001", order: 1, netValue: 326400, grossValue: 401472, vatValue: 75072, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi2n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Ławy fundamentowe żelbetowe", fieldLabel: "Nazwa" },
        { id: "fv-bi2u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m³", fieldLabel: "J.m." },
        { id: "fv-bi2q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 480, fieldLabel: "Ilość" },
        { id: "fv-bi2p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 680, fieldLabel: "Cena jedn. netto" },
        { id: "fv-bi2c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Materiały", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-b003", groupId: "g-b001", order: 2, netValue: 52500, grossValue: 64575, vatValue: 12075, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi3n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Izolacja przeciwwilgociowa fundamentów", fieldLabel: "Nazwa" },
        { id: "fv-bi3u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m²", fieldLabel: "J.m." },
        { id: "fv-bi3q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1250, fieldLabel: "Ilość" },
        { id: "fv-bi3p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 42, fieldLabel: "Cena jedn. netto" },
        { id: "fv-bi3c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Materiały", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-b004", groupId: "g-b001", order: 3, netValue: 56700, grossValue: 69729, vatValue: 13029, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi4n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Zasypka i zagęszczenie", fieldLabel: "Nazwa" },
        { id: "fv-bi4u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m³", fieldLabel: "J.m." },
        { id: "fv-bi4q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1620, fieldLabel: "Ilość" },
        { id: "fv-bi4p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 35, fieldLabel: "Cena jedn. netto" },
        { id: "fv-bi4c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
    ],
  },
  {
    id: "g-b002", parentGroupId: undefined, level: 0, order: 1, totalNet: 1581400, totalGross: 1945122, totalVat: 363722, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"),
    fieldValues: [{ id: "fv-gb2", fieldDefinitionId: "fd-grp-name", fieldType: 1, fieldScope: 0, stringValue: "2. Konstrukcja żelbetowa", fieldLabel: "Nazwa grupy" }],
    childGroups: [], items: [
      { id: "i-b005", groupId: "g-b002", order: 0, netValue: 294400, grossValue: 362112, vatValue: 67712, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi5n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Słupy żelbetowe 40×40 cm", fieldLabel: "Nazwa" },
        { id: "fv-bi5u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m³", fieldLabel: "J.m." },
        { id: "fv-bi5q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 320, fieldLabel: "Ilość" },
        { id: "fv-bi5p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 920, fieldLabel: "Cena jedn. netto" },
        { id: "fv-bi5c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-b006", groupId: "g-b002", order: 1, netValue: 1176000, grossValue: 1446480, vatValue: 270480, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi6n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Stropy żelbetowe monolityczne", fieldLabel: "Nazwa" },
        { id: "fv-bi6u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m²", fieldLabel: "J.m." },
        { id: "fv-bi6q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 4800, fieldLabel: "Ilość" },
        { id: "fv-bi6p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 245, fieldLabel: "Cena jedn. netto" },
        { id: "fv-bi6c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-b007", groupId: "g-b002", order: 2, netValue: 111000, grossValue: 136530, vatValue: 25530, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi7n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Schody żelbetowe", fieldLabel: "Nazwa" },
        { id: "fv-bi7u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-bi7q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 6, fieldLabel: "Ilość" },
        { id: "fv-bi7p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 18500, fieldLabel: "Cena jedn. netto" },
        { id: "fv-bi7c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
    ],
  },
  {
    id: "g-b003", parentGroupId: undefined, level: 0, order: 2, totalNet: 2202400, totalGross: 2708952, totalVat: 506552, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"),
    fieldValues: [{ id: "fv-gb3", fieldDefinitionId: "fd-grp-name", fieldType: 1, fieldScope: 0, stringValue: "3. Ściany i elewacja", fieldLabel: "Nazwa grupy" }],
    childGroups: [], items: [
      { id: "i-b008", groupId: "g-b003", order: 0, netValue: 899000, grossValue: 1105770, vatValue: 206770, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi8n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Ściany nośne z bloczków silikatowych", fieldLabel: "Nazwa" },
        { id: "fv-bi8u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m²", fieldLabel: "J.m." },
        { id: "fv-bi8q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 6200, fieldLabel: "Ilość" },
        { id: "fv-bi8p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 145, fieldLabel: "Cena jedn. netto" },
      ], options: undefined, components: undefined },
      { id: "i-b009", groupId: "g-b003", order: 1, netValue: 470400, grossValue: 578592, vatValue: 108192, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi9n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Elewacja — tynk silikonowy", fieldLabel: "Nazwa" },
        { id: "fv-bi9u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m²", fieldLabel: "J.m." },
        { id: "fv-bi9q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 4800, fieldLabel: "Ilość" },
        { id: "fv-bi9p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 98, fieldLabel: "Cena jedn. netto" },
      ], options: undefined, components: undefined },
      { id: "i-b010", groupId: "g-b003", order: 2, netValue: 833000, grossValue: 1024590, vatValue: 191590, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi10n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Stolarka okienna PCV 3-szybowa", fieldLabel: "Nazwa" },
        { id: "fv-bi10u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m²", fieldLabel: "J.m." },
        { id: "fv-bi10q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 980, fieldLabel: "Ilość" },
        { id: "fv-bi10p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 850, fieldLabel: "Cena jedn. netto" },
      ], options: undefined, components: undefined },
    ],
  },
  {
    id: "g-b004", parentGroupId: undefined, level: 0, order: 3, totalNet: 7957825, totalGross: 9788124.75, totalVat: 1830299.75, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"),
    fieldValues: [{ id: "fv-gb4", fieldDefinitionId: "fd-grp-name", fieldType: 1, fieldScope: 0, stringValue: "4. Pozostałe grupy (skrócone)", fieldLabel: "Nazwa grupy" }],
    childGroups: [], items: [
      { id: "i-b011", groupId: "g-b004", order: 0, netValue: 2450000, grossValue: 3013500, vatValue: 563500, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi11n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Dach i pokrycie dachowe", fieldLabel: "Nazwa" },
        { id: "fv-bi11u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-bi11q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1, fieldLabel: "Ilość" },
        { id: "fv-bi11p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 2450000, fieldLabel: "Cena jedn. netto" },
      ], options: undefined, components: undefined },
      { id: "i-b012", groupId: "g-b004", order: 1, netValue: 2850000, grossValue: 3505500, vatValue: 655500, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi12n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Instalacje sanitarne (wod-kan, CO)", fieldLabel: "Nazwa" },
        { id: "fv-bi12u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-bi12q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1, fieldLabel: "Ilość" },
        { id: "fv-bi12p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 2850000, fieldLabel: "Cena jedn. netto" },
      ], options: undefined, components: undefined },
      { id: "i-b013", groupId: "g-b004", order: 2, netValue: 1920000, grossValue: 2361600, vatValue: 441600, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi13n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Instalacja elektryczna i teletechnika", fieldLabel: "Nazwa" },
        { id: "fv-bi13u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-bi13q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1, fieldLabel: "Ilość" },
        { id: "fv-bi13p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 1920000, fieldLabel: "Cena jedn. netto" },
      ], options: undefined, components: undefined },
      { id: "i-b014", groupId: "g-b004", order: 3, netValue: 737825, grossValue: 907524.75, vatValue: 169699.75, createdAt: date("2025-06-15"), updatedAt: date("2026-01-20"), fieldValues: [
        { id: "fv-bi14n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Koszty pośrednie i organizacja placu budowy", fieldLabel: "Nazwa" },
        { id: "fv-bi14u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-bi14q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1, fieldLabel: "Ilość" },
        { id: "fv-bi14p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 737825, fieldLabel: "Cena jedn. netto" },
      ], options: undefined, components: undefined },
    ],
  },
];

// ---- Wariant B: Instalacje (2 grupy, 6 pozycji) — ce-002 ----
const installationGroups = [
  {
    id: "g-s001", parentGroupId: undefined, level: 0, order: 0, totalNet: 1120000, totalGross: 1377600, totalVat: 257600, lastCalculatedAt: date("2026-02-15"), createdAt: date("2025-07-10"), updatedAt: date("2026-02-15"),
    fieldValues: [{ id: "fv-gs1", fieldDefinitionId: "fd-grp-name", fieldType: 1, fieldScope: 0, stringValue: "1. Instalacje wodociągowe i kanalizacyjne", fieldLabel: "Nazwa grupy" }],
    childGroups: [], items: [
      { id: "i-s001", groupId: "g-s001", order: 0, netValue: 340000, grossValue: 418200, vatValue: 78200, createdAt: date("2025-07-10"), updatedAt: date("2026-02-15"), fieldValues: [
        { id: "fv-si1n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Rurociągi wodociągowe PP-R", fieldLabel: "Nazwa" },
        { id: "fv-si1u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m.b.", fieldLabel: "J.m." },
        { id: "fv-si1q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 850, fieldLabel: "Ilość" },
        { id: "fv-si1p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 400, fieldLabel: "Cena jedn. netto" },
        { id: "fv-si1c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Materiały", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-s002", groupId: "g-s001", order: 1, netValue: 420000, grossValue: 516600, vatValue: 96600, createdAt: date("2025-07-10"), updatedAt: date("2026-02-15"), fieldValues: [
        { id: "fv-si2n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Kanalizacja sanitarna PCV", fieldLabel: "Nazwa" },
        { id: "fv-si2u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m.b.", fieldLabel: "J.m." },
        { id: "fv-si2q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 700, fieldLabel: "Ilość" },
        { id: "fv-si2p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 600, fieldLabel: "Cena jedn. netto" },
        { id: "fv-si2c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Materiały", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-s003", groupId: "g-s001", order: 2, netValue: 360000, grossValue: 442800, vatValue: 82800, createdAt: date("2025-07-10"), updatedAt: date("2026-02-15"), fieldValues: [
        { id: "fv-si3n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Pompy i zestawy hydroforowe", fieldLabel: "Nazwa" },
        { id: "fv-si3u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-si3q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 2, fieldLabel: "Ilość" },
        { id: "fv-si3p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 180000, fieldLabel: "Cena jedn. netto" },
        { id: "fv-si3c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Sprzęt", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
    ],
  },
  {
    id: "g-s002", parentGroupId: undefined, level: 0, order: 1, totalNet: 1730000, totalGross: 2127900, totalVat: 397900, lastCalculatedAt: date("2026-02-15"), createdAt: date("2025-07-10"), updatedAt: date("2026-02-15"),
    fieldValues: [{ id: "fv-gs2", fieldDefinitionId: "fd-grp-name", fieldType: 1, fieldScope: 0, stringValue: "2. Instalacje grzewcze i wentylacyjne", fieldLabel: "Nazwa grupy" }],
    childGroups: [], items: [
      { id: "i-s004", groupId: "g-s002", order: 0, netValue: 680000, grossValue: 836400, vatValue: 156400, createdAt: date("2025-07-10"), updatedAt: date("2026-02-15"), fieldValues: [
        { id: "fv-si4n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Kotłownia gazowa z instalacją CO", fieldLabel: "Nazwa" },
        { id: "fv-si4u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-si4q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1, fieldLabel: "Ilość" },
        { id: "fv-si4p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 680000, fieldLabel: "Cena jedn. netto" },
        { id: "fv-si4c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-s005", groupId: "g-s002", order: 1, netValue: 570000, grossValue: 701100, vatValue: 131100, createdAt: date("2025-07-10"), updatedAt: date("2026-02-15"), fieldValues: [
        { id: "fv-si5n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Grzejniki i instalacja rozdzielcza", fieldLabel: "Nazwa" },
        { id: "fv-si5u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "szt.", fieldLabel: "J.m." },
        { id: "fv-si5q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 95, fieldLabel: "Ilość" },
        { id: "fv-si5p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 6000, fieldLabel: "Cena jedn. netto" },
        { id: "fv-si5c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Materiały", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-s006", groupId: "g-s002", order: 2, netValue: 480000, grossValue: 590400, vatValue: 110400, createdAt: date("2025-07-10"), updatedAt: date("2026-02-15"), fieldValues: [
        { id: "fv-si6n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Wentylacja mechaniczna z rekuperacją", fieldLabel: "Nazwa" },
        { id: "fv-si6u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-si6q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1, fieldLabel: "Ilość" },
        { id: "fv-si6p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 480000, fieldLabel: "Cena jedn. netto" },
        { id: "fv-si6c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Sprzęt", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
    ],
  },
];

// ---- Wariant C: Zagospodarowanie terenu (2 grupy, 5 pozycji) — ce-005 ----
const landDevelopmentGroups = [
  {
    id: "g-l001", parentGroupId: undefined, level: 0, order: 0, totalNet: 680000, totalGross: 836400, totalVat: 156400, lastCalculatedAt: date("2025-11-15"), createdAt: date("2025-11-15"),
    fieldValues: [{ id: "fv-gl1", fieldDefinitionId: "fd-grp-name", fieldType: 1, fieldScope: 0, stringValue: "1. Nawierzchnie i drogi", fieldLabel: "Nazwa grupy" }],
    childGroups: [], items: [
      { id: "i-l001", groupId: "g-l001", order: 0, netValue: 320000, grossValue: 393600, vatValue: 73600, createdAt: date("2025-11-15"), fieldValues: [
        { id: "fv-l1n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Nawierzchnia z kostki brukowej", fieldLabel: "Nazwa" },
        { id: "fv-l1u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m²", fieldLabel: "J.m." },
        { id: "fv-l1q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1600, fieldLabel: "Ilość" },
        { id: "fv-l1p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 200, fieldLabel: "Cena jedn. netto" },
        { id: "fv-l1c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Materiały", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-l002", groupId: "g-l001", order: 1, netValue: 360000, grossValue: 442800, vatValue: 82800, createdAt: date("2025-11-15"), fieldValues: [
        { id: "fv-l2n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Krawężniki i obrzeża betonowe", fieldLabel: "Nazwa" },
        { id: "fv-l2u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m.b.", fieldLabel: "J.m." },
        { id: "fv-l2q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1200, fieldLabel: "Ilość" },
        { id: "fv-l2p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 300, fieldLabel: "Cena jedn. netto" },
        { id: "fv-l2c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Materiały", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
    ],
  },
  {
    id: "g-l002", parentGroupId: undefined, level: 0, order: 1, totalNet: 770000, totalGross: 947100, totalVat: 177100, lastCalculatedAt: date("2025-11-15"), createdAt: date("2025-11-15"),
    fieldValues: [{ id: "fv-gl2", fieldDefinitionId: "fd-grp-name", fieldType: 1, fieldScope: 0, stringValue: "2. Zieleń i mała architektura", fieldLabel: "Nazwa grupy" }],
    childGroups: [], items: [
      { id: "i-l003", groupId: "g-l002", order: 0, netValue: 280000, grossValue: 344400, vatValue: 64400, createdAt: date("2025-11-15"), fieldValues: [
        { id: "fv-l3n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Nasadzenia drzew i krzewów", fieldLabel: "Nazwa" },
        { id: "fv-l3u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "szt.", fieldLabel: "J.m." },
        { id: "fv-l3q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 180, fieldLabel: "Ilość" },
        { id: "fv-l3p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 1555.56, fieldLabel: "Cena jedn. netto" },
        { id: "fv-l3c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-l004", groupId: "g-l002", order: 1, netValue: 340000, grossValue: 418200, vatValue: 78200, createdAt: date("2025-11-15"), fieldValues: [
        { id: "fv-l4n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Ławki, kosze, oświetlenie parkowe", fieldLabel: "Nazwa" },
        { id: "fv-l4u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-l4q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 40, fieldLabel: "Ilość" },
        { id: "fv-l4p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 8500, fieldLabel: "Cena jedn. netto" },
        { id: "fv-l4c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Materiały", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-l005", groupId: "g-l002", order: 2, netValue: 150000, grossValue: 184500, vatValue: 34500, createdAt: date("2025-11-15"), fieldValues: [
        { id: "fv-l5n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Trawniki i nawodnienie", fieldLabel: "Nazwa" },
        { id: "fv-l5u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m²", fieldLabel: "J.m." },
        { id: "fv-l5q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 3000, fieldLabel: "Ilość" },
        { id: "fv-l5p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 50, fieldLabel: "Cena jedn. netto" },
        { id: "fv-l5c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
    ],
  },
];

// ---- Wariant D: Garaż podziemny (2 grupy, 6 pozycji) — ce-008 ----
const garageGroups = [
  {
    id: "g-r001", parentGroupId: undefined, level: 0, order: 0, totalNet: 2100000, totalGross: 2583000, totalVat: 483000, lastCalculatedAt: date("2026-01-15"), createdAt: date("2025-08-20"), updatedAt: date("2026-01-15"),
    fieldValues: [{ id: "fv-gr1", fieldDefinitionId: "fd-grp-name", fieldType: 1, fieldScope: 0, stringValue: "1. Konstrukcja garażu", fieldLabel: "Nazwa grupy" }],
    childGroups: [], items: [
      { id: "i-r001", groupId: "g-r001", order: 0, netValue: 920000, grossValue: 1131600, vatValue: 211600, createdAt: date("2025-08-20"), updatedAt: date("2026-01-15"), fieldValues: [
        { id: "fv-r1n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Płyta denna żelbetowa 30 cm", fieldLabel: "Nazwa" },
        { id: "fv-r1u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m³", fieldLabel: "J.m." },
        { id: "fv-r1q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1150, fieldLabel: "Ilość" },
        { id: "fv-r1p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 800, fieldLabel: "Cena jedn. netto" },
        { id: "fv-r1c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Materiały", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-r002", groupId: "g-r001", order: 1, netValue: 680000, grossValue: 836400, vatValue: 156400, createdAt: date("2025-08-20"), updatedAt: date("2026-01-15"), fieldValues: [
        { id: "fv-r2n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Ściany oporowe żelbetowe", fieldLabel: "Nazwa" },
        { id: "fv-r2u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m³", fieldLabel: "J.m." },
        { id: "fv-r2q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 400, fieldLabel: "Ilość" },
        { id: "fv-r2p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 1700, fieldLabel: "Cena jedn. netto" },
        { id: "fv-r2c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-r003", groupId: "g-r001", order: 2, netValue: 500000, grossValue: 615000, vatValue: 115000, createdAt: date("2025-08-20"), updatedAt: date("2026-01-15"), fieldValues: [
        { id: "fv-r3n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Strop garażu z otworami wentylacyjnymi", fieldLabel: "Nazwa" },
        { id: "fv-r3u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m²", fieldLabel: "J.m." },
        { id: "fv-r3q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 2500, fieldLabel: "Ilość" },
        { id: "fv-r3p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 200, fieldLabel: "Cena jedn. netto" },
        { id: "fv-r3c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Materiały", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
    ],
  },
  {
    id: "g-r002", parentGroupId: undefined, level: 0, order: 1, totalNet: 2150000, totalGross: 2644500, totalVat: 494500, lastCalculatedAt: date("2026-01-15"), createdAt: date("2025-08-20"), updatedAt: date("2026-01-15"),
    fieldValues: [{ id: "fv-gr2", fieldDefinitionId: "fd-grp-name", fieldType: 1, fieldScope: 0, stringValue: "2. Wykończenie i instalacje", fieldLabel: "Nazwa grupy" }],
    childGroups: [], items: [
      { id: "i-r004", groupId: "g-r002", order: 0, netValue: 780000, grossValue: 959400, vatValue: 179400, createdAt: date("2025-08-20"), updatedAt: date("2026-01-15"), fieldValues: [
        { id: "fv-r4n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Posadzka epoksydowa garażu", fieldLabel: "Nazwa" },
        { id: "fv-r4u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "m²", fieldLabel: "J.m." },
        { id: "fv-r4q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 2600, fieldLabel: "Ilość" },
        { id: "fv-r4p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 300, fieldLabel: "Cena jedn. netto" },
        { id: "fv-r4c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Materiały", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-r005", groupId: "g-r002", order: 1, netValue: 640000, grossValue: 787200, vatValue: 147200, createdAt: date("2025-08-20"), updatedAt: date("2026-01-15"), fieldValues: [
        { id: "fv-r5n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Wentylacja mechaniczna garażu", fieldLabel: "Nazwa" },
        { id: "fv-r5u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-r5q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1, fieldLabel: "Ilość" },
        { id: "fv-r5p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 640000, fieldLabel: "Cena jedn. netto" },
        { id: "fv-r5c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Sprzęt", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-r006", groupId: "g-r002", order: 2, netValue: 730000, grossValue: 897900, vatValue: 167900, createdAt: date("2025-08-20"), updatedAt: date("2026-01-15"), fieldValues: [
        { id: "fv-r6n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Instalacja oświetlenia i zasilania", fieldLabel: "Nazwa" },
        { id: "fv-r6u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-r6q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1, fieldLabel: "Ilość" },
        { id: "fv-r6p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 730000, fieldLabel: "Cena jedn. netto" },
        { id: "fv-r6c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
    ],
  },
];

// ---- Wariant E: Kosztorys wstępny EUR (1 grupa, 3 pozycje) — ce-009 ----
const preliminaryGroups = [
  {
    id: "g-p001", parentGroupId: undefined, level: 0, order: 0, totalNet: 3200000, totalGross: 3936000, totalVat: 736000, createdAt: date("2026-01-15"),
    fieldValues: [{ id: "fv-gp1", fieldDefinitionId: "fd-grp-name", fieldType: 1, fieldScope: 0, stringValue: "1. Prace koncepcyjne i przygotowawcze", fieldLabel: "Nazwa grupy" }],
    childGroups: [], items: [
      { id: "i-p001", groupId: "g-p001", order: 0, netValue: 1200000, grossValue: 1476000, vatValue: 276000, createdAt: date("2026-01-15"), fieldValues: [
        { id: "fv-p1n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Projekt koncepcyjny i wstępne koszty", fieldLabel: "Nazwa" },
        { id: "fv-p1u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-p1q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1, fieldLabel: "Ilość" },
        { id: "fv-p1p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 1200000, fieldLabel: "Cena jedn. netto" },
        { id: "fv-p1c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-p002", groupId: "g-p001", order: 1, netValue: 950000, grossValue: 1168500, vatValue: 218500, createdAt: date("2026-01-15"), fieldValues: [
        { id: "fv-p2n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Badania geotechniczne i pomiary", fieldLabel: "Nazwa" },
        { id: "fv-p2u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-p2q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1, fieldLabel: "Ilość" },
        { id: "fv-p2p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 950000, fieldLabel: "Cena jedn. netto" },
        { id: "fv-p2c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
      { id: "i-p003", groupId: "g-p001", order: 2, netValue: 1050000, grossValue: 1291500, vatValue: 241500, createdAt: date("2026-01-15"), fieldValues: [
        { id: "fv-p3n", fieldDefinitionId: "fd-name", fieldType: 1, fieldScope: 1, stringValue: "Uzyskanie pozwoleń i opinii", fieldLabel: "Nazwa" },
        { id: "fv-p3u", fieldDefinitionId: "fd-unit", fieldType: 1, fieldScope: 1, stringValue: "kpl.", fieldLabel: "J.m." },
        { id: "fv-p3q", fieldDefinitionId: "fd-qty", fieldType: 2, fieldScope: 1, decimalValue: 1, fieldLabel: "Ilość" },
        { id: "fv-p3p", fieldDefinitionId: "fd-price", fieldType: 2, fieldScope: 2, decimalValue: 1050000, fieldLabel: "Cena jedn. netto" },
        { id: "fv-p3c", fieldDefinitionId: "fd-cat", fieldType: 1, fieldScope: 1, stringValue: "Robocizna", fieldLabel: "Kategoria" },
      ], options: undefined, components: undefined },
    ],
  },
];

// ============================================================================
//   Helper: klonuje grupy i modyfikuje nazwy etapów oraz nazwy pozycji
//   aby każdy kosztorys był unikalny
// ============================================================================
function customizeGroups(groups: any[], stageNames: string[], itemNameMap: Record<string, string>): any[] {
  return groups.map((g, gi) => {
    const newGroupName = stageNames[gi] ?? g.fieldValues[0]?.stringValue ?? `Etap ${gi + 1}`;
    const newItems = g.items.map((item: any) => {
      const nameFv = item.fieldValues.find((fv: any) => fv.fieldDefinitionId === "fd-name");
      if (nameFv && itemNameMap[nameFv.stringValue]) {
        nameFv.stringValue = itemNameMap[nameFv.stringValue];
      }
      return item;
    });
    return {
      ...g,
      fieldValues: g.fieldValues.map((fv: any) =>
        fv.fieldDefinitionId === "fd-grp-name" ? { ...fv, stringValue: newGroupName } : fv
      ),
      items: newItems,
    };
  });
}

/** Mapowanie: ID kosztorysu → (wariant grup, ewentualna customizacja, tenantId, itp.) */
type EstimateMeta = {
  tenantId: string;
  projectId: string;
  groups: any[];
  name: string;
  description: string | null;
  status: number;
  totalNet: number;
  ownerName: string;
  workScheduleId: string | null;
  sharedWith: Array<{ userId: string; fullName: string; email: string; sharedAt: string }>;
  currency: { code: string; symbol: string };
  /** Opcjonalna funkcja customizująca grupy przed użyciem */
  customize?: (groups: any[]) => any[];
};

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
    status: 1, totalNet: 6450000, ownerName: "Michał Kowalski", workScheduleId: "ws-004",
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
    status: 0, totalNet: 3200000, ownerName: "Michał Kowalski", workScheduleId: null,
    sharedWith: [],
    currency: { code: "EUR", symbol: "€" },
  },
};

export function getCostEstimateDetailsById(id: string): unknown {
  const meta = estimateMetaMap[id];
  if (!meta) {
    // Fallback: zwróć podstawową wersję dla nieznanego ID
    const fallback = estimateMetaMap["ce-001"];
    return {
      ...fallback,
      id, name: `Kosztorys (${id})`, description: null,
      totalNet: 100000, status: 0,
      ownerName: "Michał Kowalski", workScheduleId: null,
      sharedWith: [],
    };
  }
  const totalGross = Math.round(meta.totalNet * 1.23);
  const totalVat = totalGross - meta.totalNet;
  const nowIso = new Date().toISOString();

  // Build groups with unique IDs
  const rootGroups: any[] = (meta.customize ? meta.customize(meta.groups) : meta.groups).map((g: any, gi: number) => {
    const groupItems = g.items.map((item: any) => ({
      ...item,
      id: `${id}-${item.id}`,
      groupId: `${id}-g-${gi}`,
      fieldValues: item.fieldValues.map((fv: any) => ({ ...fv, id: `${id}-${fv.id}` })),
    }));
    // Compute group summaryValues for calculated fields
    const groupSummaryValues: Record<string, number> = {};
    const valueNetSum = groupItems.reduce((sum: number, it: any) => sum + (it.netValue ?? 0), 0);
    groupSummaryValues["fd-value-net"] = valueNetSum;
    return {
      ...g,
      id: `${id}-g-${gi}`,
      fieldValues: g.fieldValues.map((fv: any) => ({ ...fv, id: `${id}-${fv.id}` })),
      items: groupItems,
      summaryValues: groupSummaryValues,
    };
  });

  // Compute overall summaryValues
  const totalValueNet = rootGroups.reduce((sum: number, g: any) => sum + ((g.summaryValues?.["fd-value-net"] as number) ?? 0), 0);

  return {
    id,
    tenantId: meta.tenantId,
    projectId: meta.projectId,
    selectedCurrencyId: `curr-${meta.currency.code.toLowerCase()}`,
    selectedCurrencyCode: meta.currency.code,
    selectedCurrencySymbol: meta.currency.symbol,
    name: meta.name,
    description: meta.description,
    status: meta.status,
    totalNet: meta.totalNet,
    totalGross,
    totalVat,
    createdAt: date("2025-06-15"),
    updatedAt: date("2026-01-20"),
    lastCalculatedAt: date("2026-01-20"),
    ownerId: uid,
    ownerName: meta.ownerName,
    workScheduleId: meta.workScheduleId,
    accessLevel: 3,
    sharedWithUsers: meta.sharedWith,
    fieldSchemas: [],
    additionalFields: [],
    rootGroups,
    summaryValues: { "fd-value-net": totalValueNet },
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
  // P4
  { id: "ws-004", projectId: P4, name: "Harmonogram — Bud. A", createdAt: date("2026-01-05"), createdByUserId: "u-010", createdByUserName: "Ewa Majewska", costEstimateId: "ce-005" },
  { id: "ws-005", projectId: P4, name: "Harmonogram — Apartamenty Centrum", createdAt: date("2025-07-20"), createdByUserId: uid, createdByUserName: "Michał Kowalski", costEstimateId: "ce-007" },
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

/** Mapa wsId → builder funkcji */
const scheduleDetailsMap: Record<string, () => object> = {
  "ws-001": buildWs001Details,
  "ws-002": buildWs002Details,
  "ws-003": buildWs003Details,
  "ws-004": buildWs004Details,
  "ws-005": buildWs005Details,
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
// workScheduleStageWorkId używa nowych ID z harmonogramów (w-ws1-xxx)
// costEstimateItemId używa rzeczywistych ID pozycji kosztorysowych (i-bxxx)
const trackedCostsP1 = [
  { id: "tc-001", costEstimateItemId: "i-b001", workScheduleStageWorkId: "w-ws1-001", isAdditional: false, name: "Wykopy fundamentowe — realizacja", description: null, net: 187500, gross: 230625, vatRate: 23, contractorId: "ctr-003", contractorName: "Dębickie Przedsiębiorstwo Budowlane", date: date("2025-08-15"), number: "FV/2025/08/1245", attachments: [], createdAt: date("2025-08-16"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap I", estimateGroupName: "1. Roboty ziemne i fundamentowe", estimateItemName: "Wykopy pod fundamenty", costEstimateItemPath: "Kosztorys budowlany — Etap I > 1. Roboty ziemne i fundamentowe > Wykopy pod fundamenty", workScheduleWorkPath: null },
  { id: "tc-002", costEstimateItemId: "i-b002", workScheduleStageWorkId: "w-ws1-002", isAdditional: false, name: "Fundamenty — szalunki i beton", description: null, net: 324000, gross: 398520, vatRate: 23, contractorId: "ctr-005", contractorName: "Cemex Polska Sp. z o.o.", date: date("2025-10-05"), number: "FA/10/2025/089", attachments: [], createdAt: date("2025-10-06"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap I", estimateGroupName: "1. Roboty ziemne i fundamentowe", estimateItemName: "Ławy fundamentowe żelbetowe", costEstimateItemPath: "Kosztorys budowlany — Etap I > 1. Roboty ziemne i fundamentowe > Ławy fundamentowe żelbetowe", workScheduleWorkPath: null },
  { id: "tc-003", costEstimateItemId: "i-b005", workScheduleStageWorkId: "w-ws1-004", isAdditional: false, name: "Zbrojenie słupów i stropów", description: "Prace zbrojarskie — słupy i stropy", net: 215000, gross: 264450, vatRate: 23, contractorId: "ctr-001", contractorName: "Budimex S.A.", date: date("2025-11-20"), number: "FV/11/2025/567", attachments: [], createdAt: date("2025-11-21"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap I", estimateGroupName: "2. Konstrukcja żelbetowa", estimateItemName: "Słupy żelbetowe 40×40 cm", costEstimateItemPath: "Kosztorys budowlany — Etap I > 2. Konstrukcja żelbetowa > Słupy żelbetowe 40×40 cm", workScheduleWorkPath: null },
  { id: "tc-004", costEstimateItemId: "i-b008", workScheduleStageWorkId: "w-ws1-005", isAdditional: false, name: "Bloczki silikatowe — dostawa i murowanie", description: null, net: 178900, gross: 220047, vatRate: 23, contractorId: "ctr-008", contractorName: "Wienerberger Ceramika Budowlana", date: date("2025-12-05"), number: "FV 0012/25", attachments: [], createdAt: date("2025-12-06"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap I", estimateGroupName: "3. Ściany i elewacja", estimateItemName: "Ściany nośne z bloczków silikatowych", costEstimateItemPath: "Kosztorys budowlany — Etap I > 3. Ściany i elewacja > Ściany nośne z bloczków silikatowych", workScheduleWorkPath: null },
  { id: "tc-005", costEstimateItemId: "i-b010", workScheduleStageWorkId: null, isAdditional: false, name: "Stolarka okienna PCV — zamówienie", description: "Zamówienie na produkcję okien", net: 412000, gross: 506760, vatRate: 23, contractorId: "ctr-006", contractorName: "Saint-Gobain Construction Products", date: date("2026-01-15"), number: "45/01/2026", attachments: [], createdAt: date("2026-01-16"), updatedAt: null, sourceType: "EstimateItem" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: "Kosztorys budowlany — Etap I", estimateGroupName: "3. Ściany i elewacja", estimateItemName: "Stolarka okienna PCV 3-szybowa", costEstimateItemPath: "Kosztorys budowlany — Etap I > 3. Ściany i elewacja > Stolarka okienna PCV 3-szybowa", workScheduleWorkPath: null },
  { id: "tc-006", costEstimateItemId: null, workScheduleStageWorkId: "w-ws1-007", isAdditional: false, name: "Kable i osprzęt elektryczny", description: null, net: 156300, gross: 192249, vatRate: 23, contractorId: "ctr-007", contractorName: "Elektromontaż Rzeszów S.A.", date: date("2026-02-10"), number: "R/02/2026/01", attachments: [], createdAt: date("2026-02-11"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Etap I", stageName: "2. Instalacje wewnętrzne", workItemName: "Instalacja elektryczna", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Etap I > 2. Instalacje wewnętrzne > Instalacja elektryczna" },
  { id: "tc-007", costEstimateItemId: null, workScheduleStageWorkId: "w-ws1-003", isAdditional: false, name: "Transport i wynajem dźwigu", description: null, net: 67800, gross: 83394, vatRate: 23, contractorId: "ctr-002", contractorName: "Strabag Sp. z o.o.", date: date("2026-03-01"), number: "FV/03/2026/045", attachments: [], createdAt: date("2026-03-02"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Etap I", stageName: "1. Stan surowy otwarty", workItemName: "Ściany nośne parteru", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Etap I > 1. Stan surowy otwarty > Ściany nośne parteru" },
  { id: "tc-008", costEstimateItemId: null, workScheduleStageWorkId: "w-ws1-009", isAdditional: false, name: "Izolacje termiczne — wełna i styropian", description: null, net: 98400, gross: 121032, vatRate: 23, contractorId: "ctr-012", contractorName: "InsBud — Izolacje Budowlane", date: date("2026-03-20"), number: "67/03/2026", attachments: [], createdAt: date("2026-03-21"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Etap I", stageName: "2. Instalacje wewnętrzne", workItemName: "Instalacja grzewcza", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Etap I > 2. Instalacje wewnętrzne > Instalacja grzewcza" },
  { id: "tc-009", costEstimateItemId: null, workScheduleStageWorkId: null, isAdditional: true, name: "Projekt wentylacji — honorarium", description: "Koszty projektowe dodatkowe", net: 134500, gross: 165435, vatRate: 23, contractorId: "ctr-011", contractorName: "WentSystemy Sp. z o.o.", date: date("2026-04-05"), number: "FV/04/2026/112", attachments: [], createdAt: date("2026-04-06"), updatedAt: null, sourceType: "ProjectAdditional" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: null },
  { id: "tc-010", costEstimateItemId: null, workScheduleStageWorkId: null, isAdditional: true, name: "Szalunki — wynajem", description: "Wynajem szalunków systemowych", net: 45600, gross: 56088, vatRate: 23, contractorId: "ctr-004", contractorName: "Erbet Sp. z o.o.", date: date("2025-09-10"), number: "321/09/2025", attachments: [], createdAt: date("2025-09-11"), updatedAt: null, sourceType: "ProjectAdditional" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: null },
];

// ---- PER-PROJECT TRACKED COSTS ----

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const trackedCostsP2: any[] = [
  { id: "tc-p2-001", costEstimateItemId: null, workScheduleStageWorkId: "w-ws3-001", isAdditional: false, name: "Roboty ziemne Etap II", description: null, net: 245000, gross: 301350, vatRate: 23, contractorId: "ctr-002", contractorName: "Strabag Sp. z o.o.", date: date("2025-06-20"), number: "15/06/2025", attachments: [], createdAt: date("2025-06-21"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Etap II", stageName: "1. Fundamenty", workItemName: "Wykopy Etap II", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Etap II > 1. Fundamenty > Wykopy Etap II" },
  { id: "tc-p2-002", costEstimateItemId: null, workScheduleStageWorkId: "w-ws3-003", isAdditional: false, name: "Beton B20 fundamenty Etap II", description: null, net: 186000, gross: 228780, vatRate: 23, contractorId: "ctr-005", contractorName: "Cemex Polska Sp. z o.o.", date: date("2025-08-10"), number: "FA/08/2025/234", attachments: [], createdAt: date("2025-08-11"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Etap II", stageName: "1. Fundamenty", workItemName: "Ławy żelbetowe Etap II", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Etap II > 1. Fundamenty > Ławy żelbetowe Etap II" },
  { id: "tc-p2-003", costEstimateItemId: null, workScheduleStageWorkId: "w-ws3-005", isAdditional: false, name: "Stal zbrojeniowa — konstrukcja", description: null, net: 312000, gross: 383760, vatRate: 23, contractorId: "ctr-001", contractorName: "Budimex S.A.", date: date("2025-09-05"), number: "FV/09/2025/89", attachments: [], createdAt: date("2025-09-06"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Etap II", stageName: "2. Konstrukcja", workItemName: "Stropy Etap II", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Etap II > 2. Konstrukcja > Stropy Etap II" },
  { id: "tc-p2-004", costEstimateItemId: null, workScheduleStageWorkId: null, isAdditional: true, name: "Wynajem koparki — Etap II", description: null, net: 42500, gross: 52275, vatRate: 23, contractorId: "ctr-004", contractorName: "Erbet Sp. z o.o.", date: date("2025-11-15"), number: "R/11/2025/03", attachments: [], createdAt: date("2025-11-16"), updatedAt: null, sourceType: "ProjectAdditional" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: null },
  { id: "tc-p2-005", costEstimateItemId: null, workScheduleStageWorkId: "w-ws3-007", isAdditional: false, name: "Prace murarskie — ściany", description: null, net: 154000, gross: 189420, vatRate: 23, contractorId: "ctr-008", contractorName: "Wienerberger Ceramika Budowlana", date: date("2026-01-10"), number: "FV/01/2026/007", attachments: [], createdAt: date("2026-01-11"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Etap II", stageName: "3. Stan surowy zamknięty", workItemName: "Ściany działowe Etap II", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Etap II > 3. Stan surowy zamknięty > Ściany działowe Etap II" },
  { id: "tc-p2-006", costEstimateItemId: null, workScheduleStageWorkId: null, isAdditional: false, name: "Izolacje dachu Etap II", description: null, net: 87200, gross: 107256, vatRate: 23, contractorId: "ctr-012", contractorName: "InsBud — Izolacje Budowlane", date: date("2026-03-15"), number: "FV/03/2026/118", attachments: [], createdAt: date("2026-03-16"), updatedAt: null, sourceType: "ProjectAdditional" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: null },
];

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const trackedCostsP4: any[] = [
  { id: "tc-p4-001", costEstimateItemId: null, workScheduleStageWorkId: "w-ws4-001", isAdditional: false, name: "Przygotowanie terenu — Bud. A", description: null, net: 312000, gross: 383760, vatRate: 23, contractorId: "ctr-002", contractorName: "Strabag Sp. z o.o.", date: date("2026-02-01"), number: "FV/02/2026/201", attachments: [], createdAt: date("2026-02-02"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Bud. A", stageName: "1. Roboty przygotowawcze", workItemName: "Wykopy Bud. A", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Bud. A > 1. Roboty przygotowawcze > Wykopy Bud. A" },
  { id: "tc-p4-002", costEstimateItemId: null, workScheduleStageWorkId: "w-ws4-003", isAdditional: false, name: "Pale fundamentowe — Bud. A", description: null, net: 198000, gross: 243540, vatRate: 23, contractorId: "ctr-003", contractorName: "Dębickie Przedsiębiorstwo Budowlane", date: date("2026-03-10"), number: "FV/03/2026/089", attachments: [], createdAt: date("2026-03-11"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Bud. A", stageName: "1. Roboty przygotowawcze", workItemName: "Fundamenty palowe", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Bud. A > 1. Roboty przygotowawcze > Fundamenty palowe" },
  { id: "tc-p4-003", costEstimateItemId: null, workScheduleStageWorkId: "w-ws4-006", isAdditional: false, name: "Konstrukcja żelbetowa — apartamenty", description: null, net: 567000, gross: 697410, vatRate: 23, contractorId: "ctr-001", contractorName: "Budimex S.A.", date: date("2026-05-15"), number: "FV/05/2026/456", attachments: [], createdAt: date("2026-05-16"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Bud. A", stageName: "2. Konstrukcja", workItemName: "Stropy i słupy apartamenty", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Bud. A > 2. Konstrukcja > Stropy i słupy apartamenty" },
  { id: "tc-p4-004", costEstimateItemId: null, workScheduleStageWorkId: "w-ws5-002", isAdditional: false, name: "Ściany osłonowe — elewacja", description: null, net: 445000, gross: 547350, vatRate: 23, contractorId: "ctr-006", contractorName: "Saint-Gobain Construction Products", date: date("2025-09-20"), number: "FV/09/2025/789", attachments: [], createdAt: date("2025-09-21"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Apartamenty Centrum", stageName: "1. Stan surowy", workItemName: "Ściany osłonowe", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Apartamenty Centrum > 1. Stan surowy > Ściany osłonowe" },
  { id: "tc-p4-005", costEstimateItemId: null, workScheduleStageWorkId: null, isAdditional: true, name: "Projekt konstrukcji apartamentów", description: null, net: 98500, gross: 121155, vatRate: 23, contractorId: "ctr-011", contractorName: "WentSystemy Sp. z o.o.", date: date("2025-08-01"), number: "FV/08/2025/001", attachments: [], createdAt: date("2025-08-02"), updatedAt: null, sourceType: "ProjectAdditional" as const, scheduleName: null, stageName: null, workItemName: null, estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: null },
  { id: "tc-p4-006", costEstimateItemId: null, workScheduleStageWorkId: "w-ws5-005", isAdditional: false, name: "Instalacje sanitarne — apartamenty", description: null, net: 278000, gross: 341940, vatRate: 23, contractorId: "ctr-009", contractorName: "Wodoinstal Kraków Sp. z o.o.", date: date("2026-04-05"), number: "FV/04/2026/312", attachments: [], createdAt: date("2026-04-06"), updatedAt: null, sourceType: "ScheduleWorkItem" as const, scheduleName: "Harmonogram — Apartamenty Centrum", stageName: "2. Wykończenie", workItemName: "Instalacje wod-kan", estimateName: null, estimateGroupName: null, estimateItemName: null, costEstimateItemPath: null, workScheduleWorkPath: "Harmonogram — Apartamenty Centrum > 2. Wykończenie > Instalacje wod-kan" },
];

/** Mapa kosztów śledzonych per projekt */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const trackedCostsByProject: Record<string, any[]> = {
  [P1]: trackedCostsP1,
  [P2]: trackedCostsP2,
  [P4]: trackedCostsP4,
};

// ---- DASHBOARD — computed from source data ----

/** Wylicza dashboard dla projektu na podstawie kosztów, kosztorysów i harmonogramów */
export function getDashboard(projectId: string): object {
  // Pobierz koszty dla projektu
  const allCosts = trackedCostsByProject[projectId] || [];

  // Podziel koszty na kategorie
  const additionalCosts = allCosts.filter(c => c.isAdditional === true);
  const linkedCosts = allCosts.filter(c => c.isAdditional !== true);

  // Sumy
  const sumNet = (items: any[]) => items.reduce((s: number, c: any) => s + (c.net ?? 0), 0);
  const sumGross = (items: any[]) => items.reduce((s: number, c: any) => s + (c.gross ?? 0), 0);

  const totalCostsNet = sumNet(allCosts);
  const totalCostsGross = sumGross(allCosts);
  const linkedCostsNet = sumNet(linkedCosts);
  const linkedCostsGross = sumGross(linkedCosts);
  const additionalCostsNet = sumNet(additionalCosts);
  const additionalCostsGross = sumGross(additionalCosts);

  // Kosztorysy dla projektu — uwaga: mockCostEstimates ma totalNet/totalGross (nie net/gross)
  const projectEstimates = mockCostEstimates.filter(e => e.projectId === projectId);
  const totalBudgetNet = projectEstimates.reduce((s: number, ce: any) => s + (ce.totalNet ?? 0), 0);
  const totalBudgetGross = projectEstimates.reduce((s: number, ce: any) => s + (ce.totalGross ?? 0), 0);

  // Deviation = totalBudget − totalCosts (zgodnie z kontraktem DEVIATION_COLOR: >0 = w budżecie)
  const deviationNet = totalBudgetNet - totalCostsNet;
  const deviationGross = totalBudgetGross - totalCostsGross;
  const coveredPercent = totalBudgetNet > 0 ? Math.round((linkedCostsNet / totalBudgetNet) * 10000) / 100 : 0;

  // Cost estimate summaries
  const costEstimateSummaries = projectEstimates.map(ce => {
    const ceCosts = linkedCosts.filter(c => {
      if (c.costEstimateItemId) {
        // Sprawdź czy item należy do tego kosztorysu — szukaj w strukturze
        return ce.id === "ce-001" && c.costEstimateItemId.startsWith("i-b");
      }
      return false;
    });
    const ceCostsNet = sumNet(ceCosts);
    const ceCostsGross = sumGross(ceCosts);
    // Uproszczone: policz itemy z kosztorysu
    const totalItemsCount = ce.id === "ce-001" ? 14 : (ce.id === "ce-002" ? 8 : 6);
    const itemsWithCostsCount = ceCosts.length;
    return {
      costEstimateId: ce.id,
      costEstimateName: ce.name,
      budgetNet: ce.totalNet ?? 0,
      budgetGross: ce.totalGross ?? 0,
      costsNet: ceCostsNet,
      costsGross: ceCostsGross,
      deviationNet: (ce.totalNet ?? 0) - ceCostsNet,
      deviationGross: (ce.totalGross ?? 0) - ceCostsGross,
      deviationPercent: (ce.totalNet ?? 0) > 0 ? Math.round((ceCostsNet / (ce.totalNet ?? 0)) * 10000) / 100 : 0,
      isBudgetExceeded: ceCostsNet > (ce.totalNet ?? 0),
      additionalCostsNet: null,
      additionalCostsGross: null,
      additionalCostsCount: 0,
      costCount: ceCosts.length,
      coveredPercent: (ce.totalNet ?? 0) > 0 ? Math.round((ceCostsNet / (ce.totalNet ?? 0)) * 10000) / 100 : 0,
      totalItemsCount,
      itemsWithCostsCount,
      itemsWithoutCostsCount: totalItemsCount - itemsWithCostsCount,
      itemsOverBudgetCount: 0,
      itemsNearLimitCount: itemsWithCostsCount > 0 ? 1 : 0,
      groups: [],
      additionalCosts: { costsNet: null, costsGross: null, costCount: 0, items: [] },
    };
  });

  // Schedule summaries — użyj szczegółów harmonogramu
  const projectSchedules = scheduleListData.filter(s => s.projectId === projectId);
  const scheduleSummaries = projectSchedules.map(ws => {
    const wsDetail: any = getWorkScheduleDetails(ws.id);
    const stages = wsDetail?.stages ?? [];
    const allWorks = stages.flatMap((s: any) => s.works ?? []);
    const wsCosts = linkedCosts.filter(c =>
      c.workScheduleStageWorkId && allWorks.some((w: any) => w.id === c.workScheduleStageWorkId)
    );
    const wsCostsNet = sumNet(wsCosts);
    const wsCostsGross = sumGross(wsCosts);

    // Timeline data z harmonogramu
    let earliestStart: string | null = null;
    let latestEnd: string | null = null;
    let completedCount = 0;
    let inProgressCount = 0;
    let notStartedCount = 0;
    for (const w of allWorks) {
      for (const p of (w.periods ?? [])) {
        if (!earliestStart || p.startDate < earliestStart) earliestStart = p.startDate;
        if (!latestEnd || p.endDate > latestEnd) latestEnd = p.endDate;
      }
      if (w.isClosed) completedCount++;
      else if (w.periods?.some((p: any) => new Date(p.startDate) <= new Date())) inProgressCount++;
      else notStartedCount++;
    }
    const totalPlannedDays = earliestStart && latestEnd
      ? Math.round((new Date(latestEnd).getTime() - new Date(earliestStart).getTime()) / (1000 * 86400))
      : 0;
    const progressPercent = allWorks.length > 0 ? Math.round((completedCount / allWorks.length) * 100) : 0;

    // Stage summaries
    const stageSummaries = stages.map((stg: any) => {
      const stgCosts = linkedCosts.filter((c: any) =>
        c.workScheduleStageWorkId && (stg.works ?? []).some((w: any) => w.id === c.workScheduleStageWorkId)
      );
      const stgCostsNet = sumNet(stgCosts);
      const stgCostsGross = sumGross(stgCosts);
      const stgWorks = stg.works ?? [];
      const stgCompleted = stgWorks.filter((w: any) => w.isClosed).length;
      const stgInProgress = stgWorks.filter((w: any) => !w.isClosed && w.periods?.some((p: any) => new Date(p.startDate) <= new Date())).length;
      const stgAllPeriods = stgWorks.flatMap((w: any) => w.periods ?? []);
      let stgEarliest: string | null = null;
      let stgLatest: string | null = null;
      for (const p of stgAllPeriods) {
        if (!stgEarliest || p.startDate < stgEarliest) stgEarliest = p.startDate;
        if (!stgLatest || p.endDate > stgLatest) stgLatest = p.endDate;
      }
      const stgPlannedDays = stgEarliest && stgLatest
        ? Math.round((new Date(stgLatest).getTime() - new Date(stgEarliest).getTime()) / (1000 * 86400))
        : 0;
      const stgProgress = stgWorks.length > 0 ? Math.round((stgCompleted / stgWorks.length) * 100) : 0;
      return {
        stageId: stg.id,
        stageName: stg.name,
        order: stg.order,
        totalWorkItemsCount: stgWorks.length,
        completedWorkItemsCount: stgCompleted,
        delayedWorkItemsCount: 0,
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
        financialStatus: stgCosts.length > 0 ? 2 : 1,
        timelineStatus: stgCompleted === stgWorks.length ? 4 : (stgInProgress > 0 ? 2 : 1),
        hasLinkedSchedule: false,
        timeline: stgEarliest ? {
          plannedStart: stgEarliest,
          plannedEnd: stgLatest,
          totalPlannedDays: stgPlannedDays,
          totalWorkCount: stgWorks.length,
          completedCount: stgCompleted,
          completedLateCount: 0,
          inProgressCount: stgInProgress,
          notStartedCount: stgWorks.length - stgCompleted - stgInProgress,
          delayedCount: 0,
          progressPercent: stgProgress,
          delayDays: null,
          overallStatus: stgCompleted === stgWorks.length ? 4 : (stgInProgress > 0 ? 2 : 1),
          isDelayed: false,
          isCompleted: stgCompleted === stgWorks.length,
        } : null,
        workItems: [],
        childStages: [],
      };
    });

    return {
      workScheduleId: ws.id,
      workScheduleName: ws.name,
      hasLinkedEstimate: !!ws.costEstimateId,
      linkedCostEstimateId: ws.costEstimateId ?? null,
      totalWorkItemsCount: allWorks.length,
      workItemsWithCostsCount: wsCosts.length,
      workItemsOverBudgetCount: 0,
      workItemsNearLimitCount: 0,
      workItemsDelayedCount: 0,
      totalCostsNet: wsCostsNet > 0 ? wsCostsNet : null,
      totalCostsGross: wsCostsGross > 0 ? wsCostsGross : null,
      budgetNet: ws.costEstimateId ? (projectEstimates.find(e => e.id === ws.costEstimateId)?.totalNet ?? null) : null,
      budgetGross: ws.costEstimateId ? (projectEstimates.find(e => e.id === ws.costEstimateId)?.totalGross ?? null) : null,
      costsNet: wsCostsNet > 0 ? wsCostsNet : null,
      costsGross: wsCostsGross > 0 ? wsCostsGross : null,
      deviationNet: wsCostsNet > 0 ? ((ws.costEstimateId ? (projectEstimates.find(e => e.id === ws.costEstimateId)?.totalNet ?? 0) : 0) - wsCostsNet) : null,
      deviationGross: wsCostsGross > 0 ? ((ws.costEstimateId ? (projectEstimates.find(e => e.id === ws.costEstimateId)?.totalGross ?? 0) : 0) - wsCostsGross) : null,
      deviationPercent: ws.costEstimateId && (projectEstimates.find(e => e.id === ws.costEstimateId)?.totalNet ?? 0) > 0
        ? Math.round((wsCostsNet / (projectEstimates.find(e => e.id === ws.costEstimateId)?.totalNet ?? 1)) * 10000) / 100 : null,
      coveredPercent: ws.costEstimateId && (projectEstimates.find(e => e.id === ws.costEstimateId)?.totalNet ?? 0) > 0
        ? Math.round((wsCostsNet / (projectEstimates.find(e => e.id === ws.costEstimateId)?.totalNet ?? 1)) * 10000) / 100 : null,
      isBudgetExceeded: false,
      costCount: wsCosts.length,
      financialStatus: wsCosts.length > 0 ? 2 : 1,
      timelineStatus: completedCount === allWorks.length ? 4 : (inProgressCount > 0 ? 2 : 1),
      hasLinkedSchedule: !!ws.costEstimateId,
      timeline: earliestStart ? {
        plannedStart: earliestStart,
        plannedEnd: latestEnd,
        totalPlannedDays,
        totalWorkCount: allWorks.length,
        completedCount,
        completedLateCount: 0,
        inProgressCount,
        notStartedCount,
        delayedCount: 0,
        progressPercent,
        delayDays: null,
        overallStatus: completedCount === allWorks.length ? 4 : (inProgressCount > 0 ? 2 : 1),
        isDelayed: false,
        isCompleted: completedCount === allWorks.length,
      } : null,
      stages: stageSummaries,
    };
  });

  // Timeline summary — łączny dla projektu
  let globalEarliest: string | null = null;
  let globalLatest: string | null = null;
  let globalTotalWorks = 0;
  let globalCompleted = 0;
  let globalInProgress = 0;
  let globalNotStarted = 0;
  for (const ws of projectSchedules) {
    const detail: any = getWorkScheduleDetails(ws.id);
    const allWorks = (detail?.stages ?? []).flatMap((s: any) => s.works ?? []);
    globalTotalWorks += allWorks.length;
    for (const w of allWorks) {
      for (const p of (w.periods ?? [])) {
        if (!globalEarliest || p.startDate < globalEarliest) globalEarliest = p.startDate;
        if (!globalLatest || p.endDate > globalLatest) globalLatest = p.endDate;
      }
      if (w.isClosed) globalCompleted++;
      else if (w.periods?.some((p: any) => new Date(p.startDate) <= new Date())) globalInProgress++;
      else globalNotStarted++;
    }
  }
  const globalPlannedDays = globalEarliest && globalLatest
    ? Math.round((new Date(globalLatest).getTime() - new Date(globalEarliest).getTime()) / (1000 * 86400))
    : 0;
  const globalProgressPercent = globalTotalWorks > 0 ? Math.round((globalCompleted / globalTotalWorks) * 100) : 0;

  // Schedule cost summary
  const schedulesWithCosts = scheduleSummaries.filter(s => (s.costCount ?? 0) > 0);
  const schedulesWithoutCosts = scheduleSummaries.filter(s => (s.costCount ?? 0) === 0);
  const totalSchedulesCostsNet = sumNet(scheduleSummaries.map(s => ({ net: s.totalCostsNet ?? 0 })));
  const totalSchedulesCostsGross = sumGross(scheduleSummaries.map(s => ({ gross: s.totalCostsGross ?? 0 })));

  return {
    projectId,
    selectedCurrencyCode: "PLN",
    selectedCurrencySymbol: "zł",
    referenceDate: now,
    generatedAt: now,
    financialSummary: {
      totalBudgetNet,
      totalBudgetGross,
      estimateBudgetNet: totalBudgetNet,
      estimateBudgetGross: totalBudgetGross,
      projectReserveBudgetNet: null,
      projectReserveBudgetGross: null,
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
      financialStatus: totalCostsNet > 0 ? 2 : 1,
      totalCostCount: allCosts.length,
      linkedCostCount: linkedCosts.length,
      additionalCostCount: additionalCosts.length,
      costEstimatesCount: projectEstimates.length,
      costEstimatesWithCostsCount: costEstimateSummaries.filter(s => s.costCount > 0).length,
      costEstimatesOverBudgetCount: 0,
      workSchedulesCount: projectSchedules.length,
      scheduleCostSummary: {
        totalSchedulesCostsNet,
        totalSchedulesCostsGross,
        schedulesWithCostsCount: schedulesWithCosts.length,
        schedulesWithoutCostsCount: schedulesWithoutCosts.length,
      },
    },
    timelineSummary: {
      earliestStart: globalEarliest,
      latestEnd: globalLatest,
      totalPlannedDays: globalPlannedDays,
      totalWorkCount: globalTotalWorks,
      completedCount: globalCompleted,
      completedLateCount: 0,
      inProgressCount: globalInProgress,
      notStartedCount: globalNotStarted,
      delayedCount: 0,
      progressPercent: globalProgressPercent,
      delayDays: null,
      overallStatus: globalCompleted === globalTotalWorks ? 4 : (globalInProgress > 0 ? 2 : 1),
      isDelayed: false,
      isCompleted: globalCompleted === globalTotalWorks,
      workSchedulesCount: projectSchedules.length,
      activeSchedulesCount: projectSchedules.length,
      completedSchedulesCount: 0,
    },
    costEstimateSummaries,
    scheduleSummaries,
    projectAdditionalCosts: {
      totalAdditionalNet: additionalCostsNet,
      totalAdditionalGross: additionalCostsGross,
      additionalCostsCount: additionalCosts.length,
    },
    allCosts,
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
