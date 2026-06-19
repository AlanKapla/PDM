/**
 * Mock kosztorysów — schemat zgodny z CostEstimateDetailsWeb (direct properties + fieldSchemas).
 */

const VAT_RATE = 23;

const DEFAULT_FIELD_DEFS: ReadonlyArray<{ fieldKey: string; fieldName: string; fieldType: number }> = [
  { fieldKey: 'name', fieldName: 'Nazwa', fieldType: 100 },
  { fieldKey: 'actions', fieldName: 'Akcje', fieldType: 112 },
  { fieldKey: 'quantity', fieldName: 'Ilość', fieldType: 101 },
  { fieldKey: 'unit', fieldName: 'Jednostka', fieldType: 102 },
  { fieldKey: 'unitPriceNet', fieldName: 'Cena netto', fieldType: 103 },
  { fieldKey: 'vatRate', fieldName: 'Stawka VAT', fieldType: 104 },
  { fieldKey: 'unitPriceGross', fieldName: 'Cena brutto', fieldType: 105 },
  { fieldKey: 'netValue', fieldName: 'Wartość netto', fieldType: 106 },
  { fieldKey: 'grossValue', fieldName: 'Wartość brutto', fieldType: 107 },
  { fieldKey: 'vatValue', fieldName: 'Wartość VAT', fieldType: 108 },
  { fieldKey: 'isSelected', fieldName: 'Sumuj', fieldType: 109 },
  { fieldKey: 'isStageWork', fieldName: 'Zakres harmonogramu', fieldType: 110 },
  { fieldKey: 'files', fieldName: 'Plik', fieldType: 111 },
];

const CATEGORY_FIELD_DEF = {
  fieldKey: 'kategoria',
  fieldName: 'Kategoria',
  fieldType: 0,
};

export interface MockCostEstimateItem {
  id: string;
  groupId: string;
  relationType: number;
  order: number;
  name: string;
  quantity: number;
  unit: string;
  unitPriceNet: number;
  vatRate: number;
  unitPriceGross: number;
  netValue: number;
  grossValue: number;
  vatValue: number;
  isSelected: boolean;
  isStageWork: boolean;
  additionalFieldValues: Array<{
    id: string;
    additionalFieldId: string;
    stringValue?: string;
  }>;
  options?: undefined;
  components?: undefined;
  files: [];
  createdAt: string;
  updatedAt: string;
}

export interface MockCostEstimateGroup {
  id: string;
  parentGroupId?: undefined;
  level: number;
  order: number;
  name: string;
  totalNet: number;
  totalGross: number;
  totalVat: number;
  additionalFieldValues: [];
  lastCalculatedAt?: string;
  childGroups: [];
  items: MockCostEstimateItem[];
  createdAt: string;
  updatedAt?: string;
}

type ItemSeed = {
  id: string;
  name: string;
  unit: string;
  quantity: number;
  unitPriceNet: number;
  category?: string;
  isStageWork?: boolean;
};

type GroupSeed = {
  id: string;
  name: string;
  order: number;
  items: ItemSeed[];
  createdAt: string;
  updatedAt?: string;
  lastCalculatedAt?: string;
};

function round2(value: number): number {
  return Math.round(value * 100) / 100;
}

function calcFinancials(quantity: number, unitPriceNet: number, vatRate: number = VAT_RATE): {
  netValue: number;
  vatValue: number;
  grossValue: number;
  unitPriceGross: number;
} {
  const netValue = round2(quantity * unitPriceNet);
  const vatValue = round2(netValue * vatRate / 100);
  const grossValue = round2(netValue + vatValue);
  const unitPriceGross = round2(unitPriceNet * (1 + vatRate / 100));
  return { netValue, vatValue, grossValue, unitPriceGross };
}

function sumSelectedItems(items: MockCostEstimateItem[]): { totalNet: number; totalGross: number; totalVat: number } {
  const selected = items.filter((item) => item.isSelected);
  const totalNet = round2(selected.reduce((sum, item) => sum + item.netValue, 0));
  const totalVat = round2(selected.reduce((sum, item) => sum + item.vatValue, 0));
  const totalGross = round2(selected.reduce((sum, item) => sum + item.grossValue, 0));
  return { totalNet, totalGross, totalVat };
}

function buildMockItem(
  seed: ItemSeed,
  order: number,
  groupId: string,
  categoryFieldId: string,
  createdAt: string,
  updatedAt: string,
): MockCostEstimateItem {
  const financials = calcFinancials(seed.quantity, seed.unitPriceNet);
  return {
    id: seed.id,
    groupId,
    relationType: 0,
    order,
    name: seed.name,
    quantity: seed.quantity,
    unit: seed.unit,
    unitPriceNet: seed.unitPriceNet,
    vatRate: VAT_RATE,
    unitPriceGross: financials.unitPriceGross,
    netValue: financials.netValue,
    grossValue: financials.grossValue,
    vatValue: financials.vatValue,
    isSelected: true,
    isStageWork: seed.isStageWork ?? false,
    additionalFieldValues: seed.category
      ? [{
          id: `${seed.id}-cat`,
          additionalFieldId: categoryFieldId,
          stringValue: seed.category,
        }]
      : [],
    options: undefined,
    components: undefined,
    files: [],
    createdAt,
    updatedAt,
  };
}

function buildGroupFromSeed(seed: GroupSeed, categoryFieldId: string): MockCostEstimateGroup {
  const updatedAt = seed.updatedAt ?? seed.createdAt;
  const items = seed.items.map((itemSeed, order) =>
    buildMockItem(itemSeed, order, seed.id, categoryFieldId, seed.createdAt, updatedAt)
  );
  const totals = sumSelectedItems(items);
  return {
    id: seed.id,
    parentGroupId: undefined,
    level: 0,
    order: seed.order,
    name: seed.name,
    totalNet: totals.totalNet,
    totalGross: totals.totalGross,
    totalVat: totals.totalVat,
    additionalFieldValues: [],
    lastCalculatedAt: seed.lastCalculatedAt,
    childGroups: [],
    items,
    createdAt: seed.createdAt,
    updatedAt: seed.updatedAt,
  };
}

function buildGroups(seeds: GroupSeed[], categoryFieldId: string): MockCostEstimateGroup[] {
  return seeds.map((seed) => buildGroupFromSeed(seed, categoryFieldId));
}

const MOCK_CATEGORY_FIELD_ID = 'af-kategoria';

// ---- Wariant A: Budowlany (4 grupy, 14 pozycji) ----
const buildingGroupSeeds: GroupSeed[] = [
  {
    id: 'g-b001', name: '1. Roboty ziemne i fundamentowe', order: 0,
    createdAt: '2025-06-15T08:00:00Z', updatedAt: '2026-01-20T08:00:00Z', lastCalculatedAt: '2026-01-20T08:00:00Z',
    items: [
      { id: 'i-b001', name: 'Wykopy pod fundamenty', unit: 'm³', quantity: 2850, unitPriceNet: 95.5, category: 'Robocizna', isStageWork: true },
      { id: 'i-b002', name: 'Ławy fundamentowe żelbetowe', unit: 'm³', quantity: 480, unitPriceNet: 680, category: 'Materiały', isStageWork: true },
      { id: 'i-b003', name: 'Izolacja przeciwwilgociowa fundamentów', unit: 'm²', quantity: 1250, unitPriceNet: 42, category: 'Materiały' },
      { id: 'i-b004', name: 'Zasypka i zagęszczenie', unit: 'm³', quantity: 1620, unitPriceNet: 35, category: 'Robocizna' },
    ],
  },
  {
    id: 'g-b002', name: '2. Konstrukcja żelbetowa', order: 1,
    createdAt: '2025-06-15T08:00:00Z', updatedAt: '2026-01-20T08:00:00Z',
    items: [
      { id: 'i-b005', name: 'Słupy żelbetowe 40×40 cm', unit: 'm³', quantity: 320, unitPriceNet: 920, category: 'Robocizna', isStageWork: true },
      { id: 'i-b006', name: 'Stropy żelbetowe monolityczne', unit: 'm²', quantity: 4800, unitPriceNet: 245, category: 'Robocizna' },
      { id: 'i-b007', name: 'Schody żelbetowe', unit: 'kpl.', quantity: 6, unitPriceNet: 18500, category: 'Robocizna' },
    ],
  },
  {
    id: 'g-b003', name: '3. Ściany i elewacja', order: 2,
    createdAt: '2025-06-15T08:00:00Z', updatedAt: '2026-01-20T08:00:00Z',
    items: [
      { id: 'i-b008', name: 'Ściany nośne z bloczków silikatowych', unit: 'm²', quantity: 6200, unitPriceNet: 145, isStageWork: true },
      { id: 'i-b009', name: 'Elewacja — tynk silikonowy', unit: 'm²', quantity: 4800, unitPriceNet: 98 },
      { id: 'i-b010', name: 'Stolarka okienna PCV 3-szybowa', unit: 'm²', quantity: 980, unitPriceNet: 850 },
    ],
  },
  {
    id: 'g-b004', name: '4. Pozostałe grupy (skrócone)', order: 3,
    createdAt: '2025-06-15T08:00:00Z', updatedAt: '2026-01-20T08:00:00Z',
    items: [
      { id: 'i-b011', name: 'Dach i pokrycie dachowe', unit: 'kpl.', quantity: 1, unitPriceNet: 2450000 },
      { id: 'i-b012', name: 'Instalacje sanitarne (wod-kan, CO)', unit: 'kpl.', quantity: 1, unitPriceNet: 2850000 },
      { id: 'i-b013', name: 'Instalacja elektryczna i teletechnika', unit: 'kpl.', quantity: 1, unitPriceNet: 1920000 },
      { id: 'i-b014', name: 'Koszty pośrednie i organizacja placu budowy', unit: 'kpl.', quantity: 1, unitPriceNet: 737825 },
    ],
  },
];

// ---- Wariant B: Instalacje (2 grupy, 6 pozycji) ----
const installationGroupSeeds: GroupSeed[] = [
  {
    id: 'g-s001', name: '1. Instalacje wodociągowe i kanalizacyjne', order: 0,
    createdAt: '2025-07-10T08:00:00Z', updatedAt: '2026-02-15T08:00:00Z', lastCalculatedAt: '2026-02-15T08:00:00Z',
    items: [
      { id: 'i-s001', name: 'Rurociągi wodociągowe PP-R', unit: 'm.b.', quantity: 850, unitPriceNet: 400, category: 'Materiały' },
      { id: 'i-s002', name: 'Kanalizacja sanitarna PCV', unit: 'm.b.', quantity: 700, unitPriceNet: 600, category: 'Materiały' },
      { id: 'i-s003', name: 'Pompy i zestawy hydroforowe', unit: 'kpl.', quantity: 2, unitPriceNet: 180000, category: 'Sprzęt' },
    ],
  },
  {
    id: 'g-s002', name: '2. Instalacje grzewcze i wentylacyjne', order: 1,
    createdAt: '2025-07-10T08:00:00Z', updatedAt: '2026-02-15T08:00:00Z', lastCalculatedAt: '2026-02-15T08:00:00Z',
    items: [
      { id: 'i-s004', name: 'Kotłownia gazowa z instalacją CO', unit: 'kpl.', quantity: 1, unitPriceNet: 680000, category: 'Robocizna' },
      { id: 'i-s005', name: 'Grzejniki i instalacja rozdzielcza', unit: 'szt.', quantity: 95, unitPriceNet: 6000, category: 'Materiały' },
      { id: 'i-s006', name: 'Wentylacja mechaniczna z rekuperacją', unit: 'kpl.', quantity: 1, unitPriceNet: 480000, category: 'Sprzęt' },
    ],
  },
];

// ---- Wariant C: Zagospodarowanie terenu ----
const landDevelopmentGroupSeeds: GroupSeed[] = [
  {
    id: 'g-l001', name: '1. Nawierzchnie i drogi', order: 0,
    createdAt: '2025-11-15T08:00:00Z', lastCalculatedAt: '2025-11-15T08:00:00Z',
    items: [
      { id: 'i-l001', name: 'Nawierzchnia z kostki brukowej', unit: 'm²', quantity: 1600, unitPriceNet: 200, category: 'Materiały' },
      { id: 'i-l002', name: 'Krawężniki i obrzeża betonowe', unit: 'm.b.', quantity: 1200, unitPriceNet: 300, category: 'Materiały' },
    ],
  },
  {
    id: 'g-l002', name: '2. Zieleń i mała architektura', order: 1,
    createdAt: '2025-11-15T08:00:00Z', lastCalculatedAt: '2025-11-15T08:00:00Z',
    items: [
      { id: 'i-l003', name: 'Nasadzenia drzew i krzewów', unit: 'szt.', quantity: 180, unitPriceNet: 1555.56, category: 'Robocizna' },
      { id: 'i-l004', name: 'Ławki, kosze, oświetlenie parkowe', unit: 'kpl.', quantity: 40, unitPriceNet: 8500, category: 'Materiały' },
      { id: 'i-l005', name: 'Trawniki i nawodnienie', unit: 'm²', quantity: 3000, unitPriceNet: 50, category: 'Robocizna' },
    ],
  },
];

// ---- Wariant D: Garaż podziemny ----
const garageGroupSeeds: GroupSeed[] = [
  {
    id: 'g-r001', name: '1. Konstrukcja garażu', order: 0,
    createdAt: '2025-08-20T08:00:00Z', updatedAt: '2026-01-15T08:00:00Z', lastCalculatedAt: '2026-01-15T08:00:00Z',
    items: [
      { id: 'i-r001', name: 'Płyta denna żelbetowa 30 cm', unit: 'm³', quantity: 1150, unitPriceNet: 800, category: 'Materiały' },
      { id: 'i-r002', name: 'Ściany oporowe żelbetowe', unit: 'm³', quantity: 400, unitPriceNet: 1700, category: 'Robocizna' },
      { id: 'i-r003', name: 'Strop garażu z otworami wentylacyjnymi', unit: 'm²', quantity: 2500, unitPriceNet: 200, category: 'Materiały' },
    ],
  },
  {
    id: 'g-r002', name: '2. Wykończenie i instalacje', order: 1,
    createdAt: '2025-08-20T08:00:00Z', updatedAt: '2026-01-15T08:00:00Z', lastCalculatedAt: '2026-01-15T08:00:00Z',
    items: [
      { id: 'i-r004', name: 'Posadzka epoksydowa garażu', unit: 'm²', quantity: 2600, unitPriceNet: 300, category: 'Materiały' },
      { id: 'i-r005', name: 'Wentylacja mechaniczna garażu', unit: 'kpl.', quantity: 1, unitPriceNet: 640000, category: 'Sprzęt' },
      { id: 'i-r006', name: 'Instalacja oświetlenia i zasilania', unit: 'kpl.', quantity: 1, unitPriceNet: 730000, category: 'Robocizna' },
    ],
  },
];

// ---- Wariant E: Kosztorys wstępny EUR ----
const preliminaryGroupSeeds: GroupSeed[] = [
  {
    id: 'g-p001', name: '1. Prace koncepcyjne i przygotowawcze', order: 0,
    createdAt: '2026-01-15T08:00:00Z',
    items: [
      { id: 'i-p001', name: 'Projekt koncepcyjny i wstępne koszty', unit: 'kpl.', quantity: 1, unitPriceNet: 1200000, category: 'Robocizna' },
      { id: 'i-p002', name: 'Badania geotechniczne i pomiary', unit: 'kpl.', quantity: 1, unitPriceNet: 950000, category: 'Robocizna' },
      { id: 'i-p003', name: 'Uzyskanie pozwoleń i opinii', unit: 'kpl.', quantity: 1, unitPriceNet: 1050000, category: 'Robocizna' },
    ],
  },
];

export const buildingGroups = buildGroups(buildingGroupSeeds, MOCK_CATEGORY_FIELD_ID);
export const installationGroups = buildGroups(installationGroupSeeds, MOCK_CATEGORY_FIELD_ID);
export const landDevelopmentGroups = buildGroups(landDevelopmentGroupSeeds, MOCK_CATEGORY_FIELD_ID);
export const garageGroups = buildGroups(garageGroupSeeds, MOCK_CATEGORY_FIELD_ID);
export const preliminaryGroups = buildGroups(preliminaryGroupSeeds, MOCK_CATEGORY_FIELD_ID);

export function customizeGroups(
  groups: MockCostEstimateGroup[],
  stageNames: string[],
  itemNameMap: Record<string, string>,
): MockCostEstimateGroup[] {
  return groups.map((group, groupIndex) => ({
    ...group,
    name: stageNames[groupIndex] ?? group.name,
    items: group.items.map((item) => ({
      ...item,
      name: itemNameMap[item.name] ?? item.name,
    })),
  }));
}

export function buildMockFieldSchemas(costEstimateId: string, createdAt: string): Array<{
  id: string;
  costEstimateId: string;
  fieldName: string;
  fieldKey: string;
  fieldType: number;
  isBasicField: boolean;
  isAdditionalField: boolean;
  order: number;
  createdAt: string;
}> {
  const basicSchemas = DEFAULT_FIELD_DEFS.map((field, order) => ({
    id: `fs-${costEstimateId}-${field.fieldKey}`,
    costEstimateId,
    fieldName: field.fieldName,
    fieldKey: field.fieldKey,
    fieldType: field.fieldType,
    isBasicField: true,
    isAdditionalField: false,
    order,
    createdAt,
  }));

  const categorySchema = {
    id: `${costEstimateId}-${MOCK_CATEGORY_FIELD_ID}`,
    costEstimateId,
    fieldName: CATEGORY_FIELD_DEF.fieldName,
    fieldKey: CATEGORY_FIELD_DEF.fieldKey,
    fieldType: CATEGORY_FIELD_DEF.fieldType,
    isBasicField: false,
    isAdditionalField: true,
    order: basicSchemas.length,
    createdAt,
  };

  return [...basicSchemas, categorySchema];
}

export function buildMockAdditionalFields(costEstimateId: string, createdAt: string): Array<{
  id: string;
  costEstimateId: string;
  name: string;
  fieldType: number;
  order: number;
  createdAt: string;
}> {
  return [{
    id: `${costEstimateId}-${MOCK_CATEGORY_FIELD_ID}`,
    costEstimateId,
    name: CATEGORY_FIELD_DEF.fieldName,
    fieldType: CATEGORY_FIELD_DEF.fieldType,
    order: DEFAULT_FIELD_DEFS.length,
    createdAt,
  }];
}

export function getCategoryFieldId(costEstimateId: string): string {
  return `${costEstimateId}-${MOCK_CATEGORY_FIELD_ID}`;
}

export function remapGroupsForEstimate(
  groups: MockCostEstimateGroup[],
  estimateId: string,
  categoryFieldId: string,
): MockCostEstimateGroup[] {
  return groups.map((group, groupIndex) => {
    const groupId = `${estimateId}-g-${groupIndex}`;
    const items = group.items.map((item) => ({
      ...item,
      id: `${estimateId}-${item.id}`,
      groupId,
      additionalFieldValues: item.additionalFieldValues.map((fieldValue) => ({
        ...fieldValue,
        id: `${estimateId}-${fieldValue.id}`,
        additionalFieldId: categoryFieldId,
      })),
    }));
    const totals = sumSelectedItems(items);
    return {
      ...group,
      id: groupId,
      items,
      totalNet: totals.totalNet,
      totalGross: totals.totalGross,
      totalVat: totals.totalVat,
    };
  });
}

export type EstimateMeta = {
  tenantId: string;
  projectId: string;
  groups: MockCostEstimateGroup[];
  name: string;
  description: string | null;
  status: number;
  totalNet: number;
  ownerName: string;
  workScheduleId: string | null;
  sharedWith: Array<{ userId: string; fullName: string; email: string; sharedAt: string }>;
  currency: { code: string; symbol: string };
  customize?: (groups: MockCostEstimateGroup[]) => MockCostEstimateGroup[];
};
