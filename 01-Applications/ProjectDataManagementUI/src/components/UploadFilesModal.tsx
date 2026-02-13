import { useState, useEffect } from "react";
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
  Box,
  HStack,
  IconButton,
  List,
  ListItem,
  FormErrorMessage,
  Select,
  Radio,
  RadioGroup,
  Stack,
  Spinner,
  useBreakpointValue
} from "@chakra-ui/react";
import { X, Upload, FileText, Package } from "lucide-react";
import { handleApiError } from "../utils/handleApiError";
import { projectApi, ResourceScope } from "../api/projectApi";
import { useToastNotification } from "../hooks/useToastNotification";
import { FILE_UPLOAD } from "../utils/constants";
import { formatFileSize } from "../utils/formatters";
import type { ProjectFilePackageWeb } from "../types/project.types";

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

const modalSize = "full";
const modalScroll = "inside";

export default function UploadFilesModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  projectName,
  onFilesUploaded,
}: UploadFilesModalProps) {
  const [mode, setMode] = useState<"new" | "existing">("new");
  const [packageName, setPackageName] = useState("");
  const [selectedPackageId, setSelectedPackageId] = useState("");
  const [packages, setPackages] = useState<ProjectFilePackageWeb[]>([]);
  const [loadingPackages, setLoadingPackages] = useState(false);
  const [files, setFiles] = useState<FileWithDisplayName[]>([]);
  const [uploading, setUploading] = useState(false);
  const [packageNameError, setPackageNameError] = useState("");
  const { showSuccess, showError } = useToastNotification();

  useEffect(() => {
    if (isOpen && mode === "existing") {
      fetchMyPackages();
    }
  }, [isOpen, mode, tenantId, projectId]);

  const fetchMyPackages = async () => {
    setLoadingPackages(true);
    try {
      const response = await projectApi.getProjectFilePackages(tenantId, projectId, ResourceScope.Mine);
      const data: ProjectFilePackageWeb[] = response.data;
      setPackages(data);
    } catch (error) {
      console.error("Błąd pobierania paczek:", error);
      showError("Błąd", "Nie udało się pobrać listy paczek");
    } finally {
      setLoadingPackages(false);
    }
  };

  const validateFile = (file: File): string | null => {
    if (!FILE_UPLOAD.ALLOWED_TYPES.includes(file.type as any)) {
      return `Plik ${file.name} ma niedozwolony format. Dozwolone: ${FILE_UPLOAD.ALLOWED_TYPES_DISPLAY}`;
    }
    if (file.size > FILE_UPLOAD.MAX_FILE_SIZE) {
      return `Plik ${file.name} jest za duży. Maksymalny rozmiar: ${formatFileSize(FILE_UPLOAD.MAX_FILE_SIZE)}`;
    }
    return null;
  };

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(event.target.files || []);

    const validatedFiles: FileWithDisplayName[] = [];

    for (const file of selectedFiles) {
      const error = validateFile(file);
      if (error) {
        showError("Błąd walidacji pliku", error);
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
    if (mode === "new" && !packageName.trim()) {
      setPackageNameError("Nazwa paczki jest wymagana");
      return;
    }

    if (mode === "existing" && !selectedPackageId) {
      showError("Błąd", "Wybierz paczkę");
      return;
    }

    if (files.length === 0) {
      showError("Błąd", "Dodaj przynajmniej jeden plik");
      return;
    }

    setUploading(true);
    setPackageNameError("");

    try {
      const filesToUpload = files.map(f => ({
        file: f.file,
        displayName: f.displayName.trim() || undefined,
        comment: f.comment.trim() || undefined,
      }));

      if (mode === "new") {
        await projectApi.createPackageAndUploadFiles(
          tenantId,
          projectId,
          packageName.trim(),
          filesToUpload
        );
      } else {
        await projectApi.addFilesToPackage(
          tenantId,
          projectId,
          selectedPackageId,
          filesToUpload
        );
      }

      showSuccess("Sukces", `Przesłano ${files.length} ${files.length === 1 ? 'plik' : 'plików'}`);

      // Reset i zamknij
      setMode("new");
      setPackageName("");
      setSelectedPackageId("");
      setFiles([]);
      onFilesUploaded();
      onClose();
    } catch (error) {
      console.error("Błąd uploadu plików:", error);
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setUploading(false);
    }
  };

  const handleClose = () => {
    if (!uploading) {
      setMode("new");
      setPackageName("");
      setSelectedPackageId("");
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
      <Modal
        isOpen={isOpen}
        onClose={handleClose}
        size={modalSize}
        scrollBehavior={modalScroll}
        isCentered
      >
      <ModalOverlay />
      <ModalContent maxW={{ base: "100%", md: "600px" }} mx={{ base: 0, md: "auto" }}>
        <ModalHeader fontSize={{ base: "lg", md: "xl" }}>Dodaj pliki do projektu</ModalHeader>
        <ModalCloseButton isDisabled={uploading} />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            <Text fontSize="sm" color="gray.600">
              Projekt: <Text as="span" fontWeight="bold">{projectName}</Text>
            </Text>

            <FormControl>
              <FormLabel>Tryb dodawania</FormLabel>
              <RadioGroup value={mode} onChange={(value) => setMode(value as "new" | "existing")}>
                <Stack direction="row" spacing={4}>
                  <Radio value="new" isDisabled={uploading}>
                    Nowa paczka
                  </Radio>
                  <Radio value="existing" isDisabled={uploading}>
                    Istniejąca paczka
                  </Radio>
                </Stack>
              </RadioGroup>
            </FormControl>

            {mode === "new" ? (
              <FormControl isRequired isInvalid={!!packageNameError}>
                <FormLabel>Nazwa paczki</FormLabel>
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
                  Pliki zostaną zapisane w nowej paczce
                </Text>
              </FormControl>
            ) : (
              <FormControl isRequired>
                <FormLabel>Wybierz paczkę</FormLabel>
                {loadingPackages ? (
                  <HStack justify="center" py={2}>
                    <Spinner size="sm" />
                    <Text fontSize="sm">Ładowanie paczek...</Text>
                  </HStack>
                ) : packages.length === 0 ? (
                  <Text fontSize="sm" color="gray.500">
                    Nie masz jeszcze żadnych paczek. Przełącz się na "Nowa paczka".
                  </Text>
                ) : (
                  <>
                    <Select
                      value={selectedPackageId}
                      onChange={(e) => setSelectedPackageId(e.target.value)}
                      placeholder="Wybierz paczkę"
                      isDisabled={uploading}
                      icon={<Package size={16} />}
                    >
                      {packages.map((pkg) => (
                        <option key={pkg.id} value={pkg.id}>
                          {pkg.name} ({pkg.totalFiles} {pkg.totalFiles === 1 ? 'plik' : 'plików'})
                        </option>
                      ))}
                    </Select>
                    <Text fontSize="xs" color="gray.500" mt={1}>
                      Pliki zostaną dodane do wybranej paczki
                    </Text>
                  </>
                )}
              </FormControl>
            )}

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
