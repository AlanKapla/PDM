import { useState, useEffect, useContext } from "react";
import {
  VStack,
  Text,
  Alert,
  AlertIcon,
  Box,
  HStack,
  Divider,
  Checkbox,
} from "@chakra-ui/react";
import { User, Package } from "lucide-react";
import AppModal from "./ui/AppModal";
import { projectApi } from "../api/projectApi";
import { AuthContext } from "../context/AuthContext";
import type { ProjectMemberWeb, ProjectFilePackageWeb } from "../types/project.types";
import { useToastNotification } from "../hooks/useToastNotification";

interface ShareFilesModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  onFilesShared: () => void;
  myPackages?: ProjectFilePackageWeb[];
}

export default function ShareFilesModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  onFilesShared,
  myPackages,
}: ShareFilesModalProps) {
  const [packages, setPackages] = useState<ProjectFilePackageWeb[]>([]);
  const [selectedPackageIds, setSelectedPackageIds] = useState<Set<string>>(new Set());
  const [members, setMembers] = useState<ProjectMemberWeb[]>([]);
  const [selectedUserIds, setSelectedUserIds] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(false);
  const [loadingMembers, setLoadingMembers] = useState(false);
  const { showSuccess, showWarning, showError, showApiError } = useToastNotification();
  const { user } = useContext(AuthContext);

  useEffect(() => {
    if (isOpen) {
      setPackages(myPackages || []);
      fetchProjectMembers();
      setSelectedUserIds(new Set());
      setSelectedPackageIds(new Set());
    }
  }, [isOpen, tenantId, projectId]);

  useEffect(() => {
    if (isOpen) {
      setPackages(myPackages || []);
    }
  }, [myPackages, isOpen]);

  const fetchProjectMembers = async () => {
    try {
      setLoadingMembers(true);
      const response = await projectApi.getProjectMembers(tenantId, projectId);
      const data = response.data;
      const filteredMembers = data.filter((member: ProjectMemberWeb) =>
        member.userId !== user?.id && member.email !== user?.email
      );
      setMembers(filteredMembers);
    } catch {
      showError("Błąd", "Nie udało się pobrać listy członków projektu");
    } finally {
      setLoadingMembers(false);
    }
  };

  const findNode = (id: string, list: ProjectFilePackageWeb[]): ProjectFilePackageWeb | undefined => {
    for (const cat of list) {
      if (cat.id === id) return cat;
      const found = findNode(id, cat.subCatalogs || []);
      if (found) return found;
    }
    return undefined;
  };

  const collectAllIds = (node: ProjectFilePackageWeb): string[] => {
    const ids: string[] = [node.id];
    for (const sub of node.subCatalogs || []) {
      ids.push(...collectAllIds(sub));
    }
    return ids;
  };

  const togglePackageSelection = (packageId: string) => {
    const node = findNode(packageId, packages);
    const idsToToggle = node ? collectAllIds(node) : [packageId];
    setSelectedPackageIds((prev) => {
      const isSelected = prev.has(packageId);
      const next = new Set(prev);
      for (const id of idsToToggle) {
        if (isSelected) {
          next.delete(id);
        } else {
          next.add(id);
        }
      }
      return next;
    });
  };

  const toggleUserSelection = (userId: string) => {
    setSelectedUserIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(userId)) {
        newSet.delete(userId);
      } else {
        newSet.add(userId);
      }
      return newSet;
    });
  };

  const handleClose = () => {
    if (!loading) {
      onClose();
    }
  };

  const handleShare = async () => {
    if (selectedPackageIds.size === 0) {
      showWarning("Błąd", "Wybierz przynajmniej jeden katalog");
      return;
    }

    if (selectedUserIds.size === 0) {
      showWarning("Błąd", "Wybierz przynajmniej jednego użytkownika");
      return;
    }

    try {
      setLoading(true);

      const userIds = Array.from(selectedUserIds);
      const packageIds = Array.from(selectedPackageIds);

      await projectApi.sharePackages(tenantId, projectId, packageIds, userIds);

      showSuccess("Sukces", `Udostępniono ${packageIds.length} katalogów dla ${userIds.length} użytkownik(ów)`);
      onFilesShared();
      onClose();
    } catch (error) {
      showApiError(error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <AppModal
      isOpen={isOpen}
      onClose={handleClose}
      title="Udostępnij pliki grupowo"
      actionLabel={`Udostępnij (${selectedPackageIds.size} katalogów dla ${selectedUserIds.size})`}
      actionColorScheme="primary"
      onAction={handleShare}
      isActionLoading={loading}
      isActionDisabled={selectedPackageIds.size === 0 || selectedUserIds.size === 0 || loadingMembers}
      desktopSize="lg"
    >
      <VStack spacing={4} align="stretch">
        <Box>
          <Text fontWeight="bold" mb={2}>
            Wybierz katalogi do udostępnienia ({selectedPackageIds.size} wybrano):
          </Text>
          {packages.length === 0 ? (
            <Text fontSize="sm" color="neutral.600">
              Nie masz jeszcze żadnych katalogów do udostępnienia
            </Text>
          ) : (
            <VStack spacing={2} align="stretch" maxH={{ base: "30dvh", md: "300px" }} overflowY="auto">
              {packages.map((pkg) => {
                const isSelected = selectedPackageIds.has(pkg.id);

                return (
                  <Box
                    key={pkg.id}
                    borderWidth="1px"
                    borderRadius="md"
                    p={3}
                    bg={isSelected ? "primary.50" : "transparent"}
                    cursor="pointer"
                    role="button"
                    tabIndex={0}
                    onClick={() => togglePackageSelection(pkg.id)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" || e.key === " ") {
                        e.preventDefault();
                        togglePackageSelection(pkg.id);
                      }
                    }}
                    _hover={{ bg: isSelected ? "primary.100" : "neutral.50" }}
                    minH="44px"
                  >
                    <HStack spacing={2} align="flex-start">
                      <Checkbox
                        isChecked={isSelected}
                        onChange={(e) => {
                          e.stopPropagation();
                          togglePackageSelection(pkg.id);
                        }}
                        mt={0.5}
                      />
                      <Package size={16} aria-hidden="true" />
                      <VStack align="start" spacing={0} minW={0} flex="1">
                        <Text fontWeight="bold" fontSize="sm" noOfLines={2}>
                          {pkg.name}
                        </Text>
                        <Text fontSize="xs" color="neutral.600">
                          ({pkg.totalFiles} plików)
                        </Text>
                      </VStack>
                    </HStack>
                  </Box>
                );
              })}
            </VStack>
          )}
        </Box>

        <Alert status="info" fontSize="xs">
          <AlertIcon aria-hidden="true" />
          Udostępniasz katalogi wraz z podkatalogami. Wybrani członkowie otrzymają dostęp do wszystkich plików i podkatalogów w zaznaczonych katalogach.
        </Alert>

        <Divider />

        <Box>
          <Text fontWeight="bold" mb={2}>
            Udostępnij dla ({selectedUserIds.size}):
          </Text>
          {loadingMembers ? (
            <Text fontSize="sm" color="neutral.600">
              Ładowanie członków...
            </Text>
          ) : members.length === 0 ? (
            <Text fontSize="sm" color="neutral.600">
              Brak członków projektu do udostępnienia
            </Text>
          ) : (
            <VStack
              align="stretch"
              spacing={2}
              maxH={{ base: "30dvh", md: "200px" }}
              overflowY="auto"
              p={2}
              borderWidth="1px"
              borderRadius="md"
            >
              {members.map((member) => (
                <HStack
                  key={member.userId}
                  p={2}
                  borderRadius="md"
                  cursor="pointer"
                  role="button"
                  tabIndex={0}
                  minH="44px"
                  bg={selectedUserIds.has(member.userId) ? "primary.50" : "transparent"}
                  _hover={{ bg: selectedUserIds.has(member.userId) ? "primary.100" : "neutral.50" }}
                  onClick={() => toggleUserSelection(member.userId)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" || e.key === " ") {
                      e.preventDefault();
                      toggleUserSelection(member.userId);
                    }
                  }}
                >
                  <Checkbox
                    isChecked={selectedUserIds.has(member.userId)}
                    onChange={(e) => {
                      e.stopPropagation();
                      toggleUserSelection(member.userId);
                    }}
                  />
                  <User size={16} aria-hidden="true" />
                  <VStack align="start" spacing={0} flex="1" minW={0}>
                    <Text fontSize="sm" fontWeight="medium" noOfLines={1}>
                      {member.firstName} {member.lastName}
                    </Text>
                    <Text fontSize="xs" color="neutral.600" noOfLines={1}>
                      {member.email}
                    </Text>
                  </VStack>
                </HStack>
              ))}
            </VStack>
          )}
        </Box>

        <Alert status="info" fontSize="sm">
          <AlertIcon aria-hidden="true" />
          Wybrani członkowie otrzymają dostęp do wybranych katalogów i będą mogli je przeglądać oraz pobierać.
        </Alert>
      </VStack>
    </AppModal>
  );
}
