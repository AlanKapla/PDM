import React, { useContext, useMemo } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Heading,
  HStack,
  Icon,
  Text,
  VStack,
} from '@chakra-ui/react';
import { ArrowLeft, CheckCheck, Sparkles } from 'lucide-react';
import MainLayout from '../layout/MainLayout';
import { EmptyState, LoadingSpinner } from '../components/common';
import { AICostReviewItem } from '../components/AICostReview/AICostReviewItem';
import { AuthContext } from '../context/AuthContext';
import {
  useAcceptPendingAICostImportItem,
  usePendingAICostImportItems,
  useUpdatePendingAICostImportItem,
} from '../hooks/usePendingAICostImports';
import { useProjectDetails } from '../hooks/queries';
import { useToastNotification } from '../hooks/useToastNotification';
import { getApiErrorMessage } from '../utils/apiErrorUtils';
import {
  detectAICostReviewContext,
  getAICostReviewBackLabel,
  getAICostReviewBackPath,
  getCostDocumentTypeForContext,
} from '../utils/aiCostReviewPaths';

export default function AICostReviewPage(): React.ReactElement {
  const { projectId } = useParams<{ projectId: string }>();
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const context = detectAICostReviewContext(pathname);
  const costDocumentType = getCostDocumentTypeForContext(context);
  const { user } = useContext(AuthContext);
  const tenantId = user?.activeTenantId;
  const { showSuccess, showApiError } = useToastNotification();

  const { data: project } = useProjectDetails(tenantId ?? undefined, projectId ?? undefined);
  const {
    data: allItems = [],
    isLoading,
    error,
    refetch,
  } = usePendingAICostImportItems(tenantId ?? undefined, projectId ?? undefined);

  const items = useMemo(
    () => allItems.filter((item) => item.costDocumentType === costDocumentType),
    [allItems, costDocumentType]
  );

  const { mutateAsync: updateItem } = useUpdatePendingAICostImportItem(
    tenantId ?? '',
    projectId ?? ''
  );
  const { mutateAsync: acceptItem, isPending: isAcceptingAll } =
    useAcceptPendingAICostImportItem(tenantId ?? '', projectId ?? '');

  const reviewableCount = items.length;
  const pendingCount = items.filter((item) => item.status === 'Pending').length;
  const errorCount = items.filter((item) => item.status === 'ErrorNeedsReview').length;
  const duplicateCount = items.filter((item) => item.status === 'DuplicateDetected').length;
  const acceptAllCount = pendingCount + errorCount;

  const handleAcceptAll = async (): Promise<void> => {
    if (!tenantId || !projectId) {
      return;
    }

    const pendingItems = items.filter((item) => item.status === 'Pending');
    if (pendingItems.length === 0) {
      return;
    }

    try {
      for (const item of pendingItems) {
        if (item.parsedData) {
          await updateItem({ itemId: item.id, data: { parsedData: item.parsedData } });
        }
        await acceptItem(item.id);
      }
      showSuccess(
        'Wszystkie pozycje zaakceptowane',
        'Dokumenty zostały zapisane jako koszty.'
      );
      refetch();
    } catch (err) {
      showApiError(err);
    }
  };

  if (!tenantId || !projectId) {
    return (
      <MainLayout>
        <Box p={8}>
          <Text color="neutral.600">Brak wymaganego kontekstu (tenant lub projekt).</Text>
        </Box>
      </MainLayout>
    );
  }

  if (isLoading) {
    return (
      <MainLayout>
        <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie pozycji do weryfikacji..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
        <Button
          leftIcon={<ArrowLeft size={16} aria-hidden="true" />}
          variant="ghost"
          size="sm"
          mb={4}
          onClick={() => navigate(getAICostReviewBackPath(projectId, context))}
        >
          {getAICostReviewBackLabel(context)}
        </Button>

        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={Sparkles} boxSize={8} color="purple.500" aria-hidden="true" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Weryfikacja kosztów AI</Heading>
              {project && (
                <Text fontSize="sm" color="neutral.600">
                  {project.name}
                </Text>
              )}
            </VStack>
          </HStack>

          {acceptAllCount > 0 && (
            <Button
              leftIcon={<CheckCheck size={18} aria-hidden="true" />}
              colorScheme="green"
              size="sm"
              onClick={handleAcceptAll}
              isLoading={isAcceptingAll}
              loadingText="Akceptuję..."
            >
              Zaakceptuj wszystkie ({acceptAllCount})
            </Button>
          )}
        </HStack>

        {error && (
          <Box mb={4} p={4} bg="red.50" borderRadius="md" role="alert">
            <Text color="red.700" fontSize="sm">
              {getApiErrorMessage(error)}
            </Text>
          </Box>
        )}

        <HStack spacing={4} mb={6} flexWrap="wrap">
          <Text fontSize="sm" color="neutral.600">
            Gotowe: <strong>{pendingCount}</strong>
          </Text>
          <Text fontSize="sm" color="neutral.600">
            Wymagają ręcznej weryfikacji: <strong>{errorCount}</strong>
          </Text>
          {duplicateCount > 0 && (
            <Text fontSize="sm" color="neutral.600">
              Możliwe duplikaty: <strong>{duplicateCount}</strong>
            </Text>
          )}
        </HStack>

        {reviewableCount === 0 ? (
          <EmptyState
            icon={Sparkles}
            title="Brak pozycji do weryfikacji"
            description="Wszystkie dokumenty zostały już przetworzone lub nie ma oczekujących importów AI w tej sekcji."
          />
        ) : (
          <VStack spacing={6} align="stretch">
            {items.map((item) => (
              <AICostReviewItem
                key={item.id}
                tenantId={tenantId}
                projectId={projectId}
                item={item}
                onActionComplete={() => refetch()}
              />
            ))}
          </VStack>
        )}
      </Box>
    </MainLayout>
  );
}
