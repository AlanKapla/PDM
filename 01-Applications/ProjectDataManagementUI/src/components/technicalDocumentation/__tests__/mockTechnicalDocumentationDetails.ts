import type { ProjectTechnicalDocumentationDetailsWeb } from '../../../types/technicalDocumentation.types';

export const mockTechnicalDocumentationDetails: ProjectTechnicalDocumentationDetailsWeb = {
  project: {
    name: 'Dom jednorodzinny — etap projektu',
    investor: 'Jan Kowalski',
    location: 'ul. Przykładowa 12, Warszawa',
    designer: 'Biuro Architektoniczne XYZ',
    buildingType: 'Budynek mieszkalny jednorodzinny',
    date: 'Czerwiec 2026',
  },
  totalAreaM2: 142.5,
  rooms: [
    {
      floor: 'Parter',
      floorOrder: 0,
      totalAreaM2: 142.5,
      items: [
        { number: '1', name: 'Salon', areaM2: 28.4, category: 'mieszkalne' },
        { number: '2', name: 'Kuchnia', areaM2: 12.1, category: 'usługowe' },
      ],
    },
  ],
  roof: {
    areaM2: 165,
    pitchDegrees: 35,
    coveringType: 'dachówka ceramiczna',
    sourceDrawing: 'A-04',
  },
  timberStructure: {
    woodClass: 'C24',
    totalVolumeM3: 2.8,
    groups: [{ name: 'Krokwie', section: '8x16', groupVolumeM3: 2.8, rows: [] }],
  },
  walls: {
    external: {
      thicknessCm: 24,
      layers: [{ material: 'beton komórkowy', thicknessCm: 24 }],
    },
  },
  foundations: {
    concreteClass: 'C20/25 (B25)',
    footings: [{ symbol: 'Ł-1', widthM: 0.6, heightM: 0.4, totalLengthM: 51.22, segments: [] }],
  },
  thermalInsulation: {
    elements: [{ element: 'Ściany zewnętrzne', material: 'styropian EPS 100', thicknessCm: 10 }],
  },
  joinery: {
    exterior: {
      windows: [{ type: 'Okno', count: 6, widthCm: 120, heightCm: 140 }],
      doors: [{ type: 'Drzwi wejściowe', count: 1 }],
    },
  },
  installations: {
    heating: { type: 'Pompa ciepła', notes: 'powietrze-woda' },
    electrical: { type: 'Przyłącze elektroenergetyczne' },
  },
  validatedDrawings: [
    {
      sheetNumber: 'A-01',
      drawingType: 'rzut_parteru',
      title: 'Rzut parteru',
      scale: 50,
      validated: true,
    },
  ],
  drawingDependencies: [
    { from: 'A-02', to: 'K-01', relation: 'Układ ścian parteru → układ fundamentów' },
  ],
  materialSchedule: {
    calculatedAt: '2026-06-25T10:00:00Z',
    groups: {
      foundations: {
        concrete: [
          {
            element: 'Beton B25 — ławy fundamentowe',
            grossM3: 25.2,
            sourceType: 'calculated',
            sourceDrawing: 'K-01',
          },
        ],
      },
      slabs: {
        steel: [
          {
            element: 'Stal zbrojenia dolnego (K-02)',
            grossKg: 1287.3,
            sourceType: 'read',
            sourceDrawing: 'K-02',
          },
        ],
      },
      roof: {
        covering: [
          {
            element: 'dachówka',
            grossM2: 377.2,
            sourceType: 'read',
            sourceDrawing: 'A-04',
          },
        ],
      },
    },
    totals: {
      concreteM3: 25.2,
      steelKg: 1287.3,
      timberM3: 14.67,
      insulationM2: 122.1,
    },
  },
  auditResult: {
    warnings: ['Brak danych o fundamentach'],
    missingMaterials: ['footings'],
  },
  tokenUsage: 12500,
  processedAt: '2026-06-25T10:05:00Z',
};

export const mockGroupPipelineDetails: ProjectTechnicalDocumentationDetailsWeb = {
  projectModel: {
    project: {
      name: 'Dom jednorodzinny — pipeline grupowy',
      investor: 'Jan Kowalski',
      location: 'ul. Przykładowa 12, Warszawa',
      author: 'Biuro Architektoniczne XYZ',
      date: 'Czerwiec 2026',
      phase: 'P',
    },
    site: {
      plotAreaM2: 980,
      buildingFootprintM2: 142.5,
      buildingVolumeM3: 485,
    },
    floors: [
      {
        level: 'Parter',
        order: 0,
        totalAreaM2: 142.5,
        rooms: [
          { name: 'Salon', symbol: '1', areaM2: 28.4 },
          { name: 'Kuchnia', symbol: '2', areaM2: 12.1 },
        ],
      },
    ],
    walls: {
      external: {
        thicknessCm: 24,
        layers: [{ material: 'beton komórkowy', thicknessCm: 24 }],
      },
    },
    foundations: {
      concrete: 'C20/25 (B25)',
      footings: [{ symbol: 'Ł-1', widthM: 0.6, heightM: 0.4 }],
    },
    slab: {
      coverageDescription: 'Strop nad parterem',
      thicknessCm: 20,
      concrete: 'C25/30',
      steelBottomKg: 1170.3,
      steelTopKg: 245.8,
      areaM2: 142.5,
    },
    elevations: [
      {
        orientation: 'Północ',
        sourceDrawing: 'A-03',
        finishes: [{ zone: 'Elewacja frontowa', material: 'tynk mineralny', color: 'biały' }],
        openings: [{ type: 'okno', count: 3, widthCm: 120, heightCm: 140 }],
      },
      {
        orientation: 'Południe',
        sourceDrawing: 'A-04',
        finishes: [{ zone: 'Elewacja ogrodowa', material: 'drewno', color: 'naturalny' }],
        openings: [{ type: 'drzwi', count: 1, widthCm: 90, heightCm: 210 }],
      },
    ],
    roof: {
      pitchDegrees: 35,
      areaM2: 165,
      coveringType: 'dachówka ceramiczna',
      woodClass: 'C24',
      totalTimberVolumeM3: 2.8,
    },
    warnings: [
      {
        code: 'K-02_STEEL_DIFF',
        message: 'Różnica w masie stali dolnej — weryfikacja Agent C',
        severity: 'warning',
        sourceGroup: 'reinforcement',
      },
    ],
    extractionMetadata: {
      pipelineVersion: 'group-9-phase-v1',
      thematicGroups: ['reinforcement', 'foundations', 'architecture'],
      tokenUsage: 48200,
      processedAt: '2026-06-26T08:30:00Z',
    },
  },
  materialSchedule: {
    calculatedAt: '2026-06-26T08:30:00Z',
    groups: {
      foundations: {
        concrete: [
          {
            element: 'Beton B25 — ławy fundamentowe',
            grossM3: 25.2,
            sourceType: 'calculated',
            sourceDrawing: 'K-01',
          },
        ],
      },
      slabs: {
        steel: [
          {
            element: 'Stal zbrojenia dolnego (K-02)',
            grossKg: 1170.3,
            sourceType: 'read',
            sourceDrawing: 'K-02',
          },
        ],
      },
    },
    totals: {
      concreteM3: 25.2,
      steelKg: 1170.3,
    },
  },
  auditResult: {
    warnings: ['Stal górna — brak potwierdzenia na rysunku K-03'],
    missingMaterials: [],
    assumptions: ['Przyjęto grubość stropu 20 cm na podstawie K-02'],
    unitErrors: [
      { field: 'slab.steelBottomKg', found: '1170.3 kg', expected: 'kg' },
    ],
  },
  tokenUsage: 48200,
  processedAt: '2026-06-26T08:30:00Z',
};
