import {
  Accordion,
  AccordionButton,
  AccordionIcon,
  AccordionItem,
  AccordionPanel,
  Badge,
  Box,
  Heading,
  SimpleGrid,
  Stat,
  StatLabel,
  StatNumber,
  Table,
  Tbody,
  Td,
  Text,
  Th,
  Thead,
  Tr,
  VStack,
} from '@chakra-ui/react';
import type {
  DrawingValidationSummaryWeb,
  JoineryDoorEntryWeb,
  JoineryWindowEntryWeb,
  ProjectInfoWeb,
  ProjectTechnicalDocumentationDetailsWeb,
  RoomFloorGroupWeb,
} from '../../types/technicalDocumentation.types';
import { formatDate } from '../../utils/formatters';
import { TechnicalDocumentationAuditResultSection } from './TechnicalDocumentationAuditResultSection';
import { TechnicalDocumentationDrawingDependenciesSection } from './TechnicalDocumentationDrawingDependenciesSection';
import { TechnicalDocumentationMaterialScheduleSection } from './TechnicalDocumentationMaterialScheduleSection';
import { TechnicalDocumentationProjectModelSection } from './TechnicalDocumentationProjectModelSection';
import { TechnicalDocumentationValidatedDrawingsSection } from './TechnicalDocumentationValidatedDrawingsSection';
import { isLegacyTechnicalDocumentationDetails } from './technicalDocumentationDetailsGuards';
import { formatAreaLabel, formatDecimal, hasNumericValue } from './technicalDocumentationFormatters';

export interface TechnicalDocumentationDetailsViewProps {
  details: ProjectTechnicalDocumentationDetailsWeb;
}

function formatArea(value: number | null | undefined): string {
  return formatAreaLabel(value);
}

function InfoRow({ label, value }: { label: string; value?: string | number | null }): React.ReactElement | null {
  if (value === undefined || value === null || value === '') {
    return null;
  }

  return (
    <Box>
      <Text fontSize="xs" color="neutral.500" textTransform="uppercase" letterSpacing="wide">
        {label}
      </Text>
      <Text fontSize="sm" color="neutral.800">
        {value}
      </Text>
    </Box>
  );
}

function ProjectInfoSection({
  project,
  processedAt,
  tokenUsage,
}: {
  project?: ProjectInfoWeb;
  processedAt?: string;
  tokenUsage?: number;
}): React.ReactElement | null {
  if (!project) {
    return null;
  }

  return (
    <Box>
      <Heading size="sm" mb={3} color="neutral.700">
        Informacje o budynku
      </Heading>
      <SimpleGrid columns={{ base: 1, md: 2, lg: 4 }} spacing={3}>
        <InfoRow label="Nazwa" value={project.name} />
        <InfoRow label="Inwestor" value={project.investor} />
        <InfoRow label="Lokalizacja" value={project.location ?? project.address} />
        <InfoRow label="Projektant" value={project.designer ?? project.collaborator} />
        <InfoRow label="Typ budynku" value={project.buildingType} />
        <InfoRow label="Faza" value={project.phase} />
        <InfoRow label="Data" value={project.date} />
      </SimpleGrid>
      {processedAt && (
        <Text fontSize="xs" color="neutral.500" mt={2}>
          Przetworzono: {formatDate(processedAt)}
        </Text>
      )}
      {tokenUsage !== undefined && tokenUsage > 0 && (
        <Box mt={2}>
          <InfoRow label="Zużycie tokenów LLM" value={tokenUsage.toLocaleString('pl-PL')} />
        </Box>
      )}
    </Box>
  );
}

function JoineryWindowsTable({ items }: { items: JoineryWindowEntryWeb[] }): React.ReactElement | null {
  if (items.length === 0) {
    return null;
  }

  return (
    <Table size="sm" variant="simple" mb={4}>
      <Thead>
        <Tr>
          <Th>Typ</Th>
          <Th isNumeric>Szer. (cm)</Th>
          <Th isNumeric>Wys. (cm)</Th>
          <Th isNumeric>Ilość</Th>
        </Tr>
      </Thead>
      <Tbody>
        {items.map((item, index) => (
          <Tr key={`window-${index}`}>
            <Td>{item.type}</Td>
            <Td isNumeric>{item.widthCm ?? '—'}</Td>
            <Td isNumeric>{item.heightCm ?? '—'}</Td>
            <Td isNumeric>{item.count}</Td>
          </Tr>
        ))}
      </Tbody>
    </Table>
  );
}

function JoineryDoorsTable({ items }: { items: JoineryDoorEntryWeb[] }): React.ReactElement | null {
  if (items.length === 0) {
    return null;
  }

  return (
    <Table size="sm" variant="simple" mb={4}>
      <Thead>
        <Tr>
          <Th>Typ</Th>
          <Th>Lokalizacja</Th>
          <Th isNumeric>Ilość</Th>
        </Tr>
      </Thead>
      <Tbody>
        {items.map((item, index) => (
          <Tr key={`door-${index}`}>
            <Td>{item.type}</Td>
            <Td>{item.location ?? '—'}</Td>
            <Td isNumeric>{item.count}</Td>
          </Tr>
        ))}
      </Tbody>
    </Table>
  );
}

function RoomsByFloorTable({ floors }: { floors: RoomFloorGroupWeb[] }): React.ReactElement | null {
  if (floors.length === 0) {
    return null;
  }

  return (
    <VStack align="stretch" spacing={4}>
      {floors.map((floor) => (
        <Box key={floor.floor}>
          <Text fontSize="sm" fontWeight="semibold" mb={2} color="neutral.700">
            {floor.floor}
            {floor.totalAreaM2 ? ` — ${formatAreaLabel(floor.totalAreaM2)}` : ''}
          </Text>
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Nr</Th>
                <Th>Nazwa</Th>
                <Th isNumeric>Pow.</Th>
                <Th>Kategoria</Th>
              </Tr>
            </Thead>
            <Tbody>
              {floor.items.map((room, index) => (
                <Tr key={`${floor.floor}-${index}`}>
                  <Td>{room.number || '—'}</Td>
                  <Td>{room.name}</Td>
                  <Td isNumeric>{formatAreaLabel(room.areaM2)}</Td>
                  <Td fontSize="xs">{room.category ?? '—'}</Td>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </Box>
      ))}
    </VStack>
  );
}

function confidenceBadgeColor(score: string): string {
  if (score === 'low') {
    return 'red';
  }

  if (score === 'medium') {
    return 'orange';
  }

  return 'green';
}

function ValidationSummariesSection({
  summaries,
}: {
  summaries: DrawingValidationSummaryWeb[];
}): React.ReactElement | null {
  if (summaries.length === 0) {
    return null;
  }

  return (
    <Box>
      <Heading size="sm" mb={3} color="neutral.700">
        Walidacja ekstrakcji (CV)
      </Heading>
      <Table size="sm" variant="simple">
        <Thead>
          <Tr>
            <Th>Arkusz</Th>
            <Th>Typ</Th>
            <Th>CV</Th>
            <Th>Pewność</Th>
            <Th>Rozbieżności</Th>
          </Tr>
        </Thead>
        <Tbody>
          {summaries.map((summary, index) => (
            <Tr key={`validation-${index}`}>
              <Td>
                {summary.sheetNumber ?? summary.fileName}
                {summary.pageNumber > 0 ? ` (str. ${summary.pageNumber})` : ''}
              </Td>
              <Td>{summary.drawingType}</Td>
              <Td>{summary.crossValidationUsed ? 'Tak' : 'Nie'}</Td>
              <Td>
                <Badge colorScheme={confidenceBadgeColor(summary.confidenceScore)}>
                  {summary.confidenceScore}
                </Badge>
              </Td>
              <Td fontSize="xs" color="neutral.600">
                {summary.disagreements.length > 0 ? summary.disagreements.join(', ') : '—'}
              </Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
    </Box>
  );
}

function LegacyProjectDetailsSection({
  details,
}: {
  details: ProjectTechnicalDocumentationDetailsWeb;
}): React.ReactElement | null {
  const {
    totalAreaM2,
    rooms = [],
    roof,
    timberStructure,
    walls,
    foundations,
    thermalInsulation,
    joinery,
    installations,
  } = details;

  const installationEntries: { label: string; value?: string }[] = [
    { label: 'Wentylacja', value: installations?.ventilation?.type },
    {
      label: 'Wod-kan',
      value: installations?.plumbing?.waterSupply?.type ?? installations?.plumbing?.sewage?.type,
    },
    { label: 'Elektryczna', value: installations?.electrical?.type },
    { label: 'Ogrzewanie', value: installations?.heating?.type },
  ];

  const hasInstallations = installationEntries.some((entry) => entry.value);

  const hasStructuredSummaries =
    hasNumericValue(totalAreaM2) && totalAreaM2 > 0
    || rooms.length > 0
    || roof
    || timberStructure
    || walls
    || foundations
    || thermalInsulation
    || joinery
    || hasInstallations;

  if (!hasStructuredSummaries) {
    return null;
  }

  return (
    <Box>
      <Heading size="sm" mb={3} color="neutral.700">
        Szczegóły projektu
      </Heading>

      {hasNumericValue(totalAreaM2) && totalAreaM2 > 0 && (
        <SimpleGrid columns={{ base: 1, md: 3 }} spacing={4} mb={4}>
          <Stat>
            <StatLabel>Powierzchnia całkowita</StatLabel>
            <StatNumber fontSize="lg">{formatArea(totalAreaM2)}</StatNumber>
          </Stat>
        </SimpleGrid>
      )}

      {rooms.length > 0 && (
        <Box mb={4}>
          <Text fontSize="sm" fontWeight="semibold" mb={2} color="neutral.700">
            Pomieszczenia
          </Text>
          <RoomsByFloorTable floors={rooms} />
        </Box>
      )}

      <Accordion allowMultiple>
        {roof && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Dach
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <SimpleGrid columns={{ base: 1, md: 3 }} spacing={3}>
                <InfoRow label="Powierzchnia" value={formatAreaLabel(roof.areaM2)} />
                <InfoRow label="Kąt nachylenia" value={hasNumericValue(roof.pitchDegrees) ? `${roof.pitchDegrees}°` : undefined} />
                <InfoRow label="Pokrycie" value={roof.coveringType} />
              </SimpleGrid>
            </AccordionPanel>
          </AccordionItem>
        )}

        {timberStructure && timberStructure.groups.length > 0 && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Więźba
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <InfoRow label="Klasa drewna" value={timberStructure.woodClass} />
              <InfoRow label="Objętość" value={timberStructure.totalVolumeM3 ? `${formatDecimal(timberStructure.totalVolumeM3, 2)} m³` : undefined} />
            </AccordionPanel>
          </AccordionItem>
        )}

        {walls?.external?.layers && walls.external.layers.length > 0 && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Ściany
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <Table size="sm" variant="simple">
                <Thead>
                  <Tr>
                    <Th>Materiał</Th>
                    <Th isNumeric>Grubość [cm]</Th>
                  </Tr>
                </Thead>
                <Tbody>
                  {walls.external.layers.map((layer, index) => (
                    <Tr key={`wall-layer-${index}`}>
                      <Td>{layer.material}</Td>
                      <Td isNumeric>{layer.thicknessCm ?? '—'}</Td>
                    </Tr>
                  ))}
                </Tbody>
              </Table>
            </AccordionPanel>
          </AccordionItem>
        )}

        {foundations && (foundations.footings?.length ?? 0) > 0 && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Fundamenty
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <InfoRow label="Beton" value={foundations.concreteClass} />
              <InfoRow label="Długość ław" value={foundations.totalFootingLengthM ? `${formatDecimal(foundations.totalFootingLengthM, 2)} m` : undefined} />
            </AccordionPanel>
          </AccordionItem>
        )}

        {thermalInsulation && thermalInsulation.elements.length > 0 && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Izolacja termiczna
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <Table size="sm" variant="simple">
                <Thead>
                  <Tr>
                    <Th>Element</Th>
                    <Th>Materiał</Th>
                    <Th isNumeric>Grubość [cm]</Th>
                  </Tr>
                </Thead>
                <Tbody>
                  {thermalInsulation.elements.map((item, index) => (
                    <Tr key={`insulation-${index}`}>
                      <Td>{item.element}</Td>
                      <Td>{item.material}</Td>
                      <Td isNumeric>{item.thicknessCm ?? '—'}</Td>
                    </Tr>
                  ))}
                </Tbody>
              </Table>
            </AccordionPanel>
          </AccordionItem>
        )}

        {joinery?.exterior && (joinery.exterior.windows.length > 0 || joinery.exterior.doors.length > 0) && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Stolarka
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <JoineryWindowsTable items={joinery.exterior.windows} />
              <JoineryDoorsTable items={joinery.exterior.doors} />
            </AccordionPanel>
          </AccordionItem>
        )}

        {hasInstallations && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Instalacje
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <SimpleGrid columns={{ base: 1, md: 2 }} spacing={3}>
                {installationEntries.map((entry) => (
                  <InfoRow key={entry.label} label={entry.label} value={entry.value} />
                ))}
              </SimpleGrid>
            </AccordionPanel>
          </AccordionItem>
        )}
      </Accordion>
    </Box>
  );
}

export function TechnicalDocumentationDetailsView({
  details,
}: TechnicalDocumentationDetailsViewProps): React.ReactElement {
  const isLegacy = isLegacyTechnicalDocumentationDetails(details);
  const {
    projectModel,
    project,
    validatedDrawings,
    drawingDependencies,
    materialSchedule,
    validationSummaries,
    auditResult,
    corrections,
    tokenUsage,
    processedAt,
  } = details;

  const projectInfo: ProjectInfoWeb | undefined = isLegacy
    ? project
    : projectModel?.project
      ? {
        name: projectModel.project.name ?? '',
        investor: projectModel.project.investor,
        location: projectModel.project.location ?? projectModel.project.address,
        designer: projectModel.project.author,
        collaborator: projectModel.project.collaborator,
        date: projectModel.project.date,
        phase: projectModel.project.phase,
      }
      : project;

  const effectiveTokenUsage = tokenUsage ?? projectModel?.extractionMetadata?.tokenUsage;
  const effectiveProcessedAt = processedAt ?? projectModel?.extractionMetadata?.processedAt;

  return (
    <VStack align="stretch" spacing={6}>
      <ProjectInfoSection
        project={projectInfo}
        processedAt={effectiveProcessedAt}
        tokenUsage={effectiveTokenUsage}
      />

      {projectModel && <TechnicalDocumentationProjectModelSection projectModel={projectModel} />}

      {materialSchedule && <TechnicalDocumentationMaterialScheduleSection materialSchedule={materialSchedule} />}

      {auditResult && <TechnicalDocumentationAuditResultSection auditResult={auditResult} />}

      {validatedDrawings && validatedDrawings.length > 0 && (
        <TechnicalDocumentationValidatedDrawingsSection validatedDrawings={validatedDrawings} />
      )}

      {drawingDependencies && drawingDependencies.length > 0 && (
        <TechnicalDocumentationDrawingDependenciesSection drawingDependencies={drawingDependencies} />
      )}

      {validationSummaries && validationSummaries.length > 0 && (
        <ValidationSummariesSection summaries={validationSummaries} />
      )}

      {corrections && corrections.length > 0 && (
        <Box>
          <Heading size="sm" mb={3} color="neutral.700">
            Korekty ({corrections.length})
          </Heading>
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Pole</Th>
                <Th>Autor</Th>
                <Th>Data</Th>
                <Th>Powód</Th>
              </Tr>
            </Thead>
            <Tbody>
              {corrections.map((correction, index) => (
                <Tr key={`correction-${index}`}>
                  <Td fontSize="xs">{correction.fieldPath}</Td>
                  <Td>{correction.correctedBy ?? '—'}</Td>
                  <Td fontSize="xs">
                    {correction.correctedAt ? formatDate(correction.correctedAt) : '—'}
                  </Td>
                  <Td fontSize="xs">{correction.reason ?? '—'}</Td>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </Box>
      )}

      {isLegacy && <LegacyProjectDetailsSection details={details} />}
    </VStack>
  );
}
