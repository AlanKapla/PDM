import React, { useEffect, useState, useContext, useRef, useMemo } from "react";
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
  Tooltip,
} from "@chakra-ui/react";
import { ArrowLeft, FileText, Upload, Share2, Download, Eye, ChevronDown, ChevronUp, Clock, MessageSquare, Send, User, Plus } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import UploadFilesModal from "../components/UploadFilesModal";
import UploadNewVersionModal from "../components/UploadNewVersionModal";
import { ManageFileShareModal } from "../components/ManageFileShareModal";
import ShareFilesModal from "../components/ShareFilesModal";
import { AuthContext } from "../context/AuthContext";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import { formatDate } from "../utils/formatters";
import { projectApi, ResourceScope } from "../api/projectApi";
import type { ProjectFilePackageWeb, ProjectDetailsWeb, ProjectMemberWeb } from "../types/project.types";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import type { ResourcePermissions } from "../hooks/useResourcePermissions";
import { useTabCache } from "../hooks/useTabCache";
import { useGlobalCache } from "../hooks/useGlobalCache";
import { useAccordionIndex } from "../hooks/useAccordionIndex";

interface FileTabBaseProps {
  files: ProjectFilePackageWeb[];
  renderFileRow: (file: any, isShared: boolean, showOwner?: boolean) => JSX.Element;
  cardBg: string;
  borderColor: string;
  hoverBg: string;
  expandedPackageIds: Set<string>;
  packageFiles: Map<string, any[]>;
  loadingPackages: Set<string>;
  onTogglePackage: (packageId: string) => void;
}

type FilesTabScope = "all" | "mine" | "shared";

interface FilesTabProps extends FileTabBaseProps {
  scope: FilesTabScope;
  resourcePerms: ResourcePermissions;
  onShareFilesModalOpen?: () => void;
  onUploadModalOpen?: () => void;
}

// Konfiguracja per-scope — unikamy duplikacji logiki w osobnych komponentach
const SCOPE_CONFIG: Record<FilesTabScope, {
  description: string;
  emptyIcon: typeof FileText;
  emptyTitle: string;
  emptyDescription: string;
  packageIcon: typeof FileText;
  packageIconColor: string;
  badgeColor: string;
  isShared: boolean;
  showOwner: boolean;
  showOwnerInPackage: boolean;
  ownerLabel?: string;
}> = {
  all: {
    description: "Wszystkie pliki w projekcie (admin)",
    emptyIcon: FileText,
    emptyTitle: "Brak plików",
    emptyDescription: "Nie ma jeszcze żadnych plików w tym projekcie",
    packageIcon: FileText,
    packageIconColor: "level2.600",
    badgeColor: "level2",
    isShared: false,
    showOwner: true,
    showOwnerInPackage: true,
    ownerLabel: "właściciel",
  },
  mine: {
    description: "Twoje pliki w projekcie",
    emptyIcon: FileText,
    emptyTitle: "Brak plików",
    emptyDescription: "Nie masz jeszcze żadnych plików w tym projekcie",
    packageIcon: FileText,
    packageIconColor: "level2.600",
    badgeColor: "primary",
    isShared: false,
    showOwner: false,
    showOwnerInPackage: false,
  },
  shared: {
    description: "Pliki udostępnione przez innych członków projektu",
    emptyIcon: Share2,
    emptyTitle: "Brak udostępnionych plików",
    emptyDescription: "Nikt jeszcze nie udostępnił Ci plików w tym projekcie",
    packageIcon: Share2,
    packageIconColor: "action.600",
    badgeColor: "primary",
    isShared: true,
    showOwner: true,
    showOwnerInPackage: true,
    ownerLabel: "od",
  },
};

const FilesTab = React.memo<FilesTabProps>(({
  scope,
  files,
  resourcePerms,
  onShareFilesModalOpen,
  onUploadModalOpen,
  renderFileRow,
  cardBg,
  borderColor,
  hoverBg,
  expandedPackageIds,
  packageFiles,
  loadingPackages,
  onTogglePackage,
}) => {
  const config = SCOPE_CONFIG[scope];
  const expandedIndices = useAccordionIndex(expandedPackageIds, files || []);
  const perms = scope === "all" ? resourcePerms.all
    : scope === "mine" ? resourcePerms.mine
    : null; // shared — brak przycisków tworzenia/udostępniania

  if (!files) {
    return <LoadingSpinner />;
  }

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between">
        <Text fontSize="sm" color="neutral.600">
          {config.description}
        </Text>
        <HStack spacing={2}>
          {onShareFilesModalOpen && perms?.canShare && (
            <Button
              leftIcon={<Share2 size={18} />}
              colorScheme="gray"
              variant="outline"
              size="sm"
              onClick={onShareFilesModalOpen}
            >
              Udostępnij grupowo
            </Button>
          )}
          {onUploadModalOpen && perms?.canCreate && (
            <Button
              leftIcon={<Upload size={18} />}
              colorScheme="primary"
              size="sm"
              onClick={onUploadModalOpen}
            >
              Dodaj pliki
            </Button>
          )}
        </HStack>
      </HStack>

      {files.length === 0 ? (
        <EmptyState
          icon={config.emptyIcon}
          title={config.emptyTitle}
          description={config.emptyDescription}
        />
      ) : (
        <Accordion allowMultiple index={expandedIndices}>
          {files.map((pkg) => (
            <AccordionItem key={pkg.id} bg="white" borderWidth="1px" borderColor="neutral.200" rounded="md" mb={3}>
              <AccordionButton py={4} _hover={{ bg: 'neutral.50' }} onClick={() => onTogglePackage(pkg.id)}>
                <HStack flex="1" spacing={3}>
                  <Icon as={config.packageIcon} boxSize={5} color={config.packageIconColor} />
                  <Text fontWeight="semibold" fontSize="md">{pkg.name}</Text>
                  <Badge colorScheme={config.badgeColor} fontSize="sm">{pkg.totalFiles}</Badge>
                  {config.showOwnerInPackage && pkg.ownerName && (
                    <Text fontSize="sm" color="neutral.500">{config.ownerLabel}: {pkg.ownerName}</Text>
                  )}
                </HStack>
                {loadingPackages.has(pkg.id) ? <LoadingSpinner /> : <AccordionIcon />}
              </AccordionButton>
              <AccordionPanel pb={4}>
                {loadingPackages.has(pkg.id) ? (
                  <LoadingSpinner />
                ) : (
                  <Box overflowX="auto">
                  <Table size="sm" variant="simple">
                    <Thead>
                      <Tr>
                        <Th>Nazwa pliku</Th>
                        {config.showOwner && (
                          <Th display={{ base: "none", md: "table-cell" }}>Właściciel</Th>
                        )}
                        <Th display={{ base: "none", md: "table-cell" }}>Rozmiar</Th>
                        <Th>Akcje</Th>
                      </Tr>
                    </Thead>
                    <Tbody>
                      {(packageFiles.get(pkg.id) || []).map((file) =>
                        renderFileRow(file, config.isShared, config.showOwner)
                      )}
                    </Tbody>
                  </Table>
                  </Box>
                )}
              </AccordionPanel>
            </AccordionItem>
          ))}
        </Accordion>
      )}
    </VStack>
  );
});

export default function ProjectFiles() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { showSuccess, showError, showWarning, showInfo, toast, showApiSuccess } = useToastNotification();
  const { isOpen: isUploadModalOpen, onOpen: onUploadModalOpen, onClose: onUploadModalClose } = useDisclosure();
  const { isOpen: isUploadVersionModalOpen, onOpen: onUploadVersionModalOpen, onClose: onUploadVersionModalClose } = useDisclosure();
  const { isOpen: isManageShareModalOpen, onOpen: onManageShareModalOpen, onClose: onManageShareModalClose } = useDisclosure();
  const { isOpen: isShareFilesModalOpen, onOpen: onShareFilesModalOpen, onClose: onShareFilesModalClose } = useDisclosure();

  const [loading, setLoading] = useState(true);
  const [project, setProject] = useState<any | null>(null);
  const [members, setMembers] = useState<any[]>([]);
  const [activeTabIndex, setActiveTabIndex] = useState(0);
  const [expandedFileIds, setExpandedFileIds] = useState<Set<string>>(new Set());
  const [expandedPackageIds, setExpandedPackageIds] = useState<Set<string>>(new Set());
  const [expandedVersionIds, setExpandedVersionIds] = useState<Set<string>>(new Set());
  const [packageFiles, setPackageFiles] = useState<Map<string, any[]>>(new Map());
  const [fileVersions, setFileVersions] = useState<Map<string, any[]>>(new Map());
  const [versionComments, setVersionComments] = useState<Map<string, any[]>>(new Map());
  const [loadingPackages, setLoadingPackages] = useState<Set<string>>(new Set());
  const [loadingFiles, setLoadingFiles] = useState<Set<string>>(new Set());
  const [loadingVersions, setLoadingVersions] = useState<Set<string>>(new Set());
  const [fileForNewVersion, setFileForNewVersion] = useState<any | null>(null);
  const [fileToManageShare, setFileToManageShare] = useState<any | null>(null);
  const [newComments, setNewComments] = useState<Map<string, string>>(new Map());
  const [submittingComment, setSubmittingComment] = useState<string | null>(null);
  const hasFetchedProjectData = useRef(false);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  const resourcePerms = useResourcePermissions(projectId);

  // Tab cache tylko dla PACZEK (hierarchiczne pobieranie)
  const allFilesCache = useTabCache<ProjectFilePackageWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      const res = await projectApi.getProjectFilePackages(user.activeTenantId, projectId, ResourceScope.All);
      return res.data;
    },
    `files-packages-all-${projectId}`
  );

  // Tab cache dla Moje paczki
  const myFilesCache = useTabCache<ProjectFilePackageWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      const res = await projectApi.getProjectFilePackages(user.activeTenantId, projectId, ResourceScope.Mine);
      return res.data;
    },
    `files-packages-mine-${projectId}`
  );

  // Tab cache dla Udostępnione paczki
  const sharedFilesCache = useTabCache<ProjectFilePackageWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      const res = await projectApi.getProjectFilePackages(user.activeTenantId, projectId, ResourceScope.Shared);
      return res.data;
    },
    `files-packages-shared-${projectId}`
  );

  // Globalny cache dla project details (współdzielony między stronami projektu)
  const projectDetailsCache = useGlobalCache<ProjectDetailsWeb>(
    `project-details-${projectId}`,
    async () => {
      if (!user?.activeTenantId || !projectId) throw new Error('Missing tenant or project ID');
      const res = await projectApi.getProjectDetails(user.activeTenantId, projectId);
      return res.data;
    }
  );

  useEffect(() => {
    if (resourcePerms.raw.loading) return;
    if (hasFetchedProjectData.current) return;

    hasFetchedProjectData.current = true;
    fetchProjectData();
  }, [projectId, resourcePerms.raw.loading]);

  const fetchProjectData = async () => {
    if (!user?.activeTenantId || !projectId) return;

    if (!resourcePerms.hasAnyAccess) {
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      // Pobierz dane projektu
      const projectData = await projectDetailsCache.fetch();
      setProject(projectData);

      // Pobierz członków projektu do udostępniania plików
      try {
        const membersResponse = await projectApi.getProjectMembers(user.activeTenantId, projectId);
        // Filtruj aktualnego użytkownika
        const filteredMembers = membersResponse.data.filter(
          (member: ProjectMemberWeb) => member.userId !== user.id
        );
        setMembers(filteredMembers);
      } catch (error) {
      }

      // Pobierz wszystkie zasoby równolegle według uprawnień
      const fetchPromises = [];
      if (resourcePerms.tabs.showAll) {
        fetchPromises.push(allFilesCache.fetch());
      }
      if (resourcePerms.tabs.showMine) {
        fetchPromises.push(myFilesCache.fetch());
      }
      if (resourcePerms.tabs.showShared) {
        fetchPromises.push(sharedFilesCache.fetch());
      }

      await Promise.all(fetchPromises);
    } catch (error) {
      showError("Nie udało się pobrać danych");
    } finally {
      setLoading(false);
    }
  };

  const refreshData = () => {
    allFilesCache.clear();
    myFilesCache.clear();
    sharedFilesCache.clear();
    projectDetailsCache.clear();

    // Wyczyść lazy loaded data
    setPackageFiles(new Map());
    setFileVersions(new Map());
    setVersionComments(new Map());
    setExpandedPackageIds(new Set());
    setExpandedVersionIds(new Set());
    setExpandedFileIds(new Set());

    hasFetchedProjectData.current = false;
    fetchProjectData();
  };

  // Oblicz indeksy tabów - zapobiega niepotrzebnemu wywoływaniu useEffect
  const allFilesTabIndex = resourcePerms.tabs.showAll ? 0 : -1;
  const myFilesTabIndex =
    resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 1 :
      !resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 0 : -1;
  const sharedFilesTabIndex =
    resourcePerms.tabs.showAll && resourcePerms.tabs.showMine && resourcePerms.tabs.showShared ? 2 :
      (resourcePerms.tabs.showAll || resourcePerms.tabs.showMine) && resourcePerms.tabs.showShared ? 1 :
        !resourcePerms.tabs.showAll && !resourcePerms.tabs.showMine && resourcePerms.tabs.showShared ? 0 : -1;

  // useMemo dla danych aby zapobiec niepotrzebnym re-renderom tab components
  const allFilesData = useMemo(() => allFilesCache.data || [], [allFilesCache.data]);
  const myFilesData = useMemo(() => myFilesCache.data || [], [myFilesCache.data]);
  const sharedFilesData = useMemo(() => sharedFilesCache.data || [], [sharedFilesCache.data]);

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + " " + sizes[i];
  };

  // Pobieranie pliku przez SAS URL — używamy ukrytego <a> zamiast window.open,
  // ponieważ window.open może być blokowany przez popup blockery.
  // SAS URL z Content-Disposition: attachment wymusza pobranie z poprawną nazwą pliku.
  const handleDownloadFile = async (fileId: string, sasUrl: string) => {
    if (!user?.activeTenantId || !projectId) return;

    // Sprawdź czy SAS URL mógł wygasnąć (token ważny 60 min od załadowania danych)
    let downloadUrl = sasUrl;
    try {
      const sasExpiry = extractSasExpiry(sasUrl);
      if (!sasExpiry || sasExpiry <= new Date()) {
        // SAS wygasł — pobierz świeże wersje pliku z API
        const scope = getCurrentScope();
        const res = await projectApi.getFileVersions(user.activeTenantId, projectId, fileId, scope);
        const freshVersions = res.data;
        const freshVersion = freshVersions?.[0]; // Najnowsza wersja
        if (freshVersion?.sasUrlDownload) {
          downloadUrl = freshVersion.sasUrlDownload;
        }
      }
    } catch {
      // W razie błędu spróbuj z oryginalnym URL-em
    }

    const link = document.createElement("a");
    link.href = downloadUrl;
    link.style.display = "none";
    document.body.appendChild(link);
    link.click();
    setTimeout(() => document.body.removeChild(link), 200);
  };

  // Wyciąga datę wygaśnięcia z SAS URL (parametr "se")
  const extractSasExpiry = (sasUrl: string): Date | null => {
    try {
      const url = new URL(sasUrl);
      const se = url.searchParams.get("se");
      if (se) return new Date(se);
    } catch { /* niepoprawny URL */ }
    return null;
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

  // === Lazy loading functions ===

  const getCurrentScope = (): ResourceScope => {
    if (activeTabIndex === allFilesTabIndex) return ResourceScope.All;
    if (activeTabIndex === myFilesTabIndex) return ResourceScope.Mine;
    if (activeTabIndex === sharedFilesTabIndex) return ResourceScope.Shared;
    return ResourceScope.Mine;
  };

  const togglePackage = async (packageId: string) => {
    const isExpanding = !expandedPackageIds.has(packageId);

    setExpandedPackageIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(packageId)) {
        newSet.delete(packageId);
      } else {
        newSet.add(packageId);
      }
      return newSet;
    });

    // Lazy load files when expanding
    if (isExpanding && !packageFiles.has(packageId) && user?.activeTenantId && projectId) {
      setLoadingPackages((prev) => new Set(prev).add(packageId));
      try {
        const scope = getCurrentScope();
        const res = await projectApi.getPackageFiles(user.activeTenantId, projectId, packageId, scope);
        setPackageFiles((prev) => new Map(prev).set(packageId, res.data));
      } catch (error) {
        showError("Błąd", "Nie udało się pobrać plików");
      } finally {
        setLoadingPackages((prev) => {
          const newSet = new Set(prev);
          newSet.delete(packageId);
          return newSet;
        });
      }
    }
  };

  const toggleFileVersionsLazy = async (fileId: string) => {
    const isExpanding = !expandedVersionIds.has(fileId);

    setExpandedVersionIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(fileId)) {
        newSet.delete(fileId);
      } else {
        newSet.add(fileId);
      }
      return newSet;
    });

    // Lazy load versions when expanding
    if (isExpanding && !fileVersions.has(fileId) && user?.activeTenantId && projectId) {
      setLoadingFiles((prev) => new Set(prev).add(fileId));
      try {
        const scope = getCurrentScope();
        const res = await projectApi.getFileVersions(user.activeTenantId, projectId, fileId, scope);
        setFileVersions((prev) => new Map(prev).set(fileId, res.data));
      } catch (error) {
        showError("Błąd", "Nie udało się pobrać wersji");
      } finally {
        setLoadingFiles((prev) => {
          const newSet = new Set(prev);
          newSet.delete(fileId);
          return newSet;
        });
      }
    }
  };

  const toggleVersionComments = async (fileId: string, versionId: string) => {
    const commentKey = `${fileId}-${versionId}`;
    const isExpanding = !expandedFileIds.has(commentKey);

    setExpandedFileIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(commentKey)) {
        newSet.delete(commentKey);
      } else {
        newSet.add(commentKey);
      }
      return newSet;
    });

    // Lazy load comments when expanding
    if (isExpanding && !versionComments.has(commentKey) && user?.activeTenantId && projectId) {
      setLoadingVersions((prev) => new Set(prev).add(commentKey));
      try {
        const scope = getCurrentScope();
        const res = await projectApi.getVersionComments(user.activeTenantId, projectId, fileId, versionId, scope);
        setVersionComments((prev) => new Map(prev).set(commentKey, res.data));
      } catch (error) {
        showError("Błąd", "Nie udało się pobrać komentarzy");
      } finally {
        setLoadingVersions((prev) => {
          const newSet = new Set(prev);
          newSet.delete(commentKey);
          return newSet;
        });
      }
    }
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
    refreshData();
    onUploadVersionModalClose();
  };

  const openManageShareModal = (file: any) => {
    setFileToManageShare(file);
    onManageShareModalOpen();
  };

  const handleShareUpdated = () => {
    refreshData();
    onManageShareModalClose();
  };

  const handleAddComment = async (fileId: string, versionId: string) => {
    if (!user?.activeTenantId || !projectId) return;

    const commentKey = `${fileId}-${versionId}`;
    const comment = newComments.get(commentKey);

    if (!comment || comment.trim() === "") {
      showWarning("Uwaga", "Komentarz nie może być pusty");
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

      showApiSuccess('commentAdded');
      setNewComments((prev) => {
        const updated = new Map(prev);
        updated.delete(commentKey);
        return updated;
      });

      // Odśwież komentarze dla tej wersji
      const scope = getCurrentScope();
      const commentsRes = await projectApi.getVersionComments(user.activeTenantId, projectId, fileId, versionId, scope);
      setVersionComments((prev) => new Map(prev).set(commentKey, commentsRes.data));
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setSubmittingComment(null);
    }
  };

  const renderFileRow = (file: any, isShared: boolean = false, showOwner: boolean = true) => {
    // Backend zawsze zwraca ProjectFileWeb z polem `id` — niezależnie od scope
    const fileId = file.id;

    return (
      <React.Fragment key={fileId}>
        <Tr>
          <Td>
            <HStack spacing={2}>
              <Text fontSize="sm" fontWeight="medium">{file.displayName}</Text>
              {file.currentVersion?.versionNumber && (
                <Badge colorScheme="level2" fontSize="xs">v{file.currentVersion.versionNumber}</Badge>
              )}
              {!isShared && file.sharedWithUserIds && file.sharedWithUserIds.length > 0 && (
                <Badge colorScheme="orange" fontSize="xs" display="flex" alignItems="center" gap={1}>
                  <Share2 size={10} />
                  {file.sharedWithUserIds.length}
                </Badge>
              )}
            </HStack>
          </Td>
          {showOwner && (
            <Td display={{ base: "none", md: "table-cell" }} fontSize="sm">
              {file.originalOwnerUserName || file.ownerName || "-"}
            </Td>
          )}
          <Td display={{ base: "none", md: "table-cell" }} fontSize="sm">
            {file.currentVersion ? formatFileSize(file.currentVersion.fileSizeBytes) : "-"}
          </Td>
          <Td>
            <HStack spacing={1} flexWrap="wrap">
              {file.currentVersion && isPreviewSupported(file.currentVersion.contentType) && (
                <Tooltip label="Podgląd" hasArrow>
                  <IconButton
                    aria-label="Podgląd"
                    icon={<Eye size={16} />}
                    size="sm"
                    variant="ghost"
                    colorScheme="gray"
                    onClick={() => handlePreview(file.currentVersion.sasUrlView)}
                  />
                </Tooltip>
              )}
              {file.currentVersion && (
                <Tooltip label="Pobierz plik" hasArrow>
                  <IconButton
                    aria-label="Pobierz"
                    icon={<Download size={16} />}
                    size="sm"
                    variant="ghost"
                    colorScheme="gray"
                    onClick={() => handleDownloadFile(fileId, file.currentVersion.sasUrlDownload)}
                  />
                </Tooltip>
              )}
              {((!isShared && resourcePerms.mine.canEdit) || (isShared && resourcePerms.shared.canEdit)) && (
                <Tooltip label="Dodaj nową wersję" hasArrow>
                  <IconButton
                    aria-label="Nowa wersja"
                    icon={<Plus size={16} />}
                    size="sm"
                    variant="ghost"
                    colorScheme="gray"
                    onClick={() => openUploadVersionModal(file)}
                  />
                </Tooltip>
              )}
              {!isShared && resourcePerms.mine.canManageShare && (
                <Tooltip label="Udostępnij" hasArrow>
                  <IconButton
                    aria-label="Udostępnij"
                    icon={<Share2 size={16} />}
                    size="sm"
                    variant="ghost"
                    colorScheme="gray"
                    onClick={() => openManageShareModal(file)}
                  />
                </Tooltip>
              )}
              {file.totalVersions && file.totalVersions > 0 && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => toggleFileVersionsLazy(fileId)}
                  rightIcon={expandedVersionIds.has(fileId) ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                  isLoading={loadingFiles.has(fileId)}
                >
                  Wersje ({file.totalVersions})
                </Button>
              )
              }
            </HStack>
          </Td>
        </Tr>
        {expandedVersionIds.has(fileId) && (
          <Tr key={`${fileId}-versions`}>
            <Td colSpan={showOwner ? 4 : 3} p={0}>
              <Box bg={useColorModeValue("gray.50", "gray.900")} p={4}>
                {loadingFiles.has(fileId) ? (
                  <LoadingSpinner />
                ) : (
                  <VStack align="stretch" spacing={3}>
                    <Heading size="sm" mb={2}>
                      Historia wersji ({file.totalVersions})
                    </Heading>
                    {(fileVersions.get(fileId) || [])
                      .map((version: any) => (
                        <Box
                          key={version.id}
                          borderWidth="1px"
                          borderRadius="md"
                          p={3}
                          bg="white"
                          borderColor={version.id === file.currentVersion?.id ? "neutral.400" : "neutral.200"}
                        >
                          <HStack justify="space-between" mb={2}>
                            <HStack spacing={2} flexWrap="wrap">
                              <Badge
                                bg={version.id === file.currentVersion?.id ? "primary.50" : "neutral.50"}
                                color={version.id === file.currentVersion?.id ? "primary.600" : "neutral.500"}
                                borderWidth="1px"
                                borderColor={version.id === file.currentVersion?.id ? "primary.200" : "neutral.200"}
                              >
                                Wersja {version.versionNumber}
                                {version.id === file.currentVersion?.id && " (Aktualna)"}
                              </Badge>
                              <Badge colorScheme="neutral" fontSize="xs">
                                {version.contentType?.split("/")[1]?.toUpperCase() || "FILE"}
                              </Badge>
                              <Text fontSize="xs" color="neutral.600">
                                {formatFileSize(version.fileSizeBytes)}
                              </Text>
                            </HStack>
                            <HStack spacing={1}>
                              {isPreviewSupported(version.contentType) && (
                                <Tooltip label="Podgląd" hasArrow>
                                  <IconButton
                                    aria-label="Podgląd"
                                    icon={<Eye size={14} />}
                                    size="xs"
                                    colorScheme="level2"
                                    onClick={() => handlePreview(version.sasUrlView)}
                                  />
                                </Tooltip>
                              )}
                              <Button
                                size="xs"
                                leftIcon={<Download size={14} />}
                                onClick={() => handleDownloadFile(fileId, version.sasUrlDownload)}
                              >
                                Pobierz
                              </Button>
                            </HStack>
                          </HStack>
                          <HStack spacing={4} fontSize="xs" color="neutral.600" mb={2}>
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
                            <Button
                              size="sm"
                              variant="ghost"
                              leftIcon={<MessageSquare size={14} />}
                              onClick={() => toggleVersionComments(fileId, version.id)}
                              rightIcon={expandedFileIds.has(`${fileId}-${version.id}`) ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                              isLoading={loadingVersions.has(`${fileId}-${version.id}`)}
                            >
                              Komentarze
                            </Button>

                            {expandedFileIds.has(`${fileId}-${version.id}`) && (
                              <Box mt={3}>
                                {loadingVersions.has(`${fileId}-${version.id}`) ? (
                                  <LoadingSpinner />
                                ) : (
                                  <>
                                    {(versionComments.get(`${fileId}-${version.id}`) || []).length > 0 && (
                                      <VStack align="stretch" spacing={3} mb={3}>
                                        {(versionComments.get(`${fileId}-${version.id}`) || [])
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
                                                  bg={isMyComment ? "primary.50" : "neutral.50"}
                                                  color={isMyComment ? "primary.800" : "neutral.700"}
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

                                    {((!isShared && resourcePerms.mine.canEdit) || (isShared && resourcePerms.shared.canEdit)) && (
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
                                          colorScheme="primary"
                                          size="sm"
                                          onClick={() => handleAddComment(isShared ? file.projectFileId : file.id, version.id)}
                                          isLoading={submittingComment === `${fileId}-${version.id}`}
                                          isDisabled={!newComments.get(`${fileId}-${version.id}`)?.trim()}
                                        />
                                      </HStack>
                                    )}
                                  </>
                                )}
                              </Box>
                            )}
                          </Box>
                        </Box>
                      ))}
                  </VStack>
                )}
              </Box>
            </Td>
          </Tr>
        )}
      </React.Fragment>
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
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={FileText} boxSize={8} color="level2.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Pliki projektu</Heading>
              {project && <Text fontSize="sm" color="neutral.600">{project.name}</Text>}
            </VStack>
          </HStack>
        </HStack>

        {!project || (!resourcePerms.hasAnyAccess && !resourcePerms.raw.loading) ? (
          <Box p={{ base: 3, sm: 4, md: 8 }} textAlign="center">
            <EmptyState
              icon={FileText}
              title="Brak dostępu"
              description="Nie masz uprawnień do przeglądania plików w tym projekcie"
            />
          </Box>
        ) : (
          <Tabs colorScheme="level2" variant="enclosed" onChange={setActiveTabIndex}>
            <TabList>
              {resourcePerms.tabs.showAll && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={FileText} boxSize={4} />
                    <Text>Wszystkie</Text>
                    <Badge colorScheme="level2" ml={2}>{(allFilesCache.data || []).reduce((sum, pkg) => sum + pkg.totalFiles, 0)}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showMine && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={FileText} boxSize={4} />
                    <Text>Moje</Text>
                    <Badge colorScheme="primary" ml={2}>{(myFilesCache.data || []).reduce((sum, pkg) => sum + pkg.totalFiles, 0)}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showShared && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={Share2} boxSize={4} />
                    <Text>Udostępnione</Text>
                    <Badge colorScheme="action" ml={2}>{(sharedFilesCache.data || []).reduce((sum, pkg) => sum + pkg.totalFiles, 0)}</Badge>
                  </HStack>
                </Tab>
              )}
            </TabList>

            <TabPanels>
              {resourcePerms.tabs.showAll && (
                <TabPanel>
                  {allFilesCache.loading ? (
                    <LoadingSpinner />
                  ) : (
                    <FilesTab
                      scope="all"
                      files={allFilesData}
                      resourcePerms={resourcePerms}
                      onShareFilesModalOpen={onShareFilesModalOpen}
                      onUploadModalOpen={onUploadModalOpen}
                      renderFileRow={renderFileRow}
                      cardBg={cardBg}
                      borderColor={borderColor}
                      hoverBg={hoverBg}
                      expandedPackageIds={expandedPackageIds}
                      packageFiles={packageFiles}
                      loadingPackages={loadingPackages}
                      onTogglePackage={togglePackage}
                    />
                  )}
                </TabPanel>
              )}
              {resourcePerms.tabs.showMine && (
                <TabPanel>
                  {myFilesCache.loading ? (
                    <LoadingSpinner />
                  ) : (
                    <FilesTab
                      scope="mine"
                      files={myFilesData}
                      resourcePerms={resourcePerms}
                      onShareFilesModalOpen={onShareFilesModalOpen}
                      onUploadModalOpen={onUploadModalOpen}
                      renderFileRow={renderFileRow}
                      cardBg={cardBg}
                      borderColor={borderColor}
                      hoverBg={hoverBg}
                      expandedPackageIds={expandedPackageIds}
                      packageFiles={packageFiles}
                      loadingPackages={loadingPackages}
                      onTogglePackage={togglePackage}
                    />
                  )}
                </TabPanel>
              )}
              {resourcePerms.tabs.showShared && (
                <TabPanel>
                  {sharedFilesCache.loading ? (
                    <LoadingSpinner />
                  ) : (
                    <FilesTab
                      scope="shared"
                      files={sharedFilesData}
                      resourcePerms={resourcePerms}
                      renderFileRow={renderFileRow}
                      cardBg={cardBg}
                      borderColor={borderColor}
                      hoverBg={hoverBg}
                      expandedPackageIds={expandedPackageIds}
                      packageFiles={packageFiles}
                      loadingPackages={loadingPackages}
                      onTogglePackage={togglePackage}
                    />
                  )}
                </TabPanel>
              )}
            </TabPanels>
          </Tabs>
        )}

        {isUploadModalOpen && (
          <UploadFilesModal
            isOpen={isUploadModalOpen}
            onClose={onUploadModalClose}
            projectId={projectId || ""}
            projectName={project?.name || ""}
            tenantId={user?.activeTenantId || ""}
            onFilesUploaded={refreshData}
          />
        )}


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
            ownerUserId={fileToManageShare.ownerId}
            onShareUpdated={handleShareUpdated}
          />
        )}

        <ShareFilesModal
          isOpen={isShareFilesModalOpen}
          onClose={onShareFilesModalClose}
          projectId={projectId || ""}
          tenantId={user?.activeTenantId || ""}
          onFilesShared={refreshData}
          myPackages={
            activeTabIndex === allFilesTabIndex
              ? allFilesCache.data || undefined
              : myFilesCache.data || undefined
          }
        />
      </Box>
    </MainLayout>
  );
}
