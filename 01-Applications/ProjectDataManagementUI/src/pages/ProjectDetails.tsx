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
  useToast,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  SimpleGrid,
} from "@chakra-ui/react";
import { FolderKanban, User, Calendar, ArrowLeft, Users, FileText, DollarSign } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import AddProjectMemberModal from "../components/AddProjectMemberModal";
import { handleApiError } from "../utils/handleApiError";
import UploadFilesModal from "../components/UploadFilesModal";
import UploadNewVersionModal from "../components/UploadNewVersionModal";
import CreateWorkScheduleModal from "../components/CreateWorkScheduleModal";
import ShareCostModal from "../components/ShareCostModal";
import { ManageFileShareModal } from "../components/ManageFileShareModal";
import ShareFilesModal from "../components/ShareFilesModal";
import { projectApi } from "../api/projectApi";
import { tenantApi } from "../api/tenantApi";
import { AuthContext } from "../context/AuthContext";
import { useContext } from "react";
import { ProjectRole } from "../types/project.types";
import type { WorkScheduleSummaryWeb } from "../types/workSchedule.types";
import type { ProjectCostListItemWeb, SharedProjectCostWeb, ProjectFilePackageWeb, SharedProjectFilePackageWeb } from "../types/project.types";

/* Helpery UI */
const getProjectRoleName = (role: number) =>
  role === ProjectRole.Admin ? "Administrator" : "Członek";

const getProjectRoleColor = (role: number) =>
  role === ProjectRole.Admin ? "blue" : "green";

export default function ProjectDetails() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { isOpen, onClose } = useDisclosure();
  const { isOpen: isRemoveModalOpen, onOpen: onRemoveModalOpen, onClose: onRemoveModalClose } = useDisclosure();
  const { isOpen: isUploadModalOpen, onClose: onUploadModalClose } = useDisclosure();
  const { isOpen: isUploadVersionModalOpen, onOpen: onUploadVersionModalOpen, onClose: onUploadVersionModalClose } = useDisclosure();
  const { isOpen: isWorkScheduleModalOpen, onClose: onWorkScheduleModalClose } = useDisclosure();
  const toast = useToast();

  const [project, setProject] = useState<any | null>(null);
  const [members, setMembers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [, setLoadingMembers] = useState(false);
  const [removingMember, setRemovingMember] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{ userId: string; name: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [userTenantRole, setUserTenantRole] = useState<number | null>(null);
  const [, setMyFiles] = useState<ProjectFilePackageWeb[]>([]);
  const [, setSharedFiles] = useState<SharedProjectFilePackageWeb[]>([]);
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

      setProject(response.data);
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

      setMembers(response.data);
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
      setMyFiles(response.data);
    } catch (err) {
      console.error("Błąd pobierania moich plików:", err);
    }
  };

  const fetchSharedFiles = async () => {
    if (!user?.activeTenantId || !projectId) return;

    try {
      const response = await projectApi.getSharedFiles(user.activeTenantId, projectId);
      setSharedFiles(response.data);
    } catch (err) {
      console.error("Błąd pobierania udostępnionych plików:", err);
    }
  };

  const fetchWorkSchedules = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoadingWorkSchedules(true);
    try {
      const response = await projectApi.getMyWorkSchedules(user.activeTenantId, projectId);
      setWorkSchedules(response.data);
    } catch (err) {
      console.error("Błąd pobierania harmonogramów:", err);
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
      console.error("Błąd pobierania kosztów projektowych:", err);
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać kosztów projektowych",
        status: "error",
        duration: 3000,
      });
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
      console.error("Błąd pobierania udostępnionych kosztów:", err);
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać udostępnionych kosztów",
        status: "error",
        duration: 3000,
      });
    } finally {
      setLoadingSharedCosts(false);
    }
  };

  const _handleAddCost = async () => {
    if (!user?.activeTenantId || !projectId) return;

    if (!newCostData.name.trim()) {
      toast({
        title: "Błąd",
        description: "Nazwa kosztu jest wymagana",
        status: "error",
        duration: 3000,
      });
      return;
    }

    // Walidacja daty
    const selectedDate = new Date(newCostData.date);
    selectedDate.setHours(0, 0, 0, 0);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (selectedDate.getTime() > today.getTime()) {
      toast({
        title: "Błąd",
        description: "Data nie może być w przyszłości",
        status: "error",
        duration: 3000,
      });
      return;
    }

    // Walidacja: albo netto+VAT albo gross
    const hasNet = newCostData.netAmount && newCostData.vatRate;
    const hasGross = newCostData.grossAmount;

    if (!hasNet && !hasGross) {
      toast({
        title: "Błąd",
        description: "Podaj kwotę netto i stawkę VAT lub kwotę brutto",
        status: "error",
        duration: 3000,
      });
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

      toast({
        title: "Sukces",
        description: "Koszt został dodany",
        status: "success",
        duration: 3000,
      });

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
      console.error("Błąd podczas dodawania kosztu:", error);
      const { title, description } = handleApiError(error);
      toast({
        title,
        description,
        status: "error",
        duration: 5000,
      });
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
      vatRate: cost.vatRate?.toString() || '',
      grossAmount: cost.grossAmount.toString(),
      isClosed: cost.isClosed,
      removeDocument: false,
    });
    setDocumentFile(null);
  };

  const _handleSaveCost = async () => {
    if (!user?.activeTenantId || !projectId || !editingCostId) return;

    if (!editingCostData.name.trim()) {
      toast({
        title: "Błąd",
        description: "Nazwa kosztu jest wymagana",
        status: "error",
        duration: 3000,
      });
      return;
    }

    // Walidacja daty
    const selectedDate = new Date(editingCostData.date);
    selectedDate.setHours(0, 0, 0, 0);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (selectedDate.getTime() > today.getTime()) {
      toast({
        title: "Błąd",
        description: "Data nie może być w przyszłości",
        status: "error",
        duration: 3000,
      });
      return;
    }

    // Walidacja: albo netto+VAT albo gross
    const hasNet = editingCostData.netAmount && editingCostData.vatRate;
    const hasGross = editingCostData.grossAmount;

    if (!hasNet && !hasGross) {
      toast({
        title: "Błąd",
        description: "Podaj kwotę netto i stawkę VAT lub kwotę brutto",
        status: "error",
        duration: 3000,
      });
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
          vatRate: editingCostData.vatRate ? parseFloat(editingCostData.vatRate) : undefined,
          grossAmount: editingCostData.grossAmount ? parseFloat(editingCostData.grossAmount) : undefined,
          document: documentFile || undefined,
          removeDocument: editingCostData.removeDocument,
        }
      );

      toast({
        title: "Sukces",
        description: "Koszt został zaktualizowany",
        status: "success",
        duration: 3000,
      });

      setEditingCostId(null);
      setEditingCostData(null);
      setDocumentFile(null);
      await fetchProjectCosts();
    } catch (error) {
      console.error("Błąd podczas aktualizacji kosztu:", error);
      const { title, description } = handleApiError(error);
      toast({
        title: "Błąd",
        description: "Nie udało się zaktualizować kosztu",
        status: "error",
        duration: 5000,
      });
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

      toast({
        title: "Sukces",
        description: "Koszt został usunięty",
        status: "success",
        duration: 3000,
      });

      await fetchProjectCosts();
    } catch (error) {
      console.error("Błąd podczas usuwania kosztu:", error);
      const { title, description } = handleApiError(error);
      toast({
        title: "Błąd",
        description: "Nie udało się usunąć kosztu",
        status: "error",
        duration: 5000,
      });
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
        console.error("Błąd podczas pobierania pliku:", error);
        toast({
          title: "Błąd",
          description: "Nie udało się pobrać pliku",
          status: "error",
          duration: 3000,
        });
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
    
    // Pobierz rolę użytkownika w tenancie
    const fetchUserTenantRole = async () => {
      if (!user?.activeTenantId) {
        console.log("🔴 Brak activeTenantId");
        return;
      }
      
      try {
        const response = await tenantApi.getUserTenants();
        
        console.log("🔵 getUserTenants response:", response.status);
        
        const tenants = response.data;
        console.log("🔵 User tenants:", tenants);
        
        // Znajdź aktywny tenant i pobierz rolę
        const activeTenant = tenants.find((t: any) => t.id === user.activeTenantId);
        if (activeTenant) {
          console.log("🔵 Active tenant:", activeTenant);
          console.log("🔵 User role in tenant:", activeTenant.role);
          setUserTenantRole(activeTenant.role);
        }
      } catch (error) {
        console.error("❌ Błąd pobierania roli tenanta:", error);
      }
    };
    
    fetchUserTenantRole();
  }, [projectId, user?.activeTenantId]);

  const _handleRemoveMemberClick = (userId: string, memberName: string) => {
    setMemberToRemove({ userId, name: memberName });
    onRemoveModalOpen();
  };

  const handleConfirmRemoveMember = async () => {
    if (!user?.activeTenantId || !projectId || !memberToRemove) return;
    
    setRemovingMember(memberToRemove.userId);
    try {
      await projectApi.removeProjectMember(user.activeTenantId, projectId, memberToRemove.userId);
      
      toast({
        title: "Sukces",
        description: `Użytkownik ${memberToRemove.name} został usunięty z projektu`,
        status: "success",
        duration: 3000,
      });
      
      // Odśwież listę
      await fetchProjectDetails();
      await fetchMembers();
    } catch (error) {
      console.error("Błąd podczas usuwania członka:", error);
      const { title, description } = handleApiError(error);
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
              <HStack spacing={4} mb={4} justify="space-between" align="flex-start">
                <HStack spacing={4} flex={1}>
                  <Icon as={FolderKanban} boxSize={{ base: 8, md: 10 }} color="blue.600" />
                  <VStack align="flex-start" spacing={1} flex={1}>
                    <Heading size={{ base: "md", md: "lg" }}>{project.name}</Heading>
                  </VStack>
                </HStack>
                <HStack spacing={2} flexWrap="wrap">
                  <Badge colorScheme={project.isActive ? "green" : "gray"} fontSize="xs" px={2}>
                    {project.isActive ? "Aktywny" : "Nieaktywny"}
                  </Badge>
                  <Badge colorScheme={getProjectRoleColor(project.userRole)} fontSize="xs" px={2}>
                    {getProjectRoleName(project.userRole)}
                  </Badge>
                </HStack>
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

            {/* ====================== SZYBKI DOSTĘP ======================= */}
            <SimpleGrid columns={{ base: 1, md: 2, lg: 4 }} spacing={4}>
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
                  <Icon as={Users} boxSize={8} color="blue.600" />
                  <Text fontWeight="bold" fontSize="md">Członkowie</Text>
                </VStack>
              </Box>

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
                  <Icon as={Calendar} boxSize={8} color="purple.600" />
                  <Text fontWeight="bold" fontSize="md">Harmonogramy</Text>
                </VStack>
              </Box>

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
                  <Icon as={FileText} boxSize={8} color="purple.600" />
                  <Text fontWeight="bold" fontSize="md">Pliki</Text>
                </VStack>
              </Box>

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
                  <Text fontWeight="bold" fontSize="md">Koszty</Text>
                </VStack>
              </Box>

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
            </SimpleGrid>

            {/* Sekcje przeniesione do dedykowanych stron - dostępne przez karty powyżej */}
          </VStack>
        )}

        {/* Modals - wszystkie sekcje przeniesione na osobne strony */}

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

        {/* Modal udostępniania kosztu */}
        {costToShare && user?.activeTenantId && projectId && (
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

      </Box>
    </MainLayout>
  );
}
