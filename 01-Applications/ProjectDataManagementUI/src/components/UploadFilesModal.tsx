import { useState } from "react";
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
  FormControl,
  FormLabel,
  Input,
  Text,
  useToast,
  Box,
  HStack,
  IconButton,
  List,
  ListItem,
  FormErrorMessage,
} from "@chakra-ui/react";
import { X, Upload, FileText } from "lucide-react";

interface UploadFilesModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  projectName: string;
  onFilesUploaded: () => void;
}

interface FileWithDisplayName {
  file: File;
  displayName: string;
  comment: string;
}

const ALLOWED_TYPES = ['application/pdf', 'image/jpeg', 'image/jpg'];
const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

export default function UploadFilesModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  projectName,
  onFilesUploaded,
}: UploadFilesModalProps) {
  const [packageName, setPackageName] = useState("");
  const [files, setFiles] = useState<FileWithDisplayName[]>([]);
  const [uploading, setUploading] = useState(false);
  const [packageNameError, setPackageNameError] = useState("");
  const toast = useToast();

  const validateFile = (file: File): string | null => {
    if (!ALLOWED_TYPES.includes(file.type)) {
      return `Plik ${file.name} ma niedozwolony format. Dozwolone: PDF, JPG, JPEG`;
    }
    if (file.size > MAX_FILE_SIZE) {
      return `Plik ${file.name} jest za duży. Maksymalny rozmiar: 10MB`;
    }
    return null;
  };

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(event.target.files || []);
    
    const validatedFiles: FileWithDisplayName[] = [];
    
    for (const file of selectedFiles) {
      const error = validateFile(file);
      if (error) {
        toast({
          title: "Błąd walidacji pliku",
          description: error,
          status: "error",
          duration: 5000,
        });
        continue;
      }
      
      // Domyślna nazwa wyświetlana to nazwa pliku bez rozszerzenia
      const displayName = file.name.substring(0, file.name.lastIndexOf('.')) || file.name;
      validatedFiles.push({ file, displayName, comment: '' });
    }
    
    setFiles([...files, ...validatedFiles]);
    event.target.value = ''; // Reset input
  };

  const handleRemoveFile = (index: number) => {
    setFiles(files.filter((_, i) => i !== index));
  };

  const handleDisplayNameChange = (index: number, newDisplayName: string) => {
    const updatedFiles = [...files];
    updatedFiles[index].displayName = newDisplayName;
    setFiles(updatedFiles);
  };

  const handleCommentChange = (index: number, newComment: string) => {
    const updatedFiles = [...files];
    updatedFiles[index].comment = newComment;
    setFiles(updatedFiles);
  };

  const handleUpload = async () => {
    // Walidacja
    if (!packageName.trim()) {
      setPackageNameError("Nazwa katalogu jest wymagana");
      return;
    }
    
    if (files.length === 0) {
      toast({
        title: "Błąd",
        description: "Dodaj przynajmniej jeden plik",
        status: "error",
        duration: 3000,
      });
      return;
    }

    setUploading(true);
    setPackageNameError("");

    try {
      const { projectApi } = await import("../api/projectApi");
      
      const filesToUpload = files.map(f => ({
        file: f.file,
        displayName: f.displayName.trim() || undefined,
        comment: f.comment.trim() || undefined,
      }));

      const response = await projectApi.uploadFiles(
        tenantId,
        projectId,
        packageName.trim(),
        filesToUpload
      );

      if (response.ok) {
        toast({
          title: "Sukces",
          description: `Przesłano ${files.length} ${files.length === 1 ? 'plik' : 'plików'}`,
          status: "success",
          duration: 3000,
        });
        
        // Reset i zamknij
        setPackageName("");
        setFiles([]);
        onFilesUploaded();
        onClose();
      } else {
        const errorText = await response.text();
        toast({
          title: "Błąd przesyłania",
          description: errorText || "Nie udało się przesłać plików",
          status: "error",
          duration: 5000,
        });
      }
    } catch (error) {
      console.error("Błąd uploadu plików:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił błąd podczas przesyłania plików",
        status: "error",
        duration: 5000,
      });
    } finally {
      setUploading(false);
    }
  };

  const handleClose = () => {
    if (!uploading) {
      setPackageName("");
      setFiles([]);
      setPackageNameError("");
      onClose();
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} size="xl" isCentered>
      <ModalOverlay />
      <ModalContent maxW="600px">
        <ModalHeader>Dodaj pliki do projektu</ModalHeader>
        <ModalCloseButton isDisabled={uploading} />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            <Text fontSize="sm" color="gray.600">
              Projekt: <Text as="span" fontWeight="bold">{projectName}</Text>
            </Text>

            <FormControl isRequired isInvalid={!!packageNameError}>
              <FormLabel>Nazwa katalogu</FormLabel>
              <Input
                value={packageName}
                onChange={(e) => {
                  setPackageName(e.target.value);
                  setPackageNameError("");
                }}
                placeholder="np. Dokumentacja, Zdjęcia, Rysunki"
                isDisabled={uploading}
              />
              <FormErrorMessage>{packageNameError}</FormErrorMessage>
              <Text fontSize="xs" color="gray.500" mt={1}>
                Pliki zostaną zapisane w tym katalogu
              </Text>
            </FormControl>

            <FormControl>
              <FormLabel>Pliki (PDF, JPG, JPEG, max 10MB)</FormLabel>
              <Button
                leftIcon={<Upload size={18} />}
                onClick={() => document.getElementById('file-input')?.click()}
                isDisabled={uploading}
                width="100%"
                variant="outline"
              >
                Wybierz pliki
              </Button>
              <Input
                id="file-input"
                type="file"
                multiple
                accept=".pdf,.jpg,.jpeg,image/jpeg,image/jpg,application/pdf"
                onChange={handleFileSelect}
                display="none"
              />
            </FormControl>

            {files.length > 0 && (
              <Box>
                <Text fontSize="sm" fontWeight="medium" mb={2}>
                  Wybrane pliki ({files.length}):
                </Text>
                <List spacing={2}>
                  {files.map((item, index) => (
                    <ListItem key={index}>
                      <Box
                        p={3}
                        borderWidth="1px"
                        borderRadius="md"
                        bg="gray.50"
                      >
                        <VStack align="stretch" spacing={2}>
                          <HStack justify="space-between">
                            <HStack flex={1}>
                              <FileText size={18} />
                              <VStack align="flex-start" spacing={0} flex={1}>
                                <Text fontSize="sm" fontWeight="medium" noOfLines={1}>
                                  {item.file.name}
                                </Text>
                                <Text fontSize="xs" color="gray.500">
                                  {formatFileSize(item.file.size)}
                                </Text>
                              </VStack>
                            </HStack>
                            <IconButton
                              aria-label="Usuń plik"
                              icon={<X size={16} />}
                              size="sm"
                              colorScheme="red"
                              variant="ghost"
                              onClick={() => handleRemoveFile(index)}
                              isDisabled={uploading}
                            />
                          </HStack>
                          <FormControl size="sm">
                            <FormLabel fontSize="xs">Nazwa wyświetlana (opcjonalna)</FormLabel>
                            <Input
                              size="sm"
                              value={item.displayName}
                              onChange={(e) => handleDisplayNameChange(index, e.target.value)}
                              placeholder="Domyślnie: nazwa pliku"
                              isDisabled={uploading}
                            />
                          </FormControl>
                          <FormControl size="sm">
                            <FormLabel fontSize="xs">Komentarz (opcjonalny)</FormLabel>
                            <Input
                              size="sm"
                              value={item.comment}
                              onChange={(e) => handleCommentChange(index, e.target.value)}
                              placeholder="Dodaj komentarz do pliku"
                              isDisabled={uploading}
                            />
                          </FormControl>
                        </VStack>
                      </Box>
                    </ListItem>
                  ))}
                </List>
              </Box>
            )}
          </VStack>
        </ModalBody>
        <ModalFooter>
          <Button
            variant="ghost"
            mr={3}
            onClick={handleClose}
            isDisabled={uploading}
          >
            Anuluj
          </Button>
          <Button
            colorScheme="blue"
            onClick={handleUpload}
            isLoading={uploading}
            loadingText="Przesyłanie..."
            isDisabled={files.length === 0}
          >
            Prześlij {files.length > 0 && `(${files.length})`}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
