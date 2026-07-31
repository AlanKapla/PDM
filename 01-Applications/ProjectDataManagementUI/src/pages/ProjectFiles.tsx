import React, { useState, useContext, useMemo, useEffect, useRef } from "react";
import { useParams, useSearchParams } from "react-router-dom";
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
  Wrap,
  WrapItem,
  useBreakpointValue,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
} from "@chakra-ui/react";
import { FileText, Upload, Share2, Download, Eye, ChevronDown, ChevronUp, Clock, MessageSquare, Send, User, Plus, FolderPlus, FolderOpen, Folder, MoreVertical } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import UploadFilesModal from "../components/UploadFilesModal";
import UploadNewVersionModal from "../components/UploadNewVersionModal";
import { ManageFileShareModal } from "../components/ManageFileShareModal";
import ShareFilesModal from "../components/ShareFilesModal";
import CreateDirectoryModal from "../components/CreateDirectoryModal";
import { AuthContext } from "../context/AuthContext";
import { BackToProjectButton, LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import { formatDate } from "../utils/formatters";
import { projectApi, ResourceScope } from "../api/projectApi";
import type { ProjectFilePackageWeb, ProjectMemberWeb } from "../types/project.types";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import type { ResourcePermissions } from "../hooks/useResourcePermissions";
import {
  useProjectDetails,
  useProjectMembers,
  useFilePackages,
  usePackageFiles,
  useFileVersions,
  useVersionComments,
  fileKeys,
} from "../hooks/queries";
import { useQueryClient } from "@tanstack/react-query";

// ─────────────────────────────────────────────────────────────────────────────
// Module-scope helpers (czyste funkcje)
// ─────────────────────────────────────────────────────────────────────────────

const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + " " + sizes[i];
};

const extractSasExpiry = (sasUrl: string): Date | null => {
  try {
    const url = new URL(sasUrl);
    const se = url.searchParams.get("se");
    if (se) return new Date(se);
  } catch { /* niepoprawny URL */ }
  return null;
};

/** Finds packageId in catalog tree and returns path of ancestor package ids (inclusive). */
const findPackagePath = (
  packages: ProjectFilePackageWeb[],
  packageId: string,
  path: string[] = []
): string[] | null => {
  for (const pkg of packages) {
    const nextPath = [...path, pkg.id];
    if (pkg.id === packageId) {
      return nextPath;
    }
    const nested = findPackagePath(pkg.subCatalogs ?? [], packageId, nextPath);
    if (nested) {
      return nested;
    }
  }
  return null;
};

const isPreviewSupported = (contentType: string): boolean => {
  return contentType === "application/pdf" || contentType.startsWith("image/");
};

type FilesTabScope = "all" | "mine" | "shared";

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

// ─────────────────────────────────────────────────────────────────────────────
// VersionCommentsSection — komentarze do konkretnej wersji pliku
// ─────────────────────────────────────────────────────────────────────────────

interface VersionCommentsSectionProps {
  tenantId: string;
  projectId: string;
  fileId: string;
  versionId: string;
  scope: ResourceScope;
  isExpanded: boolean;
  canEdit: boolean;
  currentUserId: string | undefined;
  newComment: string;
  onCommentChange: (value: string) => void;
  onSubmitComment: () => void;
  isSubmitting: boolean;
  highlightCommentId?: string | null;
}

const VersionCommentsSection: React.FC<VersionCommentsSectionProps> = ({
  tenantId,
  projectId,
  fileId,
  versionId,
  scope,
  isExpanded,
  canEdit,
  currentUserId,
  newComment,
  onCommentChange,
  onSubmitComment,
  isSubmitting,
  highlightCommentId,
}) => {
  const { data: comments, isLoading } = useVersionComments(
    tenantId,
    projectId,
    fileId,
    versionId,
    scope,
    isExpanded
  );
  const didScrollRef = useRef(false);
  const list = comments ?? [];

  useEffect(() => {
    didScrollRef.current = false;
  }, [highlightCommentId]);

  useEffect(() => {
    if (!isExpanded || isLoading || !highlightCommentId || didScrollRef.current) {
      return;
    }

    const element = document.getElementById(`file-comment-${highlightCommentId}`);
    if (!element) {
      return;
    }

    didScrollRef.current = true;
    element.scrollIntoView({ behavior: "smooth", block: "center" });
  }, [isExpanded, isLoading, highlightCommentId, list]);

  if (!isExpanded) return null;
  if (isLoading) return <Box mt={3}><LoadingSpinner /></Box>;

  return (
    <Box mt={3}>
      {list.length > 0 && (
        <VStack align="stretch" spacing={3} mb={3}>
          {list.map((comment) => {
            const isMyComment = currentUserId === comment.userId;
            const isHighlighted = highlightCommentId === comment.id;
            return (
              <HStack
                key={comment.id}
                id={`file-comment-${comment.id}`}
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
                  borderWidth={isHighlighted ? "2px" : "0"}
                  borderColor={isHighlighted ? "primary.400" : undefined}
                  boxShadow={isHighlighted ? "md" : undefined}
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

      {canEdit && (
        <VStack spacing={2} align="stretch" display={{ base: "flex", md: "none" }}>
          <Textarea
            placeholder="Dodaj komentarz..."
            size="sm"
            value={newComment}
            onChange={(e) => onCommentChange(e.target.value)}
            rows={2}
            resize="vertical"
            w="100%"
          />
          <Button
            aria-label="Wyślij komentarz"
            leftIcon={<Send size={16} aria-hidden="true" />}
            colorScheme="primary"
            size="md"
            minH="44px"
            onClick={onSubmitComment}
            isLoading={isSubmitting}
            isDisabled={!newComment.trim()}
            alignSelf="stretch"
          >
            Wyślij
          </Button>
        </VStack>
      )}
      {canEdit && (
        <HStack spacing={2} display={{ base: "none", md: "flex" }}>
          <Textarea
            placeholder="Dodaj komentarz..."
            size="sm"
            value={newComment}
            onChange={(e) => onCommentChange(e.target.value)}
            rows={2}
            resize="vertical"
          />
          <IconButton
            aria-label="Wyślij komentarz"
            icon={<Send size={16} aria-hidden="true" />}
            colorScheme="primary"
            size="sm"
            onClick={onSubmitComment}
            isLoading={isSubmitting}
            isDisabled={!newComment.trim()}
          />
        </HStack>
      )}
    </Box>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// FileRow — wiersz pliku + rozwijana lista wersji + komentarze
// ─────────────────────────────────────────────────────────────────────────────

interface FileRowProps {
  file: any;
  tenantId: string;
  projectId: string;
  scope: ResourceScope;
  isShared: boolean;
  showOwner: boolean;
  isVersionsExpanded: boolean;
  expandedCommentKeys: Set<string>;
  resourcePerms: ResourcePermissions;
  currentUserId: string | undefined;
  onToggleVersions: (fileId: string) => void;
  onToggleVersionComments: (fileId: string, versionId: string) => void;
  onPreview: (sasUrlView: string) => void;
  onDownload: (fileId: string, sasUrlDownload: string) => void;
  onOpenUploadVersion: (file: any) => void;
  onOpenManageShare: (file: any) => void;
  newComments: Map<string, string>;
  onCommentChange: (commentKey: string, value: string) => void;
  onSubmitComment: (keyFileId: string, apiFileId: string, versionId: string) => void;
  submittingComment: string | null;
  highlightCommentId?: string | null;
  /** Mobile: card layout; desktop: table rows */
  layout?: "table" | "card";
}

const FileRow: React.FC<FileRowProps> = ({
  file,
  tenantId,
  projectId,
  scope,
  isShared,
  showOwner,
  isVersionsExpanded,
  expandedCommentKeys,
  resourcePerms,
  currentUserId,
  onToggleVersions,
  onToggleVersionComments,
  onPreview,
  onDownload,
  onOpenUploadVersion,
  onOpenManageShare,
  newComments,
  onCommentChange,
  onSubmitComment,
  submittingComment,
  highlightCommentId,
  layout = "table",
}) => {
  const fileId = file.id;
  const expandedBg = useColorModeValue("gray.50", "gray.900");
  const isCard = layout === "card";

  const { data: versions, isLoading: versionsLoading } = useFileVersions(
    tenantId,
    projectId,
    fileId,
    scope,
    isVersionsExpanded
  );

  const canEdit =
    (!isShared && resourcePerms.mine.canEdit) ||
    (isShared && resourcePerms.shared.canEdit);

  const canPreview = Boolean(
    file.currentVersion && isPreviewSupported(file.currentVersion.contentType)
  );
  const canDownload = Boolean(file.currentVersion);
  const canShare = !isShared && resourcePerms.mine.canManageShare;
  const canShowVersions = (file.totalVersions ?? 0) > 0;
  const hasKebabActions = canDownload || canEdit || canShare || canShowVersions;

  const handleOpenLatest = () => {
    if (canPreview) {
      onPreview(file.currentVersion.sasUrlView);
    }
  };

  const fileNameBlock = (
    <HStack spacing={2} minW={0} flexWrap="wrap">
      <Text fontSize="sm" fontWeight="medium" noOfLines={2} wordBreak="break-word">
        {file.displayName}
      </Text>
      {file.currentVersion?.versionNumber && (
        <Badge colorScheme="level2" fontSize="xs">v{file.currentVersion.versionNumber}</Badge>
      )}
      {!isShared && file.sharedWithUserIds && file.sharedWithUserIds.length > 0 && (
        <Badge colorScheme="orange" fontSize="xs" display="flex" alignItems="center" gap={1}>
          <Share2 size={10} aria-hidden="true" />
          {file.sharedWithUserIds.length}
        </Badge>
      )}
    </HStack>
  );

  const desktopFileActions = (
    <Wrap spacing={2}>
      {canPreview && (
        <WrapItem>
          <Tooltip label="Podgląd" hasArrow>
            <IconButton
              aria-label="Podgląd"
              icon={<Eye size={16} aria-hidden="true" />}
              size="sm"
              variant="ghost"
              colorScheme="gray"
              onClick={() => onPreview(file.currentVersion.sasUrlView)}
            />
          </Tooltip>
        </WrapItem>
      )}
      {canDownload && (
        <WrapItem>
          <Tooltip label="Pobierz plik" hasArrow>
            <IconButton
              aria-label="Pobierz"
              icon={<Download size={16} aria-hidden="true" />}
              size="sm"
              variant="ghost"
              colorScheme="gray"
              onClick={() => onDownload(fileId, file.currentVersion.sasUrlDownload)}
            />
          </Tooltip>
        </WrapItem>
      )}
      {canEdit && (
        <WrapItem>
          <Tooltip label="Dodaj nową wersję" hasArrow>
            <IconButton
              aria-label="Nowa wersja"
              icon={<Plus size={16} aria-hidden="true" />}
              size="sm"
              variant="ghost"
              colorScheme="gray"
              onClick={() => onOpenUploadVersion(file)}
            />
          </Tooltip>
        </WrapItem>
      )}
      {canShare && (
        <WrapItem>
          <Tooltip label="Udostępnij" hasArrow>
            <IconButton
              aria-label="Udostępnij"
              icon={<Share2 size={16} aria-hidden="true" />}
              size="sm"
              variant="ghost"
              colorScheme="gray"
              onClick={() => onOpenManageShare(file)}
            />
          </Tooltip>
        </WrapItem>
      )}
      {canShowVersions && (
        <WrapItem>
          <Button
            size="sm"
            variant="ghost"
            onClick={() => onToggleVersions(fileId)}
            rightIcon={isVersionsExpanded ? <ChevronUp size={16} aria-hidden="true" /> : <ChevronDown size={16} aria-hidden="true" />}
            isLoading={isVersionsExpanded && versionsLoading}
          >
            Wersje ({file.totalVersions})
          </Button>
        </WrapItem>
      )}
    </Wrap>
  );

  const mobileFileActions = (
    <HStack spacing={0} flexShrink={0} onClick={(e) => e.stopPropagation()}>
      {canPreview && (
        <Tooltip label="Podgląd" hasArrow>
          <IconButton
            aria-label="Podgląd"
            icon={<Eye size={16} aria-hidden="true" />}
            size="sm"
            variant="ghost"
            colorScheme="gray"
            minH="44px"
            minW="44px"
            onClick={handleOpenLatest}
          />
        </Tooltip>
      )}
      {hasKebabActions && (
        <Menu>
          <MenuButton
            as={IconButton}
            aria-label="Więcej akcji"
            icon={<MoreVertical size={16} aria-hidden="true" />}
            size="sm"
            variant="ghost"
            colorScheme="gray"
            minH="44px"
            minW="44px"
          />
          <MenuList>
            {canDownload && (
              <MenuItem
                icon={<Download size={14} aria-hidden="true" />}
                onClick={() => onDownload(fileId, file.currentVersion.sasUrlDownload)}
              >
                Pobierz
              </MenuItem>
            )}
            {canEdit && (
              <MenuItem
                icon={<Plus size={14} aria-hidden="true" />}
                onClick={() => onOpenUploadVersion(file)}
              >
                Nowa wersja
              </MenuItem>
            )}
            {canShare && (
              <MenuItem
                icon={<Share2 size={14} aria-hidden="true" />}
                onClick={() => onOpenManageShare(file)}
              >
                Udostępnij
              </MenuItem>
            )}
            {canShowVersions && (
              <MenuItem
                icon={
                  isVersionsExpanded ? (
                    <ChevronUp size={14} aria-hidden="true" />
                  ) : (
                    <ChevronDown size={14} aria-hidden="true" />
                  )
                }
                onClick={() => onToggleVersions(fileId)}
              >
                Wersje ({file.totalVersions})
              </MenuItem>
            )}
          </MenuList>
        </Menu>
      )}
    </HStack>
  );

  const versionsPanel = isVersionsExpanded && (
    <Box bg={isCard ? "neutral.50" : expandedBg} p={isCard ? 3 : 4} mt={isCard ? 3 : 0} borderRadius={isCard ? "md" : undefined}>
      {versionsLoading ? (
        <LoadingSpinner />
      ) : (
        <VStack align="stretch" spacing={3}>
          <Heading size="sm" mb={2}>
            Historia wersji ({file.totalVersions})
          </Heading>
          {(versions ?? []).map((version: any) => {
            const commentKey = `${fileId}-${version.id}`;
            const isCommentsExpanded = expandedCommentKeys.has(commentKey);
            const isCurrent = version.id === file.currentVersion?.id;
            const canPreviewVersion = isPreviewSupported(version.contentType);
            const handleOpenVersion = () => {
              if (canPreviewVersion) {
                onPreview(version.sasUrlView);
              }
            };

            return (
              <Box
                key={version.id}
                borderWidth="1px"
                borderRadius="md"
                p={3}
                bg="white"
                borderColor={isCurrent ? "neutral.400" : "neutral.200"}
              >
                <Box
                  display={{ base: "block", md: "none" }}
                  cursor={canPreviewVersion ? "pointer" : "default"}
                  _hover={canPreviewVersion ? { bg: "neutral.50" } : undefined}
                  borderRadius="md"
                  mx={-1}
                  px={1}
                  onClick={handleOpenVersion}
                  role={canPreviewVersion ? "button" : undefined}
                  tabIndex={canPreviewVersion ? 0 : undefined}
                  aria-label={
                    canPreviewVersion
                      ? `Podgląd wersji ${version.versionNumber}`
                      : undefined
                  }
                  onKeyDown={(e) => {
                    if (!canPreviewVersion) return;
                    if (e.key === "Enter" || e.key === " ") {
                      e.preventDefault();
                      handleOpenVersion();
                    }
                  }}
                >
                  <HStack justify="space-between" align="flex-start" spacing={2}>
                    <VStack align="flex-start" spacing={1} flex={1} minW={0}>
                      <Wrap spacing={2}>
                        <WrapItem>
                          <Badge
                            bg={isCurrent ? "primary.50" : "neutral.50"}
                            color={isCurrent ? "primary.600" : "neutral.600"}
                            borderWidth="1px"
                            borderColor={isCurrent ? "primary.200" : "neutral.200"}
                          >
                            Wersja {version.versionNumber}
                            {isCurrent && " (Aktualna)"}
                          </Badge>
                        </WrapItem>
                        <WrapItem>
                          <Badge colorScheme="neutral" fontSize="xs">
                            {version.contentType?.split("/")[1]?.toUpperCase() || "FILE"}
                          </Badge>
                        </WrapItem>
                        <WrapItem>
                          <Text fontSize="xs" color="neutral.600">
                            {formatFileSize(version.fileSizeBytes)}
                          </Text>
                        </WrapItem>
                      </Wrap>
                      <HStack spacing={4} fontSize="xs" color="neutral.600" flexWrap="wrap">
                        <HStack spacing={1}>
                          <User size={12} aria-hidden="true" />
                          <Text>{version.createdByUserName}</Text>
                        </HStack>
                        <HStack spacing={1}>
                          <Clock size={12} aria-hidden="true" />
                          <Text>{formatDate(version.createdAt)}</Text>
                        </HStack>
                      </HStack>
                    </VStack>
                    <HStack spacing={0} flexShrink={0} onClick={(e) => e.stopPropagation()}>
                      {canPreviewVersion && (
                        <Tooltip label="Podgląd" hasArrow>
                          <IconButton
                            aria-label="Podgląd"
                            icon={<Eye size={14} aria-hidden="true" />}
                            size="sm"
                            variant="ghost"
                            colorScheme="gray"
                            minH="44px"
                            minW="44px"
                            onClick={handleOpenVersion}
                          />
                        </Tooltip>
                      )}
                      <Menu>
                        <MenuButton
                          as={IconButton}
                          aria-label="Więcej akcji wersji"
                          icon={<MoreVertical size={14} aria-hidden="true" />}
                          size="sm"
                          variant="ghost"
                          colorScheme="gray"
                          minH="44px"
                          minW="44px"
                        />
                        <MenuList>
                          <MenuItem
                            icon={<Download size={14} aria-hidden="true" />}
                            onClick={() => onDownload(fileId, version.sasUrlDownload)}
                          >
                            Pobierz
                          </MenuItem>
                          <MenuItem
                            icon={<MessageSquare size={14} aria-hidden="true" />}
                            onClick={() => onToggleVersionComments(fileId, version.id)}
                          >
                            Komentarze
                          </MenuItem>
                        </MenuList>
                      </Menu>
                    </HStack>
                  </HStack>
                </Box>

                <Box display={{ base: "block", md: "none" }} mt={2} onClick={(e) => e.stopPropagation()}>
                  {isCommentsExpanded && (
                    <VersionCommentsSection
                      tenantId={tenantId}
                      projectId={projectId}
                      fileId={fileId}
                      versionId={version.id}
                      scope={scope}
                      isExpanded={isCommentsExpanded}
                      canEdit={canEdit}
                      currentUserId={currentUserId}
                      newComment={newComments.get(commentKey) || ""}
                      onCommentChange={(val) => onCommentChange(commentKey, val)}
                      onSubmitComment={() =>
                        onSubmitComment(file.id, version.projectFileId ?? file.id, version.id)
                      }
                      isSubmitting={submittingComment === commentKey}
                      highlightCommentId={highlightCommentId}
                    />
                  )}
                </Box>

                <HStack justify="space-between" mb={2} display={{ base: "none", md: "flex" }} flexWrap="wrap" gap={2}>
                  <HStack spacing={2} flexWrap="wrap">
                    <Badge
                      bg={isCurrent ? "primary.50" : "neutral.50"}
                      color={isCurrent ? "primary.600" : "neutral.600"}
                      borderWidth="1px"
                      borderColor={isCurrent ? "primary.200" : "neutral.200"}
                    >
                      Wersja {version.versionNumber}
                      {isCurrent && " (Aktualna)"}
                    </Badge>
                    <Badge colorScheme="neutral" fontSize="xs">
                      {version.contentType?.split("/")[1]?.toUpperCase() || "FILE"}
                    </Badge>
                    <Text fontSize="xs" color="neutral.600">
                      {formatFileSize(version.fileSizeBytes)}
                    </Text>
                  </HStack>
                  <HStack spacing={1}>
                    {canPreviewVersion && (
                      <Tooltip label="Podgląd" hasArrow>
                        <IconButton
                          aria-label="Podgląd"
                          icon={<Eye size={14} aria-hidden="true" />}
                          size="xs"
                          colorScheme="level2"
                          onClick={() => onPreview(version.sasUrlView)}
                        />
                      </Tooltip>
                    )}
                    <Button
                      size="xs"
                      leftIcon={<Download size={14} aria-hidden="true" />}
                      onClick={() => onDownload(fileId, version.sasUrlDownload)}
                    >
                      Pobierz
                    </Button>
                  </HStack>
                </HStack>

                <HStack
                  spacing={4}
                  fontSize="xs"
                  color="neutral.600"
                  mb={2}
                  flexWrap="wrap"
                  display={{ base: "none", md: "flex" }}
                >
                  <HStack spacing={1}>
                    <User size={12} aria-hidden="true" />
                    <Text>{version.createdByUserName}</Text>
                  </HStack>
                  <HStack spacing={1}>
                    <Clock size={12} aria-hidden="true" />
                    <Text>{formatDate(version.createdAt)}</Text>
                  </HStack>
                </HStack>

                <Box mt={3} display={{ base: "none", md: "block" }}>
                  <Button
                    size="sm"
                    variant="ghost"
                    leftIcon={<MessageSquare size={14} aria-hidden="true" />}
                    onClick={() => onToggleVersionComments(fileId, version.id)}
                    rightIcon={isCommentsExpanded ? <ChevronUp size={14} aria-hidden="true" /> : <ChevronDown size={14} aria-hidden="true" />}
                  >
                    Komentarze
                  </Button>

                  <VersionCommentsSection
                    tenantId={tenantId}
                    projectId={projectId}
                    fileId={fileId}
                    versionId={version.id}
                    scope={scope}
                    isExpanded={isCommentsExpanded}
                    canEdit={canEdit}
                    currentUserId={currentUserId}
                    newComment={newComments.get(commentKey) || ""}
                    onCommentChange={(val) => onCommentChange(commentKey, val)}
                    onSubmitComment={() =>
                      onSubmitComment(file.id, version.projectFileId ?? file.id, version.id)
                    }
                    isSubmitting={submittingComment === commentKey}
                    highlightCommentId={highlightCommentId}
                  />
                </Box>
              </Box>
            );
          })}
        </VStack>
      )}
    </Box>
  );

  if (isCard) {
    return (
      <Box
        borderWidth="1px"
        borderColor="neutral.200"
        borderRadius="md"
        bg="white"
        p={3}
        mb={2}
      >
        <Box
          cursor={canPreview ? "pointer" : "default"}
          _hover={canPreview ? { bg: "neutral.50" } : undefined}
          borderRadius="md"
          mx={-1}
          px={1}
          onClick={handleOpenLatest}
          role={canPreview ? "button" : undefined}
          tabIndex={canPreview ? 0 : undefined}
          aria-label={canPreview ? `Podgląd pliku ${file.displayName}` : undefined}
          onKeyDown={(e) => {
            if (!canPreview) return;
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              handleOpenLatest();
            }
          }}
        >
          <HStack justify="space-between" align="flex-start" spacing={2}>
            <VStack align="flex-start" spacing={1} flex={1} minW={0}>
              {fileNameBlock}
              {(showOwner || file.currentVersion) && (
                <Text fontSize="xs" color="neutral.600">
                  {showOwner && (file.originalOwnerUserName || file.ownerName)
                    ? `${file.originalOwnerUserName || file.ownerName}`
                    : null}
                  {showOwner && (file.originalOwnerUserName || file.ownerName) && file.currentVersion
                    ? " · "
                    : null}
                  {file.currentVersion ? formatFileSize(file.currentVersion.fileSizeBytes) : null}
                </Text>
              )}
            </VStack>
            {mobileFileActions}
          </HStack>
        </Box>
        {versionsPanel}
      </Box>
    );
  }

  return (
    <React.Fragment>
      <Tr>
        <Td>{fileNameBlock}</Td>
        {showOwner && (
          <Td display={{ base: "none", md: "table-cell" }} fontSize="sm">
            {file.originalOwnerUserName || file.ownerName || "-"}
          </Td>
        )}
        <Td display={{ base: "none", md: "table-cell" }} fontSize="sm">
          {file.currentVersion ? formatFileSize(file.currentVersion.fileSizeBytes) : "-"}
        </Td>
        <Td>{desktopFileActions}</Td>
      </Tr>

      {isVersionsExpanded && (
        <Tr key={`${fileId}-versions`}>
          <Td colSpan={showOwner ? 4 : 3} p={0}>
            {versionsPanel}
          </Td>
        </Tr>
      )}
    </React.Fragment>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// PackageFiles — lazy load plików w paczce gdy expanded
// ─────────────────────────────────────────────────────────────────────────────

interface PackageFilesProps {
  tenantId: string;
  projectId: string;
  packageId: string;
  scope: ResourceScope;
  isExpanded: boolean;
  isShared: boolean;
  showOwner: boolean;
  expandedVersionFileIds: Set<string>;
  expandedCommentKeys: Set<string>;
  resourcePerms: ResourcePermissions;
  currentUserId: string | undefined;
  onToggleVersions: (fileId: string) => void;
  onToggleVersionComments: (fileId: string, versionId: string) => void;
  onPreview: (sasUrlView: string) => void;
  onDownload: (fileId: string, sasUrlDownload: string) => void;
  onOpenUploadVersion: (file: any) => void;
  onOpenManageShare: (file: any) => void;
  newComments: Map<string, string>;
  onCommentChange: (commentKey: string, value: string) => void;
  onSubmitComment: (keyFileId: string, apiFileId: string, versionId: string) => void;
  submittingComment: string | null;
  highlightCommentId?: string | null;
  layout?: "table" | "card";
}

const PackageFiles: React.FC<PackageFilesProps> = ({
  tenantId,
  projectId,
  packageId,
  scope,
  isExpanded,
  isShared,
  showOwner,
  expandedVersionFileIds,
  expandedCommentKeys,
  resourcePerms,
  currentUserId,
  onToggleVersions,
  onToggleVersionComments,
  onPreview,
  onDownload,
  onOpenUploadVersion,
  onOpenManageShare,
  newComments,
  onCommentChange,
  onSubmitComment,
  submittingComment,
  highlightCommentId,
  layout = "table",
}) => {
  const { data: files, isLoading } = usePackageFiles(
    tenantId,
    projectId,
    packageId,
    scope,
    isExpanded
  );

  if (!isExpanded) return null;

  if (isLoading) {
    if (layout === "card") {
      return (
        <Box textAlign="center" py={4}>
          <LoadingSpinner />
        </Box>
      );
    }
    return (
      <Tr>
        <Td colSpan={showOwner ? 4 : 3} textAlign="center" py={4}>
          <LoadingSpinner />
        </Td>
      </Tr>
    );
  }

  const fileRows = (files ?? []).map((file: any) => (
    <FileRow
      key={file.id}
      file={file}
      tenantId={tenantId}
      projectId={projectId}
      scope={scope}
      isShared={isShared}
      showOwner={showOwner}
      isVersionsExpanded={expandedVersionFileIds.has(file.id)}
      expandedCommentKeys={expandedCommentKeys}
      resourcePerms={resourcePerms}
      currentUserId={currentUserId}
      onToggleVersions={onToggleVersions}
      onToggleVersionComments={onToggleVersionComments}
      onPreview={onPreview}
      onDownload={onDownload}
      onOpenUploadVersion={onOpenUploadVersion}
      onOpenManageShare={onOpenManageShare}
      newComments={newComments}
      onCommentChange={onCommentChange}
      onSubmitComment={onSubmitComment}
      submittingComment={submittingComment}
      highlightCommentId={highlightCommentId}
      layout={layout}
    />
  ));

  if (layout === "card") {
    return <VStack align="stretch" spacing={0}>{fileRows}</VStack>;
  }

  return <>{fileRows}</>;
};

// ─────────────────────────────────────────────────────────────────────────────
// DirectoryNode — rekurencyjny węzeł katalogu w drzewie
// ─────────────────────────────────────────────────────────────────────────────

interface DirectoryNodeProps {
  catalog: ProjectFilePackageWeb;
  depth: number;
  config: {
    packageIconColor: string;
    badgeColor: string;
    isShared: boolean;
    showOwner: boolean;
    showOwnerInPackage: boolean;
    ownerLabel?: string;
  };
  tenantId: string;
  projectId: string;
  resourceScope: ResourceScope;
  expandedPackageIds: Set<string>;
  expandedVersionFileIds: Set<string>;
  expandedCommentKeys: Set<string>;
  resourcePerms: ResourcePermissions;
  currentUserId: string | undefined;
  onTogglePackage: (packageId: string) => void;
  onToggleVersions: (fileId: string) => void;
  onToggleVersionComments: (fileId: string, versionId: string) => void;
  onPreview: (sasUrlView: string) => void;
  onDownload: (fileId: string, sasUrlDownload: string) => void;
  onOpenUploadVersion: (file: any) => void;
  onOpenManageShare: (file: any) => void;
  newComments: Map<string, string>;
  onCommentChange: (commentKey: string, value: string) => void;
  onSubmitComment: (keyFileId: string, apiFileId: string, versionId: string) => void;
  submittingComment: string | null;
  onCreateDirectory?: (parentId: string | undefined) => void;
  onUploadFiles?: (catalogId: string) => void;
  highlightCommentId?: string | null;
}

const DirectoryNode: React.FC<DirectoryNodeProps> = ({
  catalog,
  depth,
  config,
  tenantId,
  projectId,
  resourceScope,
  expandedPackageIds,
  expandedVersionFileIds,
  expandedCommentKeys,
  resourcePerms,
  currentUserId,
  onTogglePackage,
  onToggleVersions,
  onToggleVersionComments,
  onPreview,
  onDownload,
  onOpenUploadVersion,
  onOpenManageShare,
  newComments,
  onCommentChange,
  onSubmitComment,
  submittingComment,
  onCreateDirectory,
  onUploadFiles,
  highlightCommentId,
}) => {
  const isExpanded = expandedPackageIds.has(catalog.id);
  const isMobile = useBreakpointValue({ base: true, md: false }) ?? false;
  const filesLayout: "table" | "card" = isMobile ? "card" : "table";

  const packageFilesProps = {
    tenantId,
    projectId,
    packageId: catalog.id,
    scope: resourceScope,
    isExpanded,
    isShared: config.isShared,
    showOwner: config.showOwner,
    expandedVersionFileIds,
    expandedCommentKeys,
    resourcePerms,
    currentUserId,
    onToggleVersions,
    onToggleVersionComments,
    onPreview,
    onDownload,
    onOpenUploadVersion,
    onOpenManageShare,
    newComments,
    onCommentChange,
    onSubmitComment,
    submittingComment,
    highlightCommentId,
    layout: filesLayout,
  };

  return (
    <Box
      ml={{
        base: depth > 0 ? 2 : 0,
        md: depth > 0 ? depth * 6 : 0,
      }}
      mb={2}
    >
      <Accordion allowMultiple index={isExpanded ? [0] : []}>
        <AccordionItem bg="white" borderWidth="1px" borderColor="neutral.200" rounded="md">
          <AccordionButton
            py={3}
            _hover={{ bg: "neutral.50" }}
            onClick={() => onTogglePackage(catalog.id)}
            alignItems="flex-start"
          >
            <VStack flex="1" align="stretch" spacing={2} minW={0} pr={2}>
              <HStack spacing={2} minW={0}>
                <Icon
                  as={isExpanded ? FolderOpen : Folder}
                  boxSize={4}
                  color={config.packageIconColor}
                  aria-hidden="true"
                  flexShrink={0}
                />
                <Text fontWeight="semibold" fontSize="md" noOfLines={1} isTruncated minW={0} flex="1" textAlign="left">
                  {catalog.name}
                </Text>
                <Badge colorScheme={config.badgeColor} fontSize="sm" flexShrink={0}>
                  {catalog.totalFiles}
                </Badge>
                {config.showOwnerInPackage && catalog.ownerName && (
                  <Text
                    fontSize="sm"
                    color="neutral.600"
                    display={{ base: "none", md: "block" }}
                    noOfLines={1}
                    flexShrink={1}
                  >
                    {config.ownerLabel}: {catalog.ownerName}
                  </Text>
                )}
              </HStack>
              {config.showOwnerInPackage && catalog.ownerName && (
                <Text
                  fontSize="xs"
                  color="neutral.600"
                  display={{ base: "block", md: "none" }}
                  pl={6}
                  textAlign="left"
                  noOfLines={1}
                >
                  {config.ownerLabel}: {catalog.ownerName}
                </Text>
              )}
              {(onUploadFiles || onCreateDirectory) && (
                <HStack
                  spacing={2}
                  justify="flex-start"
                  pl={{ base: 6, md: 0 }}
                  onClick={(e: React.MouseEvent) => e.stopPropagation()}
                >
                  {onUploadFiles && (
                    <IconButton
                      as="span"
                      aria-label="Dodaj pliki do katalogu"
                      icon={<Upload size={16} aria-hidden="true" />}
                      size="sm"
                      variant="ghost"
                      colorScheme="primary"
                      minH="44px"
                      minW="44px"
                      onClick={(e: React.MouseEvent) => {
                        e.stopPropagation();
                        onUploadFiles(catalog.id);
                      }}
                    />
                  )}
                  {onCreateDirectory && (
                    <IconButton
                      as="span"
                      aria-label="Dodaj podkatalog"
                      icon={<FolderPlus size={16} aria-hidden="true" />}
                      size="sm"
                      variant="ghost"
                      minH="44px"
                      minW="44px"
                      onClick={(e: React.MouseEvent) => {
                        e.stopPropagation();
                        onCreateDirectory(catalog.id);
                      }}
                    />
                  )}
                </HStack>
              )}
            </VStack>
            <AccordionIcon flexShrink={0} mt={1} />
          </AccordionButton>
          <AccordionPanel pb={4} px={{ base: 2, md: 4 }}>
            {(catalog.subCatalogs?.length ?? 0) > 0 && (
              <Box mb={3}>
                {catalog.subCatalogs.map((sub) => (
                  <DirectoryNode
                    key={sub.id}
                    catalog={sub}
                    depth={depth + 1}
                    config={config}
                    tenantId={tenantId}
                    projectId={projectId}
                    resourceScope={resourceScope}
                    expandedPackageIds={expandedPackageIds}
                    expandedVersionFileIds={expandedVersionFileIds}
                    expandedCommentKeys={expandedCommentKeys}
                    resourcePerms={resourcePerms}
                    currentUserId={currentUserId}
                    onTogglePackage={onTogglePackage}
                    onToggleVersions={onToggleVersions}
                    onToggleVersionComments={onToggleVersionComments}
                    onPreview={onPreview}
                    onDownload={onDownload}
                    onOpenUploadVersion={onOpenUploadVersion}
                    onOpenManageShare={onOpenManageShare}
                    newComments={newComments}
                    onCommentChange={onCommentChange}
                    onSubmitComment={onSubmitComment}
                    submittingComment={submittingComment}
                    onCreateDirectory={onCreateDirectory}
                    onUploadFiles={onUploadFiles}
                    highlightCommentId={highlightCommentId}
                  />
                ))}
              </Box>
            )}
            {isMobile ? (
              <PackageFiles {...packageFilesProps} />
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
                    <PackageFiles {...packageFilesProps} />
                  </Tbody>
                </Table>
              </Box>
            )}
          </AccordionPanel>
        </AccordionItem>
      </Accordion>
    </Box>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// FilesTab — zakładka per scope (all/mine/shared)
// ─────────────────────────────────────────────────────────────────────────────

interface FilesTabProps {
  scope: FilesTabScope;
  resourceScope: ResourceScope;
  packages: ProjectFilePackageWeb[];
  resourcePerms: ResourcePermissions;
  tenantId: string;
  projectId: string;
  currentUserId: string | undefined;
  expandedPackageIds: Set<string>;
  expandedVersionFileIds: Set<string>;
  expandedCommentKeys: Set<string>;
  onTogglePackage: (packageId: string) => void;
  onToggleVersions: (fileId: string) => void;
  onToggleVersionComments: (fileId: string, versionId: string) => void;
  onPreview: (sasUrlView: string) => void;
  onDownload: (fileId: string, sasUrlDownload: string) => void;
  onOpenUploadVersion: (file: any) => void;
  onOpenManageShare: (file: any) => void;
  newComments: Map<string, string>;
  onCommentChange: (commentKey: string, value: string) => void;
  onSubmitComment: (keyFileId: string, apiFileId: string, versionId: string) => void;
  submittingComment: string | null;
  onShareFilesModalOpen?: () => void;
  onUploadModalOpen?: () => void;
  onCreateDirectory?: (parentId: string | undefined) => void;
  onUploadFiles?: (catalogId: string) => void;
  highlightCommentId?: string | null;
}

const FilesTab: React.FC<FilesTabProps> = ({
  scope,
  resourceScope,
  packages,
  resourcePerms,
  tenantId,
  projectId,
  currentUserId,
  expandedPackageIds,
  expandedVersionFileIds,
  expandedCommentKeys,
  onTogglePackage,
  onToggleVersions,
  onToggleVersionComments,
  onPreview,
  onDownload,
  onOpenUploadVersion,
  onOpenManageShare,
  newComments,
  onCommentChange,
  onSubmitComment,
  submittingComment,
  onShareFilesModalOpen,
  onUploadModalOpen,
  onCreateDirectory,
  onUploadFiles,
  highlightCommentId,
}) => {
  const config = SCOPE_CONFIG[scope];
  const perms = scope === "all" ? resourcePerms.all
    : scope === "mine" ? resourcePerms.mine
    : null;

  return (
    <VStack spacing={4} align="stretch">
      <VStack align="stretch" spacing={3} display={{ base: "flex", md: "none" }}>
        <Text fontSize="sm" color="neutral.600">
          {config.description}
        </Text>
        <Wrap spacing={2}>
          {onCreateDirectory && perms?.canCreate && (
            <WrapItem>
              <Button
                leftIcon={<FolderPlus size={16} aria-hidden="true" />}
                onClick={() => onCreateDirectory(undefined)}
                variant="outline"
                size="sm"
                minH="44px"
              >
                Dodaj katalog
              </Button>
            </WrapItem>
          )}
          {onShareFilesModalOpen && perms?.canShare && (
            <WrapItem>
              <Button
                leftIcon={<Share2 size={18} aria-hidden="true" />}
                colorScheme="gray"
                variant="outline"
                size="sm"
                minH="44px"
                onClick={onShareFilesModalOpen}
              >
                Udostępnij grupowo
              </Button>
            </WrapItem>
          )}
          {onUploadModalOpen && perms?.canCreate && (
            <WrapItem>
              <Button
                leftIcon={<Upload size={18} aria-hidden="true" />}
                colorScheme="primary"
                size="sm"
                minH="44px"
                onClick={onUploadModalOpen}
              >
                Dodaj pliki
              </Button>
            </WrapItem>
          )}
        </Wrap>
      </VStack>

      <HStack justify="space-between" display={{ base: "none", md: "flex" }} flexWrap="wrap" gap={2}>
        <Text fontSize="sm" color="neutral.600">
          {config.description}
        </Text>
        <HStack spacing={2} flexWrap="wrap">
          {onCreateDirectory && perms?.canCreate && (
            <Button
              leftIcon={<FolderPlus size={16} aria-hidden="true" />}
              onClick={() => onCreateDirectory(undefined)}
              variant="outline"
              size="sm"
            >
              Dodaj katalog
            </Button>
          )}
          {onShareFilesModalOpen && perms?.canShare && (
            <Button
              leftIcon={<Share2 size={18} aria-hidden="true" />}
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
              leftIcon={<Upload size={18} aria-hidden="true" />}
              colorScheme="primary"
              size="sm"
              onClick={onUploadModalOpen}
            >
              Dodaj pliki
            </Button>
          )}
        </HStack>
      </HStack>

      {packages.length === 0 ? (
        <EmptyState
          icon={config.emptyIcon}
          title={config.emptyTitle}
          description={config.emptyDescription}
        />
      ) : (
        <VStack spacing={0} align="stretch">
          {packages.map((pkg) => (
            <DirectoryNode
              key={pkg.id}
              catalog={pkg}
              depth={0}
              config={config}
              tenantId={tenantId}
              projectId={projectId}
              resourceScope={resourceScope}
              expandedPackageIds={expandedPackageIds}
              expandedVersionFileIds={expandedVersionFileIds}
              expandedCommentKeys={expandedCommentKeys}
              resourcePerms={resourcePerms}
              currentUserId={currentUserId}
              onTogglePackage={onTogglePackage}
              onToggleVersions={onToggleVersions}
              onToggleVersionComments={onToggleVersionComments}
              onPreview={onPreview}
              onDownload={onDownload}
              onOpenUploadVersion={onOpenUploadVersion}
              onOpenManageShare={onOpenManageShare}
              newComments={newComments}
              onCommentChange={onCommentChange}
              onSubmitComment={onSubmitComment}
              submittingComment={submittingComment}
              onCreateDirectory={onCreateDirectory}
              onUploadFiles={onUploadFiles}
              highlightCommentId={highlightCommentId}
            />
          ))}
        </VStack>
      )}
    </VStack>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// ProjectFiles — strona główna
// ─────────────────────────────────────────────────────────────────────────────

export default function ProjectFiles() {
  const { projectId } = useParams<{ projectId: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const { user } = useContext(AuthContext);
  const {showError, showWarning, showApiSuccess, showApiError } = useToastNotification();
  const { isOpen: isUploadModalOpen, onOpen: onUploadModalOpen, onClose: onUploadModalClose } = useDisclosure();
  const { isOpen: isUploadVersionModalOpen, onOpen: onUploadVersionModalOpen, onClose: onUploadVersionModalClose } = useDisclosure();
  const { isOpen: isManageShareModalOpen, onOpen: onManageShareModalOpen, onClose: onManageShareModalClose } = useDisclosure();
  const { isOpen: isShareFilesModalOpen, onOpen: onShareFilesModalOpen, onClose: onShareFilesModalClose } = useDisclosure();
  const { isOpen: isCreateDirOpen, onOpen: onCreateDirOpen, onClose: onCreateDirClose } = useDisclosure();

  const [activeTabIndex, setActiveTabIndex] = useState(0);
  const [expandedPackageIds, setExpandedPackageIds] = useState<Set<string>>(new Set());
  const [expandedVersionFileIds, setExpandedVersionFileIds] = useState<Set<string>>(new Set());
  const [expandedCommentKeys, setExpandedCommentKeys] = useState<Set<string>>(new Set());
  const [highlightCommentId, setHighlightCommentId] = useState<string | null>(null);
  const [fileForNewVersion, setFileForNewVersion] = useState<any | null>(null);
  const [fileToManageShare, setFileToManageShare] = useState<any | null>(null);
  const [newComments, setNewComments] = useState<Map<string, string>>(new Map());
  const [submittingComment, setSubmittingComment] = useState<string | null>(null);
  const [createDirParentId, setCreateDirParentId] = useState<string | undefined>(undefined);
  const [uploadTargetDirectoryId, setUploadTargetDirectoryId] = useState<string | undefined>(undefined);

  const queryClient = useQueryClient();

  const resourcePerms = useResourcePermissions(projectId);
  const isTabsFitted = useBreakpointValue({ base: true, md: false }) ?? false;

  // React Query — paczki plików per scope (lazy via `enabled`)
  const allFilesQuery = useFilePackages(
    user?.activeTenantId ?? undefined,
    projectId,
    ResourceScope.All,
    !resourcePerms.raw.loading && resourcePerms.tabs.showAll
  );
  const myFilesQuery = useFilePackages(
    user?.activeTenantId ?? undefined,
    projectId,
    ResourceScope.Mine,
    !resourcePerms.raw.loading && resourcePerms.tabs.showMine
  );
  const sharedFilesQuery = useFilePackages(
    user?.activeTenantId ?? undefined,
    projectId,
    ResourceScope.Shared,
    !resourcePerms.raw.loading && resourcePerms.tabs.showShared
  );

  // Derived loading dla pełnoekranowego spinnera (pierwsze ładowanie)
  const loading =
    resourcePerms.raw.loading ||
    (resourcePerms.tabs.showAll && allFilesQuery.isLoading) ||
    (resourcePerms.tabs.showMine && myFilesQuery.isLoading) ||
    (resourcePerms.tabs.showShared && sharedFilesQuery.isLoading);

  // Globalny cache dla project details (współdzielony między stronami projektu)
  const { data: project } = useProjectDetails(
    user?.activeTenantId ?? undefined,
    projectId
  );

  // Członkowie projektu — React Query (filtrujemy aktualnego użytkownika lokalnie)
  const { data: allMembers } = useProjectMembers(
    user?.activeTenantId ?? undefined,
    projectId
  );
  const members = useMemo(
    () => (allMembers ?? []).filter((m: ProjectMemberWeb) => m.userId !== user?.id),
    [allMembers, user?.id]
  );

  // useMemo dla danych aby zapobiec niepotrzebnym re-renderom tab components
  const allFilesData = useMemo(() => allFilesQuery.data || [], [allFilesQuery.data]);
  const myFilesData = useMemo(() => myFilesQuery.data || [], [myFilesQuery.data]);
  const sharedFilesData = useMemo(() => sharedFilesQuery.data || [], [sharedFilesQuery.data]);

  // Oblicz indeksy tabów - zapobiega niepotrzebnemu wywoływaniu useEffect
  const allFilesTabIndex = resourcePerms.tabs.showAll ? 0 : -1;
  const myFilesTabIndex =
    resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 1 :
      !resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 0 : -1;
  const sharedFilesTabIndex =
    resourcePerms.tabs.showAll && resourcePerms.tabs.showMine && resourcePerms.tabs.showShared ? 2 :
      (resourcePerms.tabs.showAll || resourcePerms.tabs.showMine) && resourcePerms.tabs.showShared ? 1 :
        !resourcePerms.tabs.showAll && !resourcePerms.tabs.showMine && resourcePerms.tabs.showShared ? 0 : -1;

  const getCurrentScope = (): ResourceScope => {
    if (activeTabIndex === allFilesTabIndex) return ResourceScope.All;
    if (activeTabIndex === myFilesTabIndex) return ResourceScope.Mine;
    if (activeTabIndex === sharedFilesTabIndex) return ResourceScope.Shared;
    return ResourceScope.Mine;
  };

  // Deep-link from notification: ?fileId=&packageId=&versionId=&commentId=
  useEffect(() => {
    if (loading || !projectId) {
      return;
    }

    const fileId = searchParams.get("fileId");
    const packageId = searchParams.get("packageId");
    const versionId = searchParams.get("versionId");
    const commentId = searchParams.get("commentId");

    if (!fileId || !packageId) {
      return;
    }

    const candidates: Array<{ tabIndex: number; packages: ProjectFilePackageWeb[] }> = [];
    if (sharedFilesTabIndex >= 0) {
      candidates.push({ tabIndex: sharedFilesTabIndex, packages: sharedFilesData });
    }
    if (myFilesTabIndex >= 0) {
      candidates.push({ tabIndex: myFilesTabIndex, packages: myFilesData });
    }
    if (allFilesTabIndex >= 0) {
      candidates.push({ tabIndex: allFilesTabIndex, packages: allFilesData });
    }

    let resolvedPath: string[] | null = null;
    let resolvedTabIndex: number | null = null;

    for (const candidate of candidates) {
      const path = findPackagePath(candidate.packages, packageId);
      if (path) {
        resolvedPath = path;
        resolvedTabIndex = candidate.tabIndex;
        break;
      }
    }

    if (!resolvedPath || resolvedTabIndex === null) {
      return;
    }

    setActiveTabIndex(resolvedTabIndex);
    setExpandedPackageIds(new Set(resolvedPath));
    setExpandedVersionFileIds(new Set([fileId]));

    if (versionId) {
      setExpandedCommentKeys(new Set([`${fileId}-${versionId}`]));
    }

    setHighlightCommentId(commentId);

    setSearchParams({}, { replace: true });
  }, [
    loading,
    projectId,
    searchParams,
    setSearchParams,
    sharedFilesTabIndex,
    myFilesTabIndex,
    allFilesTabIndex,
    sharedFilesData,
    myFilesData,
    allFilesData,
  ]);

  // === Refresh: invaliduje wszystkie zapytania domeny plików ===
  const refreshData = () => {
    queryClient.invalidateQueries({ queryKey: fileKeys.all });
    setExpandedPackageIds(new Set());
    setExpandedVersionFileIds(new Set());
    setExpandedCommentKeys(new Set());
  };

  const handleOpenCreateDirectory = (parentId: string | undefined) => {
    setCreateDirParentId(parentId);
    onCreateDirOpen();
  };

  const handleOpenUploadForDirectory = (catalogId: string) => {
    setUploadTargetDirectoryId(catalogId);
    onUploadModalOpen();
  };

  // === Toggle helpers (czyste manipulacje Set) ===
  const togglePackage = (packageId: string) => {
    setExpandedPackageIds((prev) => {
      const next = new Set(prev);
      if (next.has(packageId)) {
        next.delete(packageId);
      } else {
        next.add(packageId);
      }
      return next;
    });
  };

  const toggleFileVersionsLazy = (fileId: string) => {
    setExpandedVersionFileIds((prev) => {
      const next = new Set(prev);
      if (next.has(fileId)) {
        next.delete(fileId);
      } else {
        next.add(fileId);
      }
      return next;
    });
  };

  const toggleVersionComments = (fileId: string, versionId: string) => {
    const commentKey = `${fileId}-${versionId}`;
    setExpandedCommentKeys((prev) => {
      const next = new Set(prev);
      if (next.has(commentKey)) {
        next.delete(commentKey);
      } else {
        next.add(commentKey);
      }
      return next;
    });
  };

  const handleCommentChange = (commentKey: string, value: string) => {
    setNewComments((prev) => {
      const next = new Map(prev);
      next.set(commentKey, value);
      return next;
    });
  };

  const handlePreview = (sasUrlView: string) => {
    window.open(sasUrlView, "_blank", "noopener,noreferrer");
  };

  // Pobieranie pliku przez SAS URL — używamy ukrytego <a> zamiast window.open,
  // ponieważ window.open może być blokowany przez popup blockery.
  const handleDownloadFile = async (fileId: string, sasUrl: string) => {
    if (!user?.activeTenantId || !projectId) return;

    let downloadUrl = sasUrl;
    try {
      const sasExpiry = extractSasExpiry(sasUrl);
      if (!sasExpiry || sasExpiry <= new Date()) {
        // SAS wygasł — pobierz świeże wersje pliku przez React Query
        const scope = getCurrentScope();
        const freshVersions = await queryClient.fetchQuery({
          queryKey: fileKeys.fileVersions(user.activeTenantId, projectId, fileId, scope),
          queryFn: async () => {
            const res = await projectApi.getFileVersions(
              user.activeTenantId!,
              projectId,
              fileId,
              scope
            );
            return res.data;
          },
          staleTime: 0,
        });
        const freshVersion = freshVersions?.[0];
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

  const handleAddComment = async (keyFileId: string, apiFileId: string, versionId: string) => {
    if (!user?.activeTenantId || !projectId) return;

    const commentKey = `${keyFileId}-${versionId}`;
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
        apiFileId,
        versionId,
        comment.trim()
      );

      showApiSuccess("commentAdded");
      setNewComments((prev) => {
        const updated = new Map(prev);
        updated.delete(commentKey);
        return updated;
      });

      // Invaliduj komentarze tej wersji — RQ refetchuje automatycznie
      const scope = getCurrentScope();
      queryClient.invalidateQueries({
        queryKey: fileKeys.versionComments(
          user.activeTenantId,
          projectId,
          keyFileId,
          versionId,
          scope
        ),
      });
    } catch (error) {
      showApiError(error);
    } finally {
      setSubmittingComment(null);
    }
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

  const tenantId = user?.activeTenantId || "";

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
        <BackToProjectButton />
        <HStack justify="space-between" mb={{ base: 4, md: 8 }} flexWrap="wrap" gap={4}>
          <HStack spacing={3} minW={0}>
            <Icon as={FileText} boxSize={{ base: 6, md: 8 }} color="level2.600" aria-hidden="true" flexShrink={0} />
            <VStack align="flex-start" spacing={0} minW={0}>
              <Heading size={{ base: "md", md: "lg" }} noOfLines={1}>Pliki projektu</Heading>
              {project && <Text fontSize="sm" color="neutral.600" noOfLines={1}>{project.name}</Text>}
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
          <Tabs
            colorScheme="level2"
            variant="enclosed"
            index={activeTabIndex}
            onChange={setActiveTabIndex}
            isFitted={isTabsFitted}
          >
            <TabList
              overflowX="auto"
              overflowY="hidden"
              flexWrap="nowrap"
              css={{
                scrollbarWidth: "thin",
                "&::-webkit-scrollbar": { height: "4px" },
              }}
            >
              {resourcePerms.tabs.showAll && (
                <Tab fontWeight="bold" whiteSpace="nowrap" minH="44px" px={{ base: 2, md: 4 }}>
                  <HStack spacing={2}>
                    <Icon as={FileText} boxSize={4} aria-hidden="true" display={{ base: "none", sm: "block" }} />
                    <Text fontSize={{ base: "sm", md: "md" }}>Wszystkie</Text>
                    <Badge colorScheme="level2" ml={{ base: 0, md: 2 }}>
                      {(allFilesQuery.data || []).reduce((sum, pkg) => sum + pkg.totalFiles, 0)}
                    </Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showMine && (
                <Tab fontWeight="bold" whiteSpace="nowrap" minH="44px" px={{ base: 2, md: 4 }}>
                  <HStack spacing={2}>
                    <Icon as={FileText} boxSize={4} aria-hidden="true" display={{ base: "none", sm: "block" }} />
                    <Text fontSize={{ base: "sm", md: "md" }}>Moje</Text>
                    <Badge colorScheme="primary" ml={{ base: 0, md: 2 }}>
                      {(myFilesQuery.data || []).reduce((sum, pkg) => sum + pkg.totalFiles, 0)}
                    </Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showShared && (
                <Tab fontWeight="bold" whiteSpace="nowrap" minH="44px" px={{ base: 2, md: 4 }}>
                  <HStack spacing={2}>
                    <Icon as={Share2} boxSize={4} aria-hidden="true" display={{ base: "none", sm: "block" }} />
                    <Text fontSize={{ base: "sm", md: "md" }}>Udostępnione</Text>
                    <Badge colorScheme="action" ml={{ base: 0, md: 2 }}>
                      {(sharedFilesQuery.data || []).reduce((sum, pkg) => sum + pkg.totalFiles, 0)}
                    </Badge>
                  </HStack>
                </Tab>
              )}
            </TabList>

            <TabPanels>
              {resourcePerms.tabs.showAll && (
                <TabPanel>
                  {allFilesQuery.isLoading ? (
                    <LoadingSpinner />
                  ) : (
                    <FilesTab
                      scope="all"
                      resourceScope={ResourceScope.All}
                      packages={allFilesData}
                      resourcePerms={resourcePerms}
                      tenantId={tenantId}
                      projectId={projectId || ""}
                      currentUserId={user?.id}
                      expandedPackageIds={expandedPackageIds}
                      expandedVersionFileIds={expandedVersionFileIds}
                      expandedCommentKeys={expandedCommentKeys}
                      onTogglePackage={togglePackage}
                      onToggleVersions={toggleFileVersionsLazy}
                      onToggleVersionComments={toggleVersionComments}
                      onPreview={handlePreview}
                      onDownload={handleDownloadFile}
                      onOpenUploadVersion={openUploadVersionModal}
                      onOpenManageShare={openManageShareModal}
                      newComments={newComments}
                      onCommentChange={handleCommentChange}
                      onSubmitComment={handleAddComment}
                      submittingComment={submittingComment}
                      onShareFilesModalOpen={onShareFilesModalOpen}
                      onUploadModalOpen={onUploadModalOpen}
                      onCreateDirectory={handleOpenCreateDirectory}
                      onUploadFiles={handleOpenUploadForDirectory}
                      highlightCommentId={highlightCommentId}
                    />
                  )}
                </TabPanel>
              )}
              {resourcePerms.tabs.showMine && (
                <TabPanel>
                  {myFilesQuery.isLoading ? (
                    <LoadingSpinner />
                  ) : (
                    <FilesTab
                      scope="mine"
                      resourceScope={ResourceScope.Mine}
                      packages={myFilesData}
                      resourcePerms={resourcePerms}
                      tenantId={tenantId}
                      projectId={projectId || ""}
                      currentUserId={user?.id}
                      expandedPackageIds={expandedPackageIds}
                      expandedVersionFileIds={expandedVersionFileIds}
                      expandedCommentKeys={expandedCommentKeys}
                      onTogglePackage={togglePackage}
                      onToggleVersions={toggleFileVersionsLazy}
                      onToggleVersionComments={toggleVersionComments}
                      onPreview={handlePreview}
                      onDownload={handleDownloadFile}
                      onOpenUploadVersion={openUploadVersionModal}
                      onOpenManageShare={openManageShareModal}
                      newComments={newComments}
                      onCommentChange={handleCommentChange}
                      onSubmitComment={handleAddComment}
                      submittingComment={submittingComment}
                      onShareFilesModalOpen={onShareFilesModalOpen}
                      onUploadModalOpen={onUploadModalOpen}
                      onCreateDirectory={handleOpenCreateDirectory}
                      onUploadFiles={handleOpenUploadForDirectory}
                      highlightCommentId={highlightCommentId}
                    />
                  )}
                </TabPanel>
              )}
              {resourcePerms.tabs.showShared && (
                <TabPanel>
                  {sharedFilesQuery.isLoading ? (
                    <LoadingSpinner />
                  ) : (
                    <FilesTab
                      scope="shared"
                      resourceScope={ResourceScope.Shared}
                      packages={sharedFilesData}
                      resourcePerms={resourcePerms}
                      tenantId={tenantId}
                      projectId={projectId || ""}
                      currentUserId={user?.id}
                      expandedPackageIds={expandedPackageIds}
                      expandedVersionFileIds={expandedVersionFileIds}
                      expandedCommentKeys={expandedCommentKeys}
                      onTogglePackage={togglePackage}
                      onToggleVersions={toggleFileVersionsLazy}
                      onToggleVersionComments={toggleVersionComments}
                      onPreview={handlePreview}
                      onDownload={handleDownloadFile}
                      onOpenUploadVersion={openUploadVersionModal}
                      onOpenManageShare={openManageShareModal}
                      newComments={newComments}
                      onCommentChange={handleCommentChange}
                      onSubmitComment={handleAddComment}
                      submittingComment={submittingComment}
                      highlightCommentId={highlightCommentId}
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
            onClose={() => { setUploadTargetDirectoryId(undefined); onUploadModalClose(); }}
            projectId={projectId || ""}
            projectName={project?.name || ""}
            tenantId={tenantId}
            onFilesUploaded={refreshData}
            targetCatalogId={uploadTargetDirectoryId}
          />
        )}

        {fileForNewVersion && (
          <UploadNewVersionModal
            isOpen={isUploadVersionModalOpen}
            onClose={onUploadVersionModalClose}
            projectId={projectId || ""}
            tenantId={tenantId}
            file={fileForNewVersion}
            onVersionUploaded={handleVersionUploaded}
          />
        )}

        {fileToManageShare && (
          <ManageFileShareModal
            isOpen={isManageShareModalOpen}
            onClose={onManageShareModalClose}
            projectId={projectId || ""}
            tenantId={tenantId}
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
          tenantId={tenantId}
          onFilesShared={refreshData}
          myPackages={
            activeTabIndex === allFilesTabIndex
              ? allFilesQuery.data || undefined
              : myFilesQuery.data || undefined
          }
        />

        {isCreateDirOpen && (
          <CreateDirectoryModal
            isOpen={isCreateDirOpen}
            onClose={onCreateDirClose}
            onSuccess={() => {
              onCreateDirClose();
              refreshData();
            }}
            tenantId={tenantId}
            projectId={projectId || ""}
            catalogs={activeTabIndex === allFilesTabIndex ? allFilesData : myFilesData}
            defaultParentId={createDirParentId}
          />
        )}
      </Box>
    </MainLayout>
  );
}

