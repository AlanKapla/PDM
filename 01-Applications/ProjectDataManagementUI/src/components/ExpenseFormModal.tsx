import { useEffect, useRef, useState } from "react";
import {
  AlertDialog,
  AlertDialogBody,
  AlertDialogContent,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogOverlay,
  Button,
  Checkbox,
  FormControl,
  FormErrorMessage,
  FormLabel,
  HStack,
  IconButton,
  Input,
  Modal,
  ModalBody,
  ModalCloseButton,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalOverlay,
  Text,
  Textarea,
  VStack,
  useBreakpointValue,
  useDisclosure,
} from "@chakra-ui/react";
import { FileUp, X } from "lucide-react";
import type { ProjectCostListItemWeb } from "../types/project.types";

export interface ExpenseFormData {
  name: string;
  place: string;
  date: string;
  description: string;
  netAmount: string;
  grossAmount: string;
  isAccepted: boolean;
  removeDocument: boolean;
}

const EMPTY_FORM: ExpenseFormData = {
  name: "",
  place: "",
  date: new Date().toISOString().split("T")[0],
  description: "",
  netAmount: "",
  grossAmount: "",
  isAccepted: false,
  removeDocument: false,
};

function toFormData(cost: ProjectCostListItemWeb): ExpenseFormData {
  return {
    name: cost.name,
    place: cost.place || "",
    date: cost.date.split("T")[0],
    description: cost.description || "",
    netAmount: cost.netAmount != null && cost.netAmount !== 0 ? cost.netAmount.toString() : "",
    grossAmount: cost.grossAmount !== 0 ? cost.grossAmount.toString() : "",
    isAccepted: cost.isAccepted,
    removeDocument: false,
  };
}

interface ExpenseFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  editingCost?: ProjectCostListItemWeb | null;
  documentFile: File | null;
  onDocumentFileChange: (file: File | null) => void;
  onSave: (data: ExpenseFormData) => void;
  isSaving: boolean;
}

export default function ExpenseFormModal({
  isOpen,
  onClose,
  editingCost,
  documentFile,
  onDocumentFileChange,
  onSave,
  isSaving,
}: ExpenseFormModalProps) {
  const [form, setForm] = useState<ExpenseFormData>(EMPTY_FORM);
  const [submitted, setSubmitted] = useState(false);

  const modalSize = useBreakpointValue({ base: "full", md: "lg" });

  const {
    isOpen: isRemoveDocOpen,
    onOpen: onRemoveDocOpen,
    onClose: onRemoveDocClose,
  } = useDisclosure();
  const cancelRemoveRef = useRef<HTMLButtonElement>(null);

  const fileInputId = editingCost
    ? `edit-expense-doc-${editingCost.id}`
    : "new-expense-doc";

  // Reset/initialize form when modal opens or target cost changes
  useEffect(() => {
    if (isOpen) {
      setForm(editingCost ? toFormData(editingCost) : EMPTY_FORM);
      setSubmitted(false);
    }
  }, [isOpen, editingCost]);

  const amountMissing = !form.netAmount.trim() && !form.grossAmount.trim();
  const amountInvalid = submitted && amountMissing;

  // Show document chip when: new file selected OR existing doc not yet removed
  const hasDocument =
    !!documentFile || (!!editingCost?.hasDocument && !form.removeDocument);
  const documentName =
    documentFile?.name ?? editingCost?.documentFileName ?? "";

  const handleSave = () => {
    setSubmitted(true);
    if (!form.name.trim() || !form.date || amountMissing) return;
    onSave(form);
  };

  const handleConfirmRemoveDocument = () => {
    onDocumentFileChange(null);
    setForm((prev) => ({ ...prev, removeDocument: true }));
    onRemoveDocClose();
  };

  const isEdit = !!editingCost;

  return (
    <>
      <Modal
        isOpen={isOpen}
        onClose={onClose}
        size={modalSize}
        closeOnOverlayClick={false}
        scrollBehavior="inside"
      >
        <ModalOverlay />
        <ModalContent sx={{ "input, textarea, select": { fontSize: "16px" } }}>
          <ModalHeader>{isEdit ? "Edytuj koszt" : "Dodaj koszt"}</ModalHeader>
          <ModalCloseButton />

          <ModalBody>
            <VStack spacing={4} align="stretch">
              {/* Nazwa */}
              <FormControl isRequired isInvalid={submitted && !form.name.trim()}>
                <FormLabel>Nazwa</FormLabel>
                <Input
                  value={form.name}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, name: e.target.value }))
                  }
                  placeholder="Nazwa kosztu"
                />
                <FormErrorMessage>Nazwa jest wymagana.</FormErrorMessage>
              </FormControl>

              {/* Miejsce */}
              <FormControl>
                <FormLabel>Miejsce</FormLabel>
                <Input
                  value={form.place}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, place: e.target.value }))
                  }
                  placeholder="Np. Warszawa"
                />
              </FormControl>

              {/* Data */}
              <FormControl isRequired isInvalid={submitted && !form.date}>
                <FormLabel>Data</FormLabel>
                <Input
                  type="date"
                  value={form.date}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, date: e.target.value }))
                  }
                />
                <FormErrorMessage>Data jest wymagana.</FormErrorMessage>
              </FormControl>

              {/* Opis */}
              <FormControl>
                <FormLabel>Opis</FormLabel>
                <Textarea
                  value={form.description}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, description: e.target.value }))
                  }
                  placeholder="Opcjonalny opis kosztu"
                  rows={2}
                />
              </FormControl>

              {/* Kwota netto + brutto */}
              <FormControl isInvalid={amountInvalid}>
                <HStack align="flex-start" spacing={4}>
                  <FormControl flex={1}>
                    <FormLabel>Kwota netto</FormLabel>
                    <Input
                      type="number"
                      step="0.01"
                      min="0"
                      value={form.netAmount}
                      onChange={(e) =>
                        setForm((p) => ({ ...p, netAmount: e.target.value }))
                      }
                      placeholder="0.00"
                    />
                  </FormControl>
                  <FormControl flex={1}>
                    <FormLabel>Kwota brutto</FormLabel>
                    <Input
                      type="number"
                      step="0.01"
                      min="0"
                      value={form.grossAmount}
                      onChange={(e) =>
                        setForm((p) => ({ ...p, grossAmount: e.target.value }))
                      }
                      placeholder="0.00"
                    />
                  </FormControl>
                </HStack>
                {amountInvalid && (
                  <FormErrorMessage>
                    Podaj kwotę brutto lub netto — jedno pole jest wymagane.
                  </FormErrorMessage>
                )}
              </FormControl>

              {/* Dokument */}
              <FormControl>
                <FormLabel>Dokument</FormLabel>
                {hasDocument ? (
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
                        onDocumentFileChange(e.target.files?.[0] || null)
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

              {/* Zaakceptowane */}
              <FormControl>
                <Checkbox
                  isChecked={form.isAccepted}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, isAccepted: e.target.checked }))
                  }
                  colorScheme="green"
                >
                  Zaakceptowane
                </Checkbox>
              </FormControl>
            </VStack>
          </ModalBody>

          <ModalFooter>
            <HStack spacing={2}>
              <Button variant="ghost" onClick={onClose} isDisabled={isSaving}>
                Anuluj
              </Button>
              <Button
                colorScheme="green"
                onClick={handleSave}
                isLoading={isSaving}
              >
                Zapisz
              </Button>
            </HStack>
          </ModalFooter>
        </ModalContent>
      </Modal>

      {/* Potwierdzenie usunięcia dokumentu */}
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
                onClick={handleConfirmRemoveDocument}
                ml={3}
              >
                Tak, usuń
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>
    </>
  );
}
