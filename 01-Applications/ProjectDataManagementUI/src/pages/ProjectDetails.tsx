import { useEffect, useState, useRef } from "react";
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
  SimpleGrid,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  FormControl,
  FormLabel,
  Input,
  Tooltip,
} from "@chakra-ui/react";
import { FolderKanban, User, Calendar, ArrowLeft, Users, FileText, DollarSign, Power, Edit2, Save, X, TrendingUp, Settings } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import AddProjectMemberModal from "../components/AddProjectMemberModal";
import { handleApiError } from "../utils/handleApiError";
import UploadFilesModal from "../components/UploadFilesModal";
import UploadNewVersionModal from "../components/UploadNewVersionModal";
import WorkScheduleFormModal from "../components/WorkScheduleFormModal";
import ShareCostModal from "../components/ShareCostModal";
import { ManageFileShareModal } from "../components/ManageFileShareModal";
import ShareFilesModal from "../components/ShareFilesModal";
import { projectApi, ResourceScope } from "../api/projectApi";
import { tenantApi } from "../api/tenantApi";
import { useAuth } from "../context/AuthContext";
import { useProjectPermissions } from "../hooks/useProjectPermissions";
import { useProjectDetails, useProjectMembers, projectKeys } from '../hooks/queries';
import { useQueryClient } from '@tanstack/react-query';
import type { ProjectDetailsWeb } from "../types/project.types";
import { getRoleName, getRoleColor } from "../constants/roleCodes";
import { DeleteAlertDialog } from "../components/ui";
import { useToastNotification } from "../hooks/useToastNotification";
import type { WorkScheduleSummaryWeb } from "../types/workSchedule.types";
import type { ProjectCostListItemWeb, SharedProjectCostWeb, ProjectFilePackageWeb } from "../types/project.types";

export default function ProjectDetails() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const permissions = useProjectPermissions(projectId);
  const { isOpen, onClose } = useDisclosure();
  const { isOpen: isRemoveModalOpen, onOpen: onRemoveModalOpen, onClose: onRemoveModalClose } = useDisclosure();
  const { isOpen: isUploadModalOpen, onClose: onUploadModalClose } = useDisclosure();
  const { isOpen: isUploadVersionModalOpen, onOpen: onUploadVersionModalOpen, onClose: onUploadVersionModalClose } = useDisclosure();
  const { isOpen: isWorkScheduleModalOpen, onClose: onWorkScheduleModalClose } = useDisclosure();
  const { showSuccess, showError, showWarning, showApiSuccess } = useToastNotification();

  const {
    data: projectData,
    isLoading: isLoadingProject,
    error: projectError,
    refetch: refetchProject,
  } = useProjectDetails(user?.activeTenantId ?? undefined, projectId);

  const {
    data: membersData,
    isLoading: isLoadingMembers,
    refetch: refetchMembers,
  } = useProjectMembers(user?.activeTenantId ?? undefined, projectId);

  const queryClient = useQueryClient();

  const project = projectData ?? null;
  const members = membersData ?? [];
  const loading = isLoadingProject;
  const [removingMember, setRemovingMember] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{ userId: string; name: string } | null>(null);
  const [togglingStatus, setTogglingStatus] = useState(false);
  const [isEditingName, setIsEditingName] = useState(false);
  const [editedName, setEditedName] = useState("");
  const [updatingName, setUpdatingName] = useState(false);
  const [, setMyFiles] = useState<ProjectFilePackageWeb[]>([]);
  const [, setSharedFiles] = useState<ProjectFilePackageWeb[]>([]);
  const [, setExpandedFileIds] = useState<Set<string>>(new Set());
  const [fileForNewVersion, setFileForNewVersion] = useState<any | null>(null);
  const [newComments, setNewComments] = useState<Map<string, string>>(new Map());
  const [, setWorkSchedules] = useState<WorkScheduleSummaryWeb[]>([]);
  const [, setLoadingWorkSchedules] = useState(false);
  const [, setProjectCosts] = useState<ProjectCostListItemWeb[]>([]);
  const [, setLoadingCosts] = useState(false);
  const [, setSharedCosts] = useState<SharedProjectCostWeb[]>([]);
  const [, setLoadingSharedCosts] = useState(false);
  const [costToShare, setCostToShare] = useState<ProjectCostListItemWeb | null>(null);
  const { isOpen: isShareCostModalOpen, onOpen: onShareCostModalOpen, onClose: onShareCostModalClose } = useDisclosure();
  const { isOpen: isManageShareModalOpen, onOpen: onManageShareModalOpen, onClose: onManageShareModalClose } = useDisclosure();
  const { isOpen: isShareFilesModalOpen, onClose: onShareFilesModalClose } = useDisclosure();
  const { isOpen: isToggleStatusOpen, onOpen: onToggleStatusOpen, onClose: onToggleStatusClose } = useDisclosure();
  const cancelRefToggle = useRef<HTMLButtonElement>(null);
  const [fileToManageShare, setFileToManageShare] = useState<any | null>(null);
  const [editingCostId, setEditingCostId] = useState<string | null>(null);
  const [editingCostData, setEditingCostData] = useState<any>(null);
  const [, setSavingCost] = useState(false);
  const [, setDeletingCostId] = useState<string | null>(null);
  const [newCostData, setNewCostData] = useState<any>({
    name: '',
    place: '',
    date: new Date().toISOString().split('T')[0],
    description: '',
    netAmount: '',
    vatRate: '',
    grossAmount: '',
  });
  const [, setAddingNewCost] = useState(false);
  const [, setShowNewCostRow] = useState(false);
  const [documentFile, setDocumentFile] = useState<File | null>(null);
  const [, setSubmittingComment] = useState<string | null>(null);

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const labelColor = useColorModeValue("gray.700", "gray.300");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  useEffect(() => {
    if (projectData?.name) {
      setEditedName(projectData.name);
    }
  }, [projectData?.name]);

  const fetchProjectDetails = () => refetchProject();
  const fetchMembers = () => refetchMembers();

  const fetchMyFiles = async () => {
    if (!user?.activeTenantId || !projectId) return;

    try {
      const response = await projectApi.getProjectFilePackages(user.activeTenantId, projectId, ResourceScope.Mine);
      setMyFiles(response.data);
    } catch (err) {
    }
  };

  const fetchSharedFiles = async () => {
    if (!user?.activeTenantId || !projectId) return;

    try {
      const response = await projectApi.getProjectFilePackages(user.activeTenantId, projectId, ResourceScope.Shared);
      setSharedFiles(response.data);
    } catch (err) {
    }
  };

  const fetchWorkSchedules = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoadingWorkSchedules(true);
    try {
      const response = await projectApi.getMyWorkSchedules(user.activeTenantId, projectId);
      setWorkSchedules(response.data);
    } catch (err) {
    } finally {
      setLoadingWorkSchedules(false);
    }
  };

  const fetchProjectCosts = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoadingCosts(true);
    try {
      const response = await projectApi.getProjectUserCosts(user.activeTenantId, projectId);
      setProjectCosts(response.data);
    } catch (err) {
      showError("Błąd", "Nie udało się pobrać kosztów projektowych");
    } finally {
      setLoadingCosts(false);
    }
  };

  const fetchSharedProjectCosts = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoadingSharedCosts(true);
    try {
      const response = await projectApi.getSharedProjectCosts(user.activeTenantId, projectId);
      setSharedCosts(response.data);
    } catch (err) {
      showError("Błąd", "Nie udało się pobrać udostępnionych kosztów");
    } finally {
      setLoadingSharedCosts(false);
    }
  };

  const _handleAddCost = async () => {
    if (!user?.activeTenantId || !projectId) return;

    if (!newCostData.name.trim()) {
      showError("Błąd", "Nazwa kosztu jest wymagana");
      return;
    }

    // Walidacja daty
    const selectedDate = new Date(newCostData.date);
    selectedDate.setHours(0, 0, 0, 0);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (selectedDate.getTime() > today.getTime()) {
      showError("Błąd", "Data nie może być w przyszłości");
      return;
    }

    // Walidacja: albo netto+VAT albo gross
    const hasNet = newCostData.netAmount && newCostData.vatRate;
    const hasGross = newCostData.grossAmount;

    if (!hasNet && !hasGross) {
      showError("Błąd", "Podaj kwotę netto i stawkę VAT lub kwotę brutto");
      return;
    }

    setAddingNewCost(true);
    try {
      await projectApi.createProjectCost(
        user.activeTenantId,
        projectId,
        {
          name: newCostData.name,
          place: newCostData.place || undefined,
          date: new Date(newCostData.date),
          description: newCostData.description || undefined,
          netAmount: newCostData.netAmount ? parseFloat(newCostData.netAmount) : undefined,
          vatRate: newCostData.vatRate ? parseFloat(newCostData.vatRate) : undefined,
          grossAmount: newCostData.grossAmount ? parseFloat(newCostData.grossAmount) : undefined,
          document: documentFile || undefined,
        }
      );

      showApiSuccess('costAdded');

      // Reset formularza
      setNewCostData({
        name: '',
        place: '',
        date: new Date().toISOString().split('T')[0],
        description: '',
        netAmount: '',
        vatRate: '',
        grossAmount: '',
      });
      setDocumentFile(null);
      setShowNewCostRow(false);

      await fetchProjectCosts();
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setAddingNewCost(false);
    }
  };

  const _handleEditCost = (cost: ProjectCostListItemWeb) => {
    setEditingCostId(cost.id);
    setEditingCostData({
      name: cost.name,
      place: cost.place || '',
      date: new Date(cost.date).toISOString().split('T')[0],
      description: cost.description || '',
      netAmount: cost.netAmount?.toString() || '',
      grossAmount: cost.grossAmount.toString(),
      isAccepted: cost.isAccepted,
      removeDocument: false,
    });
    setDocumentFile(null);
  };

  const _handleSaveCost = async () => {
    if (!user?.activeTenantId || !projectId || !editingCostId) return;

    if (!editingCostData.name.trim()) {
      showError("Błąd", "Nazwa kosztu jest wymagana");
      return;
    }

    // Walidacja daty
    const selectedDate = new Date(editingCostData.date);
    selectedDate.setHours(0, 0, 0, 0);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (selectedDate.getTime() > today.getTime()) {
      showError("Błąd", "Data nie może być w przyszłości");
      return;
    }

    // Walidacja: albo netto+VAT albo gross
    const hasNet = editingCostData.netAmount && editingCostData.vatRate;
    const hasGross = editingCostData.grossAmount;

    if (!hasNet && !hasGross) {
      showError("Błąd", "Podaj kwotę netto i stawkę VAT lub kwotę brutto");
      return;
    }

    setSavingCost(true);
    try {
      const response = await projectApi.updateProjectCost(
        user.activeTenantId,
        projectId,
        editingCostId,
        {
          name: editingCostData.name,
          place: editingCostData.place || undefined,
          date: new Date(editingCostData.date),
          description: editingCostData.description || undefined,
          netAmount: editingCostData.netAmount ? parseFloat(editingCostData.netAmount) : undefined,
          grossAmount: editingCostData.grossAmount ? parseFloat(editingCostData.grossAmount) : undefined,
          isAccepted: editingCostData.isAccepted ?? false,
          document: documentFile || undefined,
          removeDocument: editingCostData.removeDocument,
        }
      );

      showApiSuccess('costUpdated');

      setEditingCostId(null);
      setEditingCostData(null);
      setDocumentFile(null);
      await fetchProjectCosts();
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError("Błąd", "Nie udało się zaktualizować kosztu");
    } finally {
      setSavingCost(false);
    }
  };

  const _handleCancelEdit = () => {
    setEditingCostId(null);
    setEditingCostData(null);
    setDocumentFile(null);
  };

  const _handleShareCost = (cost: ProjectCostListItemWeb) => {
    setCostToShare(cost);
    onShareCostModalOpen();
  };

  const _handleDeleteCost = async (costId: string) => {
    if (!user?.activeTenantId || !projectId) return;

    if (!confirm("Czy na pewno chcesz usunąć ten koszt?")) return;

    setDeletingCostId(costId);
    try {
      const response = await projectApi.deleteProjectCost(user.activeTenantId, projectId, costId);

      await projectApi.deleteProjectCost(user.activeTenantId, projectId, costId);

      showApiSuccess('costDeleted');

      await fetchProjectCosts();
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError("Błąd", "Nie udało się usunąć kosztu");
    } finally {
      setDeletingCostId(null);
    }
  };

  const _formatFileSize = (bytes: number): string => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + " " + sizes[i];
  };

  const _handleDownloadFile = (sasUrl: string, fileName: string) => {
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
        showError("Błąd", "Nie udało się pobrać pliku");
      });
  };

  const _toggleFileVersions = (fileId: string) => {
    setExpandedFileIds((prev: Set<string>) => {
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

  const _handlePreview = (sasUrlView: string) => {
    window.open(sasUrlView, '_blank', 'noopener,noreferrer');
  };

  const _isPreviewSupported = (contentType: string): boolean => {
    return (
      contentType === "application/pdf" ||
      contentType.startsWith("image/")
    );
  };

  const _openUploadVersionModal = (file: any) => {
    setFileForNewVersion(file);
    onUploadVersionModalOpen();
  };

  const handleVersionUploaded = () => {
    fetchMyFiles();
    fetchSharedFiles();
    onUploadVersionModalClose();
  };

  const _openManageShareModal = (file: any) => {
    setFileToManageShare(file);
    onManageShareModalOpen();
  };

  const handleShareUpdated = () => {
    fetchMyFiles();
    fetchSharedFiles();
    onManageShareModalClose();
  };

  const _handleAddComment = async (fileId: string, versionId: string) => {
    if (!user?.activeTenantId || !projectId) return;

    const commentKey = `${fileId}-${versionId}`;
    const comment = newComments.get(commentKey);

    if (!comment || comment.trim() === "") {
      showWarning("Uwaga", "Komentarz nie może być pusty");
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

      showApiSuccess('commentAdded');

      // Wyczyść pole komentarza
      setNewComments((prev) => {
        const updated = new Map(prev);
        updated.delete(commentKey);
        return updated;
      });

      // Odśwież listę plików
      await fetchMyFiles();
      await fetchSharedFiles();
    } catch (error) {
      showError("Błąd", "Nie udało się dodać komentarza");
    } finally {
      setSubmittingComment(null);
    }
  };

  const _handleRemoveMemberClick = (userId: string, memberName: string) => {
    setMemberToRemove({ userId, name: memberName });
    onRemoveModalOpen();
  };

  const handleConfirmRemoveMember = async () => {
    if (!user?.activeTenantId || !projectId || !memberToRemove) return;

    setRemovingMember(memberToRemove.userId);
    try {
      await projectApi.removeProjectMember(user.activeTenantId, projectId, memberToRemove.userId);

      showApiSuccess('memberRemoved');

      queryClient.invalidateQueries({
        queryKey: projectKeys.members(user.activeTenantId!, projectId!)
      });
      queryClient.invalidateQueries({
        queryKey: projectKeys.detail(user.activeTenantId!, projectId!)
      });

      // Odśwież listę
      await fetchProjectDetails();
      await fetchMembers();
    } catch (error) {
      const { title, description } = handleApiError(error);
    } finally {
      setRemovingMember(null);
      setMemberToRemove(null);
      onRemoveModalClose();
    }
  };

  const handleToggleProjectStatus = async () => {
    if (!project || !user?.activeTenantId || !projectId) return;

    const newStatus = !project.isActive;
    setTogglingStatus(true);

    try {
      await projectApi.toggleProjectStatus(user.activeTenantId, projectId, newStatus);

      showApiSuccess(newStatus ? 'activated' : 'deactivated');

      onToggleStatusClose();

      queryClient.invalidateQueries({
        queryKey: projectKeys.detail(user.activeTenantId!, projectId!)
      });

      // Odśwież dane projektu
      await fetchProjectDetails();
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setTogglingStatus(false);
    }
  };

  const handleUpdateName = async () => {
    if (!editedName.trim()) {
      showError("Błąd walidacji", "Nazwa projektu nie może być pusta");
      return;
    }

    if (!user?.activeTenantId || !projectId) return;

    setUpdatingName(true);
    try {
      await projectApi.updateProject(user.activeTenantId, projectId, { Name: editedName });

      showApiSuccess('nameUpdated');

      setIsEditingName(false);
      queryClient.invalidateQueries({
        queryKey: projectKeys.detail(user.activeTenantId!, projectId!)
      });
      queryClient.invalidateQueries({
        queryKey: projectKeys.list(user.activeTenantId!)
      });
      await fetchProjectDetails();
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setUpdatingName(false);
    }
  };

  return (
    <MainLayout>
      <Box bg={pageBg} minH="100vh" p={{ base: 3, sm: 4, md: 6 }}>
        <VStack spacing={6} maxW="1200px" mx="auto" align="stretch">
          {/* Header */}
          {loading ? (
            <VStack spacing={4} align="center" justify="center" minH="50vh">
              <Spinner size="xl" color="primary.500" />
              <Text>Ładowanie szczegółów projektu...</Text>
            </VStack>
          ) : projectError ? (
            <Alert status="error" rounded="md">
              <AlertIcon />
              {projectError.message ?? "Błąd podczas pobierania szczegółów projektu"}
            </Alert>
          ) : !project ? (
            <VStack spacing={4} align="center" justify="center" minH="50vh">
              <Text>Nie znaleziono projektu</Text>
              <Button onClick={() => navigate("/projects")}>Powrót do listy projektów</Button>
            </VStack>
          ) : (
            <>
              {/* Informacje podstawowe */}
              <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
                <VStack align="stretch" spacing={4}>
                  <HStack justify="space-between" flexWrap={{ base: "wrap", md: "nowrap" }} gap={{ base: 2, md: 2 }}>
                    <HStack spacing={3} minW={0}>
                      <FolderKanban size={32} />
                      <Heading size="lg" noOfLines={1}>Szczegóły projektu</Heading>
                    </HStack>
                    <HStack spacing={{ base: 1, md: 2 }} flexWrap="wrap" justifyContent={{ base: "flex-start", md: "flex-end" }}>
                      {!isEditingName && (
                        <>
                          {permissions.canManageStatus && (
                            <Tooltip label={project.isActive ? "Dezaktywuj projekt" : "Aktywuj projekt"}>
                              <Button
                                size={{ base: "xs", md: "sm" }}
                                variant="ghost"
                                leftIcon={<Power size={16} />}
                                colorScheme={project.isActive ? "red" : "green"}
                                onClick={onToggleStatusOpen}
                                fontSize={{ base: "xs", md: "sm" }}
                              >
                                {project.isActive ? "Dezaktywuj" : "Aktywuj"}
                              </Button>
                            </Tooltip>
                          )}
                          {permissions.canEdit && (
                            <Button
                              size={{ base: "xs", md: "sm" }}
                              variant="ghost"
                              leftIcon={<Edit2 size={16} />}
                              onClick={() => setIsEditingName(true)}
                              fontSize={{ base: "xs", md: "sm" }}
                            >
                              Edytuj
                            </Button>
                          )}
                        </>
                      )}
                    </HStack>
                  </HStack>

                  {isEditingName ? (
                    <VStack spacing={3} align="stretch">
                      <FormControl>
                        <FormLabel color={labelColor}>Nazwa projektu</FormLabel>
                        <Input
                          value={editedName}
                          onChange={(e) => setEditedName(e.target.value)}
                          onKeyPress={(e) => {
                            if (e.key === "Enter" && !updatingName) {
                              handleUpdateName();
                            }
                          }}
                        />
                      </FormControl>
                      <HStack spacing={2}>
                        <Button
                          size="sm"
                          colorScheme="primary"
                          leftIcon={<Save size={16} />}
                          onClick={handleUpdateName}
                          isLoading={updatingName}
                          flex={1}
                        >
                          Zapisz
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          leftIcon={<X size={16} />}
                          onClick={() => {
                            setIsEditingName(false);
                            setEditedName(project.name);
                          }}
                          isDisabled={updatingName}
                          flex={1}
                        >
                          Anuluj
                        </Button>
                      </HStack>
                    </VStack>
                  ) : (
                    <VStack align="flex-start" spacing={2}>
                      <HStack>
                        <Text fontSize="2xl" fontWeight="bold">
                          {project.name}
                        </Text>
                        <Badge colorScheme={project.isActive ? "green" : "gray"}>
                          {project.isActive ? "Aktywny" : "Nieaktywny"}
                        </Badge>
                      </HStack>
                      <Text fontSize="sm" color="neutral.500">
                        Utworzono: {formatDate(project.createdAt)}
                      </Text>
                      <Badge colorScheme={getRoleColor(project.userRoleCode)}>
                        {getRoleName(project.userRoleCode)}
                      </Badge>
                    </VStack>
                  )}
                </VStack>
              </Box>

              {/* ====================== SZYBKI DOSTĘP ======================= */}
              <SimpleGrid columns={{ base: 1, md: 2, lg: 4 }} spacing={4}>
                {(permissions.canViewMembers || permissions.canManageMembers) && (
                  <Box
                    as="button"
                    bg={cardBg}
                    p={6}
                    rounded="lg"
                    borderWidth="1px"
                    borderColor={borderColor}
                    shadow="sm"
                    _hover={{ bg: hoverBg, transform: "translateY(-2px)", shadow: "md" }}
                    transition="all 0.2s"
                    onClick={() => navigate(`/projects/${projectId}/members`)}
                  >
                    <VStack spacing={3}>
                      <Icon as={Users} boxSize={8} color="primary.600" />
                      <Text fontWeight="bold" fontSize="md">Członkowie</Text>
                    </VStack>
                  </Box>
                )}

                {(permissions.canWriteResources || permissions.canReadAllResources || permissions.canWriteAllResources) && (
                  <Box
                    as="button"
                    bg={cardBg}
                    p={6}
                    rounded="lg"
                    borderWidth="1px"
                    borderColor={borderColor}
                    shadow="sm"
                    _hover={{ bg: hoverBg, transform: "translateY(-2px)", shadow: "md" }}
                    transition="all 0.2s"
                    onClick={() => navigate(`/projects/${projectId}/schedules`)}
                  >
                    <VStack spacing={3}>
                      <Icon as={Calendar} boxSize={8} color="level2.600" />
                      <Text fontWeight="bold" fontSize="md">Harmonogramy</Text>
                    </VStack>
                  </Box>
                )}

                {permissions.hasAnyResourceAccess && (
                  <Box
                    as="button"
                    bg={cardBg}
                    p={6}
                    rounded="lg"
                    borderWidth="1px"
                    borderColor={borderColor}
                    shadow="sm"
                    _hover={{ bg: hoverBg, transform: "translateY(-2px)", shadow: "md" }}
                    transition="all 0.2s"
                    onClick={() => navigate(`/projects/${projectId}/files`)}
                  >
                    <VStack spacing={3}>
                      <Icon as={FileText} boxSize={8} color="level2.600" />
                      <Text fontWeight="bold" fontSize="md">Pliki</Text>
                    </VStack>
                  </Box>
                )}

                {permissions.hasAnyResourceAccess && (
                  <Box
                    as="button"
                    bg={cardBg}
                    p={6}
                    rounded="lg"
                    borderWidth="1px"
                    borderColor={borderColor}
                    shadow="sm"
                    _hover={{ bg: hoverBg, transform: "translateY(-2px)", shadow: "md" }}
                    transition="all 0.2s"
                    onClick={() => navigate(`/projects/${projectId}/costs`)}
                  >
                    <VStack spacing={3}>
                      <Icon as={DollarSign} boxSize={8} color="red.600" />
                      <Text fontWeight="bold" fontSize="md">Wydatki</Text>
                    </VStack>
                  </Box>
                )}

                {(permissions.canReadResources || permissions.canWriteResources || permissions.canReadAllResources || permissions.canWriteAllResources || permissions.canReadSharedResources) && (
                  <Box
                    as="button"
                    bg={cardBg}
                    p={6}
                    rounded="lg"
                    borderWidth="1px"
                    borderColor={borderColor}
                    shadow="sm"
                    _hover={{ bg: hoverBg, transform: "translateY(-2px)", shadow: "md" }}
                    transition="all 0.2s"
                    onClick={() => navigate(`/projects/${projectId}/cost-estimates`)}
                  >
                    <VStack spacing={3}>
                      <Icon as={FileText} boxSize={8} color="orange.600" />
                      <Text fontWeight="bold" fontSize="md">Kosztorysy</Text>
                    </VStack>
                  </Box>
                )}

                {permissions.hasAnyResourceAccess && (
                  <Box
                    as="button"
                    bg={cardBg}
                    p={6}
                    rounded="lg"
                    borderWidth="1px"
                    borderColor={borderColor}
                    shadow="sm"
                    _hover={{ bg: hoverBg, transform: "translateY(-2px)", shadow: "md" }}
                    transition="all 0.2s"
                    onClick={() => navigate(`/projects/${projectId}/dashboard`)}
                  >
                    <VStack spacing={3}>
                      <Icon as={TrendingUp} boxSize={8} color="green.600" />
                      <Text fontWeight="bold" fontSize="md">Dashboard</Text>
                    </VStack>
                  </Box>
                )}

                {permissions.canView && (
                  <Box
                    as="button"
                    bg={cardBg}
                    p={6}
                    rounded="lg"
                    borderWidth="1px"
                    borderColor={borderColor}
                    shadow="sm"
                    _hover={{ bg: hoverBg, transform: "translateY(-2px)", shadow: "md" }}
                    transition="all 0.2s"
                    onClick={() => navigate(`/projects/${projectId}/parameters`)}
                  >
                    <VStack spacing={3}>
                      <Icon as={Settings} boxSize={8} color="action.600" />
                      <Text fontWeight="bold" fontSize="md">Parametry</Text>
                    </VStack>
                  </Box>
                )}
              </SimpleGrid>

              {/* Sekcje przeniesione do dedykowanych stron - dostępne przez karty powyżej */}
            </>
          )}
        </VStack>

        {/* Modals - wszystkie sekcje przeniesione na osobne strony */}

        {/* MODAL ADD MEMBER */}
        {project && (
          <AddProjectMemberModal
            isOpen={isOpen}
            onClose={onClose}
            tenantId={project.tenantId}
            projectId={project.id}
            projectName={project.name}
            isAdmin={permissions.canManageMembers}
            onMemberAdded={() => {
              fetchMembers();
              fetchProjectDetails();
            }}
          />
        )}

        {/* Modal udostępniania kosztu */}
        {isShareCostModalOpen && costToShare && user?.activeTenantId && projectId && (
          <ShareCostModal
            isOpen={isShareCostModalOpen}
            onClose={() => {
              onShareCostModalClose();
              setCostToShare(null);
            }}
            tenantId={user.activeTenantId}
            projectId={projectId}
            cost={costToShare}
            onCostShared={() => {
              fetchProjectCosts();
              fetchSharedProjectCosts();
            }}
          />
        )}


        {/* Dialog potwierdzenia usunięcia członka */}
        <DeleteAlertDialog
          isOpen={isRemoveModalOpen}
          onClose={onRemoveModalClose}
          onConfirm={handleConfirmRemoveMember}
          itemName={memberToRemove?.name}
          isLoading={removingMember !== null}
        />

        {/* Modal uploadu plików */}
        {project && isUploadModalOpen && (
          <UploadFilesModal
            isOpen={isUploadModalOpen}
            onClose={onUploadModalClose}
            tenantId={project.tenantId}
            projectId={project.id}
            projectName={project.name}
            onFilesUploaded={() => {
              fetchMyFiles();
              fetchSharedFiles();
              showSuccess("Pliki przesłane");
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
          <WorkScheduleFormModal
            mode="create"
            isOpen={isWorkScheduleModalOpen}
            onClose={onWorkScheduleModalClose}
            tenantId={project.tenantId}
            projectId={project.id}
            projectName={project.name}
            members={members}
            onSuccess={() => {
              fetchWorkSchedules();
              showSuccess("Sukces", "Harmonogram został utworzony");
            }}
          />
        )}

        {/* Modal zarządzania udostępnieniem pliku */}
        {project && fileToManageShare && user && (
          <ManageFileShareModal
            isOpen={isManageShareModalOpen}
            onClose={onManageShareModalClose}
            tenantId={project.tenantId}
            projectId={project.id}
            fileId={fileToManageShare.id}
            fileName={fileToManageShare.displayName}
            sharedWithUserIds={fileToManageShare.sharedWithUserIds || []}
            members={members}
            currentUserId={user.id || ''}
            onShareUpdated={handleShareUpdated}
          />
        )}

        {/* Modal grupowego udostępniania plików */}
        {project && (
          <ShareFilesModal
            isOpen={isShareFilesModalOpen}
            onClose={onShareFilesModalClose}
            tenantId={project.tenantId}
            projectId={project.id}
            onFilesShared={() => {
              fetchMyFiles();
              fetchSharedFiles();
            }}
          />
        )}

        {/* Dialog potwierdzenia zmiany statusu projektu */}
        <AlertDialog
          isOpen={isToggleStatusOpen}
          leastDestructiveRef={cancelRefToggle}
          onClose={onToggleStatusClose}
        >
          <AlertDialogOverlay>
            <AlertDialogContent maxW={{ base: "90vw", md: "600px" }} mx={{ base: 4, md: 0 }}>
              <AlertDialogHeader fontSize="lg" fontWeight="bold">
                {project?.isActive ? "Dezaktywuj projekt" : "Aktywuj projekt"}
              </AlertDialogHeader>

              <AlertDialogBody>
                <VStack align="flex-start" spacing={4}>
                  <Text>
                    Czy na pewno chcesz {project?.isActive ? "zdezaktywować" : "aktywować"} projekt <Text as="span" fontWeight="bold" color="primary.500">{project?.name}</Text>?
                  </Text>
                  {project?.isActive ? (
                    <Box
                      p={4}
                      bg={useColorModeValue("orange.50", "orange.900")}
                      borderRadius="md"
                      borderWidth="1px"
                      borderColor={useColorModeValue("orange.200", "orange.700")}
                      width="100%"
                    >
                      <VStack align="flex-start" spacing={3}>
                        <HStack spacing={2}>
                          <Icon as={Power} color="orange.500" />
                          <Text fontWeight="bold" color="orange.600" fontSize="sm">
                            ⚠️ Ważne informacje:
                          </Text>
                        </HStack>
                        <Text fontSize="sm">
                          • Zdezaktywowany projekt <Text as="span" fontWeight="bold">nie będzie widoczny</Text> na liście projektów
                        </Text>
                        <Text fontSize="sm">
                          • Wszystkie dane projektu zostaną zachowane
                        </Text>
                        <Text fontSize="sm">
                          • Możesz ponownie aktywować projekt w każdej chwili
                        </Text>
                        <Text fontSize="sm" fontWeight="medium" color="orange.700" mt={2}>
                          Operacja nie usuwa projektu, tylko ukrywa go przed użytkownikami.
                        </Text>
                      </VStack>
                    </Box>
                  ) : (
                    <Box
                      p={4}
                      bg={useColorModeValue("green.50", "green.900")}
                      borderRadius="md"
                      borderWidth="1px"
                      borderColor={useColorModeValue("green.200", "green.700")}
                      width="100%"
                    >
                      <VStack align="flex-start" spacing={3}>
                        <HStack spacing={2}>
                          <Icon as={Power} color="green.500" />
                          <Text fontWeight="bold" color="green.600" fontSize="sm">
                            ℹ️ Informacje:
                          </Text>
                        </HStack>
                        <Text fontSize="sm">
                          • Projekt stanie się <Text as="span" fontWeight="bold">widoczny</Text> na liście projektów
                        </Text>
                        <Text fontSize="sm">
                          • Wszyscy członkowie projektu będą mieli dostęp
                        </Text>
                        <Text fontSize="sm">
                          • Wszystkie dane projektu są zachowane
                        </Text>
                      </VStack>
                    </Box>
                  )}
                </VStack>
              </AlertDialogBody>

              <AlertDialogFooter>
                <Button
                  ref={cancelRefToggle}
                  onClick={onToggleStatusClose}
                  isDisabled={togglingStatus}
                >
                  Anuluj
                </Button>
                <Button
                  colorScheme={project?.isActive ? "red" : "green"}
                  onClick={handleToggleProjectStatus}
                  isLoading={togglingStatus}
                  loadingText={project?.isActive ? "Dezaktywuję..." : "Aktywuję..."}
                  ml={3}
                >
                  {project?.isActive ? "Dezaktywuj projekt" : "Aktywuj projekt"}
                </Button>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialogOverlay>
        </AlertDialog>

      </Box>
    </MainLayout>
  );
}
