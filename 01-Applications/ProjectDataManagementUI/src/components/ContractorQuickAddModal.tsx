import React, { useState, useEffect } from "react";
import {
  VStack,
  FormControl,
  FormLabel,
  Input,
  FormErrorMessage,
} from "@chakra-ui/react";
import AppModal from "./ui/AppModal";
import { useCreateContractor } from "../hooks/queries/useContractors";
import { useToastNotification } from "../hooks/useToastNotification";

export interface ContractorQuickAddInitialValues {
  name?: string;
  taxId?: string;
  street?: string;
}

export interface ContractorQuickAddModalProps {
  tenantId: string;
  isOpen: boolean;
  onClose: () => void;
  onCreated: (contractorId: string, contractorName: string) => void;
  /** Wartości wstępne — np. z sugestii AI */
  initialValues?: ContractorQuickAddInitialValues;
}

interface QuickAddFormState {
  name: string;
  taxId: string;
  email: string;
  phoneNumber: string;
  street: string;
}

const EMPTY_FORM: QuickAddFormState = {
  name: "",
  taxId: "",
  email: "",
  phoneNumber: "",
  street: "",
};

export default function ContractorQuickAddModal({
  tenantId,
  isOpen,
  onClose,
  onCreated,
  initialValues,
}: ContractorQuickAddModalProps): React.ReactElement {
  const [form, setForm] = useState<QuickAddFormState>(EMPTY_FORM);
  const [nameError, setNameError] = useState<string>("");
  const mutation = useCreateContractor(tenantId);
  const { showError } = useToastNotification();

  // Wypełnij formularz danymi z AI gdy modal się otwiera
  useEffect(() => {
    if (isOpen) {
      setForm({
        name: initialValues?.name ?? "",
        taxId: initialValues?.taxId ?? "",
        email: "",
        phoneNumber: "",
        street: initialValues?.street ?? "",
      });
      setNameError("");
    }
  }, [isOpen, initialValues?.name, initialValues?.taxId, initialValues?.street]);

  const handleClose = () => {
    setNameError("");
    onClose();
  };

  const handleAction = async () => {
    if (!form.name.trim()) {
      setNameError("Nazwa jest wymagana");
      return;
    }
    setNameError("");
    try {
      const result = await mutation.mutateAsync({
        name: form.name.trim(),
        taxId: form.taxId.trim() || null,
        email: form.email.trim() || null,
        phoneNumber: form.phoneNumber.trim() || null,
        street: form.street.trim() || null,
      });
      onCreated(result.id, result.name);
      handleClose();
    } catch (err) {
      showError(
        "Błąd dodawania kontrahenta",
        err instanceof Error ? err.message : undefined
      );
    }
  };

  return (
    <AppModal
      isOpen={isOpen}
      onClose={handleClose}
      title="Dodaj kontrahenta"
      actionLabel="Dodaj"
      actionColorScheme="green"
      onAction={handleAction}
      isActionLoading={mutation.isPending}
      isActionDisabled={mutation.isPending}
    >
      <VStack spacing={4} align="stretch">
        <FormControl isRequired isInvalid={!!nameError}>
          <FormLabel>Nazwa *</FormLabel>
          <Input
            value={form.name}
            onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
            placeholder="Nazwa kontrahenta"
          />
          {nameError && <FormErrorMessage>{nameError}</FormErrorMessage>}
        </FormControl>

        <FormControl>
          <FormLabel>NIP</FormLabel>
          <Input
            value={form.taxId}
            onChange={(e) => setForm((p) => ({ ...p, taxId: e.target.value }))}
            placeholder="np. 1234567890"
          />
        </FormControl>

        <FormControl>
          <FormLabel>Email</FormLabel>
          <Input
            type="email"
            value={form.email}
            onChange={(e) => setForm((p) => ({ ...p, email: e.target.value }))}
            placeholder="kontakt@firma.pl"
          />
        </FormControl>

        <FormControl>
          <FormLabel>Telefon</FormLabel>
          <Input
            type="tel"
            value={form.phoneNumber}
            onChange={(e) =>
              setForm((p) => ({ ...p, phoneNumber: e.target.value }))
            }
            placeholder="+48 000 000 000"
          />
        </FormControl>

        <FormControl>
          <FormLabel>Adres</FormLabel>
          <Input
            value={form.street}
            onChange={(e) => setForm((p) => ({ ...p, street: e.target.value }))}
            placeholder="ul. Przykładowa 1, Warszawa"
          />
        </FormControl>
      </VStack>
    </AppModal>
  );
}
