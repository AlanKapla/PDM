/**
 * Dane demonstracyjne szablonów kosztorysów oraz kosztorysów projektów.
 * Szablony zawierają realistyczną strukturę pól dla branży budowlanej.
 */

import type {
  CostEstimateTemplate,
  CostEstimateTemplateDetails,
} from "../../types/costEstimate.types";
import type {
  CostEstimateListItemWeb,
  CostEstimateDetailsWeb,
} from "../../types/costEstimate.types.new";
import { CostEstimateStatus, CostEstimateAccessLevel } from "../../types/costEstimate.types.new";
import { DEMO_TENANT_ID, DEMO_USERS } from "./users";
import { DEMO_PROJECT_IDS } from "./projects";

// ===== IDs =====

export const DEMO_TEMPLATE_IDS = {
  budowlane: "tmpl-budowlane-001",
  drogowe: "tmpl-drogowe-002",
  instalacje: "tmpl-instalacje-003",
} as const;

// Definicje GUIDów pól używanych w szablonie "Roboty budowlane"
const FIELD = {
  // System fields
  NAME: "fd-sys-name",
  QUANTITY: "fd-sys-qty",
  UNIT: "fd-sys-unit",
  // Calculated fields
  UNIT_PRICE: "fd-calc-unit-price",
  NET_VALUE: "fd-calc-net-value",
  VAT_RATE: "fd-calc-vat",
  GROSS_VALUE: "fd-calc-gross",
  // Group header
  GROUP_NAME: "fd-grp-name",
  GROUP_CODE: "fd-grp-code",
  // Generic
  NOTES: "fd-gen-notes",
  CATEGORY: "fd-gen-category",
} as const;

// ===== LISTA SZABLONÓW =====

export const DEMO_TEMPLATE_LIST: CostEstimateTemplate[] = [
  {
    id: DEMO_TEMPLATE_IDS.budowlane,
    name: "Roboty budowlane – Standard",
    description: "Szablon ogólnobudowlany: roboty ziemne, konstrukcja, wykończenie. Obsługuje grupy wielopoziomowe i podsumowanie netto/brutto.",
    createdAt: "2023-01-20T10:00:00Z",
    createdByUserId: DEMO_USERS.anna.userId,
    createdByUserName: "Anna Kowalska",
    itemsCount: 0,
  },
  {
    id: DEMO_TEMPLATE_IDS.drogowe,
    name: "Infrastruktura drogowa",
    description: "Dedykowany szablon do kosztorysowania robót drogowych i inżynieryjnych zgodny z KNNR/KNR.",
    createdAt: "2023-03-05T09:00:00Z",
    createdByUserId: DEMO_USERS.piotr.userId,
    createdByUserName: "Piotr Wiśniewski",
    itemsCount: 0,
  },
  {
    id: DEMO_TEMPLATE_IDS.instalacje,
    name: "Instalacje wewnętrzne",
    description: "Szablon do kosztorysowania instalacji elektrycznych, sanitarnych i HVAC.",
    createdAt: "2023-04-10T08:00:00Z",
    createdByUserId: DEMO_USERS.anna.userId,
    createdByUserName: "Anna Kowalska",
    itemsCount: 0,
  },
];

// ===== SZCZEGÓŁY SZABLONU: Roboty budowlane =====

export const DEMO_TEMPLATE_DETAILS: Record<string, CostEstimateTemplateDetails> = {
  [DEMO_TEMPLATE_IDS.budowlane]: {
    id: DEMO_TEMPLATE_IDS.budowlane,
    name: "Roboty budowlane – Standard",
    description: "Szablon ogólnobudowlany z obsługą grup wielopoziomowych i podsumowaniem netto/brutto.",
    category: "Budownictwo ogólne",
    canAddGroups: true,
    canBranchGroups: true,
    maxGroupLevel: 3,
    autoNumberGroups: true,
    groupNumberFormat: "{level}.{index}",
    createdAt: "2023-01-20T10:00:00Z",
    ownerId: DEMO_USERS.anna.userId,
    ownerName: "Anna Kowalska",
    structure: {
      templateId: DEMO_TEMPLATE_IDS.budowlane,
      currencies: [
        { id: "cur-pln", code: "PLN", name: "Złoty polski", symbol: "zł", isDefault: true, order: 1 },
        { id: "cur-eur", code: "EUR", name: "Euro", symbol: "€", isDefault: false, order: 2 },
      ],
      units: [
        { id: "unit-m2", code: "m²", name: "metr kwadratowy", symbol: "m²", isDefault: false, order: 1 },
        { id: "unit-m3", code: "m³", name: "metr sześcienny", symbol: "m³", isDefault: false, order: 2 },
        { id: "unit-mb", code: "mb", name: "metr bieżący", symbol: "mb", isDefault: false, order: 3 },
        { id: "unit-szt", code: "szt.", name: "sztuka", symbol: "szt.", isDefault: true, order: 4 },
        { id: "unit-kpl", code: "kpl.", name: "komplet", symbol: "kpl.", isDefault: false, order: 5 },
        { id: "unit-t", code: "t", name: "tona", symbol: "t", isDefault: false, order: 6 },
        { id: "unit-kg", code: "kg", name: "kilogram", symbol: "kg", isDefault: false, order: 7 },
      ],
      categories: [
        { id: "cat-001", name: "Roboty ziemne", symbol: "RZ", order: 1 },
        { id: "cat-002", name: "Roboty betoniarskie", symbol: "RB", order: 2 },
        { id: "cat-003", name: "Roboty murowe", symbol: "RM", order: 3 },
        { id: "cat-004", name: "Roboty wykończeniowe", symbol: "RW", order: 4 },
        { id: "cat-005", name: "Stolarka", symbol: "ST", order: 5 },
      ],
      groupHeaderFields: [
        {
          id: FIELD.GROUP_NAME,
          fieldName: FIELD.GROUP_NAME,
          fieldType: 0, // String
          customLabel: "Nazwa działu",
          isRequired: true,
          isVisible: true,
          order: 1,
          isReadonly: false,
        },
        {
          id: FIELD.GROUP_CODE,
          fieldName: FIELD.GROUP_CODE,
          fieldType: 0,
          customLabel: "Kod działu",
          isRequired: false,
          isVisible: true,
          order: 2,
          isReadonly: false,
        },
      ],
      systemFields: [
        {
          id: FIELD.NAME,
          fieldName: FIELD.NAME,
          fieldType: 100, // ItemSystemName
          label: "Nazwa pozycji",
          isRequired: true,
          isVisible: true,
          order: 1,
        },
        {
          id: FIELD.QUANTITY,
          fieldName: FIELD.QUANTITY,
          fieldType: 101, // ItemSystemQuantity
          label: "Ilość",
          isRequired: true,
          isVisible: true,
          order: 2,
        },
        {
          id: FIELD.UNIT,
          fieldName: FIELD.UNIT,
          fieldType: 102, // ItemSystemUnit
          label: "Jednostka",
          isRequired: true,
          isVisible: true,
          order: 3,
        },
      ],
      calculatedFields: [
        {
          id: FIELD.UNIT_PRICE,
          fieldName: FIELD.UNIT_PRICE,
          fieldType: 200, // UnitPriceNet
          label: "Cena jedn. netto",
          isSortable: true,
          isFilterable: false,
          isSummable: false,
          isAutoCalculated: false,
          isReadonly: false,
          isRequired: true,
          isVisible: true,
          order: 4,
        },
        {
          id: FIELD.NET_VALUE,
          fieldName: FIELD.NET_VALUE,
          fieldType: 203, // ValueNet
          label: "Wartość netto",
          isSortable: true,
          isFilterable: false,
          isSummable: true,
          sumInGroup: true,
          sumInTotal: true,
          isAutoCalculated: true,
          isReadonly: true,
          isRequired: false,
          isVisible: true,
          order: 5,
        },
        {
          id: FIELD.VAT_RATE,
          fieldName: FIELD.VAT_RATE,
          fieldType: 201, // VatRate
          label: "VAT %",
          isSortable: false,
          isFilterable: false,
          isSummable: false,
          isAutoCalculated: false,
          isReadonly: false,
          isRequired: false,
          isVisible: true,
          order: 6,
          defaultValue: "23",
        },
        {
          id: FIELD.GROSS_VALUE,
          fieldName: FIELD.GROSS_VALUE,
          fieldType: 204, // ValueGross
          label: "Wartość brutto",
          isSortable: true,
          isFilterable: false,
          isSummable: true,
          sumInGroup: true,
          sumInTotal: true,
          isAutoCalculated: true,
          isReadonly: true,
          isRequired: false,
          isVisible: true,
          order: 7,
        },
      ],
      genericFields: [
        {
          id: FIELD.NOTES,
          fieldName: FIELD.NOTES,
          fieldType: 302, // String
          label: "Uwagi",
          isSortable: false,
          isFilterable: false,
          isRequired: false,
          isVisible: true,
          order: 8,
        },
        {
          id: FIELD.CATEGORY,
          fieldName: FIELD.CATEGORY,
          fieldType: 302,
          label: "Kategoria robót",
          isSortable: true,
          isFilterable: true,
          isRequired: false,
          isVisible: true,
          order: 9,
          allowedValues: ["Roboty ziemne", "Roboty betoniarskie", "Roboty murowe", "Roboty wykończeniowe", "Stolarka"],
        },
      ],
      summaryConfiguration: {
        showGroupSummary: true,
        showTotalSummary: true,
        groupSummaryFields: [
          { fieldId: FIELD.NET_VALUE, fieldName: FIELD.NET_VALUE, fieldType: 203, fieldLabel: "Wartość netto", fieldSource: 2, order: 1 },
          { fieldId: FIELD.GROSS_VALUE, fieldName: FIELD.GROSS_VALUE, fieldType: 204, fieldLabel: "Wartość brutto", fieldSource: 2, order: 2 },
        ],
        totalSummaryFields: [
          { fieldId: FIELD.NET_VALUE, fieldName: FIELD.NET_VALUE, fieldType: 203, fieldLabel: "Razem netto", fieldSource: 2, order: 1 },
          { fieldId: FIELD.GROSS_VALUE, fieldName: FIELD.GROSS_VALUE, fieldType: 204, fieldLabel: "Razem brutto", fieldSource: 2, order: 2 },
        ],
      },
      uiConfiguration: {
        columns: [
          { fieldId: FIELD.NAME, fieldName: FIELD.NAME, fieldType: 100, fieldLabel: "Nazwa", fieldScope: 1, order: 1 },
          { fieldId: FIELD.QUANTITY, fieldName: FIELD.QUANTITY, fieldType: 101, fieldLabel: "Ilość", fieldScope: 1, order: 2 },
          { fieldId: FIELD.UNIT, fieldName: FIELD.UNIT, fieldType: 102, fieldLabel: "Jm.", fieldScope: 1, order: 3 },
          { fieldId: FIELD.UNIT_PRICE, fieldName: FIELD.UNIT_PRICE, fieldType: 200, fieldLabel: "Cena jdn.", fieldScope: 2, order: 4 },
          { fieldId: FIELD.NET_VALUE, fieldName: FIELD.NET_VALUE, fieldType: 203, fieldLabel: "Wart. netto", fieldScope: 2, order: 5 },
          { fieldId: FIELD.VAT_RATE, fieldName: FIELD.VAT_RATE, fieldType: 201, fieldLabel: "VAT%", fieldScope: 2, order: 6 },
          { fieldId: FIELD.GROSS_VALUE, fieldName: FIELD.GROSS_VALUE, fieldType: 204, fieldLabel: "Wart. brutto", fieldScope: 2, order: 7 },
          { fieldId: FIELD.NOTES, fieldName: FIELD.NOTES, fieldType: 302, fieldLabel: "Uwagi", fieldScope: 3, order: 8 },
        ],
      },
    },
  },
  [DEMO_TEMPLATE_IDS.drogowe]: {
    id: DEMO_TEMPLATE_IDS.drogowe,
    name: "Infrastruktura drogowa",
    description: "Szablon do robót drogowych zgodny z KNNR.",
    category: "Budownictwo drogowe",
    canAddGroups: true,
    canBranchGroups: true,
    maxGroupLevel: 2,
    autoNumberGroups: true,
    groupNumberFormat: "{level}.{index}",
    createdAt: "2023-03-05T09:00:00Z",
    ownerId: DEMO_USERS.piotr.userId,
    ownerName: "Piotr Wiśniewski",
    structure: {
      templateId: DEMO_TEMPLATE_IDS.drogowe,
      currencies: [
        { id: "cur-pln", code: "PLN", name: "Złoty polski", symbol: "zł", isDefault: true, order: 1 },
        { id: "cur-eur", code: "EUR", name: "Euro", symbol: "€", isDefault: false, order: 2 },
      ],
      units: [
        { id: "unit-m2", code: "m²", name: "metr kwadratowy", symbol: "m²", isDefault: false, order: 1 },
        { id: "unit-m3", code: "m³", name: "metr sześcienny", symbol: "m³", isDefault: false, order: 2 },
        { id: "unit-mb", code: "mb", name: "metr bieżący", symbol: "mb", isDefault: true, order: 3 },
        { id: "unit-t", code: "t", name: "tona", symbol: "t", isDefault: false, order: 4 },
        { id: "unit-szt", code: "szt.", name: "sztuka", symbol: "szt.", isDefault: false, order: 5 },
      ],
      categories: [],
      groupHeaderFields: [
        { id: FIELD.GROUP_NAME, fieldName: FIELD.GROUP_NAME, fieldType: 0, customLabel: "Nazwa rozdziału", isRequired: true, isVisible: true, order: 1, isReadonly: false },
      ],
      systemFields: [
        { id: FIELD.NAME, fieldName: FIELD.NAME, fieldType: 100, label: "Opis robót", isRequired: true, isVisible: true, order: 1 },
        { id: FIELD.QUANTITY, fieldName: FIELD.QUANTITY, fieldType: 101, label: "Ilość", isRequired: true, isVisible: true, order: 2 },
        { id: FIELD.UNIT, fieldName: FIELD.UNIT, fieldType: 102, label: "Jm.", isRequired: true, isVisible: true, order: 3 },
      ],
      calculatedFields: [
        { id: FIELD.UNIT_PRICE, fieldName: FIELD.UNIT_PRICE, fieldType: 200, label: "Cena jedn.", isSortable: true, isFilterable: false, isSummable: false, isAutoCalculated: false, isReadonly: false, isRequired: true, isVisible: true, order: 4 },
        { id: FIELD.NET_VALUE, fieldName: FIELD.NET_VALUE, fieldType: 203, label: "Wartość netto", isSortable: true, isFilterable: false, isSummable: true, sumInGroup: true, sumInTotal: true, isAutoCalculated: true, isReadonly: true, isRequired: false, isVisible: true, order: 5 },
        { id: FIELD.GROSS_VALUE, fieldName: FIELD.GROSS_VALUE, fieldType: 204, label: "Wartość brutto", isSortable: true, isFilterable: false, isSummable: true, sumInGroup: true, sumInTotal: true, isAutoCalculated: true, isReadonly: true, isRequired: false, isVisible: true, order: 6 },
      ],
      genericFields: [],
    },
  },
  [DEMO_TEMPLATE_IDS.instalacje]: {
    id: DEMO_TEMPLATE_IDS.instalacje,
    name: "Instalacje wewnętrzne",
    description: "Elektryka, sanitarna, HVAC.",
    category: "Instalacje",
    canAddGroups: true,
    canBranchGroups: false,
    autoNumberGroups: true,
    groupNumberFormat: "{index}",
    createdAt: "2023-04-10T08:00:00Z",
    ownerId: DEMO_USERS.anna.userId,
    ownerName: "Anna Kowalska",
    structure: {
      templateId: DEMO_TEMPLATE_IDS.instalacje,
      currencies: [
        { id: "cur-pln", code: "PLN", name: "Złoty polski", symbol: "zł", isDefault: true, order: 1 },
      ],
      units: [
        { id: "unit-szt", code: "szt.", name: "sztuka", symbol: "szt.", isDefault: true, order: 1 },
        { id: "unit-mb", code: "mb", name: "metr bieżący", symbol: "mb", isDefault: false, order: 2 },
        { id: "unit-kpl", code: "kpl.", name: "komplet", symbol: "kpl.", isDefault: false, order: 3 },
      ],
      categories: [],
      groupHeaderFields: [
        { id: FIELD.GROUP_NAME, fieldName: FIELD.GROUP_NAME, fieldType: 0, customLabel: "Branża", isRequired: true, isVisible: true, order: 1, isReadonly: false },
      ],
      systemFields: [
        { id: FIELD.NAME, fieldName: FIELD.NAME, fieldType: 100, label: "Opis", isRequired: true, isVisible: true, order: 1 },
        { id: FIELD.QUANTITY, fieldName: FIELD.QUANTITY, fieldType: 101, label: "Ilość", isRequired: true, isVisible: true, order: 2 },
        { id: FIELD.UNIT, fieldName: FIELD.UNIT, fieldType: 102, label: "Jm.", isRequired: true, isVisible: true, order: 3 },
      ],
      calculatedFields: [
        { id: FIELD.UNIT_PRICE, fieldName: FIELD.UNIT_PRICE, fieldType: 200, label: "Cena jedn.", isSortable: true, isFilterable: false, isSummable: false, isAutoCalculated: false, isReadonly: false, isRequired: true, isVisible: true, order: 4 },
        { id: FIELD.NET_VALUE, fieldName: FIELD.NET_VALUE, fieldType: 203, label: "Wartość netto", isSortable: true, isFilterable: false, isSummable: true, sumInGroup: true, sumInTotal: true, isAutoCalculated: true, isReadonly: true, isRequired: false, isVisible: true, order: 5 },
        { id: FIELD.GROSS_VALUE, fieldName: FIELD.GROSS_VALUE, fieldType: 204, label: "Wartość brutto", isSortable: true, isFilterable: false, isSummable: true, sumInGroup: true, sumInTotal: true, isAutoCalculated: true, isReadonly: true, isRequired: false, isVisible: true, order: 6 },
      ],
      genericFields: [],
    },
  },
};

// ===== KOSZTORYSY – LISTA =====

const makeListItem = (
  id: string,
  projectId: string,
  templateId: string,
  templateName: string,
  name: string,
  description: string,
  status: CostEstimateStatus,
  ownerId: string,
  ownerName: string,
  createdAt: string,
  totalNet: number,
  totalGross: number,
  overrides?: Partial<CostEstimateListItemWeb>
): CostEstimateListItemWeb => ({
  id,
  tenantId: DEMO_TENANT_ID,
  projectId,
  templateId,
  templateName,
  name,
  description,
  status,
  totalNet,
  totalGross,
  totalVat: totalGross - totalNet,
  createdAt,
  updatedAt: createdAt,
  ownerId,
  ownerName,
  isSharedWithMe: false,
  isSharedByMe: false,
  sharedWithUsers: [],
  ...overrides,
});

export const DEMO_COST_ESTIMATE_LIST: Record<string, CostEstimateListItemWeb[]> = {
  [DEMO_PROJECT_IDS.biurowe]: [
    // ── Moje (Anna właściciel) ──
    makeListItem("ce-biu-001", DEMO_PROJECT_IDS.biurowe, DEMO_TEMPLATE_IDS.budowlane, "Roboty budowlane – Standard", "Kosztorys budowlany etap I", "Roboty ziemne, fundamenty i konstrukcja", CostEstimateStatus.Approved, DEMO_USERS.anna.userId, "Anna Kowalska", "2023-03-01T10:00:00Z", 2_450_000, 3_013_500, {
      isSharedByMe: true,
      sharedWithUsers: [
        { userId: DEMO_USERS.marta.userId, fullName: "Marta Nowak", email: "m.nowak@archplan.pl", sharedAt: "2023-05-10T09:00:00Z" },
        { userId: DEMO_USERS.piotr.userId, fullName: "Piotr Wiśniewski", email: "p.wisniewski@archplan.pl", sharedAt: "2023-05-10T09:00:00Z" },
      ],
    }),
    makeListItem("ce-biu-004", DEMO_PROJECT_IDS.biurowe, DEMO_TEMPLATE_IDS.instalacje, "Instalacje wewnętrzne", "Kosztorys zagospodarowania terenu", "Utwardzenie, zieleń, mur oporowy, parking zewnętrzny", CostEstimateStatus.Draft, DEMO_USERS.anna.userId, "Anna Kowalska", "2023-10-05T08:00:00Z", 345_000, 424_350),
    // ── Udostępnione Annie przez innych ──
    makeListItem("ce-biu-002", DEMO_PROJECT_IDS.biurowe, DEMO_TEMPLATE_IDS.budowlane, "Roboty budowlane – Standard", "Kosztorys wykończeniowy", "Elewacje, tynki, posadzki, stolarka", CostEstimateStatus.InProgress, DEMO_USERS.marta.userId, "Marta Nowak", "2023-06-20T09:00:00Z", 1_180_000, 1_451_400, {
      isSharedWithMe: true,
      sharedWithUsers: [
        { userId: DEMO_USERS.anna.userId, fullName: "Anna Kowalska", email: "anna.kowalska@archplan.pl", sharedAt: "2023-07-01T10:00:00Z" },
      ],
    }),
    makeListItem("ce-biu-003", DEMO_PROJECT_IDS.biurowe, DEMO_TEMPLATE_IDS.instalacje, "Instalacje wewnętrzne", "Instalacje elektryczne i HVAC", "Instalacje niskoprądowe, BMS, wentylacja", CostEstimateStatus.ReadyForReview, DEMO_USERS.piotr.userId, "Piotr Wiśniewski", "2023-07-10T11:00:00Z", 870_000, 1_070_100, {
      isSharedWithMe: true,
      sharedWithUsers: [
        { userId: DEMO_USERS.anna.userId, fullName: "Anna Kowalska", email: "anna.kowalska@archplan.pl", sharedAt: "2023-08-05T08:00:00Z" },
        { userId: DEMO_USERS.katarzyna.userId, fullName: "Katarzyna Wójcik", email: "k.wojcik@archplan.pl", sharedAt: "2023-08-05T08:00:00Z" },
      ],
    }),
  ],
  [DEMO_PROJECT_IDS.drogowa]: [
    makeListItem("ce-drg-001", DEMO_PROJECT_IDS.drogowa, DEMO_TEMPLATE_IDS.drogowe, "Infrastruktura drogowa", "Kosztorys robót drogowych km 0–12,5", "Podbudowy, nawierzchnia, odwodnienie", CostEstimateStatus.Approved, DEMO_USERS.piotr.userId, "Piotr Wiśniewski", "2023-06-01T09:00:00Z", 8_920_000, 10_971_600),
    makeListItem("ce-drg-002", DEMO_PROJECT_IDS.drogowa, DEMO_TEMPLATE_IDS.budowlane, "Roboty budowlane – Standard", "Obiekty towarzyszące – przepusty i mosty", "Przepusty skrzynkowe, mosty drogowe", CostEstimateStatus.Draft, DEMO_USERS.anna.userId, "Anna Kowalska", "2023-09-15T10:00:00Z", 1_560_000, 1_918_800),
  ],
  [DEMO_PROJECT_IDS.hotel]: [
    makeListItem("ce-htl-001", DEMO_PROJECT_IDS.hotel, DEMO_TEMPLATE_IDS.budowlane, "Roboty budowlane – Standard", "Kosztorys rozbudowy Skrzydła B", "Konstrukcja, elewacja i wykończenie skrzydła B", CostEstimateStatus.InProgress, DEMO_USERS.piotr.userId, "Piotr Wiśniewski", "2023-09-01T10:00:00Z", 4_320_000, 5_313_600),
  ],
};

// ===== KOSZTORYS – SZCZEGÓŁY =====

const structureBudowlane = DEMO_TEMPLATE_DETAILS[DEMO_TEMPLATE_IDS.budowlane].structure!;

const makeFieldValue = (
  id: string,
  fieldDefinitionId: string,
  fieldType: number,
  fieldScope: number,
  stringValue?: string,
  decimalValue?: number
) => ({
  id,
  fieldDefinitionId,
  fieldType,
  fieldScope,
  stringValue,
  decimalValue,
});

export const DEMO_COST_ESTIMATE_DETAILS: Record<string, CostEstimateDetailsWeb> = {
  "ce-biu-001": {
    id: "ce-biu-001",
    tenantId: DEMO_TENANT_ID,
    projectId: DEMO_PROJECT_IDS.biurowe,
    templateId: DEMO_TEMPLATE_IDS.budowlane,
    templateName: "Roboty budowlane – Standard",
    selectedCurrencyId: "cur-pln",
    selectedCurrencyCode: "PLN",
    selectedCurrencySymbol: "zł",
    name: "Kosztorys budowlany etap I",
    description: "Roboty ziemne, fundamenty i konstrukcja",
    status: CostEstimateStatus.Approved,
    totalNet: 2_450_000,
    totalGross: 3_013_500,
    totalVat: 563_500,
    createdAt: "2023-03-01T10:00:00Z",
    updatedAt: "2023-05-15T14:00:00Z",
    ownerId: DEMO_USERS.anna.userId,
    ownerName: "Anna Kowalska",
    accessLevel: CostEstimateAccessLevel.Full,
    sharedWithUsers: [],
    templateStructure: structureBudowlane,
    rootGroups: [
      {
        id: "grp-biu-001",
        level: 1,
        order: 1,
        fieldValues: [
          makeFieldValue("fv-g1-001", FIELD.GROUP_NAME, 0, 0, "Roboty przygotowawcze i ziemne"),
          makeFieldValue("fv-g1-002", FIELD.GROUP_CODE, 0, 0, "01"),
        ],
        totalNet: 320_000,
        totalGross: 393_600,
        totalVat: 73_600,
        childGroups: [],
        items: [
          {
            id: "item-biu-001",
            groupId: "grp-biu-001",
            relationType: 0,
            order: 1,
            netValue: 85_000,
            grossValue: 104_550,
            vatValue: 19_550,
            fieldValues: [
              makeFieldValue("fv-i1-001", FIELD.NAME, 100, 1, "Zdjęcie warstwy humusu gr. 30 cm"),
              makeFieldValue("fv-i1-002", FIELD.QUANTITY, 101, 1, undefined, 4200),
              makeFieldValue("fv-i1-003", FIELD.UNIT, 102, 1, "m²"),
              makeFieldValue("fv-i1-004", FIELD.UNIT_PRICE, 200, 2, undefined, 20.24),
              makeFieldValue("fv-i1-005", FIELD.NET_VALUE, 203, 2, undefined, 85_000),
              makeFieldValue("fv-i1-006", FIELD.VAT_RATE, 201, 2, undefined, 23),
              makeFieldValue("fv-i1-007", FIELD.GROSS_VALUE, 204, 2, undefined, 104_550),
            ],
            createdAt: "2023-03-01T10:00:00Z",
          },
          {
            id: "item-biu-002",
            groupId: "grp-biu-001",
            relationType: 0,
            order: 2,
            netValue: 235_000,
            grossValue: 289_050,
            vatValue: 54_050,
            fieldValues: [
              makeFieldValue("fv-i2-001", FIELD.NAME, 100, 1, "Wykop mechaniczny – grunt kat. II"),
              makeFieldValue("fv-i2-002", FIELD.QUANTITY, 101, 1, undefined, 6800),
              makeFieldValue("fv-i2-003", FIELD.UNIT, 102, 1, "m³"),
              makeFieldValue("fv-i2-004", FIELD.UNIT_PRICE, 200, 2, undefined, 34.56),
              makeFieldValue("fv-i2-005", FIELD.NET_VALUE, 203, 2, undefined, 235_000),
              makeFieldValue("fv-i2-006", FIELD.VAT_RATE, 201, 2, undefined, 23),
              makeFieldValue("fv-i2-007", FIELD.GROSS_VALUE, 204, 2, undefined, 289_050),
            ],
            createdAt: "2023-03-01T10:00:00Z",
          },
        ],
        createdAt: "2023-03-01T10:00:00Z",
      },
      {
        id: "grp-biu-002",
        level: 1,
        order: 2,
        fieldValues: [
          makeFieldValue("fv-g2-001", FIELD.GROUP_NAME, 0, 0, "Fundamenty"),
          makeFieldValue("fv-g2-002", FIELD.GROUP_CODE, 0, 0, "02"),
        ],
        totalNet: 780_000,
        totalGross: 959_400,
        totalVat: 179_400,
        childGroups: [],
        items: [
          {
            id: "item-biu-003",
            groupId: "grp-biu-002",
            relationType: 0,
            order: 1,
            netValue: 480_000,
            grossValue: 590_400,
            vatValue: 110_400,
            fieldValues: [
              makeFieldValue("fv-i3-001", FIELD.NAME, 100, 1, "Płyta fundamentowa grub. 40 cm C25/30 XC2"),
              makeFieldValue("fv-i3-002", FIELD.QUANTITY, 101, 1, undefined, 1200),
              makeFieldValue("fv-i3-003", FIELD.UNIT, 102, 1, "m³"),
              makeFieldValue("fv-i3-004", FIELD.UNIT_PRICE, 200, 2, undefined, 400),
              makeFieldValue("fv-i3-005", FIELD.NET_VALUE, 203, 2, undefined, 480_000),
              makeFieldValue("fv-i3-006", FIELD.VAT_RATE, 201, 2, undefined, 23),
              makeFieldValue("fv-i3-007", FIELD.GROSS_VALUE, 204, 2, undefined, 590_400),
            ],
            createdAt: "2023-03-05T10:00:00Z",
          },
          {
            id: "item-biu-004",
            groupId: "grp-biu-002",
            relationType: 0,
            order: 2,
            netValue: 300_000,
            grossValue: 369_000,
            vatValue: 69_000,
            fieldValues: [
              makeFieldValue("fv-i4-001", FIELD.NAME, 100, 1, "Zbrojenie płyty fundamentowej – stal AIIIN"),
              makeFieldValue("fv-i4-002", FIELD.QUANTITY, 101, 1, undefined, 120),
              makeFieldValue("fv-i4-003", FIELD.UNIT, 102, 1, "t"),
              makeFieldValue("fv-i4-004", FIELD.UNIT_PRICE, 200, 2, undefined, 2500),
              makeFieldValue("fv-i4-005", FIELD.NET_VALUE, 203, 2, undefined, 300_000),
              makeFieldValue("fv-i4-006", FIELD.VAT_RATE, 201, 2, undefined, 23),
              makeFieldValue("fv-i4-007", FIELD.GROSS_VALUE, 204, 2, undefined, 369_000),
            ],
            createdAt: "2023-03-05T10:00:00Z",
          },
        ],
        createdAt: "2023-03-05T10:00:00Z",
      },
      {
        id: "grp-biu-003",
        level: 1,
        order: 3,
        fieldValues: [
          makeFieldValue("fv-g3-001", FIELD.GROUP_NAME, 0, 0, "Szkielet żelbetowy – kondygnacje"),
          makeFieldValue("fv-g3-002", FIELD.GROUP_CODE, 0, 0, "03"),
        ],
        totalNet: 1_350_000,
        totalGross: 1_660_500,
        totalVat: 310_500,
        childGroups: [],
        items: [
          {
            id: "item-biu-005",
            groupId: "grp-biu-003",
            relationType: 0,
            order: 1,
            netValue: 650_000,
            grossValue: 799_500,
            vatValue: 149_500,
            fieldValues: [
              makeFieldValue("fv-i5-001", FIELD.NAME, 100, 1, "Słupy i ściany żelbetowe – beton C30/37"),
              makeFieldValue("fv-i5-002", FIELD.QUANTITY, 101, 1, undefined, 820),
              makeFieldValue("fv-i5-003", FIELD.UNIT, 102, 1, "m³"),
              makeFieldValue("fv-i5-004", FIELD.UNIT_PRICE, 200, 2, undefined, 792.68),
              makeFieldValue("fv-i5-005", FIELD.NET_VALUE, 203, 2, undefined, 650_000),
              makeFieldValue("fv-i5-006", FIELD.VAT_RATE, 201, 2, undefined, 23),
              makeFieldValue("fv-i5-007", FIELD.GROSS_VALUE, 204, 2, undefined, 799_500),
            ],
            createdAt: "2023-03-10T10:00:00Z",
          },
          {
            id: "item-biu-006",
            groupId: "grp-biu-003",
            relationType: 0,
            order: 2,
            netValue: 700_000,
            grossValue: 861_000,
            vatValue: 161_000,
            fieldValues: [
              makeFieldValue("fv-i6-001", FIELD.NAME, 100, 1, "Stropy gęstożebrowe Teriva 4.0 – wszystkie kondygnacje"),
              makeFieldValue("fv-i6-002", FIELD.QUANTITY, 101, 1, undefined, 8750),
              makeFieldValue("fv-i6-003", FIELD.UNIT, 102, 1, "m²"),
              makeFieldValue("fv-i6-004", FIELD.UNIT_PRICE, 200, 2, undefined, 80),
              makeFieldValue("fv-i6-005", FIELD.NET_VALUE, 203, 2, undefined, 700_000),
              makeFieldValue("fv-i6-006", FIELD.VAT_RATE, 201, 2, undefined, 23),
              makeFieldValue("fv-i6-007", FIELD.GROSS_VALUE, 204, 2, undefined, 861_000),
            ],
            createdAt: "2023-03-10T10:00:00Z",
          },
        ],
        createdAt: "2023-03-10T10:00:00Z",
      },
    ],
  },
};
