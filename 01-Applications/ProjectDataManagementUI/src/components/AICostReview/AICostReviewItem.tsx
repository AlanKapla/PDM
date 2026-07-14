import React, { useState } from 'react';
import {
  Alert,
  AlertIcon,
  Badge,
  Box,
  Button,
  Grid,
  GridItem,
  HStack,
  IconButton,
  Image,
  Text,
  Tooltip,
  VStack,
} from '@chakra-ui/react';
import { Check, Eye, X } from 'lucide-react';
import { AICostReviewItemForm } from './AICostReviewItemForm';
import DeleteAlertDialog from '../ui/DeleteAlertDialog';
import { useModal } from '../../hooks/useModal';
import {
  useAcceptPendingAICostImportItem,
  useRejectPendingAICostImportItem,
  useUpdatePendingAICostImportItem,
} from '../../hooks/usePendingAICostImports';
import { useToastNotification } from '../../hooks/useToastNotification';
import { useProjectPermissions } from '../../hooks/useProjectPermissions';
import { useTenantPermissions } from '../../hooks/useTenantPermissions';
import type { AICostImportItemWeb, ParsedCostDto } from '../../types/ai.types';

export interface AICostReviewItemProps {
  tenantId: string;
  projectId: string;
  item: AICostImportItemWeb;
  onActionComplete?: () => void;
}

function getStatusBadge(status: AICostImportItemWeb['status']): React.ReactElement {
  if (status === 'ErrorNeedsReview') {
    return <Badge colorScheme="red">Błąd analizy</Badge>;
  }
  if (status === 'DuplicateDetected') {
    return <Badge colorScheme="orange">Możliwy duplikat</Badge>;
  }
  return <Badge colorScheme="green">Gotowe do akceptacji</Badge>;
}

function createEmptyParsedData(): ParsedCostDto {
  return {
    name: '',
    contractorFound: false,
    categoryFound: false,
    confidence: 0,
  };
}

export function AICostReviewItem({
  tenantId,
  projectId,
  item,
  onActionComplete,
}: AICostReviewItemProps): React.ReactElement {
  const rejectDialog = useModal();
  const { showSuccess, showApiError } = useToastNotification();
  const { canEdit: isProjectAdmin } = useProjectPermissions(projectId);
  const { canEdit: isTenantAdmin } = useTenantPermissions();
  const canQuickAdd = isProjectAdmin || isTenantAdmin;

  const [editedData, setEditedData] = useState<ParsedCostDto>(
    item.parsedData ?? createEmptyParsedData()
  );

  const { mutateAsync: updateItem, isPending: isUpdating } = useUpdatePendingAICostImportItem(
    tenantId,
    projectId
  );
  const { mutateAsync: acceptItem, isPending: isAccepting } = useAcceptPendingAICostImportItem(
    tenantId,
    projectId
  );
  const { mutateAsync: rejectItem, isPending: isRejecting } = useRejectPendingAICostImportItem(
    tenantId,
    projectId
  );

  const isBusy = isUpdating || isAccepting || isRejecting;
  const isDuplicate = item.status === 'DuplicateDetected';
  const isImagePreview =
    item.contentType.startsWith('image/') ||
    /\.(jpg|jpeg|png)$/i.test(item.originalFileName);

  const handleAccept = async (): Promise<void> => {
    if (!editedData.name.trim()) {
      showApiError(new Error('Nazwa kosztu jest wymagana.'));
      return;
    }

    try {
      await updateItem({ itemId: item.id, data: { parsedData: editedData } });
      await acceptItem(item.id);
      showSuccess('Koszt zaakceptowany', 'Dokument został zapisany jako koszt.');
      onActionComplete?.();
    } catch (err) {
      showApiError(err);
    }
  };

  const handleReject = async (): Promise<void> => {
    try {
      await rejectItem(item.id);
      showSuccess('Pozycja odrzucona');
      rejectDialog.onClose();
      onActionComplete?.();
    } catch (err) {
      showApiError(err);
    }
  };

  const handleOpenFullSizePreview = (): void => {
    if (!item.previewUrl) {
      return;
    }
    window.open(item.previewUrl, '_blank', 'noopener,noreferrer');
  };

  return (
    <>
      <Box
        borderWidth="1px"
        borderColor="gray.200"
        borderRadius="lg"
        overflow="hidden"
        bg="white"
      >
        <HStack
          px={4}
          py={3}
          borderBottomWidth="1px"
          borderColor="gray.100"
          justify="space-between"
          flexWrap="wrap"
          gap={2}
        >
          <VStack align="flex-start" spacing={0}>
            <Text fontWeight="semibold" fontSize="sm" noOfLines={1}>
              {item.originalFileName}
            </Text>
            <Text fontSize="xs" color="neutral.600">
              {(item.fileSizeBytes / 1024 / 1024).toFixed(2)} MB
            </Text>
          </VStack>
          {getStatusBadge(item.status)}
        </HStack>

        {item.status === 'ErrorNeedsReview' && item.lastError && (
          <Alert status="error" role="alert" borderRadius={0}>
            <AlertIcon />
            {item.lastError}
          </Alert>
        )}

        {isDuplicate && (
          <Alert status="warning" role="alert" borderRadius={0}>
            <AlertIcon />
            Ten dokument wygląda na duplikat już zapisanego kosztu lub innego importu.
            Możesz go odrzucić lub zaakceptować mimo ostrzeżenia.
          </Alert>
        )}

        <Grid templateColumns={{ base: '1fr', lg: '1fr 1fr' }} gap={0}>
          <GridItem
            p={4}
            borderRightWidth={{ base: 0, lg: '1px' }}
            borderBottomWidth={{ base: '1px', lg: 0 }}
            borderColor="gray.100"
            bg="gray.50"
            minH="280px"
          >
            <HStack justify="space-between" mb={3}>
              <Text fontSize="sm" fontWeight="medium" color="neutral.700">
                Podgląd dokumentu
              </Text>
              {item.previewUrl && isImagePreview && (
                <Tooltip label="Otwórz w pełnym rozmiarze">
                  <IconButton
                    aria-label={`Otwórz dokument w pełnym rozmiarze: ${item.originalFileName}`}
                    icon={<Eye size={16} aria-hidden="true" />}
                    size="sm"
                    variant="ghost"
                    colorScheme="level2"
                    onClick={handleOpenFullSizePreview}
                  />
                </Tooltip>
              )}
            </HStack>
            {item.previewUrl && isImagePreview ? (
              <Image
                src={item.previewUrl}
                alt={`Podgląd dokumentu ${item.originalFileName}`}
                maxH="400px"
                objectFit="contain"
                mx="auto"
                borderRadius="md"
              />
            ) : (
              <Box
                p={6}
                textAlign="center"
                borderWidth="1px"
                borderStyle="dashed"
                borderColor="gray.300"
                borderRadius="md"
              >
                <Text fontSize="sm" color="neutral.600">
                  Podgląd niedostępny
                </Text>
              </Box>
            )}
          </GridItem>

          <GridItem p={4}>
            <Text fontSize="sm" fontWeight="medium" mb={3} color="neutral.700">
              Dane z analizy AI
            </Text>
            <AICostReviewItemForm
              tenantId={tenantId}
              projectId={projectId}
              costDocumentType={item.costDocumentType}
              parsedData={editedData}
              onChange={setEditedData}
              canQuickAdd={canQuickAdd}
              isDisabled={isBusy}
            />
          </GridItem>
        </Grid>

        <HStack px={4} py={3} borderTopWidth="1px" borderColor="gray.100" justify="flex-end" spacing={3}>
          <Button
            leftIcon={<X size={16} aria-hidden="true" />}
            variant="outline"
            colorScheme="red"
            size="sm"
            onClick={rejectDialog.onOpen}
            isDisabled={isBusy}
          >
            Odrzuć
          </Button>
          <Button
            leftIcon={<Check size={16} aria-hidden="true" />}
            colorScheme="green"
            size="sm"
            onClick={handleAccept}
            isLoading={isBusy}
            loadingText="Zapisuję..."
            isDisabled={isBusy || !editedData.name.trim()}
          >
            {isDuplicate ? 'Akceptuj mimo duplikatu' : 'Akceptuj'}
          </Button>
        </HStack>
      </Box>

      <DeleteAlertDialog
        isOpen={rejectDialog.isOpen}
        onClose={rejectDialog.onClose}
        onConfirm={handleReject}
        title="Odrzucić pozycję?"
        description="Czy na pewno chcesz odrzucić tę pozycję? Tej operacji nie można cofnąć."
        confirmLabel="Odrzuć"
        isLoading={isRejecting}
      />
    </>
  );
}
