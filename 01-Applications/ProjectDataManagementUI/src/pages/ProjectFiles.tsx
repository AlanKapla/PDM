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
import { projectApi, ResourceScope } from "../api/projectApi";
import type { ProjectFilePackageWeb, ProjectDetailsWeb, ProjectMemberWeb } from "../types/project.types";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import type { ResourcePermissions } from "../hooks/useResourcePermissions";
import { useTabCache } from "../hooks/useTabCache";
import { useGlobalCache } from "../hooks/useGlobalCache";
import { useAccordionIndex } from "../hooks/useAccordionIndex";

interface FileTabBaseProps {
  files: ProjectFilePackageWeb[];
  renderFileRow: (file: any, isShared: boolean) => JSX.Element;
  cardBg: string;
  borderColor: string;
  hoverBg: string;
  expandedPackageIds: Set<string>;
  packageFiles: Map<string, any[]>;
  loadingPackages: Set<string>;
  onTogglePackage: (packageId: string) => void;
}

interface AllFilesTabProps extends FileTabBaseProps {
  resourcePerms: ResourcePermissions;
  onShareFilesModalOpen: () => void;
  onUploadModalOpen: () => void;
}

interface MyFilesTabProps extends FileTabBaseProps {
  resourcePerms: ResourcePermissions;
  onShareFilesModalOpen: () => void;
  onUploadModalOpen: () => void;
}

interface SharedFilesTabProps {
  files: ProjectFilePackageWeb[];
  renderFileRow: (file: any, isShared: boolean) => JSX.Element;
  cardBg: string;
  borderColor: string;
  hoverBg: string;
  expandedPackageIds: Set<string>;
  packageFiles: Map<string, any[]>;
  loadingPackages: Set<string>;
  onTogglePackage: (packageId: string) => void;
}

// === Tab Components jako osobne komponenty z React.memo ===
const AllFilesTab = React.memo<AllFilesTabProps>(({ 
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
  onTogglePackage
}) => {
  const expandedIndices = useAccordionIndex(expandedPackageIds, files || []);

  if (!files) {
    return <LoadingSpinner />;
  }

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between">
        <Text fontSize="sm" color="gray.600">
          Wszystkie pliki w projekcie (admin)
        </Text>
        <HStack spacing={2}>
          {resourcePerms.all.canShare && (
          <Button
            leftIcon={<Share2 size={18} />}
            colorScheme="orange"
            size="sm"
            onClick={onShareFilesModalOpen}
          >
            Udostępnij grupowo
          </Button>
          )}
          {resourcePerms.all.canCreate && (
            <Button
              leftIcon={<Upload size={18} />}
              colorScheme="green"
              onClick={onUploadModalOpen}
            >
              Dodaj pliki
            </Button>
          )}
        </HStack>
      </HStack>

      {files.length === 0 ? (
        <EmptyState
          icon={FileText}
          title="Brak plików"
          description="Nie ma jeszcze żadnych plików w tym projekcie"
        />
      ) : (
        <Accordion allowMultiple index={expandedIndices}>
          {files.map((pkg) => (
            <AccordionItem key={pkg.id} bg={cardBg} borderWidth="1px" borderColor={borderColor} rounded="md" mb={3}>
              <AccordionButton py={4} _hover={{ bg: hoverBg }} onClick={() => onTogglePackage(pkg.id)}>
                <HStack flex="1" spacing={3}>
                  <Icon as={FileText} boxSize={5} color="purple.600" />
                  <Text fontWeight="bold" fontSize="lg">📦 {pkg.name}</Text>
                  <Badge colorScheme="purple" fontSize="sm">{pkg.totalFiles}</Badge>
                  <Text fontSize="sm" color="gray.500">właściciel: {pkg.ownerName}</Text>
                </HStack>
                {loadingPackages.has(pkg.id) ? <LoadingSpinner /> : <AccordionIcon />}
              </AccordionButton>
              <AccordionPanel pb={4}>
                {loadingPackages.has(pkg.id) ? (
                  <LoadingSpinner />
                ) : (
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
                    {(packageFiles.get(pkg.id) || []).map((file) => renderFileRow(file, false))}
                  </Tbody>
                </Table>
                )}
              </AccordionPanel>
            </AccordionItem>
          ))}
        </Accordion>
      )}
    </VStack>
  );
});

const MyFilesTab = React.memo<MyFilesTabProps>(({ 
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
  onTogglePackage
}) => {
  const expandedIndices = useAccordionIndex(expandedPackageIds, files || []);

  if (!files) {
    return <LoadingSpinner />;
  }

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between">
        <Text fontSize="sm" color="gray.600">
          Twoje pliki w projekcie
        </Text>
        {resourcePerms.mine.canCreate && (
        <HStack spacing={2}>
          {resourcePerms.mine.canShare && (
          <Button
            leftIcon={<Share2 size={18} />}
            colorScheme="orange"
            size="sm"
            onClick={onShareFilesModalOpen}
          >
            Udostępnij grupowo
          </Button>
          )}
          <Button
            leftIcon={<Upload size={18} />}
            colorScheme="green"
            onClick={onUploadModalOpen}
          >
            Dodaj pliki
          </Button>
        </HStack>
        )}
      </HStack>

      {files.length === 0 ? (
        <EmptyState
          icon={FileText}
          title="Brak plików"
          description="Nie masz jeszcze żadnych plików w tym projekcie"
        />
      ) : (
        <Accordion allowMultiple index={expandedIndices}>
          {files.map((pkg) => (
            <AccordionItem key={pkg.id} bg={cardBg} borderWidth="1px" borderColor={borderColor} rounded="md" mb={3}>
              <AccordionButton py={4} _hover={{ bg: hoverBg }} onClick={() => onTogglePackage(pkg.id)}>
                <HStack flex="1" spacing={3}>
                  <Icon as={FileText} boxSize={5} color="purple.600" />
                  <Text fontWeight="bold" fontSize="lg">📦 {pkg.name}</Text>
                  <Badge colorScheme="blue" fontSize="sm">{pkg.totalFiles}</Badge>
                </HStack>
                {loadingPackages.has(pkg.id) ? <LoadingSpinner /> : <AccordionIcon />}
              </AccordionButton>
              <AccordionPanel pb={4}>
                {loadingPackages.has(pkg.id) ? (
                  <LoadingSpinner />
                ) : (
                <Table size="sm" variant="simple">
                  <Thead>
                    <Tr>
                      <Th>Nazwa pliku</Th>
                      <Th display={{ base: "none", md: "table-cell" }}>Rozmiar</Th>
                      <Th>Akcje</Th>
                    </Tr>
                  </Thead>
                  <Tbody>
                    {(packageFiles.get(pkg.id) || []).map((file) => renderFileRow(file, false))}
                  </Tbody>
                </Table>
                )}
              </AccordionPanel>
            </AccordionItem>
          ))}
        </Accordion>
      )}
    </VStack>
  );
});

const SharedFilesTab = React.memo<SharedFilesTabProps>(({ 
  files, 
  renderFileRow,
  cardBg,
  borderColor,
  hoverBg,
  expandedPackageIds,
  packageFiles,
  loadingPackages,
  onTogglePackage
}) => {
  const expandedIndices = useAccordionIndex(expandedPackageIds, files || []);

  if (!files) {
    return <LoadingSpinner />;
  }

  return (
    <VStack spacing={4} align="stretch">
      <Text fontSize="sm" color="gray.600">
        Pliki udostępnione przez innych członków projektu
      </Text>

      {files.length === 0 ? (
        <EmptyState
          icon={Share2}
          title="Brak udostępnionych plików"
          description="Nikt jeszcze nie udostępnił Ci plików w tym projekcie"
        />
      ) : (
        <Accordion allowMultiple index={expandedIndices}>
          {files.map((pkg) => (
            <AccordionItem key={pkg.id} bg={cardBg} borderWidth="1px" borderColor={borderColor} rounded="md" mb={3}>
              <AccordionButton py={4} _hover={{ bg: hoverBg }} onClick={() => onTogglePackage(pkg.id)}>
                <HStack flex="1" spacing={3}>
                  <Icon as={Share2} boxSize={5} color="teal.600" />
                  <Text fontWeight="bold" fontSize="lg">📦 {pkg.name}</Text>
                  <Badge colorScheme="blue" fontSize="sm">{pkg.totalFiles}</Badge>
                  <Text fontSize="sm" color="gray.500">od: {pkg.ownerName}</Text>
                </HStack>
                {loadingPackages.has(pkg.id) ? <LoadingSpinner /> : <AccordionIcon />}
              </AccordionButton>
              <AccordionPanel pb={4}>
                {loadingPackages.has(pkg.id) ? (
                  <LoadingSpinner />
                ) : (
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
                    {(packageFiles.get(pkg.id) || []).map((file) => renderFileRow(file, true))}
                  </Tbody>
                </Table>
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
  const toast = useToast();
  const { showSuccess, showError } = useToastNotification();
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
        console.error('Error loading package files:', error);
        toast({
          title: "Błąd",
          description: "Nie udało się pobrać plików",
          status: "error",
          duration: 3000,
        });
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
        console.error('Error loading file versions:', error);
        toast({
          title: "Błąd",
          description: "Nie udało się pobrać wersji",
          status: "error",
          duration: 3000,
        });
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
        console.error('Error loading version comments:', error);
        toast({
          title: "Błąd",
          description: "Nie udało się pobrać komentarzy",
          status: "error",
          duration: 3000,
        });
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

      showSuccess("Komentarz został dodany");
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
      showError("Nie udało się dodać komentarza");
    } finally {
      setSubmittingComment(null);
    }
  };

  const renderFileRow = (file: any, isShared: boolean = false) => {
    const fileId = isShared ? file.projectFileId : file.id;
    
    return (
      <React.Fragment key={fileId}>
        <Tr>
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
          <Td display={{ base: "none", md: "table-cell" }} fontSize="sm">
            {isShared ? (file.originalOwnerUserName || "-") : (file.ownerName || "-")}
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
              {((!isShared && resourcePerms.mine.canEdit) || (isShared && resourcePerms.shared.canEdit)) && (
                <IconButton
                  aria-label="Nowa wersja"
                  icon={<Upload size={16} />}
                  size="sm"
                  variant="ghost"
                  colorScheme="green"
                  onClick={() => openUploadVersionModal(file)}
                />
              )}
              {!isShared && resourcePerms.mine.canManageShare && (
                <IconButton
                  aria-label="Zarządzaj udostępnieniem"
                  icon={<Share2 size={16} />}
                  size="sm"
                  variant="ghost"
                  colorScheme="orange"
                  onClick={() => openManageShareModal(file)}
                />
              )}
              {file.totalVersions && file.totalVersions > 0 && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => toggleFileVersionsLazy(fileId)}
                  rightIcon={expandedVersionIds.has(fileId) ? <ChevronUp size={16} />  : <ChevronDown size={16} />}
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
            <Td colSpan={4} p={0}>
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
                                        colorScheme="blue"
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

        {!project || (!resourcePerms.hasAnyAccess && !resourcePerms.raw.loading) ? (
          <Box p={8} textAlign="center">
            <EmptyState
              icon={FileText}
              title="Brak dostępu"
              description="Nie masz uprawnień do przeglądania plików w tym projekcie"
            />
          </Box>
        ) : (
        <Tabs colorScheme="purple" variant="enclosed" onChange={setActiveTabIndex}>
          <TabList>
            {resourcePerms.tabs.showAll && (
              <Tab fontWeight="bold">
                <HStack spacing={2}>
                  <Icon as={FileText} boxSize={4} />
                  <Text>Wszystkie pliki</Text>
                  <Badge colorScheme="purple" ml={2}>{(allFilesCache.data || []).reduce((sum, pkg) => sum + pkg.totalFiles, 0)}</Badge>
                </HStack>
              </Tab>
            )}
            {resourcePerms.tabs.showMine && (
              <Tab fontWeight="bold">
                <HStack spacing={2}>
                  <Icon as={FileText} boxSize={4} />
                  <Text>Moje pliki</Text>
                  <Badge colorScheme="blue" ml={2}>{(myFilesCache.data || []).reduce((sum, pkg) => sum + pkg.totalFiles, 0)}</Badge>
                </HStack>
              </Tab>
            )}
            {resourcePerms.tabs.showShared && (
              <Tab fontWeight="bold">
                <HStack spacing={2}>
                  <Icon as={Share2} boxSize={4} />
                  <Text>Udostępnione</Text>
                  <Badge colorScheme="teal" ml={2}>{(sharedFilesCache.data || []).reduce((sum, pkg) => sum + pkg.totalFiles, 0)}</Badge>
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
                  <AllFilesTab 
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
                  <MyFilesTab 
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
                  <SharedFilesTab 
                    files={sharedFilesData}
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

        <UploadFilesModal
          isOpen={isUploadModalOpen}
          onClose={onUploadModalClose}
          projectId={projectId || ""}
          projectName={project?.name || ""}
          tenantId={user?.activeTenantId || ""}
          onFilesUploaded={refreshData}
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
          onFilesShared={refreshData}
          myPackages={myFilesCache.data || undefined}
        />
      </Box>
    </MainLayout>
  );
}
