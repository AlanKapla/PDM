import { useState, useRef } from "react";
import {
  VStack,
  Text,
  Input,
  Textarea,
  FormControl,
  FormLabel,
  Alert,
  AlertIcon,
  HStack,
  Badge,
  Button,
} from "@chakra-ui/react";
import { FileText } from "lucide-react";
import AppModal from "./ui/AppModal";
import { projectApi } from "../api/projectApi";
import type { ProjectFileWeb } from "../types/project.types";
import { useToastNotification } from "../hooks/useToastNotification";

interface UploadNewVersionModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  file: ProjectFileWeb;
  onVersionUploaded: () => void;
}

export default function UploadNewVersionModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  file,
  onVersionUploaded,
}: UploadNewVersionModalProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [comment, setComment] = useState("");
  const [uploading, setUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { showSuccess, showError, showApiError } = useToastNotification();

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setSelectedFile(e.target.files[0]);
    }
  };

  const handleClose = () => {
    if (uploading) {
      return;
    }
    setSelectedFile(null);
    setComment("");
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
    onClose();
  };

  const handleUpload = async () => {
    if (!selectedFile) {
      showError("Błąd", "Wybierz plik do przesłania");
      return;
    }

    setUploading(true);

    try {
      await projectApi.uploadNewVersion(
        tenantId,
        projectId,
        (file as any).projectFileId || file.id,
        selectedFile,
        comment || undefined
      );

      showSuccess("Sukces", "Nowa wersja pliku została przesłana");

      onVersionUploaded();
      setSelectedFile(null);
      setComment("");
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
      onClose();
    } catch (error) {
      showApiError(error);
    } finally {
      setUploading(false);
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
  };

  return (
    <AppModal
      isOpen={isOpen}
      onClose={handleClose}
      title="Upload nowej wersji pliku"
      actionLabel="Prześlij wersję"
      actionColorScheme="primary"
      onAction={handleUpload}
      isActionLoading={uploading}
      isActionDisabled={!selectedFile}
      desktopSize="lg"
    >
      <VStack spacing={4} align="stretch">
        <Alert status="info">
          <AlertIcon aria-hidden="true" />
          <VStack align="start" spacing={0} flex={1} minW={0}>
            <Text fontSize="sm" fontWeight="semibold" noOfLines={2}>
              Plik bazowy: {file.displayName}
            </Text>
            <HStack flexWrap="wrap" gap={1}>
              <Text fontSize="xs" color="neutral.600">
                Aktualna wersja: v{file.currentVersion?.versionNumber}
              </Text>
              <Badge colorScheme="level2" fontSize="xs">
                {file.totalVersions} {file.totalVersions === 1 ? "wersja" : "wersji"}
              </Badge>
            </HStack>
          </VStack>
        </Alert>

        <FormControl isRequired>
          <FormLabel>Wybierz nowy plik</FormLabel>
          <Input
            ref={fileInputRef}
            type="file"
            accept="*/*"
            onChange={handleFileChange}
            display="none"
          />
          <Button
            onClick={() => fileInputRef.current?.click()}
            leftIcon={<FileText size={18} aria-hidden="true" />}
            variant="outline"
            width="100%"
            minH="44px"
            isDisabled={uploading}
          >
            {selectedFile ? selectedFile.name : "Wybierz plik"}
          </Button>
          {selectedFile && (
            <Text fontSize="xs" color="neutral.600" mt={1}>
              Rozmiar: {formatFileSize(selectedFile.size)}
            </Text>
          )}
        </FormControl>

        <FormControl>
          <FormLabel>Komentarz do wersji (opcjonalnie)</FormLabel>
          <Textarea
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="np. Poprawiono błędy w sekcji 3, zaktualizowano dane..."
            rows={3}
            isDisabled={uploading}
          />
          <Text fontSize="xs" color="neutral.600" mt={1}>
            Opisz zmiany wprowadzone w tej wersji
          </Text>
        </FormControl>
      </VStack>
    </AppModal>
  );
}
