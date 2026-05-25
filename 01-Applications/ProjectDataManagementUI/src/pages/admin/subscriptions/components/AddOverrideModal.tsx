import React from "react";
import {
  VStack,
  HStack,
  FormControl,
  FormLabel,
  Input,
  Select,
  Textarea,
  NumberInput,
  NumberInputField,
  useToast,
} from "@chakra-ui/react";
import AppModal from "../../../../components/ui/AppModal";
import { useAddSubscriptionOverride } from "../../../../hooks/queries";
import type { AddSubscriptionOverrideRequest } from "../../../../types/subscription";

interface AddOverrideModalProps {
  tenantId: string;
  isOpen: boolean;
  onClose: () => void;
}

const PRESET_KEYS = ["MaxProjects", "MaxUsers", "Feature:..."] as const;
type PresetKey = (typeof PRESET_KEYS)[number];

function isNumericKey(key: string): boolean {
  return key === "MaxProjects" || key === "MaxUsers";
}

export function AddOverrideModal({
  tenantId,
  isOpen,
  onClose,
}: AddOverrideModalProps): React.ReactElement {
  const toast = useToast();
  const { mutate: addOverride, isPending } = useAddSubscriptionOverride();

  const [keyPreset, setKeyPreset] = React.useState<PresetKey>("MaxProjects");
  const [featureName, setFeatureName] = React.useState("");
  const [numericValue, setNumericValue] = React.useState(0);
  const [stringValue, setStringValue] = React.useState("");
  const [reason, setReason] = React.useState("");
  const [expiresAt, setExpiresAt] = React.useState("");

  function resolvedKey(): string {
    if (keyPreset === "Feature:...") {
      return `Feature:${featureName}`;
    }
    return keyPreset;
  }

  function resolvedValue(): string {
    if (isNumericKey(resolvedKey())) {
      return numericValue.toString();
    }
    return stringValue;
  }

  function resetForm(): void {
    setKeyPreset("MaxProjects");
    setFeatureName("");
    setNumericValue(0);
    setStringValue("");
    setReason("");
    setExpiresAt("");
  }

  function handleClose(): void {
    resetForm();
    onClose();
  }

  function handleAdd(): void {
    const key = resolvedKey();
    const value = resolvedValue();

    if (!key || !value || !reason) return;

    const data: AddSubscriptionOverrideRequest = {
      key,
      value,
      reason,
      expiresAt: expiresAt ? new Date(expiresAt).toISOString() : null,
    };

    addOverride(
      { tenantId, data },
      {
        onSuccess: () => {
          toast({
            title: "Override dodany",
            status: "success",
            duration: 3000,
            isClosable: true,
            position: "top-right",
          });
          handleClose();
        },
        onError: () => {
          toast({
            title: "Błąd podczas dodawania override",
            status: "error",
            duration: 5000,
            isClosable: true,
            position: "top-right",
          });
        },
      },
    );
  }

  const isFeatureKey = keyPreset === "Feature:...";
  const currentKey = resolvedKey();
  const isNumeric = isNumericKey(currentKey);

  return (
    <AppModal
      isOpen={isOpen}
      onClose={handleClose}
      title="Dodaj override"
      actionLabel="Dodaj"
      onAction={handleAdd}
      isActionLoading={isPending}
      isActionDisabled={!reason || (isFeatureKey && !featureName)}
    >
      <VStack spacing={4} align="stretch">
        <FormControl isRequired>
          <FormLabel fontSize="sm">Klucz</FormLabel>
          <Select
            size="sm"
            value={keyPreset}
            onChange={(e) => setKeyPreset(e.target.value as PresetKey)}
          >
            {PRESET_KEYS.map((k) => (
              <option key={k} value={k}>
                {k}
              </option>
            ))}
          </Select>
        </FormControl>

        {isFeatureKey && (
          <FormControl isRequired>
            <FormLabel fontSize="sm">Nazwa feature</FormLabel>
            <Input
              size="sm"
              placeholder="np. BetaFeature"
              value={featureName}
              onChange={(e) => setFeatureName(e.target.value)}
            />
          </FormControl>
        )}

        <FormControl isRequired>
          <FormLabel fontSize="sm">
            Wartość{isNumeric ? " (-1 = bez limitu)" : ""}
          </FormLabel>
          {isNumeric ? (
            <NumberInput
              size="sm"
              value={numericValue}
              onChange={(_, val) => setNumericValue(isNaN(val) ? 0 : val)}
              min={-1}
            >
              <NumberInputField />
            </NumberInput>
          ) : (
            <Input
              size="sm"
              placeholder='np. "true" lub "false"'
              value={stringValue}
              onChange={(e) => setStringValue(e.target.value)}
            />
          )}
        </FormControl>

        <FormControl isRequired>
          <FormLabel fontSize="sm">Powód</FormLabel>
          <Textarea
            size="sm"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            maxLength={1024}
            rows={3}
            placeholder="Wyjaśnienie dlaczego override jest stosowany"
          />
        </FormControl>

        <FormControl>
          <FormLabel fontSize="sm">
            Wygasa (opcjonalnie — brak = bezterminowo)
          </FormLabel>
          <Input
            size="sm"
            type="datetime-local"
            value={expiresAt}
            onChange={(e) => setExpiresAt(e.target.value)}
          />
        </FormControl>

        <HStack justify="flex-end">
          {/* character count for reason */}
          <span style={{ fontSize: "11px", color: "gray" }}>
            {reason.length}/1024
          </span>
        </HStack>
      </VStack>
    </AppModal>
  );
}
