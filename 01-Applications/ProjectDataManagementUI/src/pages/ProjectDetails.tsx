import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Badge,
  Icon,
  Spinner,
  Alert,
  AlertIcon,
  Button,
  useColorModeValue,
  useDisclosure,
  IconButton,
  useToast,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
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
  Input,
  Textarea,
} from "@chakra-ui/react";
import { FolderKanban, User, Calendar, ArrowLeft, Users, UserPlus, Trash2, Upload, FileText, Share2, Download, ChevronDown, ChevronUp, Clock, MessageSquare, Send, Eye } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import AddProjectMemberModal from "../components/AddProjectMemberModal";
import { handleApiError } from "../utils/handleApiError";
import UploadFilesModal from "../components/UploadFilesModal";
import UploadNewVersionModal from "../components/UploadNewVersionModal";
import CreateWorkScheduleModal from "../components/CreateWorkScheduleModal";
import { projectApi } from "../api/projectApi";
import { tenantApi } from "../api/tenantApi";
import { useAuth } from "../hooks/useAuth";
import { ProjectRole } from "../types/project.types";
import type { WorkScheduleSummaryWeb } from "../types/workSchedule.types";

/* Helpery UI */
const getProjectRoleName = (role: number) =>
  role === ProjectRole.Admin ? "Administrator" : "Członek";

const getProjectRoleColor = (role: number) =>
  role === ProjectRole.Admin ? "blue" : "green";

export default function ProjectDetails() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { isOpen: isRemoveModalOpen, onOpen: onRemoveModalOpen, onClose: onRemoveModalClose } = useDisclosure();
  const { isOpen: isUploadModalOpen, onOpen: onUploadModalOpen, onClose: onUploadModalClose } = useDisclosure();
  const { isOpen: isUploadVersionModalOpen, onOpen: onUploadVersionModalOpen, onClose: onUploadVersionModalClose } = useDisclosure();
  const { isOpen: isWorkScheduleModalOpen, onOpen: onWorkScheduleModalOpen, onClose: onWorkScheduleModalClose } = useDisclosure();
  const toast = useToast();

  const [project, setProject] = useState<any | null>(null);
  const [members, setMembers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMembers, setLoadingMembers] = useState(false);
  const [removingMember, setRemovingMember] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{ userId: string; name: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [userTenantRole, setUserTenantRole] = useState<number | null>(null);
  const [myFiles, setMyFiles] = useState<any[]>([]);
  const [sharedFiles, setSharedFiles] = useState<any[]>([]);
  const [expandedFileIds, setExpandedFileIds] = useState<Set<string>>(new Set());
  const [fileForNewVersion, setFileForNewVersion] = useState<any | null>(null);
  const [newComments, setNewComments] = useState<Map<string, string>>(new Map());
  const [submittingComment, setSubmittingComment] = useState<string | null>(null);
  const [workSchedules, setWorkSchedules] = useState<WorkScheduleSummaryWeb[]>([]);
  const [loadingWorkSchedules, setLoadingWorkSchedules] = useState(false);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  const isProjectAdmin = project && project.userRole === ProjectRole.Admin;
  const isTenantAdmin = userTenantRole === 0; // TenantRole.Admin
  
  console.log("🔍 userTenantRole:", userTenantRole, "isTenantAdmin:", isTenantAdmin);

  const fetchProjectDetails = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoading(true);
    setError(null);

    try {
      const response = await projectApi.getProjectDetails(
        user.activeTenantId,
        projectId
      );

      if (!response.ok) {
        setError("Nie udało się pobrać danych projektu");
        setLoading(false);
        return;
      }

      setProject(await response.json());
    } catch (err) {
      console.error(err);
      setError("Błąd podczas pobierania szczegółów projektu");
    } finally {
      setLoading(false);
    }
  };

  const fetchMembers = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoadingMembers(true);

    try {
      const response = await projectApi.getProjectMembers(
        user.activeTenantId,
        projectId
      );

      if (response.ok) setMembers(await response.json());
    } catch (err) {
      console.error("Błąd pobierania członków projektu:", err);
    } finally {
      setLoadingMembers(false);
    }
  };

  const fetchMyFiles = async () => {
    if (!user?.activeTenantId || !projectId) return;

    try {
      const response = await projectApi.getMyFiles(user.activeTenantId, projectId);
      if (response.ok) {
        const data = await response.json();
        setMyFiles(data);
      }
    } catch (err) {
      console.error("Błąd pobierania moich plików:", err);
    }
  };

  const fetchSharedFiles = async () => {
    if (!user?.activeTenantId || !projectId) return;

    try {
      const response = await projectApi.getSharedFiles(user.activeTenantId, projectId);
      if (response.ok) {
        const data = await response.json();
        setSharedFiles(data);
      }
    } catch (err) {
      console.error("Błąd pobierania udostępnionych plików:", err);
    }
  };

  const fetchWorkSchedules = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoadingWorkSchedules(true);
    try {
      const response = await projectApi.getMyWorkSchedules(user.activeTenantId, projectId);
      if (response.ok) {
        const data: WorkScheduleSummaryWeb[] = await response.json();
        setWorkSchedules(data);
      }
    } catch (err) {
      console.error("Błąd pobierania harmonogramów:", err);
    } finally {
      setLoadingWorkSchedules(false);
    }
  };

  const groupFilesByPackage = (files: any[]) => {
    return files.reduce((acc, file) => {
      const pkg = file.packageName || "Bez pakietu";
      if (!acc[pkg]) acc[pkg] = [];
      acc[pkg].push(file);
      return acc;
    }, {} as Record<string, any[]>);
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
    fetchMyFiles();
    fetchSharedFiles();
    onUploadVersionModalClose();
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
      const response = await projectApi.addFileVersionComment(
        user.activeTenantId,
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
        });

        // Wyczyść pole komentarza
        setNewComments((prev) => {
          const updated = new Map(prev);
          updated.delete(commentKey);
          return updated;
        });

        // Odśwież listę plików
        await fetchMyFiles();
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
      });
    } finally {
      setSubmittingComment(null);
    }
  };

  useEffect(() => {
    fetchProjectDetails();
    fetchMembers();
    fetchMyFiles();
    fetchSharedFiles();
    fetchWorkSchedules();
    
    // Pobierz rolę użytkownika w tenancie
    const fetchUserTenantRole = async () => {
      if (!user?.activeTenantId) {
        console.log("🔴 Brak activeTenantId");
        return;
      }
      
      try {
        const response = await tenantApi.getUserTenants();
        
        console.log("🔵 getUserTenants response:", response.status);
        
        if (response.ok) {
          const tenants = await response.json();
          console.log("🔵 User tenants:", tenants);
          
          // Znajdź aktywny tenant i pobierz rolę
          const activeTenant = tenants.find((t: any) => t.id === user.activeTenantId);
          if (activeTenant) {
            console.log("🔵 Active tenant:", activeTenant);
            console.log("🔵 User role in tenant:", activeTenant.role);
            setUserTenantRole(activeTenant.role);
          }
        }
      } catch (error) {
        console.error("❌ Błąd pobierania roli tenanta:", error);
      }
    };
    
    fetchUserTenantRole();
  }, [projectId, user?.activeTenantId]);

  const handleRemoveMemberClick = (userId: string, memberName: string) => {
    setMemberToRemove({ userId, name: memberName });
    onRemoveModalOpen();
  };

  const handleConfirmRemoveMember = async () => {
    if (!user?.activeTenantId || !projectId || !memberToRemove) return;
    
    setRemovingMember(memberToRemove.userId);
    try {
      const response = await projectApi.removeProjectMember(user.activeTenantId, projectId, memberToRemove.userId);
      
      if (response.ok) {
        toast({
          title: "Sukces",
          description: `Użytkownik ${memberToRemove.name} został usunięty z projektu`,
          status: "success",
          duration: 3000,
        });
        
        // Odśwież listę
        await fetchProjectDetails();
        await fetchMembers();
      } else {
        const { title, description } = await handleApiError(response);
        toast({
          title,
          description,
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      console.error("Błąd podczas usuwania członka:", error);
    } finally {
      setRemovingMember(null);
      setMemberToRemove(null);
      onRemoveModalClose();
    }
  };

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        {/* BACK BUTTON */}
        <Button
          leftIcon={<ArrowLeft size={18} />}
          variant="ghost"
          mb={6}
          onClick={() => navigate("/projects")}
        >
          Wróć do projektów
        </Button>

        {loading ? (
          <HStack justify="center" spacing={4} py={10}>
            <Spinner size="xl" />
            <Text>Ładowanie projektu...</Text>
          </HStack>
        ) : error ? (
          <Alert status="error" rounded="md">
            <AlertIcon />
            {error}
          </Alert>
        ) : !project ? (
          <Alert status="warning" rounded="md">
            <AlertIcon />
            Projekt nie istnieje
          </Alert>
        ) : (
          <VStack spacing={6} align="stretch">
            {/* Nagłówek projektu */}
            <Box bg={cardBg} p={{ base: 4, md: 6 }} rounded="lg" borderWidth="1px" borderColor={borderColor} shadow="sm">
              <HStack spacing={4} mb={4}>
                <Icon as={FolderKanban} boxSize={{ base: 8, md: 10 }} color="blue.600" />
                <VStack align="flex-start" spacing={1} flex={1}>
                  <Heading size={{ base: "md", md: "lg" }}>{project.name}</Heading>
                  <HStack spacing={2} flexWrap="wrap">
                    <Badge colorScheme={project.isActive ? "green" : "gray"} fontSize="xs" px={2}>
                      {project.isActive ? "Aktywny" : "Nieaktywny"}
                    </Badge>
                    <Badge colorScheme={getProjectRoleColor(project.userRole)} fontSize="xs" px={2}>
                      {getProjectRoleName(project.userRole)}
                    </Badge>
                  </HStack>
                </VStack>
              </HStack>
              
              <VStack align="flex-start" spacing={2} fontSize="sm">
                <HStack>
                  <Icon as={User} boxSize={4} color="gray.500" />
                  <Text><strong>Utworzył:</strong> {project.createdByUserName}</Text>
                </HStack>
                <HStack>
                  <Icon as={Calendar} boxSize={4} color="gray.500" />
                  <Text><strong>Data utworzenia:</strong> {formatDate(project.createdAt)}</Text>
                </HStack>
                <HStack>
                  <Icon as={Users} boxSize={4} color="gray.500" />
                  <Text><strong>Liczba członków:</strong> {members.length}</Text>
                </HStack>
              </VStack>
            </Box>

            {/* ====================== SEKCJE ZWIJANE ======================= */}
            <Accordion allowMultiple>
              {/* ====================== SEKCJA: HARMONOGRAMY ======================= */}
              <AccordionItem bg={cardBg} border="1px" borderColor={borderColor} borderRadius="md" mb={4}>
                <AccordionButton py={4}>
                  <HStack flex="1" spacing={3}>
                    <Icon as={Calendar} boxSize={6} color="purple.600" />
                    <Heading size="md">Harmonogramy prac</Heading>
                    <Badge colorScheme="purple" fontSize="sm">{workSchedules.length}</Badge>
                  </HStack>
                  <Button
                    leftIcon={<Calendar size={18} />}
                    colorScheme="purple"
                    size="sm"
                    mr={2}
                    onClick={(e) => { e.stopPropagation(); onWorkScheduleModalOpen(); }}
                  >
                    Utwórz harmonogram
                  </Button>
                  <AccordionIcon />
                </AccordionButton>
                <AccordionPanel pb={4}>
                  {loadingWorkSchedules ? (
                    <HStack justify="center" py={6}>
                      <Spinner size="md" />
                    </HStack>
                  ) : workSchedules.length === 0 ? (
                    <Text color="gray.500" fontSize="sm">Brak harmonogramów. Kliknij "Utwórz harmonogram" aby dodać pierwszy.</Text>
                  ) : (
                    <VStack spacing={3} align="stretch">
                      {workSchedules.map((schedule) => (
                        <Box
                          key={schedule.id}
                          p={4}
                          borderWidth="1px"
                          borderColor={borderColor}
                          rounded="md"
                          _hover={{ bg: hoverBg }}
                          transition="0.15s"
                          cursor="pointer"
                          onClick={() => navigate(`/projects/${projectId}/schedules/${schedule.id}`)}
                        >
                          <VStack align="flex-start" spacing={2}>
                            <Text fontWeight="bold" fontSize="lg">{schedule.name}</Text>
                            <HStack spacing={4} fontSize="xs" color="gray.500">
                              <HStack spacing={1}>
                                <User size={12} />
                                <Text>{schedule.createdByUserName}</Text>
                              </HStack>
                              <HStack spacing={1}>
                                <Clock size={12} />
                                <Text>{formatDate(schedule.createdAt)}</Text>
                              </HStack>
                            </HStack>
                          </VStack>
                        </Box>
                      ))}
                    </VStack>
                  )}
                </AccordionPanel>
              </AccordionItem>

              {/* ====================== SEKCJA: CZŁONKOWIE ======================= */}
              <AccordionItem bg={cardBg} border="1px" borderColor={borderColor} borderRadius="md" mb={4}>
                <AccordionButton py={4}>
                  <HStack flex="1" spacing={3}>
                    <Icon as={Users} boxSize={6} color="blue.600" />
                    <Heading size="md">Członkowie</Heading>
                    <Badge colorScheme="blue" fontSize="sm">{members.length}</Badge>
                  </HStack>
                  {isProjectAdmin && (
                    <Button
                      leftIcon={<UserPlus size={18} />}
                      colorScheme="blue"
                      size="sm"
                      mr={2}
                      onClick={(e) => { e.stopPropagation(); onOpen(); }}
                    >
                      Dodaj członka
                    </Button>
                  )}
                  <AccordionIcon />
                </AccordionButton>
                <AccordionPanel pb={4}>

              {loadingMembers ? (
                <HStack justify="center" py={6}>
                  <Spinner size="md" />
                </HStack>
              ) : members.length === 0 ? (
                <Text color="gray.500">Brak członków w tym projekcie</Text>
              ) : (
                <VStack spacing={3} align="stretch">
                  {members.map((m) => {
                    const initials = `${m.firstName[0]}${m.lastName[0]}`;

                    return (
                      <Box
                        key={m.userId}
                        p={4}
                        borderWidth="1px"
                        borderColor={borderColor}
                        rounded="md"
                        _hover={{ bg: hoverBg }}
                        transition="0.15s"
                      >
                        <HStack justify="space-between">
                          <HStack spacing={3} flex={1}>
                            <Box
                              w="42px"
                              h="42px"
                              rounded="full"
                              bg="blue.600"
                              color="white"
                              fontWeight="bold"
                              fontSize="sm"
                              display="flex"
                              alignItems="center"
                              justifyContent="center"
                            >
                              {initials}
                            </Box>

                            <VStack align="flex-start" spacing={0}>
                              <Text fontWeight="medium">
                                {m.firstName} {m.lastName}
                              </Text>
                              <Text fontSize="xs" color="gray.500">
                                {m.email}
                              </Text>
                              <Text fontSize="xs" color="gray.500">
                                Dołączył: {formatDate(m.joinedAt)}
                              </Text>
                            </VStack>
                          </HStack>
                          
                          <HStack spacing={2}>
                            <Badge colorScheme={getProjectRoleColor(m.role)} fontSize="sm" px={3} py={1}>
                              {getProjectRoleName(m.role)}
                            </Badge>
                            {/* Pokaż przycisk usuwania tylko dla admina tenanta i tylko dla innych użytkowników */}
                            {isTenantAdmin && m.email.toLowerCase() !== user?.email.toLowerCase() && (
                              <IconButton
                                aria-label="Usuń członka"
                                icon={<Trash2 size={16} />}
                                size="sm"
                                colorScheme="red"
                                variant="ghost"
                                isDisabled={removingMember !== null}
                                onClick={() => handleRemoveMemberClick(m.userId, `${m.firstName} ${m.lastName}`)}
                              />
                            )}
                          </HStack>
                        </HStack>
                      </Box>
                    );
                  })}
                </VStack>
              )}
                </AccordionPanel>
              </AccordionItem>

              {/* ====================== SEKCJA: MOJE PLIKI ======================= */}
              <AccordionItem bg={cardBg} border="1px" borderColor={borderColor} borderRadius="md" mb={4}>
                <AccordionButton py={4}>
                  <HStack flex="1" spacing={3}>
                    <Icon as={FileText} boxSize={6} color="purple.600" />
                    <Heading size="md">Moje pliki</Heading>
                    <Badge colorScheme="purple" fontSize="sm">{myFiles.length}</Badge>
                  </HStack>
                  <Button
                    leftIcon={<Upload size={18} />}
                    colorScheme="green"
                    size="sm"
                    mr={2}
                    onClick={(e) => { e.stopPropagation(); onUploadModalOpen(); }}
                  >
                    Dodaj pliki
                  </Button>
                  <AccordionIcon />
                </AccordionButton>
                <AccordionPanel pb={4}>
                  {myFiles.length === 0 ? (
                    <Text color="gray.500" fontSize="sm">Nie masz jeszcze żadnych plików w tym projekcie</Text>
                  ) : (
                    <Accordion allowMultiple>
                      {Object.entries(groupFilesByPackage(myFiles)).map(([packageName, files]) => {
                        const fileList = files as any[];
                        return (
                        <AccordionItem key={packageName} borderWidth="1px" borderColor={borderColor} rounded="md" mb={2}>
                          <AccordionButton py={3} _hover={{ bg: hoverBg }}>
                            <HStack flex="1" spacing={2}>
                              <Text fontWeight="bold">📦 {packageName}</Text>
                              <Badge colorScheme="blue" fontSize="xs">{fileList.length}</Badge>
                            </HStack>
                            <AccordionIcon />
                          </AccordionButton>
                          <AccordionPanel pb={3}>
                          <Table size="sm" variant="simple">
                            <Thead>
                              <Tr>
                                <Th>Nazwa pliku</Th>
                                <Th display={{ base: "none", md: "table-cell" }}>Rozmiar</Th>
                                <Th>Akcje</Th>
                              </Tr>
                            </Thead>
                            <Tbody>
                              {fileList.map((file: any) => (
                                <>
                                <Tr key={file.id}>
                                  <Td>
                                    <HStack spacing={2}>
                                      <Text fontSize="sm" fontWeight="medium">{file.displayName}</Text>
                                      {file.currentVersion?.versionNumber && (
                                        <Badge colorScheme="purple" fontSize="xs">v{file.currentVersion.versionNumber}</Badge>
                                      )}
                                    </HStack>
                                  </Td>
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
                                      {file.versions && file.versions.length > 0 && (
                                        <Button
                                          size="sm"
                                          variant="ghost"
                                          onClick={() => toggleFileVersions(file.id)}
                                          rightIcon={expandedFileIds.has(file.id) ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                                        >
                                          Wersje ({file.totalVersions})
                                        </Button>
                                      )}
                                    </HStack>
                                  </Td>
                                </Tr>
                                {/* Rozwinięte wersje i komentarze */}
                                {expandedFileIds.has(file.id) && file.versions && file.versions.length > 0 && (
                                  <Tr key={`${file.id}-versions`}>
                                    <Td colSpan={3} p={0}>
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
                                                
                                                {/* Komentarze do wersji */}
                                                {version.comments && version.comments.length > 0 && (
                                                  <Box mt={3}>
                                                    <HStack spacing={1} mb={3}>
                                                      <MessageSquare size={14} />
                                                      <Text fontSize="sm" fontWeight="semibold">
                                                        Komentarze ({version.comments.length})
                                                      </Text>
                                                    </HStack>
                                                    <VStack align="stretch" spacing={3}>
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
                                                    
                                                    {/* Dodaj nowy komentarz */}
                                                    <HStack mt={3} spacing={2}>
                                                      <Textarea
                                                        placeholder="Dodaj komentarz..."
                                                        size="sm"
                                                        value={newComments.get(`${file.id}-${version.id}`) || ""}
                                                        onChange={(e) => {
                                                          setNewComments((prev) => {
                                                            const updated = new Map(prev);
                                                            updated.set(`${file.id}-${version.id}`, e.target.value);
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
                                                        onClick={() => handleAddComment(file.id, version.id)}
                                                        isLoading={submittingComment === `${file.id}-${version.id}`}
                                                        isDisabled={!newComments.get(`${file.id}-${version.id}`)?.trim()}
                                                      />
                                                    </HStack>
                                                  </Box>
                                                )}
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
                  )}
                </AccordionPanel>
              </AccordionItem>

              {/* ====================== SEKCJA: PLIKI UDOSTĘPNIONE ======================= */}
              <AccordionItem bg={cardBg} border="1px" borderColor={borderColor} borderRadius="md" mb={4}>
                <AccordionButton py={4}>
                  <HStack flex="1" spacing={3}>
                    <Icon as={Share2} boxSize={6} color="teal.600" />
                    <Heading size="md">Pliki udostępnione</Heading>
                    <Badge colorScheme="teal" fontSize="sm">{sharedFiles.length}</Badge>
                  </HStack>
                  <AccordionIcon />
                </AccordionButton>
                <AccordionPanel pb={4}>
                  {sharedFiles.length === 0 ? (
                    <Text color="gray.500" fontSize="sm">Brak udostępnionych plików</Text>
                  ) : (
                    <Accordion allowMultiple>
                      {Object.entries(groupFilesByPackage(sharedFiles)).map(([packageName, files]) => {
                        const fileList = files as any[];
                        return (
                        <AccordionItem key={packageName} borderWidth="1px" borderColor={borderColor} rounded="md" mb={2}>
                          <AccordionButton py={3} _hover={{ bg: hoverBg }}>
                            <HStack flex="1" spacing={2}>
                              <Text fontWeight="bold">📦 {packageName}</Text>
                              <Badge colorScheme="blue" fontSize="xs">{fileList.length}</Badge>
                            </HStack>
                            <AccordionIcon />
                          </AccordionButton>
                          <AccordionPanel pb={3}>
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
                              {fileList.map((file: any) => (
                                <>
                                <Tr key={file.id}>
                                  <Td>
                                    <HStack spacing={2}>
                                      <Text fontSize="sm" fontWeight="medium">{file.displayName}</Text>
                                      {file.currentVersion?.versionNumber && (
                                        <Badge colorScheme="purple" fontSize="xs">v{file.currentVersion.versionNumber}</Badge>
                                      )}
                                    </HStack>
                                  </Td>
                                  <Td display={{ base: "none", md: "table-cell" }} fontSize="sm">
                                    {file.ownerName || "-"}
                                  </Td>
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
                                      {file.versions && file.versions.length > 0 && (
                                        <Button
                                          size="sm"
                                          variant="ghost"
                                          onClick={() => toggleFileVersions(file.id)}
                                          rightIcon={expandedFileIds.has(file.id) ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                                        >
                                          Wersje ({file.totalVersions})
                                        </Button>
                                      )}
                                    </HStack>
                                  </Td>
                                </Tr>
                                {/* Rozwinięte wersje i komentarze */}
                                {expandedFileIds.has(file.id) && file.versions && file.versions.length > 0 && (
                                  <Tr key={`${file.id}-versions`}>
                                    <Td colSpan={4} p={0}>
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
                                                
                                                {/* Komentarze do wersji */}
                                                {version.comments && version.comments.length > 0 && (
                                                  <Box mt={3}>
                                                    <HStack spacing={1} mb={3}>
                                                      <MessageSquare size={14} />
                                                      <Text fontSize="sm" fontWeight="semibold">
                                                        Komentarze ({version.comments.length})
                                                      </Text>
                                                    </HStack>
                                                    <VStack align="stretch" spacing={3}>
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
                                                    
                                                    {/* Dodaj nowy komentarz */}
                                                    <HStack mt={3} spacing={2}>
                                                      <Textarea
                                                        placeholder="Dodaj komentarz..."
                                                        size="sm"
                                                        value={newComments.get(`${file.id}-${version.id}`) || ""}
                                                        onChange={(e) => {
                                                          setNewComments((prev) => {
                                                            const updated = new Map(prev);
                                                            updated.set(`${file.id}-${version.id}`, e.target.value);
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
                                                        onClick={() => handleAddComment(file.id, version.id)}
                                                        isLoading={submittingComment === `${file.id}-${version.id}`}
                                                        isDisabled={!newComments.get(`${file.id}-${version.id}`)?.trim()}
                                                      />
                                                    </HStack>
                                                  </Box>
                                                )}
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
                  )}
                </AccordionPanel>
              </AccordionItem>
            </Accordion>
          </VStack>
        )}

        {/* MODAL ADD MEMBER */}
        {project && (
          <AddProjectMemberModal
            isOpen={isOpen}
            onClose={onClose}
            tenantId={project.tenantId}
            projectId={project.id}
            projectName={project.name}
            isAdmin={isProjectAdmin}
            onMemberAdded={() => {
              fetchMembers();
              fetchProjectDetails();
            }}
          />
        )}

        {/* Modal potwierdzenia usunięcia członka */}
        <Modal isOpen={isRemoveModalOpen} onClose={onRemoveModalClose} isCentered>
          <ModalOverlay />
          <ModalContent>
            <ModalHeader>Potwierdź usunięcie</ModalHeader>
            <ModalCloseButton />
            <ModalBody>
              <VStack align="flex-start" spacing={3}>
                <Text>
                  Czy na pewno chcesz usunąć <Text as="span" fontWeight="bold">{memberToRemove?.name}</Text> z projektu?
                </Text>
                <Text fontSize="sm" color="gray.500">
                  Ta operacja jest nieodwracalna. Użytkownik straci dostęp do wszystkich zasobów projektu.
                </Text>
              </VStack>
            </ModalBody>
            <ModalFooter>
              <Button 
                variant="ghost" 
                mr={3} 
                onClick={onRemoveModalClose}
                isDisabled={removingMember !== null}
              >
                Anuluj
              </Button>
              <Button 
                colorScheme="red" 
                onClick={handleConfirmRemoveMember}
                isLoading={removingMember !== null}
                loadingText="Usuwanie..."
              >
                Usuń członka
              </Button>
            </ModalFooter>
          </ModalContent>
        </Modal>

        {/* Modal uploadu plików */}
        {project && (
          <UploadFilesModal
            isOpen={isUploadModalOpen}
            onClose={onUploadModalClose}
            tenantId={project.tenantId}
            projectId={project.id}
            projectName={project.name}
            onFilesUploaded={() => {
              fetchMyFiles();
              fetchSharedFiles();
              toast({
                title: "Pliki przesłane",
                status: "success",
                duration: 2000,
              });
            }}
          />
        )}

        {/* Modal uploadu nowej wersji pliku */}
        {project && fileForNewVersion && (
          <UploadNewVersionModal
            isOpen={isUploadVersionModalOpen}
            onClose={onUploadVersionModalClose}
            tenantId={project.tenantId}
            projectId={project.id}
            file={fileForNewVersion}
            onVersionUploaded={handleVersionUploaded}
          />
        )}

        {/* Modal tworzenia harmonogramu */}
        {project && (
          <CreateWorkScheduleModal
            isOpen={isWorkScheduleModalOpen}
            onClose={onWorkScheduleModalClose}
            tenantId={project.tenantId}
            projectId={project.id}
            projectName={project.name}
            members={members}
            onScheduleCreated={() => {
              fetchWorkSchedules();
              toast({
                title: "Sukces",
                description: "Harmonogram został utworzony",
                status: "success",
                duration: 3000,
              });
            }}
          />
        )}

      </Box>
    </MainLayout>
  );
}
