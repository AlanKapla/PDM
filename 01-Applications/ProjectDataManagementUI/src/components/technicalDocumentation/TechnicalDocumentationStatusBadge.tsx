import { Badge, HStack, Spinner } from '@chakra-ui/react';
import {
  TechnicalDocumentationStatus,
  type TechnicalDocumentationStatus as TechnicalDocumentationStatusType,
} from '../../types/technicalDocumentation.types';

export interface TechnicalDocumentationStatusBadgeProps {
  status: TechnicalDocumentationStatusType;
  showSpinner?: boolean;
}

const STATUS_CONFIG: Record<
  TechnicalDocumentationStatusType,
  { label: string; color: string; bg: string }
> = {
  [TechnicalDocumentationStatus.Pending]: {
    label: 'Oczekuje',
    color: 'yellow.800',
    bg: 'yellow.100',
  },
  [TechnicalDocumentationStatus.Processing]: {
    label: 'Przetwarzanie',
    color: 'blue.800',
    bg: 'blue.100',
  },
  [TechnicalDocumentationStatus.Completed]: {
    label: 'Ukończono',
    color: 'green.800',
    bg: 'green.100',
  },
  [TechnicalDocumentationStatus.CompletedWithWarnings]: {
    label: 'Ukończono z ostrzeżeniami',
    color: 'orange.800',
    bg: 'orange.100',
  },
  [TechnicalDocumentationStatus.Failed]: {
    label: 'Błąd',
    color: 'red.800',
    bg: 'red.100',
  },
};

export function TechnicalDocumentationStatusBadge({
  status,
  showSpinner = true,
}: TechnicalDocumentationStatusBadgeProps): React.ReactElement {
  const config = STATUS_CONFIG[status];
  const isProcessing = status === TechnicalDocumentationStatus.Processing;

  return (
    <HStack
      spacing={2}
      role={isProcessing ? 'status' : undefined}
      aria-live={isProcessing ? 'polite' : undefined}
    >
      <Badge
        color={config.color}
        bg={config.bg}
        borderRadius="md"
        px={2}
        py={0.5}
        fontSize="xs"
        fontWeight="semibold"
      >
        {config.label}
      </Badge>
      {isProcessing && showSpinner && (
        <Spinner size="xs" color="blue.600" aria-hidden="true" />
      )}
    </HStack>
  );
}
