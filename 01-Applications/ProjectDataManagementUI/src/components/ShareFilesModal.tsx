import { useState, useEffect, useContext } from "react";
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  Button,
  VStack,
  Text,
  Alert,
  AlertIcon,
  Box,
  HStack,
  Divider,
  Checkbox,
} from "@chakra-ui/react";
import { Share2, User, Package } from "lucide-react";
import { projectApi } from "../api/projectApi";
import { handleApiError } from "../utils/handleApiError";
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
  const {showSuccess, showError, showWarning, showInfo, toast, showApiError } = useToastNotification();
  const { user } = useContext(AuthContext);

  useEffect(() => {
    if (isOpen) {
      setPackages(myPackages || []);
      fetchProjectMembers();
      setSelectedUserIds(new Set());
      setSelectedPackageIds(new Set());
    }
  }, [isOpen, tenantId, projectId]);

  // Synchronizuj paczki z propsem — gdy rodzic zmieni źródło danych (np. zmiana zakładki)
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
      // Wyklucz aktualnego użytkownika z listy (sprawdź po userId lub email)
      const filteredMembers = data.filter((member: ProjectMemberWeb) => 
        member.userId !== user?.id && member.email !== user?.email
      );
      setMembers(filteredMembers);
    } catch (error) {
      showError("Błąd", "Nie udało się pobrać listy członków projektu");
    } finally {
      setLoadingMembers(false);
    }
  };

  // Helpers do kaskadowego zaznaczania
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

  // Zaznacz/odznacz katalog kaskadowo (wraz z podkatalogami)
  const togglePackageSelection = (packageId: string) => {
    const node = findNode(packageId, packages);
    const idsToToggle = node ? collectAllIds(node) : [packageId];
    setSelectedPackageIds(prev => {
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
    <Modal isOpen={isOpen} onClose={onClose} size={{ base: "full", md: "lg" }}>
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>
          <HStack spacing={2}>
            <Box display={{ base: "none", md: "block" }}>
              <Share2 size={24} aria-hidden="true" />
            </Box>
            <Box display={{ base: "block", md: "none" }}>
              <Share2 size={20} aria-hidden="true" />
            </Box>
            <Text fontSize={{ base: "md", md: "lg" }}>Udostępnij pliki grupowo</Text>
          </HStack>
        </ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            {/* Wybór paczek */}
            <Box>
              <Text fontWeight="bold" mb={2}>
                Wybierz katalogi do udostępnienia ({selectedPackageIds.size} wybrano):
              </Text>
              {packages.length === 0 ? (
                <Text fontSize="sm" color="gray.500">
                  Nie masz jeszcze żadnych katalogów do udostępnienia
                </Text>
              ) : (
                <VStack spacing={2} align="stretch" maxH="300px" overflowY="auto">
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
                        onClick={() => togglePackageSelection(pkg.id)}
                        _hover={{ bg: isSelected ? "primary.100" : "gray.50" }}
                      >
                        <HStack spacing={2}>
                          <Checkbox
                            isChecked={isSelected}
                            onChange={(e) => {
                              e.stopPropagation();
                              togglePackageSelection(pkg.id);
                            }}
                          />
                          <Package size={16} aria-hidden="true" />
                          <Text fontWeight="bold" fontSize="sm">{pkg.name}</Text>
                          <Text fontSize="xs" color="gray.500">({pkg.totalFiles} plików)</Text>
                        </HStack>
                      </Box>
                    );
                  })}
                </VStack>
              )}
            </Box>

            <Alert status="info" fontSize="xs">
              <AlertIcon />
              Udostępniasz katalogi wraz z podkatalogami. Wybrani członkowie otrzymają dostęp do wszystkich plików i podkatalogów w zaznaczonych katalogach.
            </Alert>

            <Divider />

            {/* Wybór użytkowników */}
            <Box>
              <Text fontWeight="bold" mb={2}>
                Udostępnij dla ({selectedUserIds.size}):
              </Text>
              {loadingMembers ? (
                <Text fontSize="sm" color="gray.500">
                  Ładowanie członków...
                </Text>
              ) : members.length === 0 ? (
                <Text fontSize="sm" color="gray.500">
                  Brak członków projektu do udostępnienia
                </Text>
              ) : (
                <VStack align="stretch" spacing={2} maxH="200px" overflowY="auto" p={2} borderWidth="1px" borderRadius="md">
                  {members.map((member) => (
                    <HStack
                      key={member.userId}
                      p={2}
                      borderRadius="md"
                      cursor="pointer"
                      bg={selectedUserIds.has(member.userId) ? "primary.50" : "transparent"}
                      _hover={{ bg: selectedUserIds.has(member.userId) ? "primary.100" : "gray.50" }}
                      onClick={() => toggleUserSelection(member.userId)}
                    >
                      <Checkbox
                        isChecked={selectedUserIds.has(member.userId)}
                        onChange={(e) => {
                          e.stopPropagation();
                          toggleUserSelection(member.userId);
                        }}
                      />
                      <User size={16} />
                      <VStack align="start" spacing={0} flex="1">
                        <Text fontSize="sm" fontWeight="medium">
                          {member.firstName} {member.lastName}
                        </Text>
                        <Text fontSize="xs" color="gray.600">
                          {member.email}
                        </Text>
                      </VStack>
                    </HStack>
                  ))}
                </VStack>
              )}
            </Box>

            <Alert status="info" fontSize="sm">
              <AlertIcon />
              Wybrani członkowie otrzymają dostęp do wybranych katalogów i będą mogli je przeglądać oraz pobierać.
            </Alert>
          </VStack>
        </ModalBody>
        <ModalFooter flexDirection={{ base: "column", md: "row" }} gap={2}>
          <Button 
            variant="ghost" 
            onClick={onClose}
            width={{ base: "100%", md: "auto" }}
            order={{ base: 2, md: 1 }}
          >
            Anuluj
          </Button>
          <Button
            colorScheme="primary"
            onClick={handleShare}
            isLoading={loading}
            loadingText="Udostępnianie..."
            isDisabled={selectedPackageIds.size === 0 || selectedUserIds.size === 0 || loadingMembers}
            leftIcon={<Share2 size={18} />}
            width={{ base: "100%", md: "auto" }}
            order={{ base: 1, md: 2 }}
          >
            Udostępnij ({selectedPackageIds.size} katalogów dla {selectedUserIds.size})
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
