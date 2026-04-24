import { useState } from "react";
import {
  Drawer,
  DrawerOverlay,
  DrawerContent,
  DrawerHeader,
  DrawerBody,
  DrawerFooter,
  DrawerCloseButton,
  Button,
  HStack,
  Text,
  useBreakpointValue,
} from "@chakra-ui/react";
import type { DrawerProps } from "@chakra-ui/react";
import CostForm, { validateCostForm } from "./CostForm";
import { useToastNotification } from "../../hooks/useToastNotification";
import { costTrackerApi } from "../../api/costTrackerApi";
import { handleApiError } from "../../utils/handleApiError";
import type { TrackedCostWeb, CostFormValues } from "../../types/costTracker.types";

interface CostFormDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  tenantId: string;
  projectId: string;
  /** Jeśli przekazano — tryb edycji */
  cost?: TrackedCostWeb;
  /** Kontekst dla nowego kosztu */
  costEstimateId?: string | null;
  costEstimateItemId?: string | null;
  title?: string;
}

const EMPTY_FORM: CostFormValues = {
  name: "",
  description: "",
  net: undefined,
  number: "",
  contractor: "",
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
  costEstimateId,
  costEstimateItemId,
  title,
}: CostFormDrawerProps) {
  const { showSuccess, showError } = useToastNotification();
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
          number: cost.number ?? "",
          contractor: cost.contractor ?? "",
          date: cost.date ?? "",
          newFiles: [],
          existingAttachmentIds: cost.attachments.map((a) => a.id),
        }
      : EMPTY_FORM
  );

  const [errors, setErrors] = useState<Partial<Record<keyof CostFormValues, string>>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleClose = () => {
    setValues(EMPTY_FORM);
    setErrors({});
    onClose();
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
        number: values.number?.trim() || undefined,
        contractor: values.contractor?.trim() || undefined,
        date: values.date || undefined,
        newFiles: values.newFiles ?? [],
      };

      if (isEdit && cost) {
        await costTrackerApi.updateCost(tenantId, projectId, cost.id, {
          ...payload,
          costEstimateId: cost.costEstimateId,
          costEstimateItemId: cost.costEstimateItemId,
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
    <Drawer isOpen={isOpen} onClose={handleClose} placement={placement} size={size}>
      <DrawerOverlay />
      <DrawerContent>
        <DrawerCloseButton />
        <DrawerHeader>
          <Text>{title ?? (isEdit ? "Edytuj koszt" : "Dodaj koszt")}</Text>
        </DrawerHeader>

        <DrawerBody>
          <CostForm
            values={values}
            onChange={setValues}
            existingAttachments={cost?.attachments ?? []}
            errors={errors}
            isSubmitting={isSubmitting}
          />
        </DrawerBody>

        <DrawerFooter>
          <HStack spacing={2} width="100%" justify="flex-end">
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
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
