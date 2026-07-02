import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  AlertIcon,
  Box,
  Button,
  Heading,
  HStack,
  Icon,
  Text,
  useDisclosure,
  VStack,
} from '@chakra-ui/react';
import { RefreshCw, ScanLine } from 'lucide-react';
import MainLayout from '../layout/MainLayout';
import { BackToProjectButton, LoadingSpinner } from '../components/common';
import ConfirmDialog from '../components/common/ConfirmDialog';
import { TechnicalDocumentationStatusBadge } from '../components/technicalDocumentation/TechnicalDocumentationStatusBadge';
import { TechnicalDocumentationProcessingState } from '../components/technicalDocumentation/TechnicalDocumentationProcessingState';
import { TechnicalDocumentationFileList } from '../components/technicalDocumentation/TechnicalDocumentationFileList';
import { TechnicalDocumentationDetailsView } from '../components/technicalDocumentation/TechnicalDocumentationDetailsView';
import { useAuth } from '../context/AuthContext';
import { useProjectPermissions } from '../hooks/useProjectPermissions';
import {
  useTechnicalDocumentationDetails,
  useRetryTechnicalDocumentation,
} from '../hooks/queries';
import { useToastNotification } from '../hooks/useToastNotification';
import {
  TechnicalDocumentationStatus,
} from '../types/technicalDocumentation.types';
import { formatDate } from '../utils/formatters';

export default function ProjectTechnicalDocumentationDetailsPage(): React.ReactElement {
  const { projectId, docId } = useParams<{ projectId: string; docId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const permissions = useProjectPermissions(projectId);
  const { showInfo, showApiError } = useToastNotification();
  const { isOpen: isRetryOpen, onOpen: onRetryOpen, onClose: onRetryClose } = useDisclosure();
  const [isRetrying, setIsRetrying] = useState(false);

  const tenantId = user?.activeTenantId;
  const canView = permissions.canViewTechnicalDocumentation;

  const {
    data: documentation,
    isLoading,
    error,
  } = useTechnicalDocumentationDetails(
    tenantId ?? undefined,
    projectId,
    docId
  );

  const { mutateAsync: retryDocumentation } = useRetryTechnicalDocumentation(
    tenantId ?? '',
    projectId ?? ''
  );

  const handleRetryConfirm = async (): Promise<void> => {
    if (!docId) {
      return;
    }

    setIsRetrying(true);
    try {
      await retryDocumentation(docId);
      showInfo(
        'Przetwarzanie uruchomione',
        'Przetwarzanie zostało ponownie uruchomione.'
      );
      onRetryClose();
    } catch (retryError) {
      showApiError(retryError);
    } finally {
      setIsRetrying(false);
    }
  };

  if (!canView && !permissions.loading) {
    return (
      <MainLayout>
        <Box p={{ base: 3, sm: 4, md: 8 }} textAlign="center">
          <Text color="neutral.700">
            Nie masz uprawnień do przeglądania dokumentacji technicznej w tym projekcie.
          </Text>
          <Button mt={4} variant="outline" onClick={() => navigate(`/projects/${projectId}`)}>
            Wróć do projektu
          </Button>
        </Box>
      </MainLayout>
    );
  }

  if (isLoading) {
    return (
      <MainLayout>
        <LoadingSpinner message="Ładowanie szczegółów dokumentacji..." />
      </MainLayout>
    );
  }

  if (error || !documentation) {
    return (
      <MainLayout>
        <Box p={{ base: 3, sm: 4, md: 8 }} textAlign="center">
          <Text color="neutral.700">Nie udało się załadować dokumentacji.</Text>
          <Button
            mt={4}
            variant="outline"
            onClick={() => navigate(`/projects/${projectId}/technical-documentation`)}
          >
            Wróć do listy
          </Button>
        </Box>
      </MainLayout>
    );
  }

  const isPendingOrProcessing =
    documentation.status === TechnicalDocumentationStatus.Pending ||
    documentation.status === TechnicalDocumentationStatus.Processing;

  const isCompleted =
    documentation.status === TechnicalDocumentationStatus.Completed ||
    documentation.status === TechnicalDocumentationStatus.CompletedWithWarnings;
  const isFailed = documentation.status === TechnicalDocumentationStatus.Failed;
  const errorMessageId = 'technical-documentation-error-message';

  return (
    <MainLayout>
      <VStack spacing={6} align="stretch" p={{ base: 3, sm: 4, md: 8 }}>
        <HStack spacing={3} flexWrap="wrap">
          <BackToProjectButton projectId={projectId!} />
          <Icon as={ScanLine} boxSize={7} color="teal.600" aria-hidden="true" />
          <VStack align="flex-start" spacing={1} flex={1}>
            <Heading size="lg">{documentation.name}</Heading>
            {documentation.description && (
              <Text fontSize="sm" color="neutral.600">
                {documentation.description}
              </Text>
            )}
            <HStack spacing={4} flexWrap="wrap">
              <TechnicalDocumentationStatusBadge status={documentation.status} />
              <Text fontSize="sm" color="neutral.600">
                Utworzono: {formatDate(documentation.createdAt)}
              </Text>
              {documentation.completedAt && (
                <Text fontSize="sm" color="neutral.600">
                  Ukończono: {formatDate(documentation.completedAt)}
                </Text>
              )}
            </HStack>
          </VStack>
        </HStack>

        {isPendingOrProcessing && <TechnicalDocumentationProcessingState />}

        {isFailed && (
          <Alert status="error" borderRadius="md">
            <AlertIcon />
            <Box flex={1}>
              <Text fontWeight="semibold">Przetwarzanie zakończyło się błędem</Text>
              <Text id={errorMessageId} fontSize="sm" mt={1}>
                {documentation.errorMessage ?? 'Nieznany błąd przetwarzania.'}
              </Text>
            </Box>
            {permissions.canWriteTechnicalDocumentation && (
              <Button
                size="sm"
                colorScheme="primary"
                leftIcon={<RefreshCw size={16} aria-hidden="true" />}
                onClick={onRetryOpen}
                aria-describedby={documentation.errorMessage ? errorMessageId : undefined}
              >
                Ponów przetwarzanie
              </Button>
            )}
          </Alert>
        )}

        {isCompleted && documentation.details && (
          <TechnicalDocumentationDetailsView details={documentation.details} />
        )}

        <Box>
          <Heading size="sm" mb={3}>
            Pliki źródłowe
          </Heading>
          <TechnicalDocumentationFileList files={documentation.files} />
        </Box>
      </VStack>

      <ConfirmDialog
        isOpen={isRetryOpen}
        onClose={onRetryClose}
        onConfirm={handleRetryConfirm}
        title="Ponowić przetwarzanie?"
        message="Ponowne przetwarzanie uruchomi pipeline AI na istniejących plikach. Operacja może generować koszty i potrwać kilka minut."
        confirmText="Ponów przetwarzanie"
        colorScheme="primary"
        isLoading={isRetrying}
      />
    </MainLayout>
  );
}
