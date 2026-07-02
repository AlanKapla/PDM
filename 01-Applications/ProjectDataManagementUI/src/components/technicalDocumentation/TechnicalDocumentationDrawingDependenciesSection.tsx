import { Box, Heading, Table, Tbody, Td, Text, Th, Thead, Tr } from '@chakra-ui/react';
import type { DrawingDependencyLinkWeb } from '../../types/technicalDocumentation.types';

export interface TechnicalDocumentationDrawingDependenciesSectionProps {
  drawingDependencies: DrawingDependencyLinkWeb[];
}

export function TechnicalDocumentationDrawingDependenciesSection({
  drawingDependencies,
}: TechnicalDocumentationDrawingDependenciesSectionProps): React.ReactElement {
  return (
    <Box>
      <Heading size="sm" mb={3} color="neutral.700">
        Zależności między rysunkami ({drawingDependencies.length})
      </Heading>
      <Table size="sm" variant="simple">
        <Thead>
          <Tr>
            <Th>Od</Th>
            <Th>Do</Th>
            <Th>Relacja</Th>
          </Tr>
        </Thead>
        <Tbody>
          {drawingDependencies.map((link, index) => (
            <Tr key={`dependency-${index}`}>
              <Td>{link.from}</Td>
              <Td>{link.to}</Td>
              <Td fontSize="xs" color="neutral.600">
                {link.relation}
              </Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
      {drawingDependencies.length === 0 && (
        <Text fontSize="sm" color="neutral.500">
          Brak powiązań między rysunkami.
        </Text>
      )}
    </Box>
  );
}
