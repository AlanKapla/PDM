import { useState, useEffect } from "react";
import {
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
} from "@chakra-ui/react";
import { X, Upload, FileText } from "lucide-react";
import AppModal from "./ui/AppModal";
import { flattenCatalogsForSelect } from "../utils/flattenCatalogsForSelect";
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
  /** Gdy ustawione — tryb direct: upload wprost do tego katalogu, bez wyboru */
  targetCatalogId?: string;
}

interface FileWithDisplayName {
  file: File;
  displayName: string;
  comment: string;
}

export default function UploadFilesModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  projectName,
  onFilesUploaded,
  targetCatalogId,
}: UploadFilesModalProps) {
  const isDirect = !!targetCatalogId;
  const [mode, setMode] = useState<"new" | "existing">("new");
  const [packageName, setPackageName] = useState("");
  const [selectedPackageId, setSelectedPackageId] = useState("");
  const [packages, setPackages] = useState<ProjectFilePackageWeb[]>([]);
  const [loadingPackages, setLoadingPackages] = useState(false);
  const [files, setFiles] = useState<FileWithDisplayName[]>([]);
  const [uploading, setUploading] = useState(false);
  const [packageNameError, setPackageNameError] = useState("");
  const [parentDirectoryId, setParentDirectoryId] = useState<string>("");
  const { showSuccess, showError, showApiError } = useToastNotification();

  useEffect(() => {
    if (isOpen) {
      setFiles([]);
      setPackageNameError("");
      if (!isDirect) {
        setMode("new");
        setPackageName("");
        setSelectedPackageId("");
        setParentDirectoryId("");
        fetchMyPackages();
      }
    }
  }, [isOpen, tenantId, projectId]);

  const fetchMyPackages = async () => {
    setLoadingPackages(true);
    try {
      const response = await projectApi.getProjectFilePackages(tenantId, projectId, ResourceScope.Mine);
      const data: ProjectFilePackageWeb[] = response.data;
      setPackages(data);
    } catch {
      showError("Błąd", "Nie udało się pobrać listy katalogów");
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

      const displayName = file.name.substring(0, file.name.lastIndexOf(".")) || file.name;
      validatedFiles.push({ file, displayName, comment: "" });
    }

    setFiles([...files, ...validatedFiles]);
    event.target.value = "";
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
    if (isDirect) {
      if (files.length === 0) {
        showError("Błąd", "Dodaj przynajmniej jeden plik");
        return;
      }
      setUploading(true);
      try {
        const filesToUpload = files.map((f) => ({
          file: f.file,
          displayName: f.displayName.trim() || undefined,
          comment: f.comment.trim() || undefined,
        }));
        await projectApi.addFilesToPackage(tenantId, projectId, targetCatalogId!, filesToUpload);
        showSuccess("Sukces", `Przesłano ${files.length} ${files.length === 1 ? "plik" : "plików"}`);
        setFiles([]);
        onFilesUploaded();
        onClose();
      } catch (error) {
        showApiError(error);
      } finally {
        setUploading(false);
      }
      return;
    }

    if (mode === "new" && !packageName.trim()) {
      setPackageNameError("Nazwa katalogu jest wymagana");
      return;
    }

    if (mode === "new") {
      const trimmedName = packageName.trim().toLowerCase();
      const duplicate = packages.find((p) => p.name.toLowerCase() === trimmedName);
      if (duplicate) {
        setPackageNameError("Katalog o tej nazwie już istnieje. Wybierz inną nazwę lub dodaj pliki do istniejącego katalogu.");
        return;
      }
    }

    if (mode === "existing" && !selectedPackageId) {
      showError("Błąd", "Wybierz katalog");
      return;
    }

    if (files.length === 0) {
      showError("Błąd", "Dodaj przynajmniej jeden plik");
      return;
    }

    setUploading(true);
    setPackageNameError("");

    try {
      const filesToUpload = files.map((f) => ({
        file: f.file,
        displayName: f.displayName.trim() || undefined,
        comment: f.comment.trim() || undefined,
      }));

      if (mode === "new") {
        await projectApi.createPackageAndUploadFiles(
          tenantId,
          projectId,
          packageName.trim(),
          filesToUpload,
          parentDirectoryId || undefined
        );
      } else {
        await projectApi.addFilesToPackage(
          tenantId,
          projectId,
          selectedPackageId,
          filesToUpload
        );
      }

      showSuccess("Sukces", `Przesłano ${files.length} ${files.length === 1 ? "plik" : "plików"}`);

      setMode("new");
      setPackageName("");
      setSelectedPackageId("");
      setParentDirectoryId("");
      setFiles([]);
      onFilesUploaded();
      onClose();
    } catch (error) {
      showApiError(error);
    } finally {
      setUploading(false);
    }
  };

  const handleClose = () => {
    if (!uploading) {
      setMode("new");
      setPackageName("");
      setSelectedPackageId("");
      setParentDirectoryId("");
      setFiles([]);
      setPackageNameError("");
      onClose();
    }
  };

  return (
    <AppModal
      isOpen={isOpen}
      onClose={handleClose}
      title={isDirect ? "Dodaj pliki do katalogu" : "Dodaj pliki do projektu"}
      actionLabel={files.length > 0 ? `Prześlij (${files.length})` : "Prześlij"}
      actionColorScheme="primary"
      onAction={handleUpload}
      isActionLoading={uploading}
      isActionDisabled={files.length === 0}
      desktopSize="lg"
    >
      <VStack spacing={4} align="stretch">
        <Text fontSize="sm" color="neutral.600">
          Projekt: <Text as="span" fontWeight="bold">{projectName}</Text>
        </Text>

        {!isDirect && (
          <>
            <FormControl>
              <FormLabel>Tryb dodawania</FormLabel>
              <RadioGroup value={mode} onChange={(value) => setMode(value as "new" | "existing")}>
                <Stack direction={{ base: "column", sm: "row" }} spacing={4}>
                  <Radio value="new" isDisabled={uploading}>
                    Nowy katalog
                  </Radio>
                  <Radio value="existing" isDisabled={uploading}>
                    Istniejący katalog
                  </Radio>
                </Stack>
              </RadioGroup>
            </FormControl>

            {mode === "new" ? (
              <>
                <FormControl isRequired isInvalid={!!packageNameError}>
                  <FormLabel>Nazwa katalogu</FormLabel>
                  <Input
                    value={packageName}
                    onChange={(e) => {
                      const value = e.target.value;
                      setPackageName(value);
                      if (value.trim() && packages.some((p) => p.name.toLowerCase() === value.trim().toLowerCase())) {
                        setPackageNameError("Katalog o tej nazwie już istnieje");
                      } else {
                        setPackageNameError("");
                      }
                    }}
                    placeholder="np. Dokumentacja, Zdjęcia, Rysunki"
                    isDisabled={uploading}
                  />
                  <FormErrorMessage>{packageNameError}</FormErrorMessage>
                  <Text fontSize="xs" color="neutral.600" mt={1}>
                    Pliki zostaną zapisane w nowym katalogu
                  </Text>
                </FormControl>

                <FormControl>
                  <FormLabel>Katalog nadrzędny (opcjonalnie)</FormLabel>
                  <Select
                    value={parentDirectoryId}
                    onChange={(e) => setParentDirectoryId(e.target.value)}
                    placeholder="Brak — utwórz jako katalog główny"
                    isDisabled={uploading}
                  >
                    {flattenCatalogsForSelect(packages).map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.label}
                      </option>
                    ))}
                  </Select>
                  <Text fontSize="xs" color="neutral.600" mt={1}>
                    Jeśli nie wybierzesz, katalog zostanie dodany jako główny
                  </Text>
                </FormControl>
              </>
            ) : (
              <FormControl isRequired>
                <FormLabel>Wybierz katalog</FormLabel>
                {loadingPackages ? (
                  <HStack justify="center" py={2}>
                    <Spinner size="sm" />
                    <Text fontSize="sm">Ładowanie katalogów...</Text>
                  </HStack>
                ) : packages.length === 0 ? (
                  <Text fontSize="sm" color="neutral.600">
                    Nie masz jeszcze żadnych katalogów. Przełącz się na &quot;Nowy katalog&quot;.
                  </Text>
                ) : (
                  <>
                    <Select
                      value={selectedPackageId}
                      onChange={(e) => setSelectedPackageId(e.target.value)}
                      placeholder="Wybierz katalog"
                      isDisabled={uploading}
                    >
                      {flattenCatalogsForSelect(packages).map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.label}
                        </option>
                      ))}
                    </Select>
                    <Text fontSize="xs" color="neutral.600" mt={1}>
                      Pliki zostaną dodane do wybranego katalogu
                    </Text>
                  </>
                )}
              </FormControl>
            )}
          </>
        )}

        <FormControl>
          <FormLabel>Pliki (PDF, JPG, JPEG, max 10MB)</FormLabel>
          <Button
            leftIcon={<Upload size={18} aria-hidden="true" />}
            onClick={() => document.getElementById("file-input")?.click()}
            isDisabled={uploading}
            width="100%"
            variant="outline"
            minH="44px"
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
                <ListItem key={`${item.file.name}-${index}`}>
                  <Box
                    p={3}
                    borderWidth="1px"
                    borderRadius="md"
                    bg="neutral.25"
                  >
                    <VStack align="stretch" spacing={2}>
                      <HStack justify="space-between" align="flex-start">
                        <HStack flex={1} minW={0} align="flex-start">
                          <Box flexShrink={0} pt={0.5}>
                            <FileText size={18} aria-hidden="true" />
                          </Box>
                          <VStack align="flex-start" spacing={0} flex={1} minW={0}>
                            <Text fontSize="sm" fontWeight="medium" noOfLines={2} wordBreak="break-word">
                              {item.file.name}
                            </Text>
                            <Text fontSize="xs" color="neutral.600">
                              {formatFileSize(item.file.size)}
                            </Text>
                          </VStack>
                        </HStack>
                        <IconButton
                          aria-label="Usuń plik"
                          icon={<X size={16} aria-hidden="true" />}
                          size="sm"
                          colorScheme="red"
                          variant="ghost"
                          minH="44px"
                          minW="44px"
                          onClick={() => handleRemoveFile(index)}
                          isDisabled={uploading}
                        />
                      </HStack>
                      <FormControl>
                        <FormLabel fontSize="xs">Nazwa wyświetlana (opcjonalna)</FormLabel>
                        <Input
                          size="sm"
                          value={item.displayName}
                          onChange={(e) => handleDisplayNameChange(index, e.target.value)}
                          placeholder="Domyślnie: nazwa pliku"
                          isDisabled={uploading}
                        />
                      </FormControl>
                      <FormControl>
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
    </AppModal>
  );
}
