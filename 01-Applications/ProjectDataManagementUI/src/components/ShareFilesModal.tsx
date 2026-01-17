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
  useToast,
  Divider,
  Checkbox,
  Spinner,
} from "@chakra-ui/react";
import { Share2, User, Package } from "lucide-react";
import { projectApi, ResourceScope } from "../api/projectApi";
import { handleApiError } from "../utils/handleApiError";
import { AuthContext } from "../context/AuthContext";
import type { ProjectMemberWeb, ProjectFilePackageWeb } from "../types/project.types";

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
  const [selectedFileIds, setSelectedFileIds] = useState<Set<string>>(new Set());
  const [members, setMembers] = useState<ProjectMemberWeb[]>([]);
  const [selectedUserIds, setSelectedUserIds] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(false);
  const [loadingPackages, setLoadingPackages] = useState(false);
  const [loadingMembers, setLoadingMembers] = useState(false);
  const toast = useToast();
  const { user } = useContext(AuthContext);

  useEffect(() => {
    if (isOpen) {
      if (myPackages) {
        setPackages(myPackages);
      } else {
        fetchMyPackages();
      }
      fetchProjectMembers();
      setSelectedUserIds(new Set());
      setSelectedFileIds(new Set());
    }
  }, [isOpen, tenantId, projectId, myPackages]);

  const fetchMyPackages = async () => {
    try {
      setLoadingPackages(true);
      const response = await projectApi.getProjectFilePackages(tenantId, projectId, ResourceScope.Mine);
      const data: ProjectFilePackageWeb[] = response.data;
      setPackages(data);
    } catch (error) {
      console.error("Błąd podczas pobierania plików:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać listy plików",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setLoadingPackages(false);
    }
  };

  const fetchProjectMembers = async () => {
    try {
      setLoadingMembers(true);
      const response = await projectApi.getProjectMembers(tenantId, projectId);
      const data = response.data;
      // Wyklucz aktualnego użytkownika z listy
      const filteredMembers = data.filter((member: ProjectMemberWeb) => member.email !== user?.email);
      setMembers(filteredMembers);
    } catch (error) {
      console.error("Błąd podczas pobierania członków:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać listy członków projektu",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setLoadingMembers(false);
    }
  };

  const toggleFileSelection = (fileId: string) => {
    setSelectedFileIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(fileId)) {
        newSet.delete(fileId);
      } else {
        newSet.add(fileId);
      }
      return newSet;
    });
  };

  const togglePackageSelection = (pkg: ProjectFilePackageWeb) => {
    const allSelected = pkg.files.every(f => selectedFileIds.has(f.id));
    setSelectedFileIds((prev) => {
      const newSet = new Set(prev);
      pkg.files.forEach(f => {
        if (allSelected) {
          newSet.delete(f.id);
        } else {
          newSet.add(f.id);
        }
      });
      return newSet;
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
    if (selectedFileIds.size === 0) {
      toast({
        title: "Błąd",
        description: "Wybierz przynajmniej jeden plik",
        status: "warning",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    if (selectedUserIds.size === 0) {
      toast({
        title: "Błąd",
        description: "Wybierz przynajmniej jednego użytkownika",
        status: "warning",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    try {
      setLoading(true);
      const fileIds = Array.from(selectedFileIds);
      const userIds = Array.from(selectedUserIds);
      await projectApi.shareFiles(tenantId, projectId, fileIds, userIds);

      toast({
        title: "Sukces",
        description: `Udostępniono ${selectedFileIds.size} plik(ów) dla ${selectedUserIds.size} użytkownik(ów)`,
        status: "success",
        duration: 5000,
        isClosable: true,
      });
      onFilesShared();
      onClose();
    } catch (error) {
      console.error("Błąd podczas udostępniania plików:", error);
      const { title, description } = handleApiError(error);
      toast({
        title,
        description,
        status: "error",
        duration: 5000,
        isClosable: true,
      });
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
              <Share2 size={24} />
            </Box>
            <Box display={{ base: "block", md: "none" }}>
              <Share2 size={20} />
            </Box>
            <Text fontSize={{ base: "md", md: "lg" }}>Udostępnij pliki grupowo</Text>
          </HStack>
        </ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            {/* Wybór plików */}
            <Box>
              <Text fontWeight="bold" mb={2}>
                Wybierz pliki do udostępnienia ({selectedFileIds.size}):
              </Text>
              {loadingPackages ? (
                <HStack justify="center" py={4}>
                  <Spinner size="md" />
                  <Text fontSize="sm">Ładowanie plików...</Text>
                </HStack>
              ) : packages.length === 0 ? (
                <Text fontSize="sm" color="gray.500">
                  Nie masz jeszcze żadnych plików do udostępnienia
                </Text>
              ) : (
                <VStack spacing={2} align="stretch" maxH="300px" overflowY="auto">
                  {packages.map((pkg) => {
                    const allSelected = pkg.files.every(f => selectedFileIds.has(f.id));
                    const someSelected = pkg.files.some(f => selectedFileIds.has(f.id));
                    
                    return (
                      <Box key={pkg.id} borderWidth="1px" borderRadius="md" p={3}>
                        <HStack spacing={2} mb={2}>
                          <Checkbox
                            isChecked={allSelected}
                            isIndeterminate={someSelected && !allSelected}
                            onChange={() => togglePackageSelection(pkg)}
                          />
                          <Package size={16} />
                          <Text fontWeight="bold" fontSize="sm">{pkg.name}</Text>
                        </HStack>
                        <VStack align="stretch" spacing={1} ml={6}>
                          {pkg.files.map((file) => (
                            <HStack key={file.id} spacing={2}>
                              <Checkbox
                                isChecked={selectedFileIds.has(file.id)}
                                onChange={() => toggleFileSelection(file.id)}
                              />
                              <Text fontSize="sm">{file.displayName}</Text>
                            </HStack>
                          ))}
                        </VStack>
                      </Box>
                    );
                  })}
                </VStack>
              )}
            </Box>

            <Alert status="info" fontSize="xs">
              <AlertIcon />
              Udostępniasz pliki bazowe (wszystkie wersje). Członkowie będą mieli dostęp do wszystkich wersji wybranych plików.
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
                      bg={selectedUserIds.has(member.userId) ? "blue.50" : "transparent"}
                      _hover={{ bg: selectedUserIds.has(member.userId) ? "blue.100" : "gray.50" }}
                      onClick={() => toggleUserSelection(member.userId)}
                    >
                      <Checkbox
                        isChecked={selectedUserIds.has(member.userId)}
                        onChange={() => toggleUserSelection(member.userId)}
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
              Wybrani członkowie otrzymają dostęp do wybranych plików i będą mogli je przeglądać oraz pobierać.
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
            colorScheme="blue"
            onClick={handleShare}
            isLoading={loading}
            loadingText="Udostępnianie..."
            isDisabled={selectedFileIds.size === 0 || selectedUserIds.size === 0 || loadingMembers || loadingPackages}
            leftIcon={<Share2 size={18} />}
            width={{ base: "100%", md: "auto" }}
            order={{ base: 1, md: 2 }}
          >
            Udostępnij ({selectedFileIds.size} dla {selectedUserIds.size})
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
