export const TechnicalDocumentationStatus = {
  Pending: 0,
  Processing: 1,
  Completed: 2,
  Failed: 3,
  CompletedWithWarnings: 4,
} as const;

export type TechnicalDocumentationStatus =
  (typeof TechnicalDocumentationStatus)[keyof typeof TechnicalDocumentationStatus];

export interface TechnicalDocumentationListItemWeb {
  id: string;
  projectId: string;
  name: string;
  description?: string;
  status: TechnicalDocumentationStatus;
  fileCount: number;
  createdAt: string;
  completedAt?: string;
  errorMessage?: string;
}

export interface TechnicalDocumentationFileWeb {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  sasUriPreview?: string;
  sasUriDownload?: string;
}

export interface ProjectInfoWeb {
  name: string;
  investor?: string;
  address?: string;
  location?: string;
  designer?: string;
  collaborator?: string;
  date?: string;
  phase?: string;
  buildingType?: string;
}

export interface RoomFloorItemWeb {
  number: string;
  name: string;
  areaM2: number;
  category?: string;
  notes?: string;
}

export interface RoomFloorGroupWeb {
  floor: string;
  floorOrder: number;
  totalAreaM2?: number;
  areaNotes?: string;
  items: RoomFloorItemWeb[];
}

export interface RoofWindowEntryWeb {
  type: string;
  widthCm?: number;
  heightCm?: number;
  count: number;
  location?: string;
  sourceDrawing?: string;
}

export interface RoofSummaryWeb {
  pitchDegrees?: number;
  areaM2?: number;
  coveringType?: string;
  sourceDrawing?: string;
  roofWindows?: RoofWindowEntryWeb[];
  ventilation?: { type?: string; count?: number; sourceDrawing?: string };
  drainage?: { downpipeDiameterMm?: number; minSlopePct?: number; notes?: string };
  layers?: Array<{ zone: string; sequence: string[] }>;
}

export interface TimberStructureRowWeb {
  count: number;
  lengthM: number;
  rowSumMb: number;
}

export interface TimberGroupSummaryWeb {
  name: string;
  section?: string;
  rows?: TimberStructureRowWeb[];
  groupSumMb?: number;
  groupVolumeM3?: number;
}

export interface TimberStructureSummaryWeb {
  woodClass?: string;
  sourceDrawing?: string;
  totalVolumeM3?: number;
  notes?: string;
  groups: TimberGroupSummaryWeb[];
}

export interface WallLayerSummaryWeb {
  material: string;
  thicknessCm?: number;
}

export interface WallsSummaryWeb {
  sourceDrawings?: string[];
  external?: {
    thicknessCm?: number;
    layers?: WallLayerSummaryWeb[];
    finishes?: Array<{ zone: string; material: string; color?: string; sourceDrawings?: string[] }>;
  };
  internal?: {
    loadBearing?: { thicknessCm?: number; material?: string };
    partition?: { thicknessCm?: number; material?: string };
  };
  columns?: Array<{ symbol: string; widthCm?: number; heightCm?: number; reinforcement?: string; sourceDrawing?: string }>;
}

export interface RebarBarSummaryWeb {
  pos: number;
  count: number;
  diameterMm: number;
  lengthM: number;
  totalLengthM: number;
  massKg: number;
}

export interface FloorsSummaryWeb {
  sourceDrawings?: string[];
  slabThicknessCm?: number;
  concreteClass?: string;
  reinforcement?: {
    bottom?: { sourceDrawing?: string; totalMassKg?: number; basicGrid?: string; bars?: RebarBarSummaryWeb[] };
    top?: { sourceDrawing?: string; totalMassKg?: number; notes?: string; bars?: RebarBarSummaryWeb[] };
  };
  zones?: Array<{ zone: string; sourceDrawing?: string; layers: WallLayerSummaryWeb[] }>;
}

export interface FoundationFootingSegmentWeb {
  id?: string;
  lengthM: number;
}

export interface FoundationsSummaryWeb {
  sourceDrawings?: string[];
  concreteClass?: string;
  steelSpecification?: string;
  coverageMm?: number;
  foundationLevelM?: number;
  foundationBottomLevelM?: number;
  footings?: Array<{
    symbol?: string;
    widthM?: number;
    heightM?: number;
    segments?: FoundationFootingSegmentWeb[];
    totalLengthM?: number;
  }>;
  totalFootingLengthM?: number;
  pads?: Array<{ symbol?: string; bM?: number; lM?: number; heightM?: number; count?: number; sourceDrawing?: string }>;
  foundationWall?: { material?: string; thicknessCm?: number; sourceDrawing?: string };
  connectionDetails?: Array<{ title: string; reinforcement?: string; sourceDrawing?: string }>;
}

export interface ThermalInsulationSummaryWeb {
  sourceDrawings?: string[];
  elements: Array<{ element: string; material: string; thicknessCm?: number; system?: string; notes?: string }>;
}

export interface JoineryDoorEntryWeb {
  type: string;
  count: number;
  location?: string;
  sourceDrawing?: string;
}

export interface JoineryWindowEntryWeb {
  type: string;
  location?: string;
  count: number;
  widthCm?: number;
  heightCm?: number;
  sourceDrawing?: string;
}

export interface JoinerySummaryWeb {
  sourceDrawings?: string[];
  notes?: string;
  exterior?: {
    doors: JoineryDoorEntryWeb[];
    windows: JoineryWindowEntryWeb[];
  };
  interior?: {
    doors: Array<{ type: string; floor?: string; countEstimated: number }>;
  };
}

export interface InstallationsSummaryWeb {
  ventilation?: { type?: string; notes?: string; sourceDrawings?: string[] };
  plumbing?: {
    floors?: string[];
    sewage?: { type?: string; sourceDrawing?: string };
    waterSupply?: { type?: string; notes?: string; sourceDrawing?: string };
  };
  electrical?: { type?: string; notes?: string; sourceDrawing?: string };
  heating?: { type?: string; roomNumber?: string; areaM2?: number; notes?: string };
}

export interface ProjectModelRoomWeb {
  name: string;
  symbol?: string;
  widthM?: number;
  lengthM?: number;
  heightM?: number;
  areaM2?: number;
}

export interface ProjectModelFloorWeb {
  level: string;
  order: number;
  totalAreaM2?: number;
  rooms: ProjectModelRoomWeb[];
}

export interface ProjectModelWallLayerWeb {
  material: string;
  thicknessCm?: number;
}

export interface ProjectModelWallGroupWeb {
  thicknessCm?: number;
  layers: ProjectModelWallLayerWeb[];
}

export interface ProjectModelWallsWeb {
  external?: ProjectModelWallGroupWeb;
  internalLoadBearing?: ProjectModelWallGroupWeb;
  partition?: ProjectModelWallGroupWeb;
}

export interface ProjectModelFootingWeb {
  symbol?: string;
  widthM?: number;
  heightM?: number;
  concreteClass?: string;
  reinforcement?: string;
}

export interface ProjectModelPadWeb {
  symbol?: string;
  bM?: number;
  lM?: number;
  heightM?: number;
  concreteClass?: string;
  reinforcement?: string;
}

export interface ProjectModelFoundationsWeb {
  concrete?: string;
  footings?: ProjectModelFootingWeb[];
  pads?: ProjectModelPadWeb[];
  foundationWall?: string;
}

export interface ProjectModelCeilingWeb {
  coverageDescription?: string;
  thicknessCm?: number;
  concrete?: string;
  steelBottomKg?: number;
  steelTopKg?: number;
  steelDiameterMm?: number;
}

export interface ProjectModelTimberGroupWeb {
  element: string;
  section?: string;
  count?: number;
  lengthM?: number;
  volumeM3?: number;
}

export interface ProjectModelRoofWeb {
  pitchDegrees?: number;
  areaM2?: number;
  woodClass?: string;
  timberGroups?: ProjectModelTimberGroupWeb[];
  totalTimberVolumeM3?: number;
  coveringType?: string;
}

export interface ProjectModelColumnWeb {
  symbol: string;
  bCm?: number;
  hCm?: number;
  heightM?: number;
  concreteClass?: string;
  longitudinalBars?: string;
  stirrups?: string;
}

export interface ProjectModelBeamWeb {
  symbol: string;
  spanM?: number;
  bwCm?: number;
  hCm?: number;
  concreteClass?: string;
  mainBars?: string;
}

export interface ProjectModelLintelWeb {
  symbol: string;
  spanM?: number;
  bwCm?: number;
  hCm?: number;
  concreteClass?: string;
  mainBars?: string;
  stirrups?: string;
}

export interface ProjectModelSiteWeb {
  plotAreaM2?: number;
  buildingFootprintM2?: number;
  buildingVolumeM3?: number;
}

export interface ProjectModelSlabWeb {
  coverageDescription?: string;
  thicknessCm?: number;
  concrete?: string;
  steelBottomKg?: number;
  steelTopKg?: number;
  steelDiameterMm?: number;
  areaM2?: number;
}

export interface ProjectModelElevationFinishWeb {
  zone: string;
  material: string;
  color?: string;
}

export interface ProjectModelElevationOpeningWeb {
  type: string;
  count: number;
  widthCm?: number;
  heightCm?: number;
  location?: string;
}

export interface ProjectModelElevationWeb {
  orientation: string;
  sourceDrawing?: string;
  finishes: ProjectModelElevationFinishWeb[];
  openings: ProjectModelElevationOpeningWeb[];
}

export interface ProjectModelWarningWeb {
  code?: string;
  message: string;
  severity?: string;
  sourceGroup?: string;
}

export interface ProjectModelExtractionMetadataWeb {
  pipelineVersion?: string;
  thematicGroups?: string[];
  tokenUsage?: number;
  processedAt?: string;
}

export interface ProjectModelConflictWeb {
  fieldPath: string;
  valueA?: string;
  valueB?: string;
  conflict: boolean;
}

export interface ProjectModelWeb {
  project?: {
    name?: string;
    address?: string;
    location?: string;
    investor?: string;
    author?: string;
    collaborator?: string;
    date?: string;
    phase?: string;
  };
  site?: ProjectModelSiteWeb;
  floors?: ProjectModelFloorWeb[];
  walls?: ProjectModelWallsWeb;
  foundations?: ProjectModelFoundationsWeb;
  slab?: ProjectModelSlabWeb;
  ceilings?: ProjectModelCeilingWeb[];
  roof?: ProjectModelRoofWeb;
  elevations?: ProjectModelElevationWeb[];
  columns?: ProjectModelColumnWeb[];
  beams?: ProjectModelBeamWeb[];
  lintels?: ProjectModelLintelWeb[];
  warnings?: ProjectModelWarningWeb[];
  extractionMetadata?: ProjectModelExtractionMetadataWeb;
  conflicts?: ProjectModelConflictWeb[];
  missingData?: string[];
}

export interface MaterialScheduleItemWeb {
  element: string;
  calculation?: string;
  sourceDrawings?: string[];
  netQuantity: number;
  wastePercent: number;
  grossQuantity: number;
  unit: string;
  sourceType?: string;
  specification?: string;
  missingData?: string;
}

export interface MaterialGroupWeb {
  concrete?: MaterialScheduleItemWeb[];
  steel?: MaterialScheduleItemWeb[];
  blocks?: MaterialScheduleItemWeb[];
  masonry?: MaterialScheduleItemWeb[];
  mortar?: MaterialScheduleItemWeb[];
  insulation?: MaterialScheduleItemWeb[];
  covering?: MaterialScheduleItemWeb[];
  timber?: MaterialScheduleItemWeb[];
}

export interface MaterialSummaryItemWeb {
  category: string;
  materialType: string;
  grossQuantity: number;
  unit: string;
}

export interface OpeningScheduleItemWeb {
  symbol?: string;
  widthCm?: number;
  heightCm?: number;
  widthM?: number;
  heightM?: number;
  count?: number;
  type?: string;
  material?: string;
}

export interface DetailsMaterialScheduleItemWeb {
  element: string;
  netM3?: number;
  grossM3?: number;
  netM2?: number;
  grossM2?: number;
  netKg?: number;
  grossKg?: number;
  unit?: string;
  wastePercent?: number;
  sourceType?: string;
  sourceDrawing?: string;
}

export interface MaterialScheduleWeb {
  calculatedAt?: string;
  groups?: {
    foundations?: {
      concrete?: DetailsMaterialScheduleItemWeb[];
      steel?: DetailsMaterialScheduleItemWeb[];
      masonry?: DetailsMaterialScheduleItemWeb[];
      insulation?: DetailsMaterialScheduleItemWeb[];
    };
    slabs?: {
      concrete?: DetailsMaterialScheduleItemWeb[];
      steel?: DetailsMaterialScheduleItemWeb[];
    };
    roof?: {
      timber?: DetailsMaterialScheduleItemWeb[];
      covering?: DetailsMaterialScheduleItemWeb[];
    };
    site?: {
      plotAreaM2?: number;
      buildingFootprintM2?: number;
      pavedAreaM2?: number;
      greenAreaM2?: number;
      buildingCoverageRatio?: number;
      cubatureM3?: number;
      sourceDrawing?: string;
    };
  };
  totals?: {
    concreteM3?: number;
    steelKg?: number;
    timberM3?: number;
    insulationM2?: number;
  };
}

export interface DrawingSourceWeb {
  fileName: string;
  pageNumber: number;
}

export interface DrawingClassificationWeb {
  drawingType: string;
  scale?: number;
  sheetNumber?: string;
  title?: string;
  author?: string;
  date?: string;
  investor?: string;
  location?: string;
  buildingType?: string;
  revision?: string;
  floorLevel?: string;
  floorOrder?: number;
  hasMaterialTable?: boolean;
  tableTitle?: string;
}

export interface ValidatedDrawingWeb {
  sheetNumber?: string;
  drawingType: string;
  title?: string;
  scale?: number;
  validated: boolean;
  hasMaterialTable?: boolean;
}

export interface DrawingDependencyLinkWeb {
  from: string;
  to: string;
  relation: string;
}

export interface TechnicalDocumentationCorrectionWeb {
  fieldPath: string;
  correctedBy?: string;
  correctedAt?: string;
  reason?: string;
}

export interface DetailsValidationDifferenceWeb {
  path: string;
  issue: string;
  expected?: string;
  actual?: string;
  severity: string;
  sourceDrawings: string[];
}

export interface DetailsValidationRemediationStepWeb {
  order: number;
  action: string;
  reason: string;
  pipelineStage?: string;
  sourceDrawings: string[];
}

export interface DetailsValidationImageCheckWeb {
  sheetNumber: string;
  drawingType: string;
  findings: string[];
  confirmedDifferences: string[];
  recommendedActions: string[];
}

export interface DetailsValidationResultWeb {
  differences: DetailsValidationDifferenceWeb[];
  rootCauses: string[];
  remediationSteps: DetailsValidationRemediationStepWeb[];
  imageChecks: DetailsValidationImageCheckWeb[];
}

export interface ProjectTechnicalDocumentationDetailsWeb {
  projectModel?: ProjectModelWeb;
  materialSchedule?: MaterialScheduleWeb;
  auditResult?: AuditResultWeb;
  tokenUsage?: number;
  processedAt?: string;
  /** @deprecated legacy MVP — prefer projectModel.project */
  project?: ProjectInfoWeb;
  /** @deprecated legacy MVP */
  totalAreaM2?: number;
  /** @deprecated legacy MVP — prefer projectModel.floors */
  rooms?: RoomFloorGroupWeb[];
  roof?: RoofSummaryWeb;
  timberStructure?: TimberStructureSummaryWeb;
  walls?: WallsSummaryWeb;
  floors?: FloorsSummaryWeb;
  foundations?: FoundationsSummaryWeb;
  thermalInsulation?: ThermalInsulationSummaryWeb;
  joinery?: JoinerySummaryWeb;
  /** @deprecated legacy MVP */
  installations?: InstallationsSummaryWeb;
  validatedDrawings?: ValidatedDrawingWeb[];
  drawingDependencies?: DrawingDependencyLinkWeb[];
  validationSummaries?: DrawingValidationSummaryWeb[];
  validationReview?: DetailsValidationResultWeb;
  corrections?: TechnicalDocumentationCorrectionWeb[];
}

export interface DrawingValidationSummaryWeb {
  fileName: string;
  pageNumber: number;
  sheetNumber?: string;
  drawingType: string;
  crossValidationUsed: boolean;
  confidenceScore: string;
  disagreements: string[];
}

export interface AuditResultWeb {
  warnings: string[];
  missingMaterials: string[];
  assumptions?: string[];
  crossReferenceErrors?: string[];
  unitErrors?: Array<{
    field?: string;
    found?: string;
    expected?: string;
  }>;
  /** @deprecated use missingMaterials */
  missingData?: string[];
}

export interface TechnicalDocumentationDetailsWeb {
  id: string;
  projectId: string;
  name: string;
  description?: string;
  status: TechnicalDocumentationStatus;
  fileCount: number;
  createdAt: string;
  completedAt?: string;
  errorMessage?: string;
  details?: ProjectTechnicalDocumentationDetailsWeb;
  files: TechnicalDocumentationFileWeb[];
}

export interface TechnicalDocumentationProcessingEvent {
  documentationId: string;
  projectId: string;
  tenantId: string;
  name: string;
  status: TechnicalDocumentationStatus;
  errorMessage?: string;
}

export interface CreateTechnicalDocumentationRequest {
  name: string;
  description?: string;
  files: File[];
}
