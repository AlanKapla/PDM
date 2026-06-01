import { useState } from "react";
import {
  FormControl,
  FormLabel,
  FormHelperText,
  FormErrorMessage,
  Input,
  Select,
  VStack,
} from "@chakra-ui/react";
import AppModal from "./ui/AppModal";
import { useCreateDirectory } from "../hooks/queries/useProjectFiles";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import { flattenCatalogsForSelect } from "../utils/flattenCatalogsForSelect";
import type { ProjectFilePackageWeb } from "../types/project.types";

interface CreateDirectoryModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  tenantId: string;
  projectId: string;
  catalogs: ProjectFilePackageWeb[];
  defaultParentId?: string;
}

export default function CreateDirectoryModal({
  isOpen,
  onClose,
  onSuccess,
  tenantId,
  projectId,
  catalogs,
  defaultParentId = "",
}: CreateDirectoryModalProps) {
  const [directoryName, setDirectoryName] = useState("");
  const [parentId, setParentId] = useState<string>(defaultParentId);
  const [nameError, setNameError] = useState("");

  const { showError } = useToastNotification();
  const createDirectory = useCreateDirectory();

  const handleClose = () => {
    if (!createDirectory.isPending) {
      setDirectoryName("");
      setParentId(defaultParentId);
      setNameError("");
      onClose();
    }
  };

  const handleCreate = async () => {
    const trimmedName = directoryName.trim();

    if (!trimmedName) {
      setNameError("Nazwa katalogu jest wymagana");
      return;
    }
    if (trimmedName.length > 200) {
      setNameError("Nazwa katalogu nie może przekraczać 200 znaków");
      return;
    }

    setNameError("");

    try {
      await createDirectory.mutateAsync({
        tenantId,
        projectId,
        directoryName: trimmedName,
        parentId: parentId || null,
      });

      setDirectoryName("");
      setParentId(defaultParentId);
      onSuccess();
      onClose();
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    }
  };

  const flatOptions = flattenCatalogsForSelect(catalogs);

  return (
    <AppModal
      isOpen={isOpen}
      onClose={handleClose}
      title="Utwórz katalog"
      actionLabel="Utwórz katalog"
      actionColorScheme="green"
      onAction={handleCreate}
      isActionLoading={createDirectory.isPending}
      isActionDisabled={!directoryName.trim()}
    >
      <VStack spacing={4} align="stretch">
        <FormControl isRequired isInvalid={!!nameError}>
          <FormLabel>Nazwa katalogu</FormLabel>
          <Input
            value={directoryName}
            onChange={(e) => {
              setDirectoryName(e.target.value);
              if (nameError) setNameError("");
            }}
            placeholder="np. Dokumentacja, Zdjęcia, Rysunki"
            isDisabled={createDirectory.isPending}
            maxLength={200}
          />
          <FormErrorMessage>{nameError}</FormErrorMessage>
        </FormControl>

        <FormControl>
          <FormLabel>Katalog nadrzędny (opcjonalnie)</FormLabel>
          <Select
            value={parentId}
            onChange={(e) => setParentId(e.target.value)}
            placeholder="Brak — utwórz jako katalog główny"
            isDisabled={createDirectory.isPending}
          >
            {flatOptions.map((item) => (
              <option key={item.id} value={item.id}>
                {item.label}
              </option>
            ))}
          </Select>
          <FormHelperText>
            Jeśli nie wybierzesz, katalog zostanie dodany jako główny
          </FormHelperText>
        </FormControl>
      </VStack>
    </AppModal>
  );
}
