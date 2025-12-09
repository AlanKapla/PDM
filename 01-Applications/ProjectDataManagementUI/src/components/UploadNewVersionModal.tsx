import { useState, useRef } from "react";
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  Button,
  VStack,
  Text,
  Input,
  Textarea,
  FormControl,
  FormLabel,
  useToast,
  Alert,
  AlertIcon,
  HStack,
  Badge,
} from "@chakra-ui/react";
import { Upload, FileText } from "lucide-react";
import { projectApi } from "../api/projectApi";
import type { ProjectFileWeb } from "../types/project.types";

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
  const toast = useToast();

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setSelectedFile(e.target.files[0]);
    }
  };

  const handleUpload = async () => {
    if (!selectedFile) {
      toast({
        title: "Błąd",
        description: "Wybierz plik do przesłania",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    setUploading(true);

    try {
      const response = await projectApi.uploadNewVersion(
        tenantId,
        projectId,
        (file as any).projectFileId || file.id,
        selectedFile,
        comment || undefined
      );

      if (response.ok) {
        toast({
          title: "Sukces",
          description: "Nowa wersja pliku została przesłana",
          status: "success",
          duration: 3000,
          isClosable: true,
        });

        onVersionUploaded();
        handleClose();
      } else {
        throw new Error("Nie udało się przesłać pliku");
      }
    } catch (error) {
      console.error("Błąd podczas przesyłania nowej wersji:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się przesłać nowej wersji pliku",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setUploading(false);
    }
  };

  const handleClose = () => {
    setSelectedFile(null);
    setComment("");
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
    onClose();
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} size="lg">
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>Upload nowej wersji pliku</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            <Alert status="info">
              <AlertIcon />
              <VStack align="start" spacing={0} flex={1}>
                <Text fontSize="sm" fontWeight="semibold">
                  Plik bazowy: {file.displayName}
                </Text>
                <HStack>
                  <Text fontSize="xs" color="gray.600">
                    Aktualna wersja: v{file.currentVersion?.versionNumber}
                  </Text>
                  <Badge colorScheme="purple" fontSize="xs">
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
                leftIcon={<FileText size={18} />}
                variant="outline"
                width="100%"
              >
                {selectedFile ? selectedFile.name : "Wybierz plik"}
              </Button>
              {selectedFile && (
                <Text fontSize="xs" color="gray.600" mt={1}>
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
              />
              <Text fontSize="xs" color="gray.600" mt={1}>
                Opisz zmiany wprowadzone w tej wersji
              </Text>
            </FormControl>
          </VStack>
        </ModalBody>
        <ModalFooter>
          <Button variant="ghost" mr={3} onClick={handleClose} isDisabled={uploading}>
            Anuluj
          </Button>
          <Button
            colorScheme="blue"
            leftIcon={<Upload size={18} />}
            onClick={handleUpload}
            isLoading={uploading}
            loadingText="Przesyłanie..."
          >
            Prześlij wersję
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
