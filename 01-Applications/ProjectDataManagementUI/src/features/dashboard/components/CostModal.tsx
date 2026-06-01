import React, { useState, useRef } from 'react';
import {
  VStack,
  FormControl,
  FormLabel,
  Input,
  Textarea,
  SimpleGrid,
  Alert,
  AlertIcon,
  Badge,
  Text,
  Button,
  Checkbox,
  HStack,
  IconButton,
  Tooltip,
  AlertDialog,
  AlertDialogOverlay,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogBody,
  AlertDialogFooter,
  useDisclosure,
} from '@chakra-ui/react';
import { FileUp, Eye, Sparkles, X } from 'lucide-react';
import AppModal from '../../../components/ui/AppModal';
import ContractorPicker from '../../../components/ContractorPicker';
import ContractorQuickAddModal from '../../../components/ContractorQuickAddModal';
import CostLinkSection from '../../../components/CostTracker/CostLinkSection';
import { AICostImportModal } from '../../../components/CostTracker/AICostImportModal';
import type { ParsedCostDto } from '../../../types/ai.types';
import type {
  TrackedCostWeb,
  CreateTrackedCostRequest,
  UpdateTrackedCostRequest,
} from '../types/projectDashboard.types';
import { WorkItemType } from '../types/projectDashboard.types';
import { useTrackedCostMutations } from '../hooks/useTrackedCostMutations';
import { useProjectCostMutations } from '../../../hooks/useProjectCostMutations';
import { useProjectPermissions } from '../../../hooks/useProjectPermissions';
import { useTenantPermissions } from '../../../hooks/useTenantPermissions';
import type { ProjectCostListItemWeb } from '../../../types/project.types';

type CostModalMode = 'create' | 'edit';

export interface CostModalTrackedProps {
  type: 'tracked';
  workItemType?: WorkItemType | null;
  costEstimateItemId?: string | null;
  workScheduleStageWorkId?: string | null;
  cost?: TrackedCostWeb;
  onSuccess: (cost: TrackedCostWeb) => void;
}

export interface CostModalProjectProps {
  type: 'project';
  cost?: ProjectCostListItemWeb;
  onSuccess: (cost: ProjectCostListItemWeb) => void;
}

type CostModalTypeProps = CostModalTrackedProps | CostModalProjectProps;

export interface CostModalBaseProps {
  tenantId: string;
  projectId: string;
  mode: CostModalMode;
  onClose: () => void;
  /** Dane wypełnione przez AI — przekazane przy otwieraniu modala po analizie dokumentu */
  aiPrefill?: { parsedData: ParsedCostDto; file: File };
}

export type CostModalProps = CostModalBaseProps & CostModalTypeProps;

interface CostFormState {
  name: string;
  description: string;
  net: string;
  gross: string;
  contractorId: string | null;
  date: string;
  number: string;
  newFiles: File[];
  existingAttachmentIds: string[];
  document: File | null;
  removeDocument: boolean;
}

export function CostModal(props: CostModalProps): React.ReactElement {
  const { tenantId, projectId, mode, onClose } = props;

  // Oba hooki zawsze wywoływane (React rules of hooks)
  const trackedMutations = useTrackedCostMutations({ tenantId, projectId });
  const projectMutations = useProjectCostMutations(tenantId, projectId);
  const { canEdit: isProjectAdmin } = useProjectPermissions(projectId);
  const { canEdit: isTenantAdmin } = useTenantPermissions();
  const canQuickAdd = isProjectAdmin || isTenantAdmin;

  // Powiązania kosztu (dla type=tracked w edit i create bez workItemType)
  const isTrackedWithLink = props.type === 'tracked' && (mode === 'edit' || !props.workItemType);
  const [linkItemId, setLinkItemId] = useState<string | null>(
    () => (props.type === 'tracked' && mode === 'edit' ? (props.cost?.costEstimateItemId ?? null) : null)
  );
  const [linkWorkId, setLinkWorkId] = useState<string | null>(
    () => (props.type === 'tracked' && mode === 'edit' ? (props.cost?.workScheduleStageWorkId ?? null) : null)
  );

  // Ścieżki z web modelu — nie wymagają ładowania dodatkowych danych
  const currentEstimatePath = (props.type === 'tracked' && mode === 'edit') ? (props.cost?.costEstimateItemPath ?? null) : null;
  const currentWorkPath = (props.type === 'tracked' && mode === 'edit') ? (props.cost?.workScheduleWorkPath ?? null) : null;

  const handleLinkChange = (newItemId: string | null) => {
    setLinkItemId(newItemId);
    if (newItemId !== null) {
      setLinkWorkId(null);
    }
  };

  const handleWorkChange = (workId: string | null, relatedEstimateItemId?: string | null) => {
    setLinkWorkId(workId);
    if (workId !== null && relatedEstimateItemId) {
      setLinkItemId(relatedEstimateItemId);
    }
  };

  const [aiParsedInfo, setAiParsedInfo] = useState<ParsedCostDto | null>(
    () => (mode === 'create' && props.aiPrefill ? props.aiPrefill.parsedData : null)
  );
  const [isAiContractorCreateOpen, setIsAiContractorCreateOpen] = useState(false);

  const handleAIParsed = (parsed: ParsedCostDto, file: File) => {
    setAiParsedInfo(parsed);
    setForm({
      name: parsed.name ?? '',
      description: parsed.description ?? '',
      net: parsed.net != null ? String(parsed.net) : '',
      gross: parsed.gross != null ? String(parsed.gross) : '',
      contractorId: parsed.contractorFound ? (parsed.contractorId ?? null) : null,
      date: parsed.date ? parsed.date.substring(0, 10) : '',
      number: parsed.number ?? '',
      newFiles: props.type === 'tracked' ? [file] : [],
      existingAttachmentIds: [],
      document: props.type === 'project' ? file : null,
      removeDocument: false,
    });
    setIsAIImportOpen(false);
  };

  const [form, setForm] = useState<CostFormState>(() => {
    if (mode === 'create' && props.aiPrefill) {
      const { parsedData: p, file } = props.aiPrefill;
      return {
        name: p.name ?? '',
        description: p.description ?? '',
        net: p.net != null ? String(p.net) : '',
        gross: p.gross != null ? String(p.gross) : '',
        contractorId: p.contractorFound ? (p.contractorId ?? null) : null,
        date: p.date ? p.date.substring(0, 10) : '',
        number: p.number ?? '',
        newFiles: props.type === 'tracked' ? [file] : [],
        existingAttachmentIds: [],
        document: props.type === 'project' ? file : null,
        removeDocument: false,
      };
    }
    if (props.type === 'tracked' && mode === 'edit' && props.cost) {
      const c = props.cost;
      return {
        name: c.name ?? '',
        description: c.description ?? '',
        net: c.net != null ? String(c.net) : '',
        gross: c.gross != null ? String(c.gross) : '',
        contractorId: c.contractorId ?? null,
        date: c.date ? c.date.substring(0, 10) : '',
        number: c.number ?? '',
        newFiles: [],
        existingAttachmentIds: c.attachments?.map((a) => a.id) ?? [],
        document: null,
        removeDocument: false,
      };
    }
    if (props.type === 'project' && mode === 'edit' && props.cost) {
      const c = props.cost;
      return {
        name: c.name ?? '',
        description: c.description ?? '',
        net: c.net != null && c.net !== 0 ? String(c.net) : '',
        gross: c.gross != null && c.gross !== 0 ? String(c.gross) : '',
        contractorId: c.contractorId ?? null,
        date: c.date ? c.date.split('T')[0] : '',
        number: c.number ?? '',
        newFiles: [],
        existingAttachmentIds: [],
        document: null,
        removeDocument: false,
      };
    }
    return {
      name: '',
      description: '',
      net: '',
      gross: '',
      contractorId: null,
      date: props.type === 'project' ? new Date().toISOString().split('T')[0] : '',
      number: '',
      newFiles: [],
      existingAttachmentIds: [],
      document: null,
      removeDocument: false,
    };
  });

  const [projectError, setProjectError] = useState<string | null>(null);
  const [isAIImportOpen, setIsAIImportOpen] = useState(false);
  const {
    isOpen: isRemoveDocOpen,
    onOpen: onRemoveDocOpen,
    onClose: onRemoveDocClose,
  } = useDisclosure();
  const cancelRemoveRef = useRef<HTMLButtonElement>(null);

  const title =
    mode === 'create'
      ? props.type === 'tracked'
        ? 'Dodaj koszt'
        : 'Dodaj wydatek'
      : props.type === 'tracked'
        ? 'Edytuj koszt'
        : 'Edytuj wydatek';

  const actionLabel = mode === 'create' ? 'Dodaj' : 'Zapisz zmiany';

  const isLoading =
    props.type === 'tracked'
      ? trackedMutations.isLoading
      : projectMutations.isCreating || projectMutations.isUpdating;

  const error =
    props.type === 'tracked' ? trackedMutations.error : projectError;

  // Pomocnicze dla trybu project
  const projectCost: ProjectCostListItemWeb | undefined =
    props.type === 'project' ? props.cost : undefined;
  const hasDocument =
    props.type === 'project' &&
    (!!projectCost?.hasDocument && !form.removeDocument);
  const documentName =
    projectCost?.documentFileName ?? '';
  const fileInputId = projectCost
    ? `edit-expense-doc-${projectCost.id}`
    : 'new-expense-doc';

  const toggleExistingAttachment = (attachmentId: string) => {
    setForm((prev) => ({
      ...prev,
      existingAttachmentIds: prev.existingAttachmentIds.includes(attachmentId)
        ? prev.existingAttachmentIds.filter((id) => id !== attachmentId)
        : [...prev.existingAttachmentIds, attachmentId],
    }));
  };

  const handleAction = async () => {
    if (props.type === 'tracked') {
      const trackedProps = props;
      try {
        let result: TrackedCostWeb;
        if (mode === 'create') {
          const data: CreateTrackedCostRequest = {
            name: form.name,
            description: form.description || null,
            net: form.net !== '' ? parseFloat(form.net) : null,
            gross: form.gross !== '' ? parseFloat(form.gross) : null,
            number: form.number || null,
            contractorId: form.contractorId || null,
            date: form.date || null,
            newFiles: form.newFiles.length > 0 ? form.newFiles : undefined,
            ...(trackedProps.workItemType === WorkItemType.LinkedWorkItem
              ? {
                  costEstimateItemId: trackedProps.costEstimateItemId ?? null,
                  workScheduleStageWorkId: trackedProps.workScheduleStageWorkId ?? null,
                }
              : trackedProps.workItemType === WorkItemType.ScheduleWorkItem
                ? { workScheduleStageWorkId: trackedProps.workScheduleStageWorkId ?? null }
                : trackedProps.workItemType === WorkItemType.EstimateItem
                  ? { costEstimateItemId: trackedProps.costEstimateItemId ?? null }
                  : { costEstimateItemId: linkItemId, workScheduleStageWorkId: linkWorkId }),

          };
          result = await trackedMutations.createCost(data);
        } else {
          const data: UpdateTrackedCostRequest = {
            name: form.name,
            description: form.description || null,
            net: form.net !== '' ? parseFloat(form.net) : null,
            gross: form.gross !== '' ? parseFloat(form.gross) : null,
            number: form.number || null,
            contractorId: form.contractorId || null,
            date: form.date || null,
            newFiles: form.newFiles.length > 0 ? form.newFiles : undefined,
            existingAttachmentIds: form.existingAttachmentIds,
            costEstimateItemId: linkItemId,
            workScheduleStageWorkId: linkWorkId,
          };
          result = await trackedMutations.updateCost(trackedProps.cost!.id, data);
        }
        trackedProps.onSuccess(result);
        onClose();
      } catch {
        // błąd obsługiwany przez useTrackedCostMutations
      }
    } else {
      const projectProps = props;
      setProjectError(null);
      try {
        let result: ProjectCostListItemWeb;
        if (mode === 'create') {
          result = await projectMutations.createCost({
            name: form.name,
            number: form.number || null,
            contractorId: form.contractorId || null,
            date: form.date ? new Date(form.date) : new Date(),
            description: form.description || undefined,
            net: form.net !== '' ? parseFloat(form.net) : null,
            gross: form.gross !== '' ? parseFloat(form.gross) : null,
            document: form.document || undefined,
          });
        } else {
          const existingCost = projectProps.cost!;
          result = await projectMutations.updateCost(existingCost.id, {
            name: form.name,
            number: form.number || null,
            contractorId: form.contractorId || null,
            date: form.date ? new Date(form.date) : new Date(),
            description: form.description || undefined,
            net: form.net !== '' ? parseFloat(form.net) : null,
            gross: form.gross !== '' ? parseFloat(form.gross) : null,
            document:
              form.document && !existingCost.hasDocument ? form.document : undefined,
            updatedDocument:
              form.document && existingCost.hasDocument ? form.document : undefined,
            removeDocument: form.removeDocument,
          });
        }
        projectProps.onSuccess(result);
        onClose();
      } catch (err) {
        setProjectError(err instanceof Error ? err.message : 'Wystąpił błąd zapisu');
      }
    }
  };

  return (
    <>
      <AppModal
        isOpen
        onClose={onClose}
        title={title}
        actionLabel={actionLabel}
        actionColorScheme="green"
        onAction={handleAction}
        isActionLoading={isLoading}
        isActionDisabled={!form.name.trim()}
      >
        <VStack spacing={4} align="stretch" sx={{ 'input, textarea, select': { fontSize: '16px' } }}>
          {mode === 'create' && (
            <HStack justify="flex-end">
              <Button
                variant="outline"
                size="sm"
                leftIcon={<FileUp size={14} aria-hidden="true" />}
                onClick={() => setIsAIImportOpen(true)}
              >
                Importuj z dokumentu
              </Button>
            </HStack>
          )}
          {aiParsedInfo && (
            <Alert status="info" fontSize="sm">
              <AlertIcon as={Sparkles} />
              Dane wypełnione przez AI — sprawdź i zatwierdź przed zapisaniem.
              {aiParsedInfo.confidence < 0.7 && (
                <Text as="span" ml={1} fontWeight="medium" color="orange.600">
                  (niska pewność odczytu)
                </Text>
              )}
            </Alert>
          )}
          {error && (
            <Alert status="error">
              <AlertIcon />
              {error}
            </Alert>
          )}

          {/* Pola wspólne */}
          <FormControl isRequired>
            <FormLabel>Nazwa</FormLabel>
            <Input
              value={form.name}
              onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
            />
          </FormControl>

          <FormControl>
            <FormLabel>Opis</FormLabel>
            <Textarea
              value={form.description}
              onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))}
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
                min="0"
                value={form.net}
                onChange={(e) => setForm((p) => ({ ...p, net: e.target.value }))}
              />
            </FormControl>
            <FormControl>
              <FormLabel>Kwota brutto (zł)</FormLabel>
              <Input
                type="number"
                step="0.01"
                min="0"
                value={form.gross}
                onChange={(e) => setForm((p) => ({ ...p, gross: e.target.value }))}
              />
            </FormControl>
          </SimpleGrid>

          <FormControl>
            <FormLabel>Numer faktury</FormLabel>
            <Input
              value={form.number}
              onChange={(e) => setForm((p) => ({ ...p, number: e.target.value }))}
              placeholder="np. FV/2024/001"
            />
          </FormControl>

          <FormControl>
            <HStack mb={1} spacing={2} align="center">
              <FormLabel mb={0}>Wykonawca</FormLabel>
              {aiParsedInfo?.contractorFound && form.contractorId && (
                <Badge colorScheme="purple" fontSize="2xs" px={1.5} py={0.5}>
                  ⚡ AI znalazł
                </Badge>
              )}
            </HStack>
            <ContractorPicker
              tenantId={tenantId}
              value={form.contractorId}
              onChange={(id) => setForm((p) => ({ ...p, contractorId: id }))}
              canQuickAdd={canQuickAdd}
            />
            {aiParsedInfo && !aiParsedInfo.contractorFound && aiParsedInfo.suggestedContractor && !form.contractorId && (
              <Alert status="warning" mt={2} fontSize="sm">
                <AlertIcon />
                <VStack align="flex-start" flex={1} spacing={1}>
                  <Text fontSize="sm">
                    AI sugeruje: <strong>{aiParsedInfo.suggestedContractor.name}</strong>
                    {aiParsedInfo.suggestedContractor.nip && <> · NIP: {aiParsedInfo.suggestedContractor.nip}</>}
                  </Text>
                  <Button size="xs" colorScheme="purple" onClick={() => setIsAiContractorCreateOpen(true)}>
                    Utwórz kontrahenta
                  </Button>
                </VStack>
              </Alert>
            )}
          </FormControl>

          <FormControl>
            <FormLabel>Data</FormLabel>
            <Input
              type="date"
              value={form.date}
              onChange={(e) => setForm((p) => ({ ...p, date: e.target.value }))}
            />
          </FormControl>

          {/* Pola tylko dla TrackedCost */}
          {props.type === 'tracked' && (
            <>
              <FormControl>
                <FormLabel>Załączniki</FormLabel>
                <Input
                  type="file"
                  multiple
                  onChange={(e) =>
                    setForm((p) => ({
                      ...p,
                      newFiles: e.target.files ? Array.from(e.target.files) : [],
                    }))
                  }
                  sx={{ paddingTop: '6px' }}
                />
                {form.newFiles.length > 0 && (
                  <VStack align="stretch" spacing={1} mt={2}>
                    {form.newFiles.map((f, i) => (
                      <HStack
                        key={i}
                        spacing={2}
                        px={3}
                        py={2}
                        borderWidth="1px"
                        borderRadius="md"
                        borderColor="neutral.200"
                        bg="neutral.50"
                      >
                        <Text fontSize="sm" flex={1} isTruncated>{f.name}</Text>
                        <Text fontSize="xs" color="neutral.500">{(f.size / 1024).toFixed(0)} KB</Text>
                        {f.type.startsWith('image/') && (
                          <Tooltip label="Podgląd">
                            <IconButton
                              aria-label="Podgląd pliku"
                              icon={<Eye size={14} />}
                              size="xs"
                              variant="ghost"
                              colorScheme="level2"
                              onClick={() => window.open(URL.createObjectURL(f), '_blank')}
                            />
                          </Tooltip>
                        )}
                        <IconButton
                          aria-label="Usuń plik"
                          icon={<X size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="red"
                          onClick={() =>
                            setForm((p) => ({
                              ...p,
                              newFiles: p.newFiles.filter((_, idx) => idx !== i),
                            }))
                          }
                        />
                      </HStack>
                    ))}
                  </VStack>
                )}
              </FormControl>

              {mode === 'edit' && props.cost && (props.cost.attachments?.length ?? 0) > 0 && (
                <FormControl>
                  <FormLabel>Istniejące załączniki (odznacz aby usunąć)</FormLabel>
                  <VStack align="stretch" spacing={1}>
                    {props.cost.attachments.map((att) => (
                      <HStack key={att.id} spacing={2}>
                        <Checkbox
                          isChecked={form.existingAttachmentIds.includes(att.id)}
                          onChange={() => toggleExistingAttachment(att.id)}
                          flex={1}
                        >
                          <Text fontSize="sm">{att.originalFileName}</Text>
                        </Checkbox>
                        {att.fileUrl && (
                          <Tooltip label="Podgląd">
                            <IconButton
                              aria-label={`Podgląd ${att.originalFileName}`}
                              icon={<Eye size={14} />}
                              size="xs"
                              variant="ghost"
                              colorScheme="level2"
                              onClick={() => window.open(att.fileUrl, '_blank')}
                            />
                          </Tooltip>
                        )}
                      </HStack>
                    ))}
                  </VStack>
                </FormControl>
              )}
            </>
          )}

          {/* Pola tylko dla ProjectCost */}
          {props.type === 'project' && (
            <>
              <FormControl>
                <FormLabel>Dokument</FormLabel>
                {form.document ? (
                  <HStack
                    spacing={2}
                    px={3}
                    py={2}
                    borderWidth="1px"
                    borderRadius="md"
                    borderColor="neutral.200"
                    bg="neutral.50"
                  >
                    <Text fontSize="sm" flex={1} isTruncated>{form.document.name}</Text>
                    <Text fontSize="xs" color="neutral.500">{(form.document.size / 1024).toFixed(0)} KB</Text>
                    {form.document.type.startsWith('image/') && (
                      <Tooltip label="Podgląd">
                        <IconButton
                          aria-label="Podgląd dokumentu"
                          icon={<Eye size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="level2"
                          onClick={() => window.open(URL.createObjectURL(form.document!), '_blank')}
                        />
                      </Tooltip>
                    )}
                    <IconButton
                      aria-label="Usuń wybrany plik"
                      icon={<X size={14} />}
                      size="xs"
                      variant="ghost"
                      colorScheme="red"
                      onClick={() => setForm((p) => ({ ...p, document: null }))}
                    />
                  </HStack>
                ) : hasDocument ? (
                  <HStack
                    spacing={2}
                    px={3}
                    py={2}
                    borderWidth="1px"
                    borderRadius="md"
                    borderColor="neutral.200"
                    display="inline-flex"
                    maxW="full"
                  >
                    <Text fontSize="sm" isTruncated maxW="220px">
                      {documentName}
                    </Text>
                    <IconButton
                      aria-label="Usuń dokument"
                      icon={<X size={14} />}
                      size="xs"
                      variant="ghost"
                      colorScheme="red"
                      onClick={onRemoveDocOpen}
                    />
                  </HStack>
                ) : (
                  <>
                    <Input
                      type="file"
                      accept=".pdf,.jpg,.jpeg,.png"
                      onChange={(e) =>
                        setForm((p) => ({ ...p, document: e.target.files?.[0] ?? null }))
                      }
                      display="none"
                      id={fileInputId}
                    />
                    <Button
                      as="label"
                      htmlFor={fileInputId}
                      leftIcon={<FileUp size={16} />}
                      variant="outline"
                      size="sm"
                      cursor="pointer"
                    >
                      Dodaj plik
                    </Button>
                  </>
                )}
                <Text fontSize="xs" color="neutral.500" mt={1}>
                  Obsługiwane formaty: PDF, JPG, PNG
                </Text>
              </FormControl>


            </>
          )}

          {/* Sekcja powiązania — tracked edit i create bez kontekstu */}
          {isTrackedWithLink && (
            <CostLinkSection
              currentEstimatePath={currentEstimatePath}
              currentWorkPath={currentWorkPath}
              selectedItemId={linkItemId}
              selectedWorkId={linkWorkId}
              onChange={handleLinkChange}
              onWorkChange={handleWorkChange}
              tenantId={tenantId}
              projectId={projectId}
            />
          )}
        </VStack>
      </AppModal>

      {/* AlertDialog potwierdzenia usunięcia dokumentu — tylko tryb project */}
      {props.type === 'project' && (
        <AlertDialog
          isOpen={isRemoveDocOpen}
          leastDestructiveRef={cancelRemoveRef}
          onClose={onRemoveDocClose}
        >
          <AlertDialogOverlay>
            <AlertDialogContent>
              <AlertDialogHeader fontSize="lg" fontWeight="bold">
                Usuń dokument
              </AlertDialogHeader>
              <AlertDialogBody>
                Czy na pewno chcesz usunąć dołączony dokument?
              </AlertDialogBody>
              <AlertDialogFooter>
                <Button ref={cancelRemoveRef} onClick={onRemoveDocClose}>
                  Nie
                </Button>
                <Button
                  colorScheme="red"
                  onClick={() => {
                    setForm((p) => ({ ...p, document: null, removeDocument: true }));
                    onRemoveDocClose();
                  }}
                  ml={3}
                >
                  Tak, usuń
                </Button>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialogOverlay>
        </AlertDialog>
      )}

      {mode === 'create' && (
        <AICostImportModal
          isOpen={isAIImportOpen}
          onClose={() => setIsAIImportOpen(false)}
          tenantId={tenantId}
          projectId={projectId}
          costType={props.type === 'project' ? 'ProjectCost' : 'TrackedCost'}
          onParsed={handleAIParsed}
        />
      )}

      {isAiContractorCreateOpen && aiParsedInfo?.suggestedContractor && (
        <ContractorQuickAddModal
          isOpen
          tenantId={tenantId}
          onClose={() => setIsAiContractorCreateOpen(false)}
          initialValues={{
            name: aiParsedInfo.suggestedContractor.name,
            taxId: aiParsedInfo.suggestedContractor.nip,
            street: aiParsedInfo.suggestedContractor.address,
          }}
          onCreated={(id) => {
            setForm((p) => ({ ...p, contractorId: id }));
            setIsAiContractorCreateOpen(false);
          }}
        />
      )}
    </>
  );
}

export default CostModal;
