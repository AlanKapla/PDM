import { useEffect, useState, useContext } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Badge,
  Icon,
  Button,
  useColorModeValue,
  useDisclosure,
  IconButton,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
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
  Textarea,
  useToast,
} from "@chakra-ui/react";
import { ArrowLeft, FileText, Upload, Share2, Download, Eye, ChevronDown, ChevronUp, Clock, MessageSquare, Send, User } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import UploadFilesModal from "../components/UploadFilesModal";
import UploadNewVersionModal from "../components/UploadNewVersionModal";
import { ManageFileShareModal } from "../components/ManageFileShareModal";
import ShareFilesModal from "../components/ShareFilesModal";
import { AuthContext } from "../context/AuthContext";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import { projectApi } from "../api/projectApi";
import type { ProjectFilePackageWeb, SharedProjectFilePackageWeb } from "../types/project.types";
import { canEditProject, canViewProject } from "../types/project.types";

export default function ProjectFiles() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const toast = useToast();
  const { showSuccess, showError } = useToastNotification();
  const { isOpen: isUploadModalOpen, onOpen: onUploadModalOpen, onClose: onUploadModalClose } = useDisclosure();
  const { isOpen: isUploadVersionModalOpen, onOpen: onUploadVersionModalOpen, onClose: onUploadVersionModalClose } = useDisclosure();
  const { isOpen: isManageShareModalOpen, onOpen: onManageShareModalOpen, onClose: onManageShareModalClose } = useDisclosure();
  const { isOpen: isShareFilesModalOpen, onOpen: onShareFilesModalOpen, onClose: onShareFilesModalClose } = useDisclosure();

  const [loading, setLoading] = useState(true);
  const [project, setProject] = useState<any | null>(null);
  const [myFiles, setMyFiles] = useState<ProjectFilePackageWeb[]>([]);
  const [sharedFiles, setSharedFiles] = useState<SharedProjectFilePackageWeb[]>([]);
  const [members, setMembers] = useState<any[]>([]);
  const [expandedFileIds, setExpandedFileIds] = useState<Set<string>>(new Set());
  const [fileForNewVersion, setFileForNewVersion] = useState<any | null>(null);
  const [fileToManageShare, setFileToManageShare] = useState<any | null>(null);
  const [newComments, setNewComments] = useState<Map<string, string>>(new Map());
  const [submittingComment, setSubmittingComment] = useState<string | null>(null);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  const userCanEdit = canEditProject(project?.userRole);
  const userCanView = canViewProject(project?.userRole);

  useEffect(() => {
    fetchData();
  }, [projectId]);

  const fetchData = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoading(true);
    try {
      const projectRes = await projectApi.getProjectDetails(user.activeTenantId, projectId);
      setProject(projectRes.data);
      
      const userRole = projectRes.data.userRole;
      const canEdit = canEditProject(userRole);
      const canView = canViewProject(userRole);
      
      // Pobieraj tylko te dane do których user ma dostęp
      const promises: Promise<any>[] = [
        projectApi.getProjectMembers(user.activeTenantId, projectId),
      ];
      
      if (canEdit) {
        promises.push(projectApi.getMyFiles(user.activeTenantId, projectId));
      }
      
      if (canView) {
        promises.push(projectApi.getSharedFiles(user.activeTenantId, projectId));
      }
      
      const results = await Promise.all(promises);
      
      setMembers(results[0].data);
      
      if (canEdit) {
        setMyFiles(results[1].data);
        if (canView) {
          setSharedFiles(results[2].data);
        }
      } else if (canView) {
        setSharedFiles(results[1].data);
      }
    } catch (error) {
      showError("Nie udało się pobrać danych");
    } finally {
      setLoading(false);
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + " " + sizes[i];
  };

  const handleDownloadFile = (sasUrl: string, fileName: string) => {
    fetch(sasUrl, { method: "GET", mode: "cors" })
      .then(response => response.blob())
      .then(blob => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
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
        });
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

  const handlePreview = (sasUrlView: string) => {
    window.open(sasUrlView, '_blank', 'noopener,noreferrer');
  };

  const isPreviewSupported = (contentType: string): boolean => {
    return (
      contentType === "application/pdf" ||
      contentType.startsWith("image/")
    );
  };

  const openUploadVersionModal = (file: any) => {
    setFileForNewVersion(file);
    onUploadVersionModalOpen();
  };

  const handleVersionUploaded = () => {
    fetchData();
    onUploadVersionModalClose();
  };

  const openManageShareModal = (file: any) => {
    setFileToManageShare(file);
    onManageShareModalOpen();
  };

  const handleShareUpdated = () => {
    fetchData();
    onManageShareModalClose();
  };

  const handleAddComment = async (fileId: string, versionId: string) => {
    if (!user?.activeTenantId || !projectId) return;

    const commentKey = `${fileId}-${versionId}`;
    const comment = newComments.get(commentKey);

    if (!comment || comment.trim() === "") {
      toast({
        title: "Uwaga",
        description: "Komentarz nie może być pusty",
        status: "warning",
        duration: 3000,
      });
      return;
    }

    try {
      setSubmittingComment(commentKey);
      await projectApi.addFileVersionComment(
        user.activeTenantId,
        projectId,
        fileId,
        versionId,
        comment.trim()
      );

      showSuccess("Komentarz został dodany");
      setNewComments((prev) => {
        const updated = new Map(prev);
        updated.delete(commentKey);
        return updated;
      });
      await fetchData();
    } catch (error) {
      showError("Nie udało się dodać komentarza");
    } finally {
      setSubmittingComment(null);
    }
  };

  const renderFileRow = (file: any, isShared: boolean = false) => {
    const fileId = isShared ? file.projectFileId : file.id;
    
    return (
      <>
        <Tr key={fileId}>
          <Td>
            <HStack spacing={2}>
              <Text fontSize="sm" fontWeight="medium">{file.displayName}</Text>
              {file.currentVersion?.versionNumber && (
                <Badge colorScheme="purple" fontSize="xs">v{file.currentVersion.versionNumber}</Badge>
              )}
              {!isShared && file.sharedWithUserIds && file.sharedWithUserIds.length > 0 && (
                <Badge colorScheme="orange" fontSize="xs" display="flex" alignItems="center" gap={1}>
                  <Share2 size={10} />
                  {file.sharedWithUserIds.length}
                </Badge>
              )}
            </HStack>
          </Td>
          {isShared && (
            <Td display={{ base: "none", md: "table-cell" }} fontSize="sm">
              {file.originalOwnerUserName || "-"}
            </Td>
          )}
          <Td display={{ base: "none", md: "table-cell" }} fontSize="sm">
            {file.currentVersion ? formatFileSize(file.currentVersion.fileSizeBytes) : "-"}
          </Td>
          <Td>
            <HStack spacing={1} flexWrap="wrap">
              {file.currentVersion && isPreviewSupported(file.currentVersion.contentType) && (
                <IconButton
                  aria-label="Podgląd"
                  icon={<Eye size={16} />}
                  size="sm"
                  variant="ghost"
                  colorScheme="purple"
                  onClick={() => handlePreview(file.currentVersion.sasUrlView)}
                />
              )}
              {file.currentVersion && (
                <IconButton
                  aria-label="Pobierz"
                  icon={<Download size={16} />}
                  size="sm"
                  variant="ghost"
                  colorScheme="blue"
                  onClick={() => {
                    const fileName = file.fileName || file.displayName || 'plik';
                    handleDownloadFile(file.currentVersion.sasUrlDownload, fileName);
                  }}
                />
              )}
              <IconButton
                aria-label="Nowa wersja"
                icon={<Upload size={16} />}
                size="sm"
                variant="ghost"
                colorScheme="green"
                onClick={() => openUploadVersionModal(file)}
              />
              {!isShared && (
                <IconButton
                  aria-label="Zarządzaj udostępnieniem"
                  icon={<Share2 size={16} />}
                  size="sm"
                  variant="ghost"
                  colorScheme="orange"
                  onClick={() => openManageShareModal(file)}
                />
              )}
              {file.versions && file.versions.length > 0 && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => toggleFileVersions(fileId)}
                  rightIcon={expandedFileIds.has(fileId) ? <ChevronUp size={16} />  : <ChevronDown size={16} />}
                >
                  Wersje ({file.totalVersions})
                </Button>
              )}
            </HStack>
          </Td>
        </Tr>
        {expandedFileIds.has(fileId) && file.versions && file.versions.length > 0 && (
          <Tr key={`${fileId}-versions`}>
            <Td colSpan={isShared ? 4 : 3} p={0}>
              <Box bg={useColorModeValue("gray.50", "gray.900")} p={4}>
                <VStack align="stretch" spacing={3}>
                  <Heading size="sm" mb={2}>
                    Historia wersji ({file.totalVersions})
                  </Heading>
                  {file.versions
                    .sort((a: any, b: any) => b.versionNumber - a.versionNumber)
                    .map((version: any) => (
                      <Box
                        key={version.id}
                        borderWidth="1px"
                        borderRadius="md"
                        p={3}
                        bg={version.id === file.currentVersion?.id ? useColorModeValue("blue.50", "blue.900") : cardBg}
                        borderColor={version.id === file.currentVersion?.id ? "blue.300" : borderColor}
                      >
                        <HStack justify="space-between" mb={2}>
                          <HStack spacing={2} flexWrap="wrap">
                            <Badge colorScheme={version.id === file.currentVersion?.id ? "blue" : "gray"}>
                              Wersja {version.versionNumber}
                              {version.id === file.currentVersion?.id && " (Aktualna)"}
                            </Badge>
                            <Badge colorScheme="purple" fontSize="xs">
                              {version.contentType?.split("/")[1]?.toUpperCase() || "FILE"}
                            </Badge>
                            <Text fontSize="xs" color="gray.600">
                              {formatFileSize(version.fileSizeBytes)}
                            </Text>
                          </HStack>
                          <HStack spacing={1}>
                            {isPreviewSupported(version.contentType) && (
                              <IconButton
                                aria-label="Podgląd"
                                icon={<Eye size={14} />}
                                size="xs"
                                colorScheme="purple"
                                onClick={() => handlePreview(version.sasUrlView)}
                              />
                            )}
                            <Button
                              size="xs"
                              leftIcon={<Download size={14} />}
                              onClick={() => {
                                const fileName = file.fileName || file.displayName || 'plik';
                                const isCurrentVersion = version.id === file.currentVersion?.id;
                                const finalName = isCurrentVersion 
                                  ? fileName 
                                  : fileName.replace(/(\.[^.]+)$/, `_v${version.versionNumber}$1`);
                                handleDownloadFile(version.sasUrlDownload, finalName);
                              }}
                            >
                              Pobierz
                            </Button>
                          </HStack>
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
                        
                        {/* Komentarze */}
                        <Box mt={3}>
                          <HStack spacing={1} mb={3}>
                            <MessageSquare size={14} />
                            <Text fontSize="sm" fontWeight="semibold">
                              Komentarze ({version.comments?.length || 0})
                            </Text>
                          </HStack>
                          
                          {version.comments && version.comments.length > 0 && (
                            <VStack align="stretch" spacing={3} mb={3}>
                              {version.comments
                                .sort((a: any, b: any) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
                                .map((comment: any) => {
                                  const isMyComment = user?.id === comment.userId;
                                  return (
                                    <HStack
                                      key={comment.id}
                                      justify={isMyComment ? "flex-end" : "flex-start"}
                                      w="100%"
                                    >
                                      <Box
                                        maxW="75%"
                                        bg={isMyComment ? "blue.500" : useColorModeValue("gray.100", "gray.700")}
                                        color={isMyComment ? "white" : useColorModeValue("black", "white")}
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
                          )}
                          
                          <HStack spacing={2}>
                            <Textarea
                              placeholder="Dodaj komentarz..."
                              size="sm"
                              value={newComments.get(`${fileId}-${version.id}`) || ""}
                              onChange={(e) => {
                                setNewComments((prev) => {
                                  const updated = new Map(prev);
                                  updated.set(`${fileId}-${version.id}`, e.target.value);
                                  return updated;
                                });
                              }}
                              rows={2}
                              resize="vertical"
                            />
                            <IconButton
                              aria-label="Wyślij komentarz"
                              icon={<Send size={16} />}
                              colorScheme="blue"
                              size="sm"
                              onClick={() => handleAddComment(isShared ? file.projectFileId : file.id, version.id)}
                              isLoading={submittingComment === `${fileId}-${version.id}`}
                              isDisabled={!newComments.get(`${fileId}-${version.id}`)?.trim()}
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
    );
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie plików..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={FileText} boxSize={8} color="purple.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Pliki projektu</Heading>
              {project && <Text fontSize="sm" color="gray.600">{project.name}</Text>}
            </VStack>
          </HStack>
        </HStack>

        {!project || !userCanView ? (
          <Box p={8} textAlign="center">
            <EmptyState
              icon={FileText}
              title="Brak dostępu"
              description="Nie masz uprawnień do przeglądania plików w tym projekcie"
            />
          </Box>
        ) : (
        <Tabs colorScheme="purple" variant="enclosed">
          <TabList>
            {userCanEdit && (
              <Tab fontWeight="bold">
                <HStack spacing={2}>
                  <Icon as={FileText} boxSize={4} />
                  <Text>Moje pliki</Text>
                  <Badge colorScheme="blue" ml={2}>{myFiles.reduce((sum, pkg) => sum + pkg.totalFiles, 0)}</Badge>
                </HStack>
              </Tab>
            )}
            {userCanView && (
              <Tab fontWeight="bold">
                <HStack spacing={2}>
                  <Icon as={Share2} boxSize={4} />
                  <Text>Udostępnione</Text>
                  <Badge colorScheme="teal" ml={2}>{sharedFiles.reduce((sum, pkg) => sum + pkg.totalSharedFiles, 0)}</Badge>
                </HStack>
              </Tab>
            )}
          </TabList>

          <TabPanels>
            {/* TAB 1: MOJE PLIKI */}
            {userCanEdit && (
            <TabPanel>
              <VStack spacing={4} align="stretch">
                <HStack justify="space-between">
                  <Text fontSize="sm" color="gray.600">
                    Twoje pliki w projekcie
                  </Text>
                  <HStack spacing={2}>
                    <Button
                      leftIcon={<Share2 size={18} />}
                      colorScheme="orange"
                      size="sm"
                      onClick={onShareFilesModalOpen}
                    >
                      Udostępnij grupowo
                    </Button>
                    <Button
                      leftIcon={<Upload size={18} />}
                      colorScheme="green"
                      onClick={onUploadModalOpen}
                    >
                      Dodaj pliki
                    </Button>
                  </HStack>
                </HStack>

                {myFiles.length === 0 ? (
                  <EmptyState
                    icon={FileText}
                    title="Brak plików"
                    description="Nie masz jeszcze żadnych plików w tym projekcie"
                  />
                ) : (
                  <Accordion allowMultiple>
                    {myFiles.map((pkg) => (
                      <AccordionItem key={pkg.id} bg={cardBg} borderWidth="1px" borderColor={borderColor} rounded="md" mb={3}>
                        <AccordionButton py={4} _hover={{ bg: hoverBg }}>
                          <HStack flex="1" spacing={3}>
                            <Icon as={FileText} boxSize={5} color="purple.600" />
                            <Text fontWeight="bold" fontSize="lg">📦 {pkg.name}</Text>
                            <Badge colorScheme="blue" fontSize="sm">{pkg.totalFiles}</Badge>
                          </HStack>
                          <AccordionIcon />
                        </AccordionButton>
                        <AccordionPanel pb={4}>
                          <Table size="sm" variant="simple">
                            <Thead>
                              <Tr>
                                <Th>Nazwa pliku</Th>
                                <Th display={{ base: "none", md: "table-cell" }}>Rozmiar</Th>
                                <Th>Akcje</Th>
                              </Tr>
                            </Thead>
                            <Tbody>
                              {pkg.files.map((file) => renderFileRow(file, false))}
                            </Tbody>
                          </Table>
                        </AccordionPanel>
                      </AccordionItem>
                    ))}
                  </Accordion>
                )}
              </VStack>
            </TabPanel>
            )}

            {/* TAB 2: PLIKI UDOSTĘPNIONE */}
            {userCanView && (
            <TabPanel>
              <VStack spacing={4} align="stretch">
                <Text fontSize="sm" color="gray.600">
                  Pliki udostępnione przez innych członków projektu
                </Text>

                {sharedFiles.length === 0 ? (
                  <EmptyState
                    icon={Share2}
                    title="Brak udostępnionych plików"
                    description="Nikt jeszcze nie udostępnił Ci plików w tym projekcie"
                  />
                ) : (
                  <Accordion allowMultiple>
                    {sharedFiles.map((pkg) => (
                      <AccordionItem key={pkg.packageId} bg={cardBg} borderWidth="1px" borderColor={borderColor} rounded="md" mb={3}>
                        <AccordionButton py={4} _hover={{ bg: hoverBg }}>
                          <HStack flex="1" spacing={3}>
                            <Icon as={Share2} boxSize={5} color="teal.600" />
                            <Text fontWeight="bold" fontSize="lg">📦 {pkg.packageName}</Text>
                            <Badge colorScheme="blue" fontSize="sm">{pkg.totalSharedFiles}</Badge>
                            <Text fontSize="sm" color="gray.500">od: {pkg.packageOwnerName}</Text>
                          </HStack>
                          <AccordionIcon />
                        </AccordionButton>
                        <AccordionPanel pb={4}>
                          <Table size="sm" variant="simple">
                            <Thead>
                              <Tr>
                                <Th>Nazwa pliku</Th>
                                <Th display={{ base: "none", md: "table-cell" }}>Właściciel</Th>
                                <Th display={{ base: "none", md: "table-cell" }}>Rozmiar</Th>
                                <Th>Akcje</Th>
                              </Tr>
                            </Thead>
                            <Tbody>
                              {pkg.files.map((file) => renderFileRow(file, true))}
                            </Tbody>
                          </Table>
                        </AccordionPanel>
                      </AccordionItem>
                    ))}
                  </Accordion>
                )}
              </VStack>
            </TabPanel>
            )}
          </TabPanels>
        </Tabs>
        )}

        <UploadFilesModal
          isOpen={isUploadModalOpen}
          onClose={onUploadModalClose}
          projectId={projectId || ""}
          tenantId={user?.activeTenantId || ""}
          projectName={project?.name || ""}
          onFilesUploaded={fetchData}
        />

        {fileForNewVersion && (
          <UploadNewVersionModal
            isOpen={isUploadVersionModalOpen}
            onClose={onUploadVersionModalClose}
            projectId={projectId || ""}
            tenantId={user?.activeTenantId || ""}
            file={fileForNewVersion}
            onVersionUploaded={handleVersionUploaded}
          />
        )}

        {fileToManageShare && (
          <ManageFileShareModal
            isOpen={isManageShareModalOpen}
            onClose={onManageShareModalClose}
            projectId={projectId || ""}
            tenantId={user?.activeTenantId || ""}
            fileId={fileToManageShare.id}
            fileName={fileToManageShare.displayName}
            sharedWithUserIds={fileToManageShare.sharedWithUserIds || []}
            members={members}
            currentUserId={user?.id || ""}
            onShareUpdated={handleShareUpdated}
          />
        )}

        <ShareFilesModal
          isOpen={isShareFilesModalOpen}
          onClose={onShareFilesModalClose}
          projectId={projectId || ""}
          tenantId={user?.activeTenantId || ""}
          onFilesShared={fetchData}
        />
      </Box>
    </MainLayout>
  );
}
