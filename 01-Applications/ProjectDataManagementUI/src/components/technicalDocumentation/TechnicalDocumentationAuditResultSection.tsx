import {
  Alert,
  AlertIcon,
  Box,
  Heading,
  List,
  ListItem,
  Table,
  Tbody,
  Td,
  Text,
  Th,
  Thead,
  Tr,
  VStack,
} from '@chakra-ui/react';
import type { AuditResultWeb } from '../../types/technicalDocumentation.types';

export interface TechnicalDocumentationAuditResultSectionProps {
  auditResult: AuditResultWeb;
}

function StringListSection({
  title,
  items,
  status,
}: {
  title: string;
  items: string[];
  status: 'warning' | 'info' | 'error';
}): React.ReactElement | null {
  if (items.length === 0) {
    return null;
  }

  return (
    <Box>
      <Heading size="xs" mb={2} color="neutral.700">
        {title}
      </Heading>
      <Alert status={status} variant="left-accent" borderRadius="md">
        <AlertIcon />
        <List spacing={1} fontSize="sm">
          {items.map((item, index) => (
            <ListItem key={`${title}-${index}`}>{item}</ListItem>
          ))}
        </List>
      </Alert>
    </Box>
  );
}

export function TechnicalDocumentationAuditResultSection({
  auditResult,
}: TechnicalDocumentationAuditResultSectionProps): React.ReactElement | null {
  const missingMaterials = auditResult.missingMaterials.length > 0
    ? auditResult.missingMaterials
    : (auditResult.missingData ?? []);

  const hasContent = auditResult.warnings.length > 0
    || missingMaterials.length > 0
    || (auditResult.assumptions?.length ?? 0) > 0
    || (auditResult.crossReferenceErrors?.length ?? 0) > 0
    || (auditResult.unitErrors?.length ?? 0) > 0;

  if (!hasContent) {
    return null;
  }

  return (
    <Box>
      <Heading size="sm" mb={3} color="neutral.700">
        Wynik audytu
      </Heading>
      <VStack align="stretch" spacing={4}>
        <StringListSection title="Ostrzeżenia" items={auditResult.warnings} status="warning" />
        <StringListSection title="Brakujące materiały" items={missingMaterials} status="error" />
        <StringListSection
          title="Założenia"
          items={auditResult.assumptions ?? []}
          status="info"
        />
        <StringListSection
          title="Błędy odwołań krzyżowych"
          items={auditResult.crossReferenceErrors ?? []}
          status="error"
        />

        {(auditResult.unitErrors?.length ?? 0) > 0 && (
          <Box>
            <Heading size="xs" mb={2} color="neutral.700">
              Błędy jednostek
            </Heading>
            <Table size="sm" variant="simple">
              <Thead>
                <Tr>
                  <Th>Pole</Th>
                  <Th>Znaleziono</Th>
                  <Th>Oczekiwano</Th>
                </Tr>
              </Thead>
              <Tbody>
                {auditResult.unitErrors?.map((error, index) => (
                  <Tr key={`unit-error-${index}`}>
                    <Td fontSize="xs">{error.field ?? '—'}</Td>
                    <Td>{error.found ?? '—'}</Td>
                    <Td>{error.expected ?? '—'}</Td>
                  </Tr>
                ))}
              </Tbody>
            </Table>
          </Box>
        )}

        {auditResult.warnings.length === 0
          && missingMaterials.length === 0
          && (auditResult.assumptions?.length ?? 0) === 0
          && (auditResult.crossReferenceErrors?.length ?? 0) === 0
          && (auditResult.unitErrors?.length ?? 0) === 0 && (
          <Text fontSize="sm" color="neutral.500">
            Brak uwag audytowych.
          </Text>
        )}
      </VStack>
    </Box>
  );
}
