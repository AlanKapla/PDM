import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  Text,
  VStack,
  HStack,
  Spinner,
  Alert,
  AlertIcon,
  Badge,
  Accordion,
  AccordionItem,
  AccordionButton,
  AccordionPanel,
  AccordionIcon,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Link,
  useToast,
  Button,
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  useColorModeValue,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalCloseButton,
  ModalFooter,
  Checkbox,
  useDisclosure,
} from "@chakra-ui/react";
import { Download, ArrowLeft, FileText, Eye, ChevronLeft, ChevronRight, Share2, Package } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import ShareFilesModal from "../components/ShareFilesModal";
import { projectApi } from "../api/projectApi";
import type { ProjectFileWeb } from "../types/project.types";

interface GroupedFiles {
  [packageName: string]: ProjectFileWeb[];
}

export default function MyFiles() {
  const { tenantId, projectId } = useParams<{ tenantId: string; projectId: string }>();
  const [files, setFiles] = useState<ProjectFileWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [previewFile, setPreviewFile] = useState<ProjectFileWeb | null>(null);
  const [currentPackageFiles, setCurrentPackageFiles] = useState<ProjectFileWeb[]>([]);
  const [selectedFileIds, setSelectedFileIds] = useState<Set<string>>(new Set());
  const { isOpen: isShareModalOpen, onOpen: onShareModalOpen, onClose: onShareModalClose } = useDisclosure();
  const toast = useToast();
  const navigate = useNavigate();
  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");

  useEffect(() => {
    fetchMyFiles();
  }, [tenantId, projectId]);

  const fetchMyFiles = async () => {
    if (!tenantId || !projectId) return;

    try {
      setLoading(true);
      const response = await projectApi.getMyFiles(tenantId, projectId);
      if (response.ok) {
        const data = await response.json();
        setFiles(data);
      } else {
        throw new Error("Nie udało się pobrać plików");
      }
    } catch (error) {
      console.error("Błąd podczas pobierania plików:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać listy plików",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setLoading(false);
    }
  };

  const groupFilesByPackage = (files: ProjectFileWeb[]): GroupedFiles => {
    return files.reduce((acc, file) => {
      if (!acc[file.packageName]) {
        acc[file.packageName] = [];
      }
      acc[file.packageName].push(file);
      return acc;
    }, {} as GroupedFiles);
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + " " + sizes[i];
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString("pl-PL", {
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const handleDownload = async (sasUrl: string, fileName: string) => {
    try {
      const response = await fetch(sasUrl, {
        method: 'GET',
        mode: 'cors',
      });
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileName;
      link.style.display = 'none';
      document.body.appendChild(link);
      link.click();
      
      setTimeout(() => {
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      }, 100);
    } catch (error) {
      console.error("Błąd podczas pobierania pliku:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać pliku",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    }
  };

  const handleDownloadSelectedAsZip = async () => {
    const selectedFiles = getSelectedFiles();
    if (selectedFiles.length === 0) return;

    try {
      toast({
        title: "Pobieranie...",
        description: `Przygotowywanie ${selectedFiles.length} plików do pobrania`,
        status: "info",
        duration: 3000,
        isClosable: true,
      });

      // Dynamiczny import JSZip
      const JSZip = (await import("jszip")).default;
      const zip = new JSZip();

      // Pobierz wszystkie pliki i dodaj do ZIP - używając mode: 'no-cors' nie zadziała dla blob
      // Zamiast tego pobierzemy pliki przez proxy lub użyjemy bezpośredniego dostępu
      const filePromises = selectedFiles.map(async (file) => {
        try {
          // Pobierz plik z włączonymi credentials
          const response = await fetch(file.sasUrl, {
            method: 'GET',
            mode: 'cors',
          });
          
          if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
          }
          
          const blob = await response.blob();
          zip.file(file.fileName, blob);
          return { success: true, fileName: file.fileName };
        } catch (error) {
          console.error(`Błąd pobierania ${file.fileName}:`, error);
          return { success: false, fileName: file.fileName };
        }
      });

      const results = await Promise.all(filePromises);
      const successCount = results.filter(r => r.success).length;
      const failedCount = results.filter(r => !r.success).length;

      if (successCount === 0) {
        toast({
          title: "Błąd",
          description: "Nie udało się pobrać żadnego pliku. Spróbuj pobrać je pojedynczo.",
          status: "error",
          duration: 5000,
          isClosable: true,
        });
        return;
      }

      // Generuj ZIP i pobierz
      const zipBlob = await zip.generateAsync({ type: "blob" });
      const url = window.URL.createObjectURL(zipBlob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `pliki_${new Date().toISOString().split("T")[0]}.zip`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);

      if (failedCount > 0) {
        toast({
          title: "Częściowy sukces",
          description: `Pobrano ${successCount} z ${selectedFiles.length} plików. ${failedCount} plików nie udało się pobrać.`,
          status: "warning",
          duration: 5000,
          isClosable: true,
        });
      } else {
        toast({
          title: "Sukces",
          description: "Wszystkie pliki zostały pobrane jako archiwum ZIP",
          status: "success",
          duration: 3000,
          isClosable: true,
        });
      }

      setSelectedFileIds(new Set());
    } catch (error) {
      console.error("Błąd podczas tworzenia archiwum ZIP:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się utworzyć archiwum ZIP. Spróbuj pobrać pliki pojedynczo.",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    }
  };

  const handlePreview = (file: ProjectFileWeb, packageFiles: ProjectFileWeb[]) => {
    setPreviewFile(file);
    setCurrentPackageFiles(packageFiles);
  };

  const closePreview = () => {
    setPreviewFile(null);
    setCurrentPackageFiles([]);
  };

  const isPreviewSupported = (contentType: string): boolean => {
    return (
      contentType === "application/pdf" ||
      contentType.startsWith("image/")
    );
  };

  const navigateFile = (direction: "prev" | "next") => {
    if (!previewFile || currentPackageFiles.length === 0) return;

    const currentIndex = currentPackageFiles.findIndex(f => f.id === previewFile.id);
    let newIndex: number;

    if (direction === "prev") {
      newIndex = currentIndex > 0 ? currentIndex - 1 : currentPackageFiles.length - 1;
    } else {
      newIndex = currentIndex < currentPackageFiles.length - 1 ? currentIndex + 1 : 0;
    }

    setPreviewFile(currentPackageFiles[newIndex]);
  };

  const getCurrentFileIndex = (): string => {
    if (!previewFile || currentPackageFiles.length === 0) return "";
    const currentIndex = currentPackageFiles.findIndex(f => f.id === previewFile.id);
    return `${currentIndex + 1} / ${currentPackageFiles.length}`;
  };

  const toggleFileSelection = (fileId: string) => {
    setSelectedFileIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(fileId)) {
        newSet.delete(fileId);
      } else {
        newSet.add(fileId);
      }
      return newSet;
    });
  };

  const togglePackageSelection = (packageFiles: ProjectFileWeb[]) => {
    const packageFileIds = packageFiles.map((f) => f.id);
    const allSelected = packageFileIds.every((id) => selectedFileIds.has(id));

    setSelectedFileIds((prev) => {
      const newSet = new Set(prev);
      if (allSelected) {
        packageFileIds.forEach((id) => newSet.delete(id));
      } else {
        packageFileIds.forEach((id) => newSet.add(id));
      }
      return newSet;
    });
  };

  const getSelectedFiles = (): ProjectFileWeb[] => {
    return files.filter((f) => selectedFileIds.has(f.id));
  };

  const handleFilesShared = () => {
    setSelectedFileIds(new Set());
    fetchMyFiles();
  };

  const groupedFiles = groupFilesByPackage(files);
  const packageNames = Object.keys(groupedFiles).sort();

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 6 }} maxW="1400px" mx="auto">
        {/* Breadcrumb */}
        <Breadcrumb mb={4} fontSize="sm" display={{ base: "none", md: "flex" }}>
          <BreadcrumbItem>
            <BreadcrumbLink onClick={() => navigate("/projects")}>Projekty</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbItem>
            <BreadcrumbLink onClick={() => navigate(`/projects/${projectId}`)}>Szczegóły projektu</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbItem isCurrentPage>
            <BreadcrumbLink>Moje pliki</BreadcrumbLink>
          </BreadcrumbItem>
        </Breadcrumb>

        {/* Przycisk powrotu */}
        <Button
          leftIcon={<ArrowLeft size={20} />}
          variant="ghost"
          mb={6}
          size={{ base: "sm", md: "md" }}
          onClick={() => navigate(`/projects/${projectId}`)}
        >
          Wróć do projektu
        </Button>

        {/* Nagłówek */}
        <VStack mb={6} spacing={3} align="stretch">
          <HStack spacing={3}>
            <Box display={{ base: "none", md: "block" }}>
              <FileText size={32} />
            </Box>
            <Box display={{ base: "block", md: "none" }}>
              <FileText size={24} />
            </Box>
            <Heading size={{ base: "md", md: "lg" }}>Moje pliki</Heading>
          </HStack>
          {selectedFileIds.size > 0 && (
            <VStack spacing={2} align="stretch">
              <Badge colorScheme="blue" fontSize="md" px={3} py={1} textAlign="center">
                Wybrano: {selectedFileIds.size}
              </Badge>
              <HStack spacing={2} flexWrap="wrap">
                <Button
                  leftIcon={<Package size={18} />}
                  colorScheme="purple"
                  size="sm"
                  flex={{ base: 1, md: "none" }}
                  onClick={handleDownloadSelectedAsZip}
                >
                  Pobierz ZIP
                </Button>
                <Button
                  leftIcon={<Share2 size={18} />}
                  colorScheme="green"
                  size="sm"
                  flex={{ base: 1, md: "none" }}
                  onClick={onShareModalOpen}
                >
                  Udostępnij
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  flex={{ base: 1, md: "none" }}
                  onClick={() => setSelectedFileIds(new Set())}
                >
                  Odznacz
                </Button>
              </HStack>
            </VStack>
          )}
        </VStack>

        {loading ? (
          <Box display="flex" justifyContent="center" alignItems="center" minH="400px">
            <Spinner size="xl" color="blue.500" />
            <Text ml={4}>Ładowanie plików...</Text>
          </Box>
        ) : files.length === 0 ? (
          <Alert status="info" borderRadius="md">
            <AlertIcon />
            Nie masz jeszcze żadnych przesłanych plików w tym projekcie.
          </Alert>
        ) : (

          <VStack spacing={4} align="stretch">
            <Accordion allowMultiple>
              {packageNames.map((packageName) => {
                const packageFiles = groupedFiles[packageName];
                const totalSize = packageFiles.reduce((sum, file) => sum + file.fileSizeBytes, 0);

                return (
                  <AccordionItem
                    key={packageName}
                    bg={cardBg}
                    border="1px"
                    borderColor={borderColor}
                    borderRadius="md"
                    mb={2}
                  >
                    <AccordionButton _expanded={{ bg: "blue.50" }} py={4}>
                      <HStack flex="1" spacing={3}>
                        <Checkbox
                          isChecked={packageFiles.every((f) => selectedFileIds.has(f.id))}
                          isIndeterminate={
                            packageFiles.some((f) => selectedFileIds.has(f.id)) &&
                            !packageFiles.every((f) => selectedFileIds.has(f.id))
                          }
                          onChange={() => togglePackageSelection(packageFiles)}
                          onClick={(e) => e.stopPropagation()}
                        />
                        <Box flex="1" textAlign="left">
                          <HStack spacing={3}>
                            <Text fontWeight="bold" fontSize="lg">
                              📦 {packageName}
                            </Text>
                            <Badge colorScheme="blue">{packageFiles.length} plików</Badge>
                            <Text fontSize="sm" color="gray.600">
                              ({formatFileSize(totalSize)})
                            </Text>
                          </HStack>
                        </Box>
                      </HStack>
                      <AccordionIcon />
                    </AccordionButton>
                    <AccordionPanel pb={4}>
                    <Table variant="simple" size="sm">
                    <Thead>
                      <Tr>
                        <Th width="40px"></Th>
                        <Th>Nazwa pliku</Th>
                        <Th display={{ base: "none", md: "table-cell" }}>Typ</Th>
                        <Th display={{ base: "none", lg: "table-cell" }}>Rozmiar</Th>
                        <Th display={{ base: "none", lg: "table-cell" }}>Data przesłania</Th>
                        <Th>Akcje</Th>
                      </Tr>
                    </Thead>
                    <Tbody>
                      {packageFiles.map((file) => (
                        <Tr key={file.id}>
                          <Td>
                            <Checkbox
                              isChecked={selectedFileIds.has(file.id)}
                              onChange={() => toggleFileSelection(file.id)}
                            />
                          </Td>
                          <Td>
                            <VStack align="start" spacing={0}>
                              {isPreviewSupported(file.contentType) ? (
                                <Link
                                  color="blue.600"
                                  fontWeight="medium"
                                  onClick={() => handlePreview(file, packageFiles)}
                                  cursor="pointer"
                                  _hover={{ textDecoration: "underline" }}
                                  fontSize={{ base: "sm", md: "md" }}
                                >
                                  {file.displayName}
                                </Link>
                              ) : (
                                <Text fontWeight="medium" fontSize={{ base: "sm", md: "md" }}>{file.displayName}</Text>
                              )}
                              {file.displayName !== file.fileName && (
                                <Text fontSize="xs" color="gray.500">
                                  ({file.fileName})
                                </Text>
                              )}
                              <HStack spacing={1} display={{ base: "flex", md: "none" }} mt={1} flexWrap="wrap">
                                <Badge colorScheme={file.contentType.includes("pdf") ? "red" : "green"} fontSize="xs">
                                  {file.contentType.split("/")[1]?.toUpperCase() || "FILE"}
                                </Badge>
                                <Text fontSize="xs" color="gray.600">
                                  {formatFileSize(file.fileSizeBytes)}
                                </Text>
                              </HStack>
                            </VStack>
                          </Td>
                          <Td display={{ base: "none", md: "table-cell" }}>
                            <Badge colorScheme={file.contentType.includes("pdf") ? "red" : "green"}>
                              {file.contentType.split("/")[1]?.toUpperCase() || "FILE"}
                            </Badge>
                          </Td>
                          <Td display={{ base: "none", lg: "table-cell" }}>{formatFileSize(file.fileSizeBytes)}</Td>
                          <Td display={{ base: "none", lg: "table-cell" }}>
                            <Text fontSize="sm">{formatDate(file.uploadedAt)}</Text>
                          </Td>
                          <Td>
                            <VStack spacing={1} align="stretch" display={{ base: "flex", md: "none" }}>
                              {isPreviewSupported(file.contentType) && (
                                <Link
                                  color="purple.500"
                                  onClick={() => handlePreview(file, packageFiles)}
                                  cursor="pointer"
                                  display="inline-flex"
                                  alignItems="center"
                                  gap={1}
                                  fontSize="xs"
                                >
                                  <Eye size={14} />
                                  Podgląd
                                </Link>
                              )}
                              <Link
                                color="blue.500"
                                onClick={() => handleDownload(file.sasUrl, file.fileName)}
                                cursor="pointer"
                                display="inline-flex"
                                alignItems="center"
                                gap={1}
                                fontSize="xs"
                              >
                                <Download size={14} />
                                Pobierz
                              </Link>
                            </VStack>
                            <HStack spacing={3} display={{ base: "none", md: "flex" }}>
                              {isPreviewSupported(file.contentType) && (
                                <Link
                                  color="purple.500"
                                  onClick={() => handlePreview(file, packageFiles)}
                                  cursor="pointer"
                                  display="inline-flex"
                                  alignItems="center"
                                  gap={1}
                                >
                                  <Eye size={16} />
                                  Podgląd
                                </Link>
                              )}
                              <Link
                                color="blue.500"
                                onClick={() => handleDownload(file.sasUrl, file.fileName)}
                                cursor="pointer"
                                display="inline-flex"
                                alignItems="center"
                                gap={1}
                              >
                                <Download size={16} />
                                Pobierz
                              </Link>
                            </HStack>
                          </Td>
                        </Tr>
                      ))}
                    </Tbody>
                  </Table>
                    </AccordionPanel>
                  </AccordionItem>
                );
              })}
            </Accordion>
          </VStack>
        )}

        {/* Modal podglądu pliku */}
        <Modal isOpen={!!previewFile} onClose={closePreview} size={{ base: "full", md: "6xl" }}>
          <ModalOverlay />
          <ModalContent maxH={{ base: "100vh", md: "90vh" }}>
            <ModalHeader>
              <VStack spacing={3} align="stretch" width="100%">
                <VStack align="start" spacing={1}>
                  <Text fontSize={{ base: "md", md: "lg" }}>{previewFile?.displayName}</Text>
                  {previewFile && previewFile.displayName !== previewFile.fileName && (
                    <Text fontSize="sm" fontWeight="normal" color="gray.500">
                      {previewFile.fileName}
                    </Text>
                  )}
                </VStack>
                {currentPackageFiles.length > 1 && (
                  <>
                    <HStack spacing={2} justify="center" display={{ base: "flex", md: "none" }}>
                      <Button
                        size="xs"
                        leftIcon={<ChevronLeft size={16} />}
                        onClick={() => navigateFile("prev")}
                        variant="ghost"
                      >
                        Poprzedni
                      </Button>
                      <Badge colorScheme="blue" fontSize="sm" px={2} py={1}>
                        {getCurrentFileIndex()}
                      </Badge>
                      <Button
                        size="xs"
                        rightIcon={<ChevronRight size={16} />}
                        onClick={() => navigateFile("next")}
                        variant="ghost"
                      >
                        Następny
                      </Button>
                    </HStack>
                    <HStack spacing={2} display={{ base: "none", md: "flex" }}>
                      <Button
                        size="sm"
                        leftIcon={<ChevronLeft size={18} />}
                        onClick={() => navigateFile("prev")}
                        variant="ghost"
                      >
                        Poprzedni
                      </Button>
                      <Badge colorScheme="blue" fontSize="md" px={3} py={1}>
                        {getCurrentFileIndex()}
                      </Badge>
                      <Button
                        size="sm"
                        rightIcon={<ChevronRight size={18} />}
                        onClick={() => navigateFile("next")}
                        variant="ghost"
                      >
                        Następny
                      </Button>
                    </HStack>
                  </>
                )}
              </VStack>
            </ModalHeader>
            <ModalCloseButton />
            <ModalBody pb={6} display="flex" justifyContent="center" alignItems="center" overflow="auto" p={{ base: 2, md: 6 }}>
              {previewFile && (
                <>
                  {previewFile.contentType === "application/pdf" ? (
                    <Box width="100%" height={{ base: "500px", md: "700px" }}>
                      <iframe
                        src={previewFile.sasUrl}
                        width="100%"
                        height="100%"
                        title={previewFile.displayName}
                        style={{ border: "none" }}
                      />
                    </Box>
                  ) : previewFile.contentType.startsWith("image/") ? (
                    <Box maxW="100%" maxH={{ base: "500px", md: "700px" }} display="flex" justifyContent="center">
                      <img
                        src={previewFile.sasUrl}
                        alt={previewFile.displayName}
                        style={{ maxWidth: "100%", maxHeight: "100%", objectFit: "contain" }}
                      />
                    </Box>
                  ) : (
                    <Alert status="warning">
                      <AlertIcon />
                      Podgląd nie jest dostępny dla tego typu pliku.
                    </Alert>
                  )}
                </>
              )}
            </ModalBody>
            <ModalFooter>
              <VStack spacing={2} width={{ base: "100%", md: "auto" }}>
                <Button
                  leftIcon={<Download size={18} />}
                  colorScheme="blue"
                  onClick={() => previewFile && handleDownload(previewFile.sasUrl, previewFile.fileName)}
                  width={{ base: "100%", md: "auto" }}
                  size={{ base: "sm", md: "md" }}
                >
                  Pobierz plik
                </Button>
                <Button 
                  variant="ghost" 
                  onClick={closePreview}
                  width={{ base: "100%", md: "auto" }}
                  size={{ base: "sm", md: "md" }}
                >
                  Zamknij
                </Button>
              </VStack>
            </ModalFooter>
          </ModalContent>
        </Modal>

        {/* Modal udostępniania plików */}
        {tenantId && projectId && (
          <ShareFilesModal
            isOpen={isShareModalOpen}
            onClose={onShareModalClose}
            tenantId={tenantId}
            projectId={projectId}
            selectedFiles={getSelectedFiles()}
            onFilesShared={handleFilesShared}
          />
        )}
      </Box>
    </MainLayout>
  );
}
