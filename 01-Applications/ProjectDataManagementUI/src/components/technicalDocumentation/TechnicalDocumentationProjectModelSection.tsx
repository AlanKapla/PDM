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
  ProjectModelCeilingWeb,
  ProjectModelColumnWeb,
  ProjectModelElevationWeb,
  ProjectModelFloorWeb,
  ProjectModelSlabWeb,
  ProjectModelTimberGroupWeb,
  ProjectModelWallGroupWeb,
  ProjectModelWarningWeb,
  ProjectModelWeb,
} from '../../types/technicalDocumentation.types';
import { TechnicalDocumentationExtractionMetadataSection } from './TechnicalDocumentationExtractionMetadataSection';
import {
  formatAreaLabel,
  formatAreaOrUndefined,
  formatDecimal,
  formatVolumeM3OrUndefined,
  hasNumericValue,
} from './technicalDocumentationFormatters';

export interface TechnicalDocumentationProjectModelSectionProps {
  projectModel: ProjectModelWeb;
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

function WallGroupPanel({
  title,
  group,
}: {
  title: string;
  group?: ProjectModelWallGroupWeb;
}): React.ReactElement | null {
  if (!group || (group.layers.length === 0 && !hasNumericValue(group.thicknessCm))) {
    return null;
  }

  return (
    <Box mb={4}>
      <Text fontSize="sm" fontWeight="semibold" mb={2} color="neutral.700">
        {title}
        {hasNumericValue(group.thicknessCm) ? ` (${group.thicknessCm} cm)` : ''}
      </Text>
      {group.layers.length > 0 && (
        <Table size="sm" variant="simple">
          <Thead>
            <Tr>
              <Th>Materiał</Th>
              <Th isNumeric>Grubość (cm)</Th>
            </Tr>
          </Thead>
          <Tbody>
            {group.layers.map((layer, index) => (
              <Tr key={`${title}-layer-${index}`}>
                <Td>{layer.material}</Td>
                <Td isNumeric>{layer.thicknessCm ?? '—'}</Td>
              </Tr>
            ))}
          </Tbody>
        </Table>
      )}
    </Box>
  );
}

function FloorsPanel({ floors }: { floors: ProjectModelFloorWeb[] }): React.ReactElement {
  return (
    <Accordion allowMultiple>
      {floors.map((floor) => (
        <AccordionItem key={`${floor.order}-${floor.level}`}>
          <h3>
            <AccordionButton>
              <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                      {floor.level}
                      {hasNumericValue(floor.totalAreaM2) && floor.totalAreaM2 > 0
                        ? ` — ${formatAreaLabel(floor.totalAreaM2)}`
                        : ''}
              </Box>
              <AccordionIcon />
            </AccordionButton>
          </h3>
          <AccordionPanel pb={4}>
            {floor.rooms.length > 0 ? (
              <Table size="sm" variant="simple">
                <Thead>
                  <Tr>
                    <Th>Nazwa</Th>
                    <Th>Symbol</Th>
                    <Th isNumeric>Powierzchnia</Th>
                  </Tr>
                </Thead>
                <Tbody>
                  {floor.rooms.map((room, index) => (
                    <Tr key={`${floor.level}-room-${index}`}>
                      <Td>{room.name}</Td>
                      <Td>{room.symbol ?? '—'}</Td>
                            <Td isNumeric>{formatAreaLabel(room.areaM2)}</Td>
                    </Tr>
                  ))}
                </Tbody>
              </Table>
            ) : (
              <Text fontSize="sm" color="neutral.500">
                Brak pomieszczeń na tej kondygnacji.
              </Text>
            )}
          </AccordionPanel>
        </AccordionItem>
      ))}
    </Accordion>
  );
}

function CeilingsTable({ ceilings }: { ceilings: ProjectModelCeilingWeb[] }): React.ReactElement {
  return (
    <Table size="sm" variant="simple">
      <Thead>
        <Tr>
          <Th>Opis</Th>
          <Th>Beton</Th>
          <Th isNumeric>Grubość (cm)</Th>
          <Th isNumeric>Stal dolna (kg)</Th>
          <Th isNumeric>Stal górna (kg)</Th>
        </Tr>
      </Thead>
      <Tbody>
        {ceilings.map((ceiling, index) => (
          <Tr key={`ceiling-${index}`}>
            <Td>{ceiling.coverageDescription ?? '—'}</Td>
            <Td>{ceiling.concrete ?? '—'}</Td>
            <Td isNumeric>{ceiling.thicknessCm ?? '—'}</Td>
            <Td isNumeric>{ceiling.steelBottomKg ?? '—'}</Td>
            <Td isNumeric>{ceiling.steelTopKg ?? '—'}</Td>
          </Tr>
        ))}
      </Tbody>
    </Table>
  );
}

function TimberGroupsTable({ groups }: { groups: ProjectModelTimberGroupWeb[] }): React.ReactElement {
  return (
    <Table size="sm" variant="simple">
      <Thead>
        <Tr>
          <Th>Element</Th>
          <Th>Przekrój</Th>
          <Th isNumeric>Szt.</Th>
          <Th isNumeric>Dł. (m)</Th>
          <Th isNumeric>Obj. (m³)</Th>
        </Tr>
      </Thead>
      <Tbody>
        {groups.map((group, index) => (
          <Tr key={`timber-${index}`}>
            <Td>{group.element}</Td>
            <Td>{group.section ?? '—'}</Td>
            <Td isNumeric>{group.count ?? '—'}</Td>
            <Td isNumeric>{formatDecimal(group.lengthM, 2)}</Td>
            <Td isNumeric>{formatDecimal(group.volumeM3, 3)}</Td>
          </Tr>
        ))}
      </Tbody>
    </Table>
  );
}

function SlabPanel({ slab }: { slab: ProjectModelSlabWeb }): React.ReactElement {
  return (
    <Table size="sm" variant="simple">
      <Thead>
        <Tr>
          <Th>Opis</Th>
          <Th>Beton</Th>
          <Th isNumeric>Grubość (cm)</Th>
          <Th isNumeric>Pow. (m²)</Th>
          <Th isNumeric>Stal dolna (kg)</Th>
          <Th isNumeric>Stal górna (kg)</Th>
        </Tr>
      </Thead>
      <Tbody>
        <Tr>
          <Td>{slab.coverageDescription ?? '—'}</Td>
          <Td>{slab.concrete ?? '—'}</Td>
          <Td isNumeric>{slab.thicknessCm ?? '—'}</Td>
          <Td isNumeric>{slab.areaM2 ?? '—'}</Td>
          <Td isNumeric>{slab.steelBottomKg ?? '—'}</Td>
          <Td isNumeric>{slab.steelTopKg ?? '—'}</Td>
        </Tr>
      </Tbody>
    </Table>
  );
}

function ElevationsTable({ elevations }: { elevations: ProjectModelElevationWeb[] }): React.ReactElement {
  return (
    <Table size="sm" variant="simple">
      <Thead>
        <Tr>
          <Th>Orientacja</Th>
          <Th>Arkusz</Th>
          <Th isNumeric>Otwory</Th>
          <Th isNumeric>Wykończenia</Th>
        </Tr>
      </Thead>
      <Tbody>
        {elevations.map((elevation, index) => (
          <Tr key={`elevation-${index}`}>
            <Td>{elevation.orientation}</Td>
            <Td>{elevation.sourceDrawing ?? '—'}</Td>
            <Td isNumeric>{elevation.openings.length}</Td>
            <Td isNumeric>{elevation.finishes.length}</Td>
          </Tr>
        ))}
      </Tbody>
    </Table>
  );
}

function warningBadgeColor(severity?: string): string {
  if (severity === 'error') {
    return 'red';
  }

  return 'orange';
}

function WarningsList({ warnings }: { warnings: ProjectModelWarningWeb[] }): React.ReactElement {
  return (
    <VStack align="stretch" spacing={2}>
      {warnings.map((warning, index) => (
        <Box key={`warning-${index}`} p={3} borderWidth="1px" borderRadius="md" borderColor="neutral.200">
          <Badge colorScheme={warningBadgeColor(warning.severity)} mb={1}>
            {warning.severity ?? 'warning'}
          </Badge>
          <Text fontSize="sm" color="neutral.800">
            {warning.message}
          </Text>
          {(warning.code || warning.sourceGroup) && (
            <Text fontSize="xs" color="neutral.500" mt={1}>
              {[warning.code, warning.sourceGroup].filter(Boolean).join(' · ')}
            </Text>
          )}
        </Box>
      ))}
    </VStack>
  );
}

function ColumnsTable({ columns }: { columns: ProjectModelColumnWeb[] }): React.ReactElement {
  return (
    <Table size="sm" variant="simple">
      <Thead>
        <Tr>
          <Th>Symbol</Th>
          <Th isNumeric>B (cm)</Th>
          <Th isNumeric>H (cm)</Th>
          <Th isNumeric>Wys. (m)</Th>
          <Th>Beton</Th>
        </Tr>
      </Thead>
      <Tbody>
        {columns.map((column, index) => (
          <Tr key={`column-${index}`}>
            <Td>{column.symbol}</Td>
            <Td isNumeric>{column.bCm ?? '—'}</Td>
            <Td isNumeric>{column.hCm ?? '—'}</Td>
            <Td isNumeric>{formatDecimal(column.heightM, 2)}</Td>
            <Td>{column.concreteClass ?? '—'}</Td>
          </Tr>
        ))}
      </Tbody>
    </Table>
  );
}

export function TechnicalDocumentationProjectModelSection({
  projectModel,
}: TechnicalDocumentationProjectModelSectionProps): React.ReactElement | null {
  const {
    site,
    floors,
    walls,
    foundations,
    slab,
    ceilings,
    roof,
    elevations,
    columns,
    beams,
    lintels,
    warnings,
    extractionMetadata,
    conflicts,
    missingData,
  } = projectModel;

  const derivedWarnings: ProjectModelWarningWeb[] = warnings ?? [];
  const hasDerivedWarnings = derivedWarnings.length > 0;
  const legacyWarnings: ProjectModelWarningWeb[] = !hasDerivedWarnings
    ? [
      ...(conflicts?.map((conflict) => ({
        message: `Konflikt: ${conflict.fieldPath} (${conflict.valueA ?? '?'} vs ${conflict.valueB ?? '?'})`,
        severity: 'warning',
      })) ?? []),
      ...(missingData?.map((item) => ({
        message: item,
        severity: 'warning',
      })) ?? []),
    ]
    : [];

  const displayWarnings = hasDerivedWarnings ? derivedWarnings : legacyWarnings;

  const hasSlab = slab
    && (slab.concrete
      || slab.coverageDescription
      || hasNumericValue(slab.thicknessCm)
      || hasNumericValue(slab.steelBottomKg)
      || hasNumericValue(slab.steelTopKg)
      || hasNumericValue(slab.areaM2));

  const hasElevations = (elevations?.length ?? 0) > 0;

  const hasSite = site
    && (hasNumericValue(site.plotAreaM2)
      || hasNumericValue(site.buildingFootprintM2)
      || hasNumericValue(site.buildingVolumeM3));

  const hasWalls = walls
    && ((walls.external?.layers.length ?? 0) > 0
      || (walls.internalLoadBearing?.layers.length ?? 0) > 0
      || (walls.partition?.layers.length ?? 0) > 0);

  const hasFoundations = foundations
    && ((foundations.footings?.length ?? 0) > 0
      || (foundations.pads?.length ?? 0) > 0
      || foundations.concrete
      || foundations.foundationWall);

  const hasRoof = roof
    && (hasNumericValue(roof.pitchDegrees)
      || hasNumericValue(roof.areaM2)
      || roof.coveringType
      || (roof.timberGroups?.length ?? 0) > 0
      || hasNumericValue(roof.totalTimberVolumeM3));

  const hasContent = hasSite
    || (floors?.length ?? 0) > 0
    || hasWalls
    || hasFoundations
    || hasSlab
    || (ceilings?.length ?? 0) > 0
    || hasRoof
    || hasElevations
    || (columns?.length ?? 0) > 0
    || (beams?.length ?? 0) > 0
    || (lintels?.length ?? 0) > 0
    || displayWarnings.length > 0
    || extractionMetadata !== undefined;

  if (!hasContent) {
    return null;
  }

  return (
    <Box>
      <Heading size="sm" mb={3} color="neutral.700">
        Model projektu (agregacja)
      </Heading>

      <Accordion allowMultiple defaultIndex={[0]}>
        {hasSite && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Działka i kubatura
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <SimpleGrid columns={{ base: 1, md: 3 }} spacing={3}>
                <InfoRow label="Pow. działki" value={formatAreaOrUndefined(site?.plotAreaM2)} />
                <InfoRow label="Pow. zabudowy" value={formatAreaOrUndefined(site?.buildingFootprintM2)} />
                <InfoRow label="Kubatura" value={formatVolumeM3OrUndefined(site?.buildingVolumeM3)} />
              </SimpleGrid>
            </AccordionPanel>
          </AccordionItem>
        )}

        {floors && floors.length > 0 && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Kondygnacje ({floors.length})
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <FloorsPanel floors={floors} />
            </AccordionPanel>
          </AccordionItem>
        )}

        {hasWalls && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Ściany (model)
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <WallGroupPanel title="Zewnętrzne" group={walls?.external} />
              <WallGroupPanel title="Nośne wewnętrzne" group={walls?.internalLoadBearing} />
              <WallGroupPanel title="Działowe" group={walls?.partition} />
            </AccordionPanel>
          </AccordionItem>
        )}

        {hasFoundations && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Fundamenty (model)
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <SimpleGrid columns={{ base: 1, md: 2 }} spacing={3} mb={4}>
                <InfoRow label="Beton" value={foundations?.concrete} />
                <InfoRow label="Ściana fundamentowa" value={foundations?.foundationWall} />
              </SimpleGrid>
              {(foundations?.footings?.length ?? 0) > 0 && (
                <Table size="sm" variant="simple" mb={4}>
                  <Thead>
                    <Tr>
                      <Th>Symbol</Th>
                      <Th isNumeric>Szer. (m)</Th>
                      <Th isNumeric>Wys. (m)</Th>
                      <Th>Beton</Th>
                    </Tr>
                  </Thead>
                  <Tbody>
                    {foundations?.footings?.map((footing, index) => (
                      <Tr key={`footing-${index}`}>
                        <Td>{footing.symbol ?? '—'}</Td>
                        <Td isNumeric>{formatDecimal(footing.widthM, 2)}</Td>
                        <Td isNumeric>{formatDecimal(footing.heightM, 2)}</Td>
                        <Td>{footing.concreteClass ?? '—'}</Td>
                      </Tr>
                    ))}
                  </Tbody>
                </Table>
              )}
              {(foundations?.pads?.length ?? 0) > 0 && (
                <Table size="sm" variant="simple">
                  <Thead>
                    <Tr>
                      <Th>Symbol</Th>
                      <Th isNumeric>B (m)</Th>
                      <Th isNumeric>L (m)</Th>
                      <Th isNumeric>H (m)</Th>
                    </Tr>
                  </Thead>
                  <Tbody>
                    {foundations?.pads?.map((pad, index) => (
                      <Tr key={`pad-${index}`}>
                        <Td>{pad.symbol ?? '—'}</Td>
                        <Td isNumeric>{formatDecimal(pad.bM, 2)}</Td>
                        <Td isNumeric>{formatDecimal(pad.lM, 2)}</Td>
                        <Td isNumeric>{formatDecimal(pad.heightM, 2)}</Td>
                      </Tr>
                    ))}
                  </Tbody>
                </Table>
              )}
            </AccordionPanel>
          </AccordionItem>
        )}

        {hasSlab && slab && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Płyta fundamentowa / strop
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <SlabPanel slab={slab} />
            </AccordionPanel>
          </AccordionItem>
        )}

        {ceilings && ceilings.length > 0 && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Stropy / stropodachy ({ceilings.length})
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <CeilingsTable ceilings={ceilings} />
            </AccordionPanel>
          </AccordionItem>
        )}

        {hasRoof && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Dach (model)
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <SimpleGrid columns={{ base: 1, md: 4 }} spacing={3} mb={4}>
                <InfoRow label="Powierzchnia" value={formatAreaOrUndefined(roof?.areaM2)} />
                <InfoRow label="Kąt nachylenia" value={hasNumericValue(roof?.pitchDegrees) ? `${roof.pitchDegrees}°` : undefined} />
                <InfoRow label="Pokrycie" value={roof?.coveringType} />
                <InfoRow label="Klasa drewna" value={roof?.woodClass} />
                <InfoRow label="Łączna obj. drewna" value={formatVolumeM3OrUndefined(roof?.totalTimberVolumeM3)} />
              </SimpleGrid>
              {roof?.timberGroups && roof.timberGroups.length > 0 && (
                <TimberGroupsTable groups={roof.timberGroups} />
              )}
            </AccordionPanel>
          </AccordionItem>
        )}

        {hasElevations && elevations && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Elewacje ({elevations.length})
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <ElevationsTable elevations={elevations} />
            </AccordionPanel>
          </AccordionItem>
        )}

        {columns && columns.length > 0 && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Słupy ({columns.length})
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <ColumnsTable columns={columns} />
            </AccordionPanel>
          </AccordionItem>
        )}

        {beams && beams.length > 0 && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Belki ({beams.length})
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <Table size="sm" variant="simple">
                <Thead>
                  <Tr>
                    <Th>Symbol</Th>
                    <Th isNumeric>Rozpiętość (m)</Th>
                    <Th isNumeric>B (cm)</Th>
                    <Th isNumeric>H (cm)</Th>
                  </Tr>
                </Thead>
                <Tbody>
                  {beams.map((beam, index) => (
                    <Tr key={`beam-${index}`}>
                      <Td>{beam.symbol}</Td>
                      <Td isNumeric>{formatDecimal(beam.spanM, 2)}</Td>
                      <Td isNumeric>{beam.bwCm ?? '—'}</Td>
                      <Td isNumeric>{beam.hCm ?? '—'}</Td>
                    </Tr>
                  ))}
                </Tbody>
              </Table>
            </AccordionPanel>
          </AccordionItem>
        )}

        {lintels && lintels.length > 0 && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Nadproża ({lintels.length})
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <Table size="sm" variant="simple">
                <Thead>
                  <Tr>
                    <Th>Symbol</Th>
                    <Th isNumeric>Rozpiętość (m)</Th>
                    <Th isNumeric>B (cm)</Th>
                    <Th isNumeric>H (cm)</Th>
                  </Tr>
                </Thead>
                <Tbody>
                  {lintels.map((lintel, index) => (
                    <Tr key={`lintel-${index}`}>
                      <Td>{lintel.symbol}</Td>
                      <Td isNumeric>{formatDecimal(lintel.spanM, 2)}</Td>
                      <Td isNumeric>{lintel.bwCm ?? '—'}</Td>
                      <Td isNumeric>{lintel.hCm ?? '—'}</Td>
                    </Tr>
                  ))}
                </Tbody>
              </Table>
            </AccordionPanel>
          </AccordionItem>
        )}

        {displayWarnings.length > 0 && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Ostrzeżenia modelu ({displayWarnings.length})
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <WarningsList warnings={displayWarnings} />
            </AccordionPanel>
          </AccordionItem>
        )}

        {conflicts && conflicts.length > 0 && hasDerivedWarnings && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Konflikty agregacji ({conflicts.length})
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <Table size="sm" variant="simple">
                <Thead>
                  <Tr>
                    <Th>Pole</Th>
                    <Th>Wartość A</Th>
                    <Th>Wartość B</Th>
                  </Tr>
                </Thead>
                <Tbody>
                  {conflicts.map((conflict, index) => (
                    <Tr key={`conflict-${index}`}>
                      <Td fontSize="xs">{conflict.fieldPath}</Td>
                      <Td>{conflict.valueA ?? '—'}</Td>
                      <Td>{conflict.valueB ?? '—'}</Td>
                    </Tr>
                  ))}
                </Tbody>
              </Table>
            </AccordionPanel>
          </AccordionItem>
        )}

        {missingData && missingData.length > 0 && hasDerivedWarnings && (
          <AccordionItem>
            <h3>
              <AccordionButton>
                <Box flex="1" textAlign="left" fontSize="sm" fontWeight="medium">
                  Braki w modelu ({missingData.length})
                </Box>
                <AccordionIcon />
              </AccordionButton>
            </h3>
            <AccordionPanel pb={4}>
              <VStack align="stretch" spacing={1}>
                {missingData.map((item, index) => (
                  <Text key={`model-missing-${index}`} fontSize="sm" color="neutral.600">
                    {item}
                  </Text>
                ))}
              </VStack>
            </AccordionPanel>
          </AccordionItem>
        )}
      </Accordion>

      {extractionMetadata && (
        <Box mt={4}>
          <TechnicalDocumentationExtractionMetadataSection metadata={extractionMetadata} />
        </Box>
      )}
    </Box>
  );
}
