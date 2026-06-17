import { useState } from "react";
import {
  Drawer,
  DrawerOverlay,
  DrawerContent,
  DrawerHeader,
  DrawerBody,
  DrawerFooter,
  DrawerCloseButton,
  Alert,
  AlertIcon,
  Button,
  HStack,
  Text,
  VStack,
  useBreakpointValue,
} from "@chakra-ui/react";
import type { DrawerProps } from "@chakra-ui/react";
import { FileUp } from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
import CostForm, { validateCostForm } from "./CostForm";
import CostLinkSection from "./CostLinkSection";
import { AICostImportModal } from "./AICostImportModal";
import { useToastNotification } from "../../hooks/useToastNotification";
import { useProjectPermissions } from "../../hooks/useProjectPermissions";
import { useTenantPermissions } from "../../hooks/useTenantPermissions";
import { costTrackerApi } from "../../api/costTrackerApi";
import { costTrackerKeys } from "../../hooks/queries";
import { handleApiError } from "../../utils/handleApiError";
import type { TrackedCostWeb, CostFormValues } from "../../types/costTracker.types";
import type { ParsedCostDto } from "../../types/ai.types";

interface CostFormDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  tenantId: string;
  projectId: string;
  /** Jeśli przekazano — tryb edycji */
  cost?: TrackedCostWeb;
  /** Wartości początkowe formularza (np. z AI) */
  initialValues?: CostFormValues;
  /** Kontekst dla nowego kosztu */
  costEstimateId?: string | null;
  costEstimateItemId?: string | null;
  title?: string;
}

const EMPTY_FORM: CostFormValues = {
  name: "",
  description: "",
  net: undefined,
  gross: undefined,
  number: "",
  contractorId: null,
  date: "",
  newFiles: [],
  existingAttachmentIds: undefined,
};

export default function CostFormDrawer({
  isOpen,
  onClose,
  onSuccess,
  tenantId,
  projectId,
  cost,
  initialValues,
  costEstimateId,
  costEstimateItemId,
  title,
}: CostFormDrawerProps) {
  const { showSuccess, showError } = useToastNotification();
  const queryClient = useQueryClient();
  const { canEdit: isProjectAdmin } = useProjectPermissions(projectId);
  const { canEdit: isTenantAdmin } = useTenantPermissions();
  const canQuickAdd = isProjectAdmin || isTenantAdmin;
  const placement = useBreakpointValue({
    base: "bottom",
    md: "right",
  }) as DrawerProps["placement"];
  const size = useBreakpointValue({ base: "full", md: "md" }) as string;

  const isEdit = !!cost;

  const [values, setValues] = useState<CostFormValues>(() =>
    cost
      ? {
          name: cost.name,
          description: cost.description ?? "",
          net: cost.net ?? undefined,
          gross: cost.gross ?? undefined,
          number: cost.number ?? "",
          contractorId: cost.contractorId ?? null,
          date: cost.date ?? "",
          newFiles: [],
          existingAttachmentIds: cost.attachments.map((a) => a.id),
        }
      : (initialValues ?? EMPTY_FORM)
  );

  const [errors, setErrors] = useState<Partial<Record<keyof CostFormValues, string>>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isAIImportOpen, setIsAIImportOpen] = useState(false);
  const [aiParsedInfo, setAiParsedInfo] = useState<ParsedCostDto | null>(null);

  // Zarządzanie powiązaniem kosztu
  const [linkItemId, setLinkItemId] = useState<string | null>(
    () => cost?.costEstimateItemId ?? null
  );
  const [linkWorkId, setLinkWorkId] = useState<string | null>(
    () => cost?.workScheduleStageWorkId ?? null
  );

  const handleLinkChange = (newItemId: string | null) => {
    setLinkItemId(newItemId);
    // Zmiana pozycji kosztorysu — wyczyść zakres pracy, bo może nie być spójny
    if (newItemId !== null) {
      setLinkWorkId(null);
    }
  };

  const handleWorkChange = (workId: string | null, relatedEstimateItemId?: string | null) => {
    setLinkWorkId(workId);
    // Auto-ustaw pozycję kosztorysu na podstawie wybranego zakresu pracy
    if (workId !== null && relatedEstimateItemId) {
      setLinkItemId(relatedEstimateItemId);
    }
  };

  const handleClose = () => {
    setValues(EMPTY_FORM);
    setErrors({});
    setAiParsedInfo(null);
    onClose();
  };

  const handleAIParsed = (parsed: ParsedCostDto, file: File) => {
    setAiParsedInfo(parsed);
    setValues({
      name: parsed.name ?? '',
      description: parsed.description ?? '',
      net: parsed.net ?? undefined,
      gross: parsed.gross ?? undefined,
      number: parsed.number ?? '',
      contractorId: parsed.contractorFound ? (parsed.contractorId ?? null) : null,
      date: parsed.date ? parsed.date.substring(0, 10) : '',
      newFiles: [file],
    });
    setIsAIImportOpen(false);
  };

  const handleSubmit = async () => {
    const validationErrors = validateCostForm(values);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }
    setErrors({});
    setIsSubmitting(true);

    try {
      const payload = {
        name: values.name.trim(),
        description: values.description?.trim() || undefined,
        net: values.net !== undefined && values.net !== "" ? Number(values.net) : undefined,
        gross: values.gross !== undefined && values.gross !== "" ? Number(values.gross) : undefined,
        number: values.number?.trim() || undefined,
        contractorId: values.contractorId?.trim() || undefined,
        date: values.date || undefined,
        newFiles: values.newFiles ?? [],
      };

      if (isEdit && cost) {
        await costTrackerApi.updateCost(tenantId, projectId, cost.id, {
          ...payload,
          costEstimateItemId: linkItemId ?? undefined,
          workScheduleStageWorkId: linkWorkId ?? undefined,
          existingAttachmentIds: values.existingAttachmentIds,
        });
        showSuccess("Koszt zaktualizowany");
      } else {
        await costTrackerApi.createCost(tenantId, projectId, {
          ...payload,
          costEstimateId: costEstimateId ?? undefined,
          costEstimateItemId: costEstimateItemId ?? undefined,
        });
        showSuccess("Koszt dodany");
      }

      queryClient.invalidateQueries({ queryKey: costTrackerKeys.byProject(tenantId, projectId) });
      queryClient.invalidateQueries({ queryKey: costTrackerKeys.costs(tenantId, projectId) });
      handleClose();
      onSuccess();
    } catch (err) {
      const { title: errTitle, description } = handleApiError(err);
      showError(errTitle, description);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
    <Drawer isOpen={isOpen} onClose={handleClose} placement={placement} size={size}>
      <DrawerOverlay />
      <DrawerContent>
        <DrawerCloseButton />
        <DrawerHeader>
          <Text>{title ?? (isEdit ? "Edytuj koszt" : "Dodaj koszt")}</Text>
        </DrawerHeader>

        <DrawerBody>
          <VStack spacing={4} align="stretch">
            {aiParsedInfo && (
              <Alert status="info" fontSize="sm">
                <AlertIcon />
                Dane wypełnione przez AI — sprawdź i zatwierdź przed zapisaniem.
                {aiParsedInfo.confidence < 0.7 && (
                  <Text as="span" ml={1} fontWeight="medium" color="orange.600">(niska pewność)</Text>
                )}
              </Alert>
            )}
            <CostForm
              values={values}
              onChange={setValues}
              existingAttachments={cost?.attachments ?? []}
              errors={errors}
              isSubmitting={isSubmitting}
              tenantId={tenantId}
              canQuickAdd={canQuickAdd}
            />
            {isEdit && (
              <CostLinkSection
                currentEstimatePath={cost?.costEstimateItemPath ?? null}
                currentWorkPath={cost?.workScheduleWorkPath ?? null}
                selectedItemId={linkItemId}
                selectedWorkId={linkWorkId}
                onChange={handleLinkChange}
                onWorkChange={handleWorkChange}
                tenantId={tenantId}
                projectId={projectId}
              />
            )}
          </VStack>
        </DrawerBody>

        <DrawerFooter>
          <HStack spacing={2} width="100%" justify="space-between">
            {!isEdit && (
              <Button
                variant="outline"
                size="sm"
                leftIcon={<FileUp size={14} aria-hidden="true" />}
                onClick={() => setIsAIImportOpen(true)}
                isDisabled={isSubmitting}
              >
                Importuj z dokumentu
              </Button>
            )}
            <HStack spacing={2} ml={isEdit ? 'auto' : undefined}>
              <Button
                variant="ghost"
                onClick={handleClose}
                isDisabled={isSubmitting}
                width={{ base: "full", md: "auto" }}
              >
                Anuluj
              </Button>
              <Button
                colorScheme="primary"
                onClick={handleSubmit}
                isLoading={isSubmitting}
                width={{ base: "full", md: "auto" }}
              >
                {isEdit ? "Zapisz zmiany" : "Dodaj koszt"}
              </Button>
            </HStack>
          </HStack>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>

    {!isEdit && (
      <AICostImportModal
        isOpen={isAIImportOpen}
        onClose={() => setIsAIImportOpen(false)}
        tenantId={tenantId}
        projectId={projectId}
        costType="TrackedCost"
        onParsed={handleAIParsed}
      />
    )}
    </>
  );
}
