import React, { useState } from 'react';
import {
  VStack,
  FormControl,
  FormLabel,
  Input,
  Textarea,
  SimpleGrid,
  Alert,
  AlertIcon,
  Checkbox,
  Text,
} from '@chakra-ui/react';
import AppModal from '../../../components/ui/AppModal';
import type { TrackedCostWeb, CreateTrackedCostRequest, UpdateTrackedCostRequest } from '../types/projectDashboard.types';
import { WorkItemType } from '../types/projectDashboard.types';
import { useTrackedCostMutations } from '../hooks/useTrackedCostMutations';

export interface TrackedCostModalProps {
  tenantId: string;
  projectId: string;
  mode: 'create' | 'edit';
  /** null = koszt dodatkowy projektu (bez żadnego powiązania) */
  workItemType?: WorkItemType | null;
  costEstimateItemId?: string | null;
  workScheduleStageWorkId?: string | null;
  cost?: TrackedCostWeb;
  onSuccess: (cost: TrackedCostWeb) => void;
  onClose: () => void;
}

/** Modal do dodawania i edycji kosztów śledzonych. */
export function TrackedCostModal({
  tenantId,
  projectId,
  mode,
  workItemType,
  costEstimateItemId,
  workScheduleStageWorkId,
  cost,
  onSuccess,
  onClose,
}: TrackedCostModalProps): React.ReactElement {
  const { createCost, updateCost, isLoading, error } = useTrackedCostMutations({
    tenantId,
    projectId,
  });

  const [name, setName] = useState(cost?.name ?? '');
  const [description, setDescription] = useState(cost?.description ?? '');
  const [net, setNet] = useState(cost?.net != null ? String(cost.net) : '');
  const [gross, setGross] = useState(cost?.gross != null ? String(cost.gross) : '');
  const [contractor, setContractor] = useState(cost?.contractor ?? '');
  const [date, setDate] = useState(cost?.date ? cost.date.substring(0, 10) : '');
  const [number, setNumber] = useState(cost?.number ?? '');
  const [newFiles, setNewFiles] = useState<File[]>([]);
  const [existingAttachmentIds, setExistingAttachmentIds] = useState<string[]>(
    cost?.attachments.map((a) => a.id) ?? []
  );

  const toggleExistingAttachment = (attachmentId: string) => {
    setExistingAttachmentIds((prev) =>
      prev.includes(attachmentId)
        ? prev.filter((id) => id !== attachmentId)
        : [...prev, attachmentId]
    );
  };

  const handleAction = async () => {
    try {
      let result: TrackedCostWeb;
      if (mode === 'create') {
        const data: CreateTrackedCostRequest = {
          name,
          description: description || null,
          net: net !== '' ? parseFloat(net) : null,
          gross: gross !== '' ? parseFloat(gross) : null,
          number: number || null,
          contractor: contractor || null,
          date: date || null,
          newFiles: newFiles.length > 0 ? newFiles : undefined,
          ...(workItemType === WorkItemType.LinkedWorkItem
            ? {
                costEstimateItemId: costEstimateItemId ?? null,
                workScheduleStageWorkId: workScheduleStageWorkId ?? null,
              }
            : workItemType === WorkItemType.ScheduleWorkItem
              ? { workScheduleStageWorkId: workScheduleStageWorkId ?? null }
              : workItemType === WorkItemType.EstimateItem
                ? { costEstimateItemId: costEstimateItemId ?? null }
                : {}),
        };
        result = await createCost(data);
      } else {
        const data: UpdateTrackedCostRequest = {
          name,
          description: description || null,
          net: net !== '' ? parseFloat(net) : null,
          gross: gross !== '' ? parseFloat(gross) : null,
          number: number || null,
          contractor: contractor || null,
          date: date || null,
          newFiles: newFiles.length > 0 ? newFiles : undefined,
          existingAttachmentIds,
        };
        result = await updateCost(cost!.id, data);
      }
      onSuccess(result);
      onClose();
    } catch {
      // błąd wyświetlany z hooka
    }
  };

  return (
    <AppModal
      isOpen
      onClose={onClose}
      title={mode === 'create' ? 'Dodaj koszt' : 'Edytuj koszt'}
      actionLabel={mode === 'create' ? 'Dodaj koszt' : 'Zapisz zmiany'}
      actionColorScheme="green"
      onAction={handleAction}
      isActionLoading={isLoading}
      isActionDisabled={!name.trim()}
    >
      <VStack spacing={4} align="stretch" sx={{ 'input, textarea, select': { fontSize: '16px' } }}>
        <FormControl isRequired>
          <FormLabel>Nazwa</FormLabel>
          <Input value={name} onChange={(e) => setName(e.target.value)} />
        </FormControl>

        <FormControl>
          <FormLabel>Opis</FormLabel>
          <Textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={3}
            resize="vertical"
          />
        </FormControl>

        <SimpleGrid columns={2} spacing={3}>
          <FormControl>
            <FormLabel>Kwota netto (zł)</FormLabel>
            <Input
              type="number"
              step="0.01"
              value={net}
              onChange={(e) => setNet(e.target.value)}
            />
          </FormControl>
          <FormControl>
            <FormLabel>Kwota brutto (zł)</FormLabel>
            <Input
              type="number"
              step="0.01"
              value={gross}
              onChange={(e) => setGross(e.target.value)}
            />
          </FormControl>
        </SimpleGrid>

        <FormControl>
          <FormLabel>Numer faktury</FormLabel>
          <Input value={number} onChange={(e) => setNumber(e.target.value)} placeholder="np. FV/2024/001" />
        </FormControl>

        <FormControl>
          <FormLabel>Wykonawca</FormLabel>
          <Input value={contractor} onChange={(e) => setContractor(e.target.value)} />
        </FormControl>

        <FormControl>
          <FormLabel>Data</FormLabel>
          <Input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </FormControl>

        <FormControl>
          <FormLabel>Załączniki</FormLabel>
          <Input
            type="file"
            multiple
            onChange={(e) => setNewFiles(e.target.files ? Array.from(e.target.files) : [])}
            sx={{ paddingTop: '6px' }}
          />
        </FormControl>

        {mode === 'edit' && cost && cost.attachments.length > 0 && (
          <FormControl>
            <FormLabel>Istniejące załączniki (odznacz aby usunąć)</FormLabel>
            <VStack align="stretch" spacing={1}>
              {cost.attachments.map((att) => (
                <Checkbox
                  key={att.id}
                  isChecked={existingAttachmentIds.includes(att.id)}
                  onChange={() => toggleExistingAttachment(att.id)}
                >
                  <Text fontSize="sm">{att.originalFileName}</Text>
                </Checkbox>
              ))}
            </VStack>
          </FormControl>
        )}

        {error && (
          <Alert status="error" borderRadius="md">
            <AlertIcon />
            {error}
          </Alert>
        )}
      </VStack>
    </AppModal>
  );
}

export default TrackedCostModal;
