import { useEffect, useState, useContext } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";
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
  Checkbox,
  useDisclosure,
  Tooltip,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  Textarea,
  IconButton,
} from "@chakra-ui/react";
import { Download, ArrowLeft, FileText, Eye, Share2, Package, MessageSquare, Clock, User, ChevronDown, ChevronUp, Trash2, Upload, Send } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import ShareFilesModal from "../components/ShareFilesModal";
import UploadNewVersionModal from "../components/UploadNewVersionModal";
import { projectApi } from "../api/projectApi";
import type { ProjectFileWeb } from "../types/project.types";

interface GroupedFiles {
  [packageName: string]: ProjectFileWeb[];
}

export default function MyFiles() {
  const { tenantId, projectId } = useParams<{ tenantId: string; projectId: string }>();
  const { user } = useContext(AuthContext);
  const [files, setFiles] = useState<ProjectFileWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedFileIds, setSelectedFileIds] = useState<Set<string>>(new Set());
  const [expandedFileIds, setExpandedFileIds] = useState<Set<string>>(new Set());
  const [fileToDelete, setFileToDelete] = useState<{ id: string; name: string } | null>(null);
  const [fileForNewVersion, setFileForNewVersion] = useState<ProjectFileWeb | null>(null);
  const [newComments, setNewComments] = useState<Map<string, string>>(new Map());
  const [submittingComment, setSubmittingComment] = useState<string | null>(null);
  const { isOpen: isShareModalOpen, onOpen: onShareModalOpen, onClose: onShareModalClose } = useDisclosure();
  const { isOpen: isDeleteModalOpen, onOpen: onDeleteModalOpen, onClose: onDeleteModalClose } = useDisclosure();
  const { isOpen: isUploadVersionModalOpen, onOpen: onUploadVersionModalOpen, onClose: onUploadVersionModalClose } = useDisclosure();
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
      setFiles(response.data);
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

  const handleDownload = (sasUrl: string, fileName: string) => {
    // Debug: sprawdź co trafia jako fileName
    console.log('handleDownload fileName:', fileName);
    // Użyj fetch + createElement('a') z download attribute aby zagwarantować fileName
    fetch(sasUrl, { method: 'GET', mode: 'cors' })
      .then(response => {
        if (response.status !== 200) throw new Error(`HTTP error! status: ${response.status}`);
        return response.blob();
      })
      .then(blob => {
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
      })
      .catch(error => {
        console.error("Błąd podczas pobierania pliku:", error);
        toast({
          title: "Błąd",
          description: "Nie udało się pobrać pliku",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
      });
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
        if (!file.currentVersion) return { success: false, fileName: file.fileName };
        try {
          // Pobierz plik z włączonymi credentials
          const response = await fetch(file.currentVersion.sasUrlDownload, {
            method: 'GET',
            mode: 'cors',
          });
          
          if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
          }
          
          const blob = await response.blob();
          zip.file(file.displayName, blob);
          return { success: true, fileName: file.displayName };
        } catch (error) {
          console.error(`Błąd pobierania ${file.displayName}:`, error);
          return { success: false, fileName: file.displayName };
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

  const handlePreview = (sasUrlView: string) => {
    // Użyj bezpośrednio SAS URL - nazwa z Content-Disposition (parametr rscd)
    window.open(sasUrlView, '_blank', 'noopener,noreferrer');
  };

  const isPreviewSupported = (contentType: string): boolean => {
    return (
      contentType === "application/pdf" ||
      contentType.startsWith("image/")
    );
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

  const openDeleteModal = (fileId: string, fileName: string) => {
    setFileToDelete({ id: fileId, name: fileName });
    onDeleteModalOpen();
  };

  const handleDeleteFile = async () => {
    if (!tenantId || !projectId || !fileToDelete) return;

    try {
      await projectApi.deleteFile(tenantId, projectId, fileToDelete.id);
      
      toast({
        title: "Sukces",
        description: "Plik został usunięty",
        status: "success",
        duration: 3000,
        isClosable: true,
      });
      
      // Usuń plik z lokalnego stanu
      setFiles((prevFiles) => prevFiles.filter((f) => f.id !== fileToDelete.id));
        
        // Usuń z zaznaczonych jeśli był zaznaczony
        setSelectedFileIds((prev) => {
          const newSet = new Set(prev);
          newSet.delete(fileToDelete.id);
          return newSet;
        });
        
        onDeleteModalClose();
        setFileToDelete(null);
    } catch (error) {
      console.error("Błąd podczas usuwania pliku:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się usunąć pliku",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    }
  };

  const handleFilesShared = () => {
    setSelectedFileIds(new Set());
    fetchMyFiles();
  };

  const handleAddComment = async (fileId: string, versionId: string) => {
    if (!tenantId || !projectId) return;

    const commentKey = `${fileId}-${versionId}`;
    const comment = newComments.get(commentKey);

    if (!comment || comment.trim() === "") {
      toast({
        title: "Uwaga",
        description: "Komentarz nie może być pusty",
        status: "warning",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    try {
      setSubmittingComment(commentKey);
      const response = await projectApi.addFileVersionComment(
        tenantId,
        projectId,
        fileId,
        versionId,
        comment.trim()
      );

      toast({
        title: "Sukces",
        description: "Komentarz został dodany",
        status: "success",
        duration: 3000,
        isClosable: true,
      });

      // Wyczyść pole komentarza
      setNewComments((prev) => {
        const updated = new Map(prev);
        updated.delete(commentKey);
          return updated;
      });

      // Odśwież listę plików aby pobrać nowy komentarz
      await fetchMyFiles();
    } catch (error) {
      console.error("Błąd podczas dodawania komentarza:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się dodać komentarza",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setSubmittingComment(null);
    }
  };

  const openUploadVersionModal = (file: ProjectFileWeb) => {
    setFileForNewVersion(file);
    onUploadVersionModalOpen();
  };

  const handleVersionUploaded = () => {
    fetchMyFiles();
  };

  const toggleFileVersions = (fileId: string) => {
    setExpandedFileIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(fileId)) {
        newSet.delete(fileId);
      } else {
        newSet.add(fileId);
      }
      return newSet;
    });
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
              <VStack spacing={1}>
                <Badge colorScheme="blue" fontSize="md" px={3} py={1} textAlign="center">
                  Wybrano: {selectedFileIds.size} {selectedFileIds.size === 1 ? 'plik' : 'plików'}
                </Badge>
                <Text fontSize="xs" color="gray.600" textAlign="center">
                  (pliki bazowe ze wszystkimi wersjami)
                </Text>
              </VStack>
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
                const totalSize = packageFiles.reduce((sum, file) => sum + (file.currentVersion?.fileSizeBytes || 0), 0);

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
                        <Tooltip label="Zaznacz wszystkie pliki bazowe w pakiecie (ze wszystkimi wersjami)" fontSize="xs">
                          <Checkbox
                            isChecked={packageFiles.every((f) => selectedFileIds.has(f.id))}
                            isIndeterminate={
                              packageFiles.some((f) => selectedFileIds.has(f.id)) &&
                              !packageFiles.every((f) => selectedFileIds.has(f.id))
                            }
                            onChange={() => togglePackageSelection(packageFiles)}
                            onClick={(e) => e.stopPropagation()}
                          />
                        </Tooltip>
                        <Box flex="1" textAlign="left">
                          <HStack spacing={3} flexWrap="wrap">
                            <Text fontWeight="bold" fontSize="lg">
                              📦 {packageName}
                            </Text>
                            <Badge colorScheme="blue">{packageFiles.length} plików</Badge>
                            {packageFiles.some(f => f.totalVersions > 1) && (
                              <Badge colorScheme="purple">
                                {packageFiles.filter(f => f.totalVersions > 1).length} z wersjami
                              </Badge>
                            )}
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
                        <>
                        <Tr key={file.id}>
                          <Td>
                            <Tooltip 
                              label={file.totalVersions > 1 
                                ? `Zaznacz plik bazowy z ${file.totalVersions} wersjami` 
                                : "Zaznacz plik"} 
                              fontSize="xs"
                            >
                              <Checkbox
                                isChecked={selectedFileIds.has(file.id)}
                                onChange={() => toggleFileSelection(file.id)}
                              />
                            </Tooltip>
                          </Td>
                          <Td>
                            <VStack align="start" spacing={0}>
                              {file.currentVersion && isPreviewSupported(file.currentVersion.contentType) ? (
                                <Link
                                  color="blue.600"
                                  fontWeight="medium"
                                  onClick={() => handlePreview(file.currentVersion!.sasUrlView)}
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
                                <Badge colorScheme={file.currentVersion?.contentType.includes("pdf") ? "red" : "green"} fontSize="xs">
                                  {file.currentVersion?.contentType.split("/")[1]?.toUpperCase() || "FILE"}
                                </Badge>
                                <Text fontSize="xs" color="gray.600">
                                  {formatFileSize(file.currentVersion?.fileSizeBytes || 0)}
                                </Text>
                              </HStack>
                            </VStack>
                          </Td>
                          <Td display={{ base: "none", md: "table-cell" }}>
                            <Badge colorScheme={file.currentVersion?.contentType.includes("pdf") ? "red" : "green"}>
                              {file.currentVersion?.contentType.split("/")[1]?.toUpperCase() || "FILE"}
                            </Badge>
                          </Td>
                          <Td display={{ base: "none", lg: "table-cell" }}>{formatFileSize(file.currentVersion?.fileSizeBytes || 0)}</Td>
                          <Td display={{ base: "none", lg: "table-cell" }}>
                            <Text fontSize="sm">{formatDate(file.currentVersion?.createdAt || file.createdAt)}</Text>
                            {file.totalVersions > 1 && (
                              <Badge colorScheme="purple" ml={2} fontSize="xs">
                                v{file.currentVersion?.versionNumber}
                              </Badge>
                            )}
                          </Td>
                          <Td>
                            <VStack spacing={1} align="stretch" display={{ base: "flex", md: "none" }}>
                              {file.currentVersion && isPreviewSupported(file.currentVersion.contentType) && (
                                <Link
                                  color="blue.500"
                                  onClick={() => handlePreview(file.currentVersion!.sasUrlView)}
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
                              {file.currentVersion && (
                                <Link
                                  color="blue.500"
                                  onClick={() => handleDownload(file.currentVersion!.sasUrlDownload, file.fileName)}
                                  cursor="pointer"
                                  display="inline-flex"
                                  alignItems="center"
                                  gap={1}
                                  fontSize="xs"
                                >
                                  <Download size={14} />
                                  Pobierz
                                </Link>
                              )}
                              {file.versions.length > 0 && (
                                <Link
                                  color="gray.600"
                                  onClick={() => toggleFileVersions(file.id)}
                                  cursor="pointer"
                                  display="inline-flex"
                                  alignItems="center"
                                  gap={1}
                                  fontSize="xs"
                                >
                                  {expandedFileIds.has(file.id) ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                                  Wersje ({file.totalVersions})
                                </Link>
                              )}
                              {file.isOwner && (
                                <Link
                                  color="green.500"
                                  onClick={() => openUploadVersionModal(file)}
                                  cursor="pointer"
                                  display="inline-flex"
                                  alignItems="center"
                                  gap={1}
                                  fontSize="xs"
                                >
                                  <Upload size={14} />
                                  Nowa wersja
                                </Link>
                              )}
                              {file.isOwner && (
                                <Link
                                  color="red.500"
                                  onClick={() => openDeleteModal(file.id, file.displayName)}
                                  cursor="pointer"
                                  display="inline-flex"
                                  alignItems="center"
                                  gap={1}
                                  fontSize="xs"
                                >
                                  <Trash2 size={14} />
                                  Usuń
                                </Link>
                              )}
                            </VStack>
                            <HStack spacing={3} display={{ base: "none", md: "flex" }}>
                              {file.currentVersion && isPreviewSupported(file.currentVersion.contentType) && (
                                <Link
                                  color="blue.500"
                                  onClick={() => handlePreview(file.currentVersion!.sasUrlView)}
                                  cursor="pointer"
                                  display="inline-flex"
                                  alignItems="center"
                                  gap={1}
                                >
                                  <Eye size={16} />
                                  Podgląd
                                </Link>
                              )}
                              {file.currentVersion && (
                                <Link
                                  color="blue.500"
                                  onClick={() => handleDownload(file.currentVersion!.sasUrlDownload, file.fileName)}
                                  cursor="pointer"
                                  display="inline-flex"
                                  alignItems="center"
                                  gap={1}
                                >
                                  <Download size={16} />
                                  Pobierz
                                </Link>
                              )}
                              {file.versions.length > 0 && (
                                <Button
                                  size="sm"
                                  variant="ghost"
                                  onClick={() => toggleFileVersions(file.id)}
                                  rightIcon={expandedFileIds.has(file.id) ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                                >
                                  Wersje ({file.totalVersions})
                                </Button>
                              )}
                              {file.isOwner && (
                                <Link
                                  color="green.500"
                                  onClick={() => openUploadVersionModal(file)}
                                  cursor="pointer"
                                  display="inline-flex"
                                  alignItems="center"
                                  gap={1}
                                >
                                  <Upload size={16} />
                                  Nowa wersja
                                </Link>
                              )}
                              {file.isOwner && (
                                <Link
                                  color="red.500"
                                  onClick={() => openDeleteModal(file.id, file.displayName)}
                                  cursor="pointer"
                                  display="inline-flex"
                                  alignItems="center"
                                  gap={1}
                                >
                                  <Trash2 size={16} />
                                  Usuń
                                </Link>
                              )}
                            </HStack>
                          </Td>
                        </Tr>
                        {/* Rozwinięte wersje i komentarze */}
                        {expandedFileIds.has(file.id) && file.versions.length > 0 && (
                          <Tr key={`${file.id}-versions`}>
                            <Td colSpan={6} p={0}>
                              <Box bg="gray.50" p={4}>
                                <VStack align="stretch" spacing={3}>
                                  <Heading size="sm" mb={2}>
                                    Historia wersji ({file.totalVersions})
                                  </Heading>
                                  {file.versions
                                    .sort((a, b) => b.versionNumber - a.versionNumber)
                                    .map((version) => (
                                      <Box
                                        key={version.id}
                                        borderWidth="1px"
                                        borderRadius="md"
                                        p={3}
                                        bg={version.id === file.currentVersion?.id ? "blue.50" : "white"}
                                        borderColor={version.id === file.currentVersion?.id ? "blue.300" : "gray.200"}
                                      >
                                        <HStack justify="space-between" mb={2}>
                                          <HStack spacing={2}>
                                            <Badge colorScheme={version.id === file.currentVersion?.id ? "blue" : "gray"}>
                                              Wersja {version.versionNumber}
                                              {version.id === file.currentVersion?.id && " (Aktualna)"}
                                            </Badge>
                                            <Badge colorScheme="purple" fontSize="xs">
                                              {version.contentType.split("/")[1]?.toUpperCase()}
                                            </Badge>
                                            <Text fontSize="xs" color="gray.600">
                                              {formatFileSize(version.fileSizeBytes)}
                                            </Text>
                                          </HStack>
                                          <Button
                                            size="xs"
                                            leftIcon={<Download size={14} />}
                                            onClick={() => handleDownload(version.sasUrlDownload, file.fileName.replace(/(\.[^.]+)$/, `_v${version.versionNumber}$1`))}
                                          >
                                            Pobierz
                                          </Button>
                                        </HStack>
                                        <HStack spacing={4} fontSize="xs" color="gray.600" mb={2}>
                                          <HStack spacing={1}>
                                            <User size={12} />
                                            <Text>{version.createdByUserName}</Text>
                                          </HStack>
                                          <HStack spacing={1}>
                                            <Clock size={12} />
                                            <Text>{formatDate(version.createdAt)}</Text>
                                          </HStack>
                                        </HStack>
                                        
                                        {/* Komentarze do wersji */}
                                        {version.comments.length > 0 && (
                                          <Box mt={3}>
                                            <HStack spacing={1} mb={3}>
                                              <MessageSquare size={14} />
                                              <Text fontSize="sm" fontWeight="semibold">
                                                Komentarze ({version.comments.length})
                                              </Text>
                                            </HStack>
                                            <VStack align="stretch" spacing={3}>
                                              {version.comments
                                                .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
                                                .map((comment) => {
                                                  const isMyComment = user?.id === comment.userId;
                                                  return (
                                                    <HStack
                                                      key={comment.id}
                                                      justify={isMyComment ? "flex-end" : "flex-start"}
                                                      w="100%"
                                                    >
                                                      <Box
                                                        maxW="75%"
                                                        bg={isMyComment ? "blue.500" : "gray.100"}
                                                        color={isMyComment ? "white" : "black"}
                                                        p={3}
                                                        borderRadius="lg"
                                                        borderBottomRightRadius={isMyComment ? "sm" : "lg"}
                                                        borderBottomLeftRadius={isMyComment ? "lg" : "sm"}
                                                      >
                                                        <VStack align="stretch" spacing={1}>
                                                          <HStack justify="space-between">
                                                            <Text fontSize="xs" fontWeight="bold" opacity={isMyComment ? 0.9 : 1}>
                                                              {comment.userName}
                                                            </Text>
                                                            {comment.isEdited && (
                                                              <Badge colorScheme={isMyComment ? "whiteAlpha" : "gray"} fontSize="2xs">
                                                                Edytowano
                                                              </Badge>
                                                            )}
                                                          </HStack>
                                                          <Text fontSize="sm">{comment.content}</Text>
                                                          <Text fontSize="2xs" opacity={0.7} textAlign={isMyComment ? "right" : "left"}>
                                                            {formatDate(comment.editedAt || comment.createdAt)}
                                                          </Text>
                                                        </VStack>
                                                      </Box>
                                                    </HStack>
                                                  );
                                                })}
                                            </VStack>
                                          </Box>
                                        )}

                                        {/* Dodaj nowy komentarz */}
                                        <Box mt={version.comments.length > 0 ? 3 : 2}>
                                          <HStack spacing={2}>
                                            <Textarea
                                              placeholder="Dodaj komentarz..."
                                              size="sm"
                                              resize="none"
                                              rows={2}
                                              value={newComments.get(`${file.id}-${version.id}`) || ""}
                                              onChange={(e) => {
                                                setNewComments((prev) => {
                                                  const updated = new Map(prev);
                                                  updated.set(`${file.id}-${version.id}`, e.target.value);
                                                  return updated;
                                                });
                                              }}
                                              onKeyDown={(e) => {
                                                if (e.key === "Enter" && !e.shiftKey) {
                                                  e.preventDefault();
                                                  handleAddComment(file.id, version.id);
                                                }
                                              }}
                                            />
                                            <IconButton
                                              aria-label="Wyślij komentarz"
                                              icon={<Send size={16} />}
                                              colorScheme="blue"
                                              size="sm"
                                              isLoading={submittingComment === `${file.id}-${version.id}`}
                                              onClick={() => handleAddComment(file.id, version.id)}
                                            />
                                          </HStack>
                                        </Box>
                                      </Box>
                                    ))}
                                </VStack>
                              </Box>
                            </Td>
                          </Tr>
                        )}
                        </>
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

        {/* Modal udostępniania plików */}
        {tenantId && projectId && (
          <ShareFilesModal
            isOpen={isShareModalOpen}
            onClose={onShareModalClose}
            tenantId={tenantId}
            projectId={projectId}
            onFilesShared={handleFilesShared}
          />
        )}

        {/* Modal uploadu nowej wersji */}
        {tenantId && projectId && fileForNewVersion && (
          <UploadNewVersionModal
            isOpen={isUploadVersionModalOpen}
            onClose={onUploadVersionModalClose}
            tenantId={tenantId}
            projectId={projectId}
            file={fileForNewVersion}
            onVersionUploaded={handleVersionUploaded}
          />
        )}

        {/* Modal potwierdzenia usunięcia */}
        <Modal isOpen={isDeleteModalOpen} onClose={onDeleteModalClose} isCentered>
          <ModalOverlay />
          <ModalContent>
            <ModalHeader>Potwierdzenie usunięcia</ModalHeader>
            <ModalCloseButton />
            <ModalBody>
              <VStack spacing={4} align="stretch">
                <Alert status="warning">
                  <AlertIcon />
                  <Text>Ta operacja jest nieodwracalna!</Text>
                </Alert>
                <Text>
                  Czy na pewno chcesz usunąć plik <strong>"{fileToDelete?.name}"</strong>?
                </Text>
                <Text fontSize="sm" color="gray.600">
                  Zostaną usunięte wszystkie wersje tego pliku ({files.find(f => f.id === fileToDelete?.id)?.totalVersions || 0}).
                </Text>
              </VStack>
            </ModalBody>
            <ModalFooter>
              <Button variant="ghost" mr={3} onClick={onDeleteModalClose}>
                Anuluj
              </Button>
              <Button colorScheme="red" leftIcon={<Trash2 size={16} />} onClick={handleDeleteFile}>
                Usuń plik
              </Button>
            </ModalFooter>
          </ModalContent>
        </Modal>
      </Box>
    </MainLayout>
  );
}
