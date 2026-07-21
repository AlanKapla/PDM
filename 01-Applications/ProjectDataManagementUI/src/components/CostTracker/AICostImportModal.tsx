import React, { useState } from 'react';
import {
  VStack,
  HStack,
  Button,
  Text,
  Divider,
} from '@chakra-ui/react';
import AppModal from '../ui/AppModal';
import MultiDocumentDropzone from '../ui/MultiDocumentDropzone';
import type { FileRejection } from '../ui/MultiDocumentDropzone';
import { useAICostDocumentParser } from '../../hooks/useAICostDocumentParser';
import { useAICostImportBatch } from '../../hooks/useAICostImportBatch';
import { useToastNotification } from '../../hooks/useToastNotification';
import type {
  AICostImportRejectedFile,
  CostDocumentType,
  ParsedCostDto,
  TrackedCostContext,
} from '../../types/ai.types';

interface AICostImportModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  costType: CostDocumentType;
  /** Wywoływane po pomyślnym przeanalizowaniu pojedynczego dokumentu */
  onParsed: (data: ParsedCostDto, file: File) => void;
  /** Kontekst trackera przekazywany przy batch upload (TrackedCost) */
  trackedCostContext?: TrackedCostContext;
}

function formatRejectedFilesDescription(
  rejected: ReadonlyArray<AICostImportRejectedFile | FileRejection>
): string {
  return rejected
    .map((item: AICostImportRejectedFile | FileRejection) => `${item.fileName}: ${item.reason}`)
    .join('\n');
}

export function AICostImportModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  costType,
  onParsed,
  trackedCostContext,
}: AICostImportModalProps): React.ReactElement {
  const [files, setFiles] = useState<File[]>([]);

  const { showApiError, showInfo, showError } = useToastNotification();

  const { mutateAsync: parseDocument, isPending: isParsing } = useAICostDocumentParser({
    tenantId,
    projectId,
  });

  const { mutateAsync: submitBatch, isPending: isSubmittingBatch } = useAICostImportBatch({
    tenantId,
    projectId,
  });

  const isBusy = isParsing || isSubmittingBatch;
  const isBatch = files.length > 1;

  const handleClose = (): void => {
    setFiles([]);
    onClose();
  };

  const handleSizeExceeded = (currentBytes: number, limitBytes: number): void => {
    const currentMb = (currentBytes / (1024 * 1024)).toFixed(1);
    const limitMb = (limitBytes / (1024 * 1024)).toFixed(0);
    showError(
      'Przekroczono limit rozmiaru',
      `Wybrane pliki ważą ${currentMb} MB. Maksymalny łączny rozmiar to ${limitMb} MB.`
    );
  };

  const handleFilesRejected = (rejections: FileRejection[]): void => {
    showError(
      'Nieobsługiwane pliki',
      formatRejectedFilesDescription(rejections)
    );
  };

  const handleAnalyze = async (): Promise<void> => {
    if (files.length === 0) {
      return;
    }

    if (files.length === 1) {
      try {
        const result = await parseDocument({ file: files[0], costType });
        onParsed(result, files[0]);
        handleClose();
      } catch (err) {
        showApiError(err);
      }
      return;
    }

    try {
      const result = await submitBatch({
        files,
        costType,
        trackedCostContext,
      });

      const rejectedFiles: AICostImportRejectedFile[] = result.rejectedFiles ?? [];
      const submittedCount = files.length;
      const acceptedCount = result.totalFiles;

      if (rejectedFiles.length > 0) {
        showError(
          'Niektóre pliki odrzucone',
          formatRejectedFilesDescription(rejectedFiles)
        );
      }

      if (acceptedCount > 0 && rejectedFiles.length > 0) {
        showInfo(
          'Analiza w tle',
          `Do analizy przyjęto ${acceptedCount} z ${submittedCount} dokumentów. Otrzymasz powiadomienie po zakończeniu przetwarzania zaakceptowanych plików.`
        );
      } else if (acceptedCount > 0) {
        showInfo(
          'Analiza w tle',
          'Dokumenty są analizowane w tle. Otrzymasz powiadomienie po zakończeniu.'
        );
      } else if (rejectedFiles.length > 0) {
        showInfo(
          'Brak plików do analizy',
          'Żaden z przesłanych plików nie został zaakceptowany.'
        );
      }

      handleClose();
    } catch (err) {
      showApiError(err);
    }
  };

  const analyzeLabel = isBatch
    ? `Wyślij ${files.length} dokumentów`
    : 'Analizuj dokument';

  return (
    <AppModal isOpen={isOpen} onClose={handleClose} title="Importuj koszt z dokumentu" hideFooter>
      <VStack spacing={4} align="stretch">
        <Text fontSize="sm" color="neutral.600">
          JPG, PNG lub PDF. Jeden plik — natychmiastowa analiza. Wiele plików — analiza w tle
          (łącznie do 50 MB).
        </Text>

        <MultiDocumentDropzone
          files={files}
          onChange={setFiles}
          onSizeExceeded={handleSizeExceeded}
          onFilesRejected={handleFilesRejected}
          isDisabled={isBusy}
          maxTotalSizeMB={50}
        />

        <Divider />

        <HStack justify="flex-end">
          <Button variant="ghost" onClick={handleClose} isDisabled={isBusy}>
            Anuluj
          </Button>
          <Button
            colorScheme="primary"
            onClick={handleAnalyze}
            isDisabled={files.length === 0}
            isLoading={isBusy}
            loadingText={isBatch ? 'Wysyłam...' : 'Analizuję...'}
          >
            {analyzeLabel}
          </Button>
        </HStack>
      </VStack>
    </AppModal>
  );
}
