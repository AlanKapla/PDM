import { Alert, AlertIcon, HStack, Spinner, Text } from '@chakra-ui/react';

export function TechnicalDocumentationProcessingState(): React.ReactElement {
  return (
    <Alert status="info" borderRadius="md" role="status" aria-live="polite">
      <AlertIcon />
      <HStack spacing={3}>
        <Spinner size="sm" aria-hidden="true" />
        <Text color="neutral.700">Trwa przetwarzanie dokumentacji…</Text>
      </HStack>
    </Alert>
  );
}
