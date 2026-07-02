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
  Text,
} from '@chakra-ui/react';
import type { ValidatedDrawingWeb } from '../../types/technicalDocumentation.types';

export interface TechnicalDocumentationValidatedDrawingsSectionProps {
  validatedDrawings: ValidatedDrawingWeb[];
}

function formatSheetLabel(drawing: ValidatedDrawingWeb): string {
  return drawing.sheetNumber ?? drawing.title ?? drawing.drawingType;
}

function DrawingDetailPanel({ drawing }: { drawing: ValidatedDrawingWeb }): React.ReactElement {
  return (
    <Box>
      <SimpleGrid columns={{ base: 1, md: 2 }} spacing={3}>
        <Box>
          <Text fontSize="xs" color="neutral.500" textTransform="uppercase">
            Tytuł
          </Text>
          <Text fontSize="sm">{drawing.title ?? '—'}</Text>
        </Box>
        <Box>
          <Text fontSize="xs" color="neutral.500" textTransform="uppercase">
            Typ
          </Text>
          <Text fontSize="sm">{drawing.drawingType}</Text>
        </Box>
        <Box>
          <Text fontSize="xs" color="neutral.500" textTransform="uppercase">
            Skala
          </Text>
          <Text fontSize="sm">
            {drawing.scale !== undefined ? `1:${drawing.scale}` : '—'}
          </Text>
        </Box>
        <Box>
          <Text fontSize="xs" color="neutral.500" textTransform="uppercase">
            Status
          </Text>
          <Badge colorScheme={drawing.validated ? 'green' : 'gray'}>
            {drawing.validated ? 'Zwalidowany' : 'Niezwalidowany'}
          </Badge>
        </Box>
        {drawing.hasMaterialTable && (
          <Box>
            <Text fontSize="xs" color="neutral.500" textTransform="uppercase">
              Tabela materiałowa
            </Text>
            <Badge colorScheme="blue">Tak</Badge>
          </Box>
        )}
      </SimpleGrid>
    </Box>
  );
}

export function TechnicalDocumentationValidatedDrawingsSection({
  validatedDrawings,
}: TechnicalDocumentationValidatedDrawingsSectionProps): React.ReactElement {
  return (
    <Box>
      <Heading size="sm" mb={3} color="neutral.700">
        Zwalidowane rysunki ({validatedDrawings.length})
      </Heading>
      <Accordion allowMultiple reduceMotion>
        {validatedDrawings.map((drawing, index) => (
          <AccordionItem key={`validated-${drawing.sheetNumber ?? index}`} borderColor="neutral.200">
            <AccordionButton px={3} py={2}>
              <Box flex="1" textAlign="left">
                <Text fontSize="sm" fontWeight="semibold">
                  {formatSheetLabel(drawing)}
                </Text>
                <Text fontSize="xs" color="neutral.500">
                  {drawing.drawingType}
                </Text>
              </Box>
              <AccordionIcon />
            </AccordionButton>
            <AccordionPanel pb={4}>
              <DrawingDetailPanel drawing={drawing} />
            </AccordionPanel>
          </AccordionItem>
        ))}
      </Accordion>
    </Box>
  );
}
