import { useId, useState } from 'react';
import {
  Box,
  Text,
  Icon,
  VStack,
  HStack,
  IconButton,
} from '@chakra-ui/react';
import { FileUp, X } from 'lucide-react';
import { formatFileSize } from '../../utils/formatters';

const ALLOWED_MIME_TYPES = new Set(['application/pdf', 'image/jpeg']);

export interface MultiDocumentDropzoneProps {
  files: File[];
  onFilesChange: (files: File[]) => void;
  accept?: string;
  maxSizeBytes?: number;
  isDisabled?: boolean;
  errorMessage?: string;
}

function validateFile(file: File, maxSizeBytes: number): string | null {
  if (!ALLOWED_MIME_TYPES.has(file.type)) {
    return `${file.name}: dozwolone są tylko pliki PDF i JPG.`;
  }
  if (file.size > maxSizeBytes) {
    return `${file.name}: maksymalny rozmiar to ${formatFileSize(maxSizeBytes)}.`;
  }
  return null;
}

export function MultiDocumentDropzone({
  files,
  onFilesChange,
  accept = '.pdf,.jpg,.jpeg',
  maxSizeBytes = 52_428_800,
  isDisabled = false,
  errorMessage,
}: MultiDocumentDropzoneProps): React.ReactElement {
  const inputId = useId();
  const [validationErrors, setValidationErrors] = useState<string[]>([]);

  const addFiles = (incoming: FileList | File[]): void => {
    if (isDisabled) {
      return;
    }

    const nextFiles = [...files];
    const errors: string[] = [];

    Array.from(incoming).forEach((file) => {
      const error = validateFile(file, maxSizeBytes);
      if (error) {
        errors.push(error);
        return;
      }
      const isDuplicate = nextFiles.some(
        (existing) => existing.name === file.name && existing.size === file.size
      );
      if (!isDuplicate) {
        nextFiles.push(file);
      }
    });

    setValidationErrors(errors);
    onFilesChange(nextFiles);
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>): void => {
    if (e.target.files) {
      addFiles(e.target.files);
    }
    e.target.value = '';
  };

  const handleDrop = (e: React.DragEvent<HTMLLabelElement>): void => {
    e.preventDefault();
    if (isDisabled || !e.dataTransfer.files.length) {
      return;
    }
    addFiles(e.dataTransfer.files);
  };

  const handleDragOver = (e: React.DragEvent<HTMLLabelElement>): void => {
    e.preventDefault();
  };

  const handleRemove = (index: number): void => {
    const nextFiles = files.filter((_, i) => i !== index);
    onFilesChange(nextFiles);
    setValidationErrors([]);
  };

  const displayError = errorMessage ?? (validationErrors.length > 0 ? validationErrors.join(' ') : undefined);

  return (
    <VStack spacing={3} align="stretch">
      <Box
        as="label"
        htmlFor={isDisabled ? undefined : inputId}
        display="block"
        border="2px dashed"
        borderColor={files.length > 0 ? 'green.400' : 'gray.300'}
        borderRadius="md"
        p={6}
        textAlign="center"
        cursor={isDisabled ? 'not-allowed' : 'pointer'}
        opacity={isDisabled ? 0.6 : 1}
        bg={files.length > 0 ? 'green.50' : 'gray.50'}
        _hover={!isDisabled ? { borderColor: 'primary.400', bg: 'primary.50' } : {}}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        aria-label="Strefa przesyłania plików PDF lub JPG — kliknij lub przeciągnij pliki"
      >
        <input
          id={inputId}
          type="file"
          accept={accept}
          multiple
          style={{ display: 'none' }}
          onChange={handleFileChange}
          disabled={isDisabled}
          aria-label="Wybierz pliki PDF lub JPG"
          tabIndex={-1}
        />
        <VStack spacing={2}>
          <Icon
            as={FileUp}
            boxSize={8}
            color={files.length > 0 ? 'green.500' : 'gray.400'}
            aria-hidden="true"
          />
          <Text fontSize="sm" color={files.length > 0 ? 'green.700' : 'neutral.600'} fontWeight="medium">
            {files.length > 0
              ? `Wybrano ${files.length} plik(ów)`
              : 'Przeciągnij pliki lub kliknij, aby wybrać'}
          </Text>
          <Text fontSize="xs" color="neutral.600">
            PDF, JPG · maks. {formatFileSize(maxSizeBytes)} na plik
          </Text>
        </VStack>
      </Box>

      {displayError && (
        <Text fontSize="sm" color="red.600" role="alert">
          {displayError}
        </Text>
      )}

      {files.length > 0 && (
        <VStack spacing={2} align="stretch">
          {files.map((file, index) => (
            <HStack
              key={`${file.name}-${file.size}-${index}`}
              justify="space-between"
              p={2}
              borderWidth="1px"
              borderRadius="md"
              borderColor="gray.200"
            >
              <Box flex={1} minW={0}>
                <Text fontSize="sm" fontWeight="medium" noOfLines={1}>
                  {file.name}
                </Text>
                <Text fontSize="xs" color="neutral.600">
                  {formatFileSize(file.size)}
                </Text>
              </Box>
              <IconButton
                aria-label={`Usuń plik ${file.name}`}
                icon={<X size={16} aria-hidden="true" />}
                size="sm"
                variant="ghost"
                onClick={() => handleRemove(index)}
                isDisabled={isDisabled}
              />
            </HStack>
          ))}
        </VStack>
      )}
    </VStack>
  );
}
