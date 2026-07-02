import {
  Box,
  Button,
  HStack,
  Icon,
  Table,
  Tbody,
  Td,
  Text,
  Th,
  Thead,
  Tr,
} from '@chakra-ui/react';
import { Download, Eye, FileText } from 'lucide-react';
import type { TechnicalDocumentationFileWeb } from '../../types/technicalDocumentation.types';
import { formatFileSize } from '../../utils/formatters';

export interface TechnicalDocumentationFileListProps {
  files: TechnicalDocumentationFileWeb[];
}

const isPreviewSupported = (contentType: string): boolean => {
  return contentType === 'application/pdf' || contentType.startsWith('image/');
};

const getContentTypeLabel = (contentType: string): string => {
  if (contentType === 'application/pdf') {
    return 'PDF';
  }
  if (contentType === 'image/jpeg') {
    return 'JPG';
  }
  return contentType;
};

export function TechnicalDocumentationFileList({
  files,
}: TechnicalDocumentationFileListProps): React.ReactElement {
  if (files.length === 0) {
    return (
      <Box p={4} textAlign="center">
        <Text color="neutral.600" fontSize="sm">
          Brak powiązanych plików.
        </Text>
      </Box>
    );
  }

  return (
    <Box overflowX="auto">
      <Table size="sm" variant="simple">
        <Thead>
          <Tr>
            <Th>Nazwa pliku</Th>
            <Th>Rozmiar</Th>
            <Th>Typ</Th>
            <Th textAlign="right">Akcje</Th>
          </Tr>
        </Thead>
        <Tbody>
          {files.map((file) => (
            <Tr key={file.id}>
              <Td>
                <HStack spacing={2}>
                  <Icon as={FileText} boxSize={4} color="neutral.600" aria-hidden="true" />
                  <Text fontSize="sm" noOfLines={1}>
                    {file.fileName}
                  </Text>
                </HStack>
              </Td>
              <Td>
                <Text fontSize="sm" color="neutral.700">
                  {formatFileSize(file.fileSize)}
                </Text>
              </Td>
              <Td>
                <Text fontSize="sm" color="neutral.700">
                  {getContentTypeLabel(file.contentType)}
                </Text>
              </Td>
              <Td>
                <HStack spacing={2} justify="flex-end">
                  {file.sasUriPreview && isPreviewSupported(file.contentType) && (
                    <Button
                      size="xs"
                      variant="outline"
                      leftIcon={<Eye size={14} aria-hidden="true" />}
                      onClick={() => window.open(file.sasUriPreview, '_blank', 'noopener,noreferrer')}
                    >
                      Podgląd
                    </Button>
                  )}
                  {file.sasUriDownload && (
                    <Button
                      size="xs"
                      variant="outline"
                      leftIcon={<Download size={14} aria-hidden="true" />}
                      onClick={() => window.open(file.sasUriDownload, '_blank', 'noopener,noreferrer')}
                    >
                      Pobierz
                    </Button>
                  )}
                </HStack>
              </Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
    </Box>
  );
}
