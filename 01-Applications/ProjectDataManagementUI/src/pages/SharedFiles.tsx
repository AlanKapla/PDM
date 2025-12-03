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
} from "@chakra-ui/react";
import { Download, ArrowLeft, Users, Eye, ChevronLeft, ChevronRight, Share2 } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { projectApi } from "../api/projectApi";
import type { SharedProjectFileWeb } from "../types/project.types";

interface GroupedSharedFiles {
  [packageName: string]: {
    [sharedByUserName: string]: SharedProjectFileWeb[];
  };
}

export default function SharedFiles() {
  const { tenantId, projectId } = useParams<{ tenantId: string; projectId: string }>();
  const [files, setFiles] = useState<SharedProjectFileWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [previewFile, setPreviewFile] = useState<SharedProjectFileWeb | null>(null);
  const [currentGroupFiles, setCurrentGroupFiles] = useState<SharedProjectFileWeb[]>([]);
  const toast = useToast();
  const navigate = useNavigate();
  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");

  useEffect(() => {
    fetchSharedFiles();
  }, [tenantId, projectId]);

  const fetchSharedFiles = async () => {
    if (!tenantId || !projectId) return;

    try {
      setLoading(true);
      const response = await projectApi.getSharedFiles(tenantId, projectId);
      if (response.ok) {
        const data = await response.json();
        setFiles(data);
      } else {
        throw new Error("Nie udało się pobrać udostępnionych plików");
      }
    } catch (error) {
      console.error("Błąd podczas pobierania udostępnionych plików:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać listy udostępnionych plików",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setLoading(false);
    }
  };

  const groupFilesByPackageAndUser = (files: SharedProjectFileWeb[]): GroupedSharedFiles => {
    return files.reduce((acc, file) => {
      if (!acc[file.packageName]) {
        acc[file.packageName] = {};
      }
      if (!acc[file.packageName][file.sharedByUserName]) {
        acc[file.packageName][file.sharedByUserName] = [];
      }
      acc[file.packageName][file.sharedByUserName].push(file);
      return acc;
    }, {} as GroupedSharedFiles);
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

  const handlePreview = (file: SharedProjectFileWeb, groupFiles: SharedProjectFileWeb[]) => {
    setPreviewFile(file);
    setCurrentGroupFiles(groupFiles);
  };

  const closePreview = () => {
    setPreviewFile(null);
    setCurrentGroupFiles([]);
  };

  const isPreviewSupported = (contentType: string): boolean => {
    return (
      contentType === "application/pdf" ||
      contentType.startsWith("image/")
    );
  };

  const navigateFile = (direction: "prev" | "next") => {
    if (!previewFile || currentGroupFiles.length === 0) return;

    const currentIndex = currentGroupFiles.findIndex((f) => f.id === previewFile.id);
    let newIndex: number;

    if (direction === "prev") {
      newIndex = currentIndex > 0 ? currentIndex - 1 : currentGroupFiles.length - 1;
    } else {
      newIndex = currentIndex < currentGroupFiles.length - 1 ? currentIndex + 1 : 0;
    }

    setPreviewFile(currentGroupFiles[newIndex]);
  };

  const getCurrentFileIndex = (): string => {
    if (!previewFile || currentGroupFiles.length === 0) return "";
    const currentIndex = currentGroupFiles.findIndex((f) => f.id === previewFile.id);
    return `${currentIndex + 1} / ${currentGroupFiles.length}`;
  };

  const groupedFiles = groupFilesByPackageAndUser(files);
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
            <BreadcrumbLink>Udostępnione pliki</BreadcrumbLink>
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
        <HStack mb={6} spacing={3}>
          <Box display={{ base: "none", md: "block" }}>
            <Share2 size={32} />
          </Box>
          <Box display={{ base: "block", md: "none" }}>
            <Share2 size={24} />
          </Box>
          <Heading size={{ base: "md", md: "lg" }}>Udostępnione pliki</Heading>
        </HStack>

        {loading ? (
          <Box display="flex" justifyContent="center" alignItems="center" minH="400px">
            <Spinner size="xl" color="blue.500" />
            <Text ml={4}>Ładowanie udostępnionych plików...</Text>
          </Box>
        ) : files.length === 0 ? (
          <Alert status="info" borderRadius="md">
            <AlertIcon />
            Nie masz jeszcze żadnych udostępnionych plików w tym projekcie.
          </Alert>
        ) : (
          <VStack spacing={4} align="stretch">
            <Accordion allowMultiple>
              {packageNames.map((packageName) => {
                const userGroups = groupedFiles[packageName];
                const userNames = Object.keys(userGroups).sort();
                const totalFilesInPackage = Object.values(userGroups).flat().length;

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
                      <Box flex="1" textAlign="left">
                        <HStack spacing={3}>
                          <Text fontWeight="bold" fontSize="lg">
                            📦 {packageName}
                          </Text>
                          <Badge colorScheme="blue">{totalFilesInPackage} plików</Badge>
                          <Badge colorScheme="purple">{userNames.length} użytkowników</Badge>
                        </HStack>
                      </Box>
                      <AccordionIcon />
                    </AccordionButton>
                    <AccordionPanel pb={4}>
                      <VStack spacing={4} align="stretch">
                        {userNames.map((userName) => {
                          const userFiles = userGroups[userName];
                          const totalSize = userFiles.reduce((sum, file) => sum + file.fileSizeBytes, 0);

                          return (
                            <Box
                              key={userName}
                              border="1px"
                              borderColor="gray.300"
                              borderRadius="md"
                              overflow="hidden"
                            >
                              <Box bg="gray.100" px={4} py={2}>
                                <HStack spacing={3}>
                                  <Users size={18} />
                                  <Text fontWeight="semibold">
                                    Udostępnił: {userName}
                                  </Text>
                                  <Badge colorScheme="green">{userFiles.length} plików</Badge>
                                  <Text fontSize="sm" color="gray.600">
                                    ({formatFileSize(totalSize)})
                                  </Text>
                                </HStack>
                              </Box>
                              <Table variant="simple" size="sm">
                                <Thead>
                                  <Tr>
                                    <Th>Nazwa pliku</Th>
                                    <Th display={{ base: "none", lg: "table-cell" }}>Właściciel</Th>
                                    <Th display={{ base: "none", md: "table-cell" }}>Typ</Th>
                                    <Th display={{ base: "none", lg: "table-cell" }}>Rozmiar</Th>
                                    <Th display={{ base: "none", xl: "table-cell" }}>Data udostępnienia</Th>
                                    <Th>Akcje</Th>
                                  </Tr>
                                </Thead>
                                <Tbody>
                                  {userFiles.map((file) => (
                                    <Tr key={file.id}>
                                      <Td>
                                        <VStack align="start" spacing={0}>
                                          {isPreviewSupported(file.contentType) ? (
                                            <Link
                                              color="blue.600"
                                              fontWeight="medium"
                                              onClick={() => handlePreview(file, userFiles)}
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
                                              {file.originalOwnerUserName}
                                            </Text>
                                          </HStack>
                                        </VStack>
                                      </Td>
                                      <Td display={{ base: "none", lg: "table-cell" }}>
                                        <Text fontSize="sm">{file.originalOwnerUserName}</Text>
                                      </Td>
                                      <Td display={{ base: "none", md: "table-cell" }}>
                                        <Badge colorScheme={file.contentType.includes("pdf") ? "red" : "green"}>
                                          {file.contentType.split("/")[1]?.toUpperCase() || "FILE"}
                                        </Badge>
                                      </Td>
                                      <Td display={{ base: "none", lg: "table-cell" }}>{formatFileSize(file.fileSizeBytes)}</Td>
                                      <Td display={{ base: "none", xl: "table-cell" }}>
                                        <Text fontSize="sm">{formatDate(file.sharedAt)}</Text>
                                      </Td>
                                      <Td>
                                        <VStack spacing={1} align="stretch" display={{ base: "flex", md: "none" }}>
                                          {isPreviewSupported(file.contentType) && (
                                            <Link
                                              color="purple.500"
                                              onClick={() => handlePreview(file, userFiles)}
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
                                              onClick={() => handlePreview(file, userFiles)}
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
                            </Box>
                          );
                        })}
                      </VStack>
                    </AccordionPanel>
                  </AccordionItem>
                );
              })}
            </Accordion>
          </VStack>
        )}

        {/* Modal podglądu pliku */}
        <Modal isOpen={!!previewFile} onClose={closePreview} size="6xl">
          <ModalOverlay />
          <ModalContent maxH="90vh">
            <ModalHeader>
              <HStack justify="space-between" align="center" width="100%">
                <VStack align="start" spacing={1} flex="1">
                  <Text>{previewFile?.displayName}</Text>
                  {previewFile && previewFile.displayName !== previewFile.fileName && (
                    <Text fontSize="sm" fontWeight="normal" color="gray.500">
                      {previewFile.fileName}
                    </Text>
                  )}
                  {previewFile && (
                    <HStack spacing={2} fontSize="xs">
                      <Badge colorScheme="purple">
                        Udostępnił: {previewFile.sharedByUserName}
                      </Badge>
                      <Badge colorScheme="blue">
                        Właściciel: {previewFile.originalOwnerUserName}
                      </Badge>
                    </HStack>
                  )}
                </VStack>
                {currentGroupFiles.length > 1 && (
                  <HStack spacing={2}>
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
                )}
              </HStack>
            </ModalHeader>
            <ModalCloseButton />
            <ModalBody pb={6} display="flex" justifyContent="center" alignItems="center" overflow="auto">
              {previewFile && (
                <>
                  {previewFile.contentType === "application/pdf" ? (
                    <Box width="100%" height="700px">
                      <iframe
                        src={previewFile.sasUrl}
                        width="100%"
                        height="100%"
                        title={previewFile.displayName}
                        style={{ border: "none" }}
                      />
                    </Box>
                  ) : previewFile.contentType.startsWith("image/") ? (
                    <Box maxW="100%" maxH="700px" display="flex" justifyContent="center">
                      <img
                        src={previewFile.sasUrl}
                        alt={previewFile.displayName}
                        style={{ maxWidth: "100%", maxHeight: "700px", objectFit: "contain" }}
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
              <HStack spacing={3}>
                <Button
                  leftIcon={<Download size={18} />}
                  colorScheme="blue"
                  onClick={() => previewFile && handleDownload(previewFile.sasUrl, previewFile.fileName)}
                >
                  Pobierz plik
                </Button>
                <Button variant="ghost" onClick={closePreview}>
                  Zamknij
                </Button>
              </HStack>
            </ModalFooter>
          </ModalContent>
        </Modal>
      </Box>
    </MainLayout>
  );
}
