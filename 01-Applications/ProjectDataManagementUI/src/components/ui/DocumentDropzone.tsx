import React, { useId } from 'react';
import { Box, Text, Icon, VStack } from '@chakra-ui/react';
import { FileUp } from 'lucide-react';

interface DocumentDropzoneProps {
  accept?: string;
  maxSizeMB?: number;
  value: File | null;
  onChange: (file: File | null) => void;
  isDisabled?: boolean;
}

export function DocumentDropzone({
  accept = '.jpg,.jpeg,.png,.pdf',
  maxSizeMB = 20,
  value,
  onChange,
  isDisabled = false,
}: DocumentDropzoneProps): React.ReactElement {
  const inputId = useId();

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    if (file && file.size > maxSizeMB * 1024 * 1024) {
      onChange(null);
      return;
    }
    onChange(file);
    e.target.value = '';
  };

  const handleDrop = (e: React.DragEvent<HTMLLabelElement>) => {
    e.preventDefault();
    if (isDisabled) return;
    const file = e.dataTransfer.files?.[0] ?? null;
    if (file) {
      onChange(file);
    }
  };

  const handleDragOver = (e: React.DragEvent<HTMLLabelElement>) => {
    e.preventDefault();
  };

  return (
    <Box
      as="label"
      htmlFor={isDisabled ? undefined : inputId}
      display="block"
      border="2px dashed"
      borderColor={value ? 'green.400' : 'gray.300'}
      borderRadius="md"
      p={6}
      textAlign="center"
      cursor={isDisabled ? 'not-allowed' : 'pointer'}
      opacity={isDisabled ? 0.6 : 1}
      bg={value ? 'green.50' : 'gray.50'}
      _hover={!isDisabled ? { borderColor: 'primary.400', bg: 'primary.50' } : {}}
      onDrop={handleDrop}
      onDragOver={handleDragOver}
      aria-label="Strefa przesyłania pliku — kliknij lub przeciągnij plik"
    >
      <input
        id={inputId}
        type="file"
        accept={accept}
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
          color={value ? 'green.500' : 'gray.400'}
          aria-hidden="true"
        />
        <Text fontSize="sm" color={value ? 'green.700' : 'gray.600'} fontWeight="medium">
          {value ? value.name : 'Przeciągnij plik lub kliknij, aby wybrać'}
        </Text>
        <Text fontSize="xs" color="gray.400">
          JPG, PNG, PDF · maks. {maxSizeMB} MB
        </Text>
        {value && (
          <Text fontSize="xs" color="gray.400">
            {(value.size / 1024 / 1024).toFixed(2)} MB
          </Text>
        )}
      </VStack>
    </Box>
  );
}
