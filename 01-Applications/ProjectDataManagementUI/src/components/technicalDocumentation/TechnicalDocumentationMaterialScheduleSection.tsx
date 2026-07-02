import {
  Accordion,
  AccordionButton,
  AccordionIcon,
  AccordionItem,
  AccordionPanel,
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
} from '@chakra-ui/react';
import type {
  DetailsMaterialScheduleItemWeb,
  MaterialScheduleWeb,
} from '../../types/technicalDocumentation.types';
import { formatDate } from '../../utils/formatters';
import { formatDecimal } from './technicalDocumentationFormatters';

export interface TechnicalDocumentationMaterialScheduleSectionProps {
  materialSchedule: MaterialScheduleWeb;
}

function ScheduleItemsTable({
  title,
  items,
}: {
  title: string;
  items: DetailsMaterialScheduleItemWeb[];
}): React.ReactElement | null {
  if (items.length === 0) {
    return null;
  }

  return (
    <Box mb={4}>
      <Text fontSize="sm" fontWeight="semibold" mb={2} color="neutral.700">
        {title}
      </Text>
      <Table size="sm" variant="simple">
        <Thead>
          <Tr>
            <Th>Element</Th>
            <Th isNumeric>Netto</Th>
            <Th isNumeric>Brutto</Th>
            <Th>Źródło</Th>
          </Tr>
        </Thead>
        <Tbody>
          {items.map((item, index) => (
            <Tr key={`${title}-${index}`}>
              <Td>{item.element}</Td>
              <Td isNumeric>
                {formatDecimal(item.grossM3 ?? item.grossM2 ?? item.grossKg ?? 0, 2)}
              </Td>
              <Td isNumeric>
                {formatDecimal(item.grossM3 ?? item.grossM2 ?? item.grossKg ?? 0, 2)}
              </Td>
              <Td fontSize="xs">{item.sourceDrawing ?? item.sourceType ?? '—'}</Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
    </Box>
  );
}

export function TechnicalDocumentationMaterialScheduleSection({
  materialSchedule,
}: TechnicalDocumentationMaterialScheduleSectionProps): React.ReactElement {
  const groups = materialSchedule.groups;

  return (
    <Box>
      <Heading size="sm" mb={3} color="neutral.700">
        Harmonogram materiałów
      </Heading>
      {materialSchedule.calculatedAt && (
        <Text fontSize="xs" color="neutral.500" mb={3}>
          Obliczono: {formatDate(materialSchedule.calculatedAt)}
        </Text>
      )}

      {materialSchedule.totals && (
        <SimpleGrid columns={{ base: 2, md: 4 }} spacing={3} mb={4}>
          <Stat size="sm">
            <StatLabel>Beton [m³]</StatLabel>
            <StatNumber fontSize="md">{formatDecimal(materialSchedule.totals.concreteM3, 2)}</StatNumber>
          </Stat>
          <Stat size="sm">
            <StatLabel>Stal [kg]</StatLabel>
            <StatNumber fontSize="md">{formatDecimal(materialSchedule.totals.steelKg, 2)}</StatNumber>
          </Stat>
          <Stat size="sm">
            <StatLabel>Drewno [m³]</StatLabel>
            <StatNumber fontSize="md">{formatDecimal(materialSchedule.totals.timberM3, 2)}</StatNumber>
          </Stat>
          <Stat size="sm">
            <StatLabel>Izolacja [m²]</StatLabel>
            <StatNumber fontSize="md">{formatDecimal(materialSchedule.totals.insulationM2, 2)}</StatNumber>
          </Stat>
        </SimpleGrid>
      )}

      <Accordion allowMultiple reduceMotion>
        {groups?.foundations && (
          <AccordionItem>
            <AccordionButton>
              <Box flex="1" textAlign="left" fontWeight="semibold">
                Fundamenty
              </Box>
              <AccordionIcon />
            </AccordionButton>
            <AccordionPanel>
              <ScheduleItemsTable title="Beton" items={groups.foundations.concrete ?? []} />
              <ScheduleItemsTable title="Stal" items={groups.foundations.steel ?? []} />
              <ScheduleItemsTable title="Murowanie" items={groups.foundations.masonry ?? []} />
              <ScheduleItemsTable title="Izolacja" items={groups.foundations.insulation ?? []} />
            </AccordionPanel>
          </AccordionItem>
        )}
        {groups?.slabs && (
          <AccordionItem>
            <AccordionButton>
              <Box flex="1" textAlign="left" fontWeight="semibold">
                Stropy
              </Box>
              <AccordionIcon />
            </AccordionButton>
            <AccordionPanel>
              <ScheduleItemsTable title="Beton" items={groups.slabs.concrete ?? []} />
              <ScheduleItemsTable title="Stal" items={groups.slabs.steel ?? []} />
            </AccordionPanel>
          </AccordionItem>
        )}
        {groups?.roof && (
          <AccordionItem>
            <AccordionButton>
              <Box flex="1" textAlign="left" fontWeight="semibold">
                Dach
              </Box>
              <AccordionIcon />
            </AccordionButton>
            <AccordionPanel>
              <ScheduleItemsTable title="Drewno" items={groups.roof.timber ?? []} />
              <ScheduleItemsTable title="Pokrycie" items={groups.roof.covering ?? []} />
            </AccordionPanel>
          </AccordionItem>
        )}
      </Accordion>
    </Box>
  );
}
