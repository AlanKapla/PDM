import {
  Accordion,
  AccordionButton,
  AccordionIcon,
  AccordionItem,
  AccordionPanel,
  Box,
  Heading,
  SimpleGrid,
  Tag,
  Text,
  Wrap,
  WrapItem,
} from '@chakra-ui/react';
import type { ProjectModelExtractionMetadataWeb } from '../../types/technicalDocumentation.types';
import { formatDate } from '../../utils/formatters';

export interface TechnicalDocumentationExtractionMetadataSectionProps {
  metadata: ProjectModelExtractionMetadataWeb;
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

export function TechnicalDocumentationExtractionMetadataSection({
  metadata,
}: TechnicalDocumentationExtractionMetadataSectionProps): React.ReactElement | null {
  const hasThematicGroups = (metadata.thematicGroups?.length ?? 0) > 0;
  const hasContent = Boolean(metadata.pipelineVersion)
    || hasThematicGroups
    || metadata.tokenUsage !== undefined
    || metadata.processedAt !== undefined;

  if (!hasContent) {
    return null;
  }

  return (
    <Accordion allowToggle>
      <AccordionItem border="none">
        <h3>
          <AccordionButton px={0}>
            <Box flex="1" textAlign="left">
              <Heading size="sm" color="neutral.700">
                Metadane pipeline
              </Heading>
            </Box>
            <AccordionIcon />
          </AccordionButton>
        </h3>
        <AccordionPanel px={0} pb={4}>
          <SimpleGrid columns={{ base: 1, md: 3 }} spacing={3} mb={hasThematicGroups ? 4 : 0}>
            <InfoRow label="Wersja pipeline" value={metadata.pipelineVersion} />
            <InfoRow
              label="Zużycie tokenów"
              value={metadata.tokenUsage !== undefined
                ? metadata.tokenUsage.toLocaleString('pl-PL')
                : undefined}
            />
            <InfoRow
              label="Przetworzono"
              value={metadata.processedAt ? formatDate(metadata.processedAt) : undefined}
            />
          </SimpleGrid>
          {hasThematicGroups && (
            <Box>
              <Text fontSize="xs" color="neutral.500" textTransform="uppercase" letterSpacing="wide" mb={2}>
                Grupy tematyczne
              </Text>
              <Wrap spacing={2}>
                {metadata.thematicGroups?.map((group) => (
                  <WrapItem key={group}>
                    <Tag size="sm" colorScheme="teal" variant="subtle">
                      {group}
                    </Tag>
                  </WrapItem>
                ))}
              </Wrap>
            </Box>
          )}
        </AccordionPanel>
      </AccordionItem>
    </Accordion>
  );
}
