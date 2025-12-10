import React, { useEffect, useState, useContext } from "react";
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
  useDisclosure,
  Textarea,
  IconButton,
} from "@chakra-ui/react";
import { Download, ArrowLeft, Users, Eye, Share2, MessageSquare, Clock, User, ChevronDown, ChevronUp, Upload, Send } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import UploadNewVersionModal from "../components/UploadNewVersionModal";
import { projectApi } from "../api/projectApi";
import type { SharedProjectFileWeb, ProjectFileWeb } from "../types/project.types";

interface GroupedSharedFiles {
  [packageName: string]: {
    [sharedByUserName: string]: SharedProjectFileWeb[];
  };
}

export default function SharedFiles() {
  const { tenantId, projectId } = useParams<{ tenantId: string; projectId: string }>();
  const { user } = useContext(AuthContext);
  const [files, setFiles] = useState<SharedProjectFileWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedFileIds, setExpandedFileIds] = useState<Set<string>>(new Set());
  const [expandedAccordionIndices, setExpandedAccordionIndices] = useState<number[]>([]);
  const [fileForNewVersion, setFileForNewVersion] = useState<ProjectFileWeb | null>(null);
  const [newComments, setNewComments] = useState<Map<string, string>>(new Map());
  const [submittingComment, setSubmittingComment] = useState<string | null>(null);
  const { isOpen: isUploadVersionModalOpen, onOpen: onUploadVersionModalOpen, onClose: onUploadVersionModalClose } = useDisclosure();
  const toast = useToast();
  const navigate = useNavigate();
  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");

  useEffect(() => {
    fetchSharedFiles(true);
  }, [tenantId, projectId]);

  const fetchSharedFiles = async (showLoading = false) => {
    if (!tenantId || !projectId) return;

    try {
      if (showLoading) setLoading(true);
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

      if (response.ok) {
        toast({
          title: "Sukces",
          description: "Komentarz został dodany",
          status: "success",
          duration: 3000,
          isClosable: true,
        });

        setNewComments((prev) => {
          const updated = new Map(prev);
          updated.delete(commentKey);
          return updated;
        });

        await fetchSharedFiles();
      } else {
        throw new Error("Nie udało się dodać komentarza");
      }
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

  const openUploadVersionModal = (file: SharedProjectFileWeb) => {
    const projectFile: ProjectFileWeb = {
      id: file.projectFileId,
      fileName: file.fileName,
      displayName: file.displayName,
      packageName: file.packageName,
      createdAt: file.uploadedAt,
      ownerId: file.originalOwnerUserId,
      ownerName: file.originalOwnerUserName,
      currentVersion: file.currentVersion,
      versions: file.versions,
      totalVersions: file.totalVersions,
      isOwner: false,
      isShared: true,
    };
    setFileForNewVersion(projectFile);
    onUploadVersionModalOpen();
  };

  const handleVersionUploaded = () => {
    onUploadVersionModalClose();
    setFileForNewVersion(null);
    fetchSharedFiles();
  };

  const handlePreview = (sasUrlView: string) => {
    window.open(sasUrlView, '_blank', 'noopener,noreferrer');
  };

  const isPreviewSupported = (contentType: string): boolean => {
    return (
      contentType === "application/pdf" ||
      contentType.startsWith("image/")
    );
  };

  const handleDownload = async (sasUrlDownload: string, fileName: string) => {
    try {
      console.log('handleDownload fileName:', fileName);
      const response = await fetch(sasUrlDownload, {
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
            <Accordion 
              allowMultiple 
              index={expandedAccordionIndices}
              onChange={(indices) => setExpandedAccordionIndices(indices as number[])}
            >
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
                          const totalSize = userFiles.reduce((sum, file) => sum + (file.currentVersion?.fileSizeBytes || file.fileSizeBytes), 0);

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
                                    <React.Fragment key={file.id}>
                                    <Tr>
                                      <Td>
                                        <VStack align="start" spacing={0}>
                                          {isPreviewSupported(file.currentVersion?.contentType || file.contentType) ? (
                                            <Link
                                              color="blue.600"
                                              fontWeight="medium"
                                              onClick={() => handlePreview(file.currentVersion?.sasUrlView || file.sasUrl)}
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
                                            <Badge colorScheme={(file.currentVersion?.contentType || file.contentType).includes("pdf") ? "red" : "green"} fontSize="xs">
                                              {(file.currentVersion?.contentType || file.contentType).split("/")[1]?.toUpperCase() || "FILE"}
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
                                        <Badge colorScheme={(file.currentVersion?.contentType || file.contentType).includes("pdf") ? "red" : "green"}>
                                          {(file.currentVersion?.contentType || file.contentType).split("/")[1]?.toUpperCase() || "FILE"}
                                        </Badge>
                                      </Td>
                                      <Td display={{ base: "none", lg: "table-cell" }}>{formatFileSize(file.currentVersion?.fileSizeBytes || file.fileSizeBytes)}</Td>
                                      <Td display={{ base: "none", xl: "table-cell" }}>
                                        <HStack spacing={2}>
                                          <Text fontSize="sm">{formatDate(file.sharedAt)}</Text>
                                          {file.currentVersion?.versionNumber && (
                                            <Badge colorScheme="purple" fontSize="xs">
                                              v{file.currentVersion.versionNumber}
                                            </Badge>
                                          )}
                                        </HStack>
                                      </Td>
                                      <Td>
                                        <VStack spacing={1} align="stretch" display={{ base: "flex", md: "none" }}>
                                          {isPreviewSupported(file.currentVersion?.contentType || file.contentType) && (
                                            <Link
                                              color="purple.500"
                                              onClick={() => handlePreview(file.currentVersion?.sasUrlView || file.sasUrl)}
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
                                            onClick={() => handleDownload(file.currentVersion?.sasUrlDownload || file.sasUrl, file.fileName)}
                                            cursor="pointer"
                                            display="inline-flex"
                                            alignItems="center"
                                            gap={1}
                                            fontSize="xs"
                                          >
                                            <Download size={14} />
                                            Pobierz
                                          </Link>
                                          {file.versions.length > 0 && (
                                            <Link
                                              color="gray.600"
                                              onClick={() => toggleFileVersions(file.projectFileId)}
                                              cursor="pointer"
                                              display="inline-flex"
                                              alignItems="center"
                                              gap={1}
                                              fontSize="xs"
                                            >
                                              {expandedFileIds.has(file.projectFileId) ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                                              Wersje ({file.totalVersions})
                                            </Link>
                                          )}
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
                                            Upload nowej wersji
                                          </Link>
                                        </VStack>
                                        <HStack spacing={3} display={{ base: "none", md: "flex" }}>
                                          {isPreviewSupported(file.currentVersion?.contentType || file.contentType) && (
                                            <Link
                                              color="purple.500"
                                              onClick={() => handlePreview(file.currentVersion?.sasUrlView || file.sasUrl)}
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
                                            onClick={() => handleDownload(file.currentVersion?.sasUrlDownload || file.sasUrl, file.fileName)}
                                            cursor="pointer"
                                            display="inline-flex"
                                            alignItems="center"
                                            gap={1}
                                          >
                                            <Download size={16} />
                                            Pobierz
                                          </Link>
                                          {file.versions.length > 0 && (
                                            <Button
                                              size="sm"
                                              variant="ghost"
                                              leftIcon={expandedFileIds.has(file.projectFileId) ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                                              onClick={() => toggleFileVersions(file.projectFileId)}
                                            >
                                              Wersje ({file.totalVersions})
                                            </Button>
                                          )}
                                          <Button
                                            size="sm"
                                            variant="ghost"
                                            colorScheme="green"
                                            leftIcon={<Upload size={16} />}
                                            onClick={() => openUploadVersionModal(file)}
                                          >
                                            Upload nowej wersji
                                          </Button>
                                        </HStack>
                                      </Td>
                                    </Tr>
                                    {/* Rozwinięte wersje pliku */}
                                    {expandedFileIds.has(file.projectFileId) && (
                                      <Tr>
                                        <Td colSpan={6} bg="gray.50" p={4}>
                                          <Box>
                                            <VStack align="stretch" spacing={4}>
                                              {file.versions.map((version) => (
                                                <Box
                                                  key={version.id}
                                                  p={3}
                                                  bg="white"
                                                  borderRadius="md"
                                                  borderWidth="1px"
                                                  borderColor="gray.200"
                                                >
                                                  <HStack justify="space-between" mb={2}>
                                                    <HStack spacing={2}>
                                                      <Badge colorScheme="purple" fontSize="sm">
                                                        Wersja {version.versionNumber}
                                                      </Badge>
                                                      <Badge colorScheme={(version.contentType).includes("pdf") ? "red" : "green"} fontSize="xs">
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
                                                        value={newComments.get(`${file.projectFileId}-${version.id}`) || ""}
                                                        onChange={(e) => {
                                                          setNewComments((prev) => {
                                                            const updated = new Map(prev);
                                                            updated.set(`${file.projectFileId}-${version.id}`, e.target.value);
                                                            return updated;
                                                          });
                                                        }}
                                                        onKeyDown={(e) => {
                                                          if (e.key === "Enter" && !e.shiftKey) {
                                                            e.preventDefault();
                                                            handleAddComment(file.projectFileId, version.id);
                                                          }
                                                        }}
                                                      />
                                                      <IconButton
                                                        aria-label="Wyślij komentarz"
                                                        icon={<Send size={16} />}
                                                        colorScheme="blue"
                                                        size="sm"
                                                        isLoading={submittingComment === `${file.projectFileId}-${version.id}`}
                                                        onClick={() => handleAddComment(file.projectFileId, version.id)}
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
                                  </React.Fragment>
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


      </Box>

      {/* Modal uploadu nowej wersji */}
      {fileForNewVersion && (
        <UploadNewVersionModal
          isOpen={isUploadVersionModalOpen}
          onClose={onUploadVersionModalClose}
          file={fileForNewVersion}
          tenantId={tenantId!}
          projectId={projectId!}
          onVersionUploaded={handleVersionUploaded}
        />
      )}
    </MainLayout>
  );
}
