import { useState } from 'react';
import {
  VStack,
  HStack,
  Button,
  Text,
  Divider,
} from '@chakra-ui/react';
import AppModal from '../ui/AppModal';
import { DocumentDropzone } from '../ui/DocumentDropzone';
import { useAICostDocumentParser } from '../../hooks/useAICostDocumentParser';
import { useToastNotification } from '../../hooks/useToastNotification';
import { handleApiError } from '../../utils/handleApiError';
import type { CostDocumentType, ParsedCostDto } from '../../types/ai.types';

interface AICostImportModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  costType: CostDocumentType;
  /** Wywoływane po pomyślnym przeanalizowaniu dokumentu — przekazuje dane do formularza */
  onParsed: (data: ParsedCostDto, file: File) => void;
}

export function AICostImportModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  costType,
  onParsed,
}: AICostImportModalProps) {
  const [file, setFile] = useState<File | null>(null);

  const {showError, showApiError } = useToastNotification();

  const { mutateAsync: parseDocument, isPending: isParsing } = useAICostDocumentParser({
    tenantId,
    projectId,
  });

  const handleClose = () => {
    setFile(null);
    onClose();
  };

  const handleAnalyze = async () => {
    if (!file) return;
    try {
      const result = await parseDocument({ file, costType });
      onParsed(result, file);
      handleClose();
    } catch (err) {
      showApiError(err);
    }
  };

  return (
    <AppModal isOpen={isOpen} onClose={handleClose} title="Importuj koszt z dokumentu" hideFooter>
      <VStack spacing={4} align="stretch">
        <Text fontSize="sm" color="gray.600">
          Załaduj fakturę lub rachunek w formacie JPG lub PNG. AI wypełni formularz kosztu danymi z dokumentu.
        </Text>
        <DocumentDropzone value={file} onChange={setFile} isDisabled={isParsing} />
        <Divider />
        <HStack justify="flex-end">
          <Button variant="ghost" onClick={handleClose} isDisabled={isParsing}>
            Anuluj
          </Button>
          <Button
            colorScheme="primary"
            onClick={handleAnalyze}
            isDisabled={!file}
            isLoading={isParsing}
            loadingText="Analizuję..."
          >
            Analizuj dokument
          </Button>
        </HStack>
      </VStack>
    </AppModal>
  );
}
