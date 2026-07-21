import React, { useId, useCallback } from 'react';
import {
  Box,
  HStack,
  Icon,
  IconButton,
  Text,
  VStack,
} from '@chakra-ui/react';
import { FileUp, X } from 'lucide-react';

const ACCEPTED_EXTENSIONS: readonly string[] = ['.jpg', '.jpeg', '.png', '.pdf'];
const ACCEPTED_MIME_TYPES: readonly string[] = [
  'image/jpeg',
  'image/png',
  'application/pdf',
];
const BYTES_PER_MB = 1024 * 1024;

export interface FileRejection {
  fileName: string;
  reason: string;
}

function getFileExtension(file: File): string {
  return file.name.includes('.')
    ? `.${file.name.split('.').pop()?.toLowerCase() ?? ''}`
    : '';
}

function validateFile(file: File): string | null {
  const extension = getFileExtension(file);
  if (!ACCEPTED_EXTENSIONS.includes(extension)) {
    return `Niedozwolone rozszerzenie: ${extension || '(brak)'}`;
  }
  if (file.type && !ACCEPTED_MIME_TYPES.includes(file.type)) {
    return `Niedozwolony typ pliku: ${file.type}`;
  }
  return null;
}

function formatFileSize(bytes: number): string {
  return `${(bytes / BYTES_PER_MB).toFixed(2)} MB`;
}

export interface MultiDocumentDropzoneProps {
  accept?: string;
  maxTotalSizeMB?: number;
  files: File[];
  onChange: (files: File[]) => void;
  onSizeExceeded?: (currentBytes: number, limitBytes: number) => void;
  onFilesRejected?: (rejections: FileRejection[]) => void;
  isDisabled?: boolean;
}

export default function MultiDocumentDropzone({
  accept = '.jpg,.jpeg,.png,.pdf',
  maxTotalSizeMB = 50,
  files,
  onChange,
  onSizeExceeded,
  onFilesRejected,
  isDisabled = false,
}: MultiDocumentDropzoneProps): React.ReactElement {
  const inputId = useId();
  const limitBytes = maxTotalSizeMB * BYTES_PER_MB;

  const totalBytes = files.reduce((sum: number, file: File) => sum + file.size, 0);

  const processNewFiles = useCallback(
    (newFiles: File[]): void => {
      const accepted: File[] = [];
      const rejected: FileRejection[] = [];

      for (const file of newFiles) {
        const reason = validateFile(file);
        if (reason) {
          rejected.push({ fileName: file.name, reason });
        } else {
          accepted.push(file);
        }
      }

      if (rejected.length > 0) {
        onFilesRejected?.(rejected);
      }

      if (accepted.length === 0) {
        return;
      }

      const nextFiles = [...files, ...accepted];
      const nextTotal = nextFiles.reduce((sum: number, file: File) => sum + file.size, 0);
      if (nextTotal > limitBytes) {
        onSizeExceeded?.(nextTotal, limitBytes);
        return;
      }
      onChange(nextFiles);
    },
    [files, limitBytes, onChange, onFilesRejected, onSizeExceeded]
  );

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>): void => {
    const selected = Array.from(event.target.files ?? []);
    if (selected.length === 0) {
      return;
    }
    processNewFiles(selected);
    event.target.value = '';
  };

  const handleDrop = (event: React.DragEvent<HTMLLabelElement>): void => {
    event.preventDefault();
    if (isDisabled) {
      return;
    }
    const dropped = Array.from(event.dataTransfer.files ?? []);
    if (dropped.length === 0) {
      return;
    }
    processNewFiles(dropped);
  };

  const handleDragOver = (event: React.DragEvent<HTMLLabelElement>): void => {
    event.preventDefault();
  };

  const handleRemove = (index: number): void => {
    const nextFiles = files.filter((_, fileIndex: number) => fileIndex !== index);
    onChange(nextFiles);
  };

  const hasFiles = files.length > 0;

  return (
    <VStack spacing={3} align="stretch">
      <Box
        as="label"
        htmlFor={isDisabled ? undefined : inputId}
        display="block"
        border="2px dashed"
        borderColor={hasFiles ? 'green.400' : 'gray.300'}
        borderRadius="md"
        p={6}
        textAlign="center"
        cursor={isDisabled ? 'not-allowed' : 'pointer'}
        opacity={isDisabled ? 0.6 : 1}
        bg={hasFiles ? 'green.50' : 'gray.50'}
        _hover={!isDisabled ? { borderColor: 'primary.400', bg: 'primary.50' } : undefined}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        aria-label="Strefa przesyłania plików — kliknij lub przeciągnij pliki"
      >
        <input
          id={inputId}
          type="file"
          accept={accept}
          multiple
          style={{ display: 'none' }}
          onChange={handleFileChange}
          disabled={isDisabled}
          aria-hidden="true"
          tabIndex={-1}
        />
        <VStack spacing={2}>
          <Icon
            as={FileUp}
            boxSize={8}
            color={hasFiles ? 'green.500' : 'gray.400'}
            aria-hidden="true"
          />
          <Text fontSize="sm" color={hasFiles ? 'green.700' : 'neutral.600'} fontWeight="medium">
            {hasFiles
              ? `${files.length} plik(ów) wybranych`
              : 'Przeciągnij pliki lub kliknij, aby wybrać'}
          </Text>
          <Text fontSize="xs" color="gray.500">
            JPG, PNG, PDF · łącznie maks. {maxTotalSizeMB} MB
          </Text>
          {hasFiles && (
            <Text fontSize="xs" color="gray.500">
              {formatFileSize(totalBytes)} / {maxTotalSizeMB} MB
            </Text>
          )}
        </VStack>
      </Box>

      {hasFiles && (
        <VStack spacing={2} align="stretch" role="list" aria-label="Lista wybranych plików">
          {files.map((file: File, index: number) => (
            <HStack
              key={`${file.name}-${file.size}-${index}`}
              role="listitem"
              p={2}
              borderWidth="1px"
              borderColor="gray.200"
              borderRadius="md"
              justify="space-between"
              bg="white"
            >
              <VStack align="flex-start" spacing={0} flex={1} minW={0}>
                <Text fontSize="sm" fontWeight="medium" noOfLines={1}>
                  {file.name}
                </Text>
                <Text fontSize="xs" color="neutral.600">
                  {formatFileSize(file.size)}
                </Text>
              </VStack>
              <IconButton
                aria-label={`Usuń plik ${file.name}`}
                icon={<X size={16} aria-hidden="true" />}
                size="sm"
                variant="ghost"
                colorScheme="red"
                isDisabled={isDisabled}
                onClick={() => handleRemove(index)}
              />
            </HStack>
          ))}
        </VStack>
      )}
    </VStack>
  );
}
