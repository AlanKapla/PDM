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
import CostForm, { validateCostForm } from "./CostForm";
import { useToastNotification } from "../../hooks/useToastNotification";
import { costTrackerApi } from "../../api/costTrackerApi";
import { handleApiError } from "../../utils/handleApiError";
import type { CostEstimateSummaryWeb, TrackerGroupWeb, CostFormValues } from "../../types/costTracker.types";

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
  contractor: "",
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
  const { showSuccess, showError } = useToastNotification();
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
    setActiveStep((s) => s + 1);
  };

  const handleBack = () => {
    if (activeStep === 3 && selectedEstimateId === "project") {
      setActiveStep(0);
      return;
    }
    setActiveStep((s) => Math.max(0, s - 1));
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
        contractor: formValues.contractor?.trim() || undefined,
        date: formValues.date || undefined,
        costEstimateId,
        costEstimateItemId,
        newFiles: formValues.newFiles ?? [],
      });

      showSuccess("Koszt dodany");
      handleClose();
      onSuccess();
    } catch (err) {
      const { title, description } = handleApiError(err);
      showError(title, description);
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
    <Text fontSize="sm" color="gray.500" mb={4}>
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
          <CostForm
            values={formValues}
            onChange={setFormValues}
            errors={formErrors}
            isSubmitting={isSubmitting}
          />
        );

      default:
        return null;
    }
  };

  const footer = (
    <HStack spacing={2} width="100%" justify="flex-end">
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
          colorScheme="blue"
          onClick={handleNext}
          isDisabled={!canNext()}
          width={{ base: "full", md: "auto" }}
        >
          Dalej
        </Button>
      ) : (
        <Button
          colorScheme="blue"
          onClick={handleSubmit}
          isLoading={isSubmitting}
          width={{ base: "full", md: "auto" }}
        >
          Zapisz
        </Button>
      )}
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
      <Drawer isOpen={isOpen} onClose={handleClose} placement="bottom" size="full">
        <DrawerOverlay />
        <DrawerContent>
          <DrawerCloseButton />
          <DrawerHeader>Dodaj koszt</DrawerHeader>
          <DrawerBody>{body}</DrawerBody>
          <DrawerFooter>{footer}</DrawerFooter>
        </DrawerContent>
      </Drawer>
    );
  }

  return (
    <Modal isOpen={isOpen} onClose={handleClose} size="xl" isCentered>
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>Dodaj koszt</ModalHeader>
        <ModalCloseButton />
        <ModalBody>{body}</ModalBody>
        <ModalFooter>{footer}</ModalFooter>
      </ModalContent>
    </Modal>
  );
}
