import { useState, useMemo } from "react";
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
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
  VStack,
  Text,
  Select,
  FormControl,
  FormLabel,
  Stepper,
  Step,
  StepIndicator,
  StepStatus,
  StepIcon,
  StepNumber,
  StepTitle,
  StepSeparator,
  useSteps,
  useBreakpointValue,
  Box,
} from "@chakra-ui/react";
import { FileUp } from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
import CostForm, { validateCostForm } from "./CostForm";
import { AICostImportModal } from "./AICostImportModal";
import { useToastNotification } from "../../hooks/useToastNotification";
import { useProjectPermissions } from "../../hooks/useProjectPermissions";
import { useTenantPermissions } from "../../hooks/useTenantPermissions";
import { costTrackerApi } from "../../api/costTrackerApi";
import { costTrackerKeys } from "../../hooks/queries";
import { handleApiError } from "../../utils/handleApiError";
import type { CostEstimateSummaryWeb, TrackerGroupWeb, CostFormValues } from "../../types/costTracker.types";
import type { ParsedCostDto, TrackedCostContext } from "../../types/ai.types";

interface CostFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  tenantId: string;
  projectId: string;
  costEstimateSummaries: CostEstimateSummaryWeb[];
}

// Rekurencyjne spłaszczenie drzewa grup z zachowaniem poziomów wcięcia
function flattenGroups(
  groups: TrackerGroupWeb[],
  depth = 0
): Array<{ group: TrackerGroupWeb; depth: number }> {
  return groups.flatMap((g) => [
    { group: g, depth },
    ...flattenGroups(g.childGroups, depth + 1),
  ]);
}

const STEPS = [
  { title: "Kosztorys" },
  { title: "Etap" },
  { title: "Pozycja" },
  { title: "Dane kosztu" },
];

const EMPTY_FORM: CostFormValues = {
  name: "",
  description: "",
  net: undefined,
  gross: undefined,
  number: "",
  contractorId: null,
  date: "",
  newFiles: [],
};

export default function CostFormModal({
  isOpen,
  onClose,
  onSuccess,
  tenantId,
  projectId,
  costEstimateSummaries,
}: CostFormModalProps) {
  const {showSuccess, showError, showApiError } = useToastNotification();
  const queryClient = useQueryClient();
  const { canEdit: isProjectAdmin } = useProjectPermissions(projectId);
  const { canEdit: isTenantAdmin } = useTenantPermissions();
  const canQuickAdd = isProjectAdmin || isTenantAdmin;
  const isMobile = useBreakpointValue({ base: true, md: false });

  const { activeStep, setActiveStep } = useSteps({ index: 0, count: STEPS.length });

  // Krok 0: wybór kosztorysu (null = koszt projektu)
  const [selectedEstimateId, setSelectedEstimateId] = useState<string | null | "project">("project");
  // Krok 1: wybór grupy
  const [selectedGroupId, setSelectedGroupId] = useState<string>("");
  // Krok 2: wybór pozycji (null = koszt dodatkowy kosztorysu)
  const [selectedItemId, setSelectedItemId] = useState<string | "additional">("additional");
  // Krok 3: formularz
  const [formValues, setFormValues] = useState<CostFormValues>(EMPTY_FORM);
  const [formErrors, setFormErrors] = useState<Partial<Record<keyof CostFormValues, string>>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isAIImportOpen, setIsAIImportOpen] = useState(false);
  const [aiParsedInfo, setAiParsedInfo] = useState<ParsedCostDto | null>(null);

  const handleAIParsed = (parsed: ParsedCostDto, file: File) => {
    setAiParsedInfo(parsed);
    setFormValues({
      name: parsed.name ?? '',
      description: parsed.description ?? '',
      net: parsed.net ?? undefined,
      gross: parsed.gross ?? undefined,
      number: parsed.number ?? '',
      contractorId: parsed.contractorFound ? (parsed.contractorId ?? null) : null,
      date: parsed.date ? parsed.date.substring(0, 10) : '',
      newFiles: [file],
    });
    setActiveStep(3);
    setIsAIImportOpen(false);
  };

  const selectedEstimate = useMemo(
    () => costEstimateSummaries.find((e) => e.costEstimateId === selectedEstimateId),
    [costEstimateSummaries, selectedEstimateId]
  );

  const flatGroups = useMemo(
    () => (selectedEstimate ? flattenGroups(selectedEstimate.groups) : []),
    [selectedEstimate]
  );

  const selectedGroup = useMemo(
    () => flatGroups.find((fg) => fg.group.groupId === selectedGroupId)?.group,
    [flatGroups, selectedGroupId]
  );

  const aiTrackedCostContext = useMemo((): TrackedCostContext | undefined => {
    if (selectedItemId !== "additional" && selectedItemId !== "") {
      return { costEstimateItemId: selectedItemId };
    }
    return undefined;
  }, [selectedItemId]);

  const handleClose = () => {
    setActiveStep(0);
    setSelectedEstimateId("project");
    setSelectedGroupId("");
    setSelectedItemId("additional");
    setFormValues(EMPTY_FORM);
    setFormErrors({});
    onClose();
  };

  const handleNext = () => {
    if (selectedEstimateId === "project") {
      // Pomiń kroki 1–3, idź prosto do formularza
      setActiveStep(3);
      return;
    }
    if (activeStep === 0) {
      setSelectedGroupId("");
      setSelectedItemId("additional");
    }
    if (activeStep === 1) {
      setSelectedItemId("additional");
    }
    setActiveStep(activeStep + 1);
  };

  const handleBack = () => {
    if (activeStep === 3 && selectedEstimateId === "project") {
      setActiveStep(0);
      return;
    }
    setActiveStep(Math.max(0, activeStep - 1));
  };

  const canNext = () => {
    if (activeStep === 0) return true; // zawsze można przejść
    if (activeStep === 1) return !!selectedGroupId;
    if (activeStep === 2) return true;
    return false;
  };

  const handleSubmit = async () => {
    const validationErrors = validateCostForm(formValues);
    if (Object.keys(validationErrors).length > 0) {
      setFormErrors(validationErrors);
      return;
    }
    setFormErrors({});
    setIsSubmitting(true);

    try {
      let costEstimateId: string | null = null;
      let costEstimateItemId: string | null = null;

      if (selectedEstimateId !== "project" && selectedEstimate) {
        costEstimateId = selectedEstimate.costEstimateId;
        if (selectedItemId !== "additional") {
          costEstimateItemId = selectedItemId;
        }
      }

      await costTrackerApi.createCost(tenantId, projectId, {
        name: formValues.name.trim(),
        description: formValues.description?.trim() || undefined,
        net: formValues.net !== undefined && formValues.net !== "" ? Number(formValues.net) : undefined,
        gross: formValues.gross !== undefined && formValues.gross !== "" ? Number(formValues.gross) : undefined,
        number: formValues.number?.trim() || undefined,
        contractorId: formValues.contractorId?.trim() || undefined,
        date: formValues.date || undefined,
        costEstimateId,
        costEstimateItemId,
        newFiles: formValues.newFiles ?? [],
      });

      queryClient.invalidateQueries({ queryKey: costTrackerKeys.byProject(tenantId, projectId) });
      queryClient.invalidateQueries({ queryKey: costTrackerKeys.costs(tenantId, projectId) });
      showSuccess("Koszt dodany");
      handleClose();
      onSuccess();
    } catch (err) {
      showApiError(err);
    } finally {
      setIsSubmitting(false);
    }
  };

  const stepLabels = isMobile ? null : (
    <Stepper index={activeStep} size="sm" mb={6}>
      {STEPS.map((step, i) => (
        <Step key={i}>
          <StepIndicator>
            <StepStatus
              complete={<StepIcon />}
              incomplete={<StepNumber />}
              active={<StepNumber />}
            />
          </StepIndicator>
          <StepTitle>{step.title}</StepTitle>
          <StepSeparator />
        </Step>
      ))}
    </Stepper>
  );

  const mobileStepLabel = isMobile ? (
    <Text fontSize="sm" color="neutral.500" mb={4}>
      Krok {activeStep + 1} z {STEPS.length} — {STEPS[activeStep].title}
    </Text>
  ) : null;

  const renderStep = () => {
    switch (activeStep) {
      case 0:
        return (
          <VStack spacing={4} align="stretch">
            <FormControl>
              <FormLabel>Typ kosztu</FormLabel>
              <Select
                value={selectedEstimateId ?? "project"}
                onChange={(e) => setSelectedEstimateId(e.target.value === "project" ? "project" : e.target.value)}
              >
                <option value="project">Koszt dodatkowy projektu</option>
                {costEstimateSummaries.map((est) => (
                  <option key={est.costEstimateId} value={est.costEstimateId}>
                    {est.costEstimateName}
                  </option>
                ))}
              </Select>
            </FormControl>
          </VStack>
        );

      case 1:
        return (
          <VStack spacing={4} align="stretch">
            <FormControl isRequired>
              <FormLabel>Etap / pod-etap</FormLabel>
              <Select
                value={selectedGroupId}
                onChange={(e) => setSelectedGroupId(e.target.value)}
                placeholder="Wybierz etap..."
              >
                {flatGroups.map(({ group, depth }) => (
                  <option key={group.groupId} value={group.groupId}>
                    {"—".repeat(depth)} {group.groupName}
                  </option>
                ))}
              </Select>
            </FormControl>
          </VStack>
        );

      case 2:
        return (
          <VStack spacing={4} align="stretch">
            <FormControl>
              <FormLabel>Pozycja</FormLabel>
              <Select
                value={selectedItemId}
                onChange={(e) => setSelectedItemId(e.target.value)}
              >
                <option value="additional">Koszt dodatkowy kosztorysu</option>
                {(selectedGroup?.items ?? []).map((item) => (
                  <option key={item.costEstimateItemId} value={item.costEstimateItemId}>
                    {item.name}
                  </option>
                ))}
              </Select>
            </FormControl>
          </VStack>
        );

      case 3:
        return (
          <VStack spacing={3} align="stretch">
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
              values={formValues}
              onChange={setFormValues}
              errors={formErrors}
              isSubmitting={isSubmitting}
              tenantId={tenantId}
              canQuickAdd={canQuickAdd}
            />
          </VStack>
        );

      default:
        return null;
    }
  };

  const footer = (
    <HStack spacing={2} width="100%" justify="space-between">
      <Button
        variant="outline"
        size="sm"
        leftIcon={<FileUp size={14} aria-hidden="true" />}
        onClick={() => setIsAIImportOpen(true)}
        isDisabled={isSubmitting}
      >
        Importuj z dokumentu
      </Button>
      <HStack spacing={2}>
        {activeStep > 0 && (
          <Button
            variant="ghost"
            onClick={handleBack}
            isDisabled={isSubmitting}
            width={{ base: "full", md: "auto" }}
          >
            Wstecz
          </Button>
        )}
        <Button
          variant="ghost"
          onClick={handleClose}
          isDisabled={isSubmitting}
          width={{ base: "full", md: "auto" }}
        >
          Anuluj
        </Button>
        {activeStep < STEPS.length - 1 ? (
          <Button
            colorScheme="primary"
            onClick={handleNext}
            isDisabled={!canNext()}
            width={{ base: "full", md: "auto" }}
          >
            Dalej
          </Button>
        ) : (
          <Button
            colorScheme="primary"
            onClick={handleSubmit}
            isLoading={isSubmitting}
            width={{ base: "full", md: "auto" }}
          >
            Zapisz
          </Button>
        )}
      </HStack>
    </HStack>
  );

  const body = (
    <Box>
      {mobileStepLabel}
      {stepLabels}
      {renderStep()}
    </Box>
  );

  // Na mobile renderuj jako bottom-sheet drawer
  if (isMobile) {
    return (
      <>
        <Drawer isOpen={isOpen} onClose={handleClose} placement="bottom" size="full">
          <DrawerOverlay />
          <DrawerContent>
            <DrawerCloseButton />
            <DrawerHeader>Dodaj koszt</DrawerHeader>
            <DrawerBody>{body}</DrawerBody>
            <DrawerFooter>{footer}</DrawerFooter>
          </DrawerContent>
        </Drawer>
        <AICostImportModal
          isOpen={isAIImportOpen}
          onClose={() => setIsAIImportOpen(false)}
          tenantId={tenantId}
          projectId={projectId}
          costType="TrackedCost"
          onParsed={handleAIParsed}
          trackedCostContext={aiTrackedCostContext}
        />
      </>
    );
  }

  return (
    <>
      <Modal isOpen={isOpen} onClose={handleClose} size={{ base: "full", md: "xl" }} isCentered scrollBehavior="inside">
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>Dodaj koszt</ModalHeader>
          <ModalCloseButton />
          <ModalBody>{body}</ModalBody>
          <ModalFooter>{footer}</ModalFooter>
        </ModalContent>
      </Modal>
      <AICostImportModal
        isOpen={isAIImportOpen}
        onClose={() => setIsAIImportOpen(false)}
        tenantId={tenantId}
        projectId={projectId}
        costType="TrackedCost"
        onParsed={handleAIParsed}
        trackedCostContext={aiTrackedCostContext}
      />
    </>
  );
}
