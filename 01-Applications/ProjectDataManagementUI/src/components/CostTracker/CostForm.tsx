import { useRef } from "react";
import {
  VStack,
  HStack,
  FormControl,
  FormLabel,
  FormErrorMessage,
  Input,
  Textarea,
  NumberInput,
  NumberInputField,
  Button,
  Text,
  Box,
  IconButton,
} from "@chakra-ui/react";
import { Plus, X } from "lucide-react";
import AttachmentList from "./AttachmentList";
import { useToastNotification } from "../../hooks/useToastNotification";
import type { TrackedCostAttachmentWeb, CostFormValues } from "../../types/costTracker.types";

interface CostFormProps {
  values: CostFormValues;
  onChange: (values: CostFormValues) => void;
  existingAttachments?: TrackedCostAttachmentWeb[];
  errors?: Partial<Record<keyof CostFormValues, string>>;
  isSubmitting?: boolean;
}

const MAX_FILE_SIZE = 52 * 1024 * 1024; // 52 MB

export default function CostForm({
  values,
  onChange,
  existingAttachments = [],
  errors = {},
  isSubmitting = false,
}: CostFormProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { showWarning } = useToastNotification();

  const set = (patch: Partial<CostFormValues>) => onChange({ ...values, ...patch });

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const all = Array.from(e.target.files ?? []);
    const rejected = all.filter((f) => f.size > MAX_FILE_SIZE);
    const picked = all.filter((f) => f.size <= MAX_FILE_SIZE);
    if (rejected.length > 0) {
      const names = rejected.map((f) => f.name).join(", ");
      showWarning("Plik za duży", `Przekroczono limit 52 MB: ${names}`);
    }
    set({ newFiles: [...(values.newFiles ?? []), ...picked] });
    e.target.value = "";
  };

  const removeNewFile = (index: number) => {
    const updated = (values.newFiles ?? []).filter((_, i) => i !== index);
    set({ newFiles: updated });
  };

  const removeExisting = (id: string) => {
    const current = values.existingAttachmentIds ?? existingAttachments.map((a) => a.id);
    set({ existingAttachmentIds: current.filter((eId) => eId !== id) });
  };

  const removedIds = existingAttachments
    .map((a) => a.id)
    .filter((id) => !(values.existingAttachmentIds ?? existingAttachments.map((a) => a.id)).includes(id));

  return (
    <VStack spacing={4} align="stretch">
      {/* Nazwa */}
      <FormControl isRequired isInvalid={!!errors.name}>
        <FormLabel>Nazwa</FormLabel>
        <Input
          value={values.name}
          onChange={(e) => set({ name: e.target.value })}
          placeholder="Nazwa kosztu"
          maxLength={300}
          isDisabled={isSubmitting}
        />
        <FormErrorMessage>{errors.name}</FormErrorMessage>
      </FormControl>

      {/* Kwoty + numer faktury */}
      <HStack spacing={4} align="flex-start" flexDir={{ base: "column", sm: "row" }}>
        <FormControl isInvalid={!!errors.net} flex={1}>
          <FormLabel>Kwota netto (PLN)</FormLabel>
          <NumberInput
            value={values.net ?? ""}
            onChange={(_, num) => set({ net: isNaN(num) ? undefined : num })}
            min={0}
            precision={2}
            isDisabled={isSubmitting}
          >
            <NumberInputField placeholder="0,00" />
          </NumberInput>
          <FormErrorMessage>{errors.net}</FormErrorMessage>
        </FormControl>

        <FormControl flex={1}>
          <FormLabel>Numer faktury</FormLabel>
          <Input
            value={values.number ?? ""}
            onChange={(e) => set({ number: e.target.value })}
            placeholder="np. FV/2024/001"
            maxLength={100}
            isDisabled={isSubmitting}
          />
        </FormControl>
      </HStack>

      {/* Wykonawca */}
      <FormControl>
        <FormLabel>Wykonawca</FormLabel>
        <Input
          value={values.contractor ?? ""}
          onChange={(e) => set({ contractor: e.target.value })}
          placeholder="Nazwa wykonawcy"
          maxLength={300}
          isDisabled={isSubmitting}
        />
      </FormControl>

      {/* Data */}
      <FormControl>
        <FormLabel>Data</FormLabel>
        <Input
          type="date"
          value={values.date ?? ""}
          onChange={(e) => set({ date: e.target.value || undefined })}
          isDisabled={isSubmitting}
        />
      </FormControl>

      {/* Opis */}
      <FormControl>
        <FormLabel>Opis</FormLabel>
        <Textarea
          value={values.description ?? ""}
          onChange={(e) => set({ description: e.target.value })}
          placeholder="Opcjonalny opis kosztu"
          maxLength={2000}
          rows={3}
          isDisabled={isSubmitting}
        />
      </FormControl>

      {/* Istniejące załączniki */}
      {existingAttachments.length > 0 && (
        <Box>
          <Text fontSize="sm" fontWeight="semibold" mb={2}>
            Załączniki
          </Text>
          <AttachmentList
            attachments={existingAttachments}
            removedIds={removedIds}
            onRemove={removeExisting}
          />
        </Box>
      )}

      {/* Nowe pliki */}
      <Box>
        <HStack mb={2}>
          <Text fontSize="sm" fontWeight="semibold">
            Dodaj pliki
          </Text>
          <Button
            size="xs"
            leftIcon={<Plus size={12} />}
            onClick={() => fileInputRef.current?.click()}
            isDisabled={isSubmitting}
          >
            Wybierz pliki
          </Button>
        </HStack>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          style={{ display: "none" }}
          onChange={handleFileChange}
        />
        {(values.newFiles ?? []).length > 0 && (
          <VStack align="stretch" spacing={1}>
            {(values.newFiles ?? []).map((file, idx) => (
              <HStack
                key={`${file.name}-${idx}`}
                px={2}
                py={1}
                borderRadius="md"
                bg="blue.50"
                _dark={{ bg: "blue.900" }}
              >
                <Text fontSize="sm" flex={1} noOfLines={1}>
                  {file.name}
                </Text>
                <IconButton
                  aria-label="Usuń plik"
                  icon={<X size={12} />}
                  size="xs"
                  variant="ghost"
                  colorScheme="red"
                  onClick={() => removeNewFile(idx)}
                  isDisabled={isSubmitting}
                />
              </HStack>
            ))}
          </VStack>
        )}
        <Text fontSize="xs" color="neutral.500" mt={1}>
          Maksymalny rozmiar pliku: 52 MB
        </Text>
      </Box>
    </VStack>
  );
}

// Walidacja formularza — wywoływana przed submitem
export function validateCostForm(values: CostFormValues): Partial<Record<keyof CostFormValues, string>> {
  const errors: Partial<Record<keyof CostFormValues, string>> = {};

  if (!values.name?.trim()) {
    errors.name = "Nazwa jest wymagana";
  }

  const hasNet = values.net !== undefined && values.net !== "" && !isNaN(Number(values.net));

  if (!hasNet) {
    errors.net = "Podaj kwotę netto";
  }

  return errors;
}
