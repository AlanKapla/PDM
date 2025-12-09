import { useState, useEffect } from "react";
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
  Select,
  Alert,
  AlertIcon,
  Box,
  Badge,
  HStack,
  useToast,
  Divider,
} from "@chakra-ui/react";
import { Share2, User } from "lucide-react";
import { projectApi } from "../api/projectApi";
import { useAuth } from "../hooks/useAuth";
import type { ProjectFileWeb, ProjectMemberWeb } from "../types/project.types";

interface ShareFilesModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  selectedFiles: ProjectFileWeb[];
  onFilesShared: () => void;
}

export default function ShareFilesModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  selectedFiles,
  onFilesShared,
}: ShareFilesModalProps) {
  const [members, setMembers] = useState<ProjectMemberWeb[]>([]);
  const [selectedUserId, setSelectedUserId] = useState("");
  const [loading, setLoading] = useState(false);
  const [loadingMembers, setLoadingMembers] = useState(false);
  const toast = useToast();
  const { user } = useAuth();

  useEffect(() => {
    if (isOpen) {
      fetchProjectMembers();
      setSelectedUserId("");
    }
  }, [isOpen, tenantId, projectId]);

  const fetchProjectMembers = async () => {
    try {
      setLoadingMembers(true);
      const response = await projectApi.getProjectMembers(tenantId, projectId);
      if (response.ok) {
        const data = await response.json();
        // Wyklucz aktualnego użytkownika z listy
        const filteredMembers = data.filter((member: ProjectMemberWeb) => member.email !== user?.email);
        setMembers(filteredMembers);
      } else {
        throw new Error("Nie udało się pobrać listy członków");
      }
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

  const handleShare = async () => {
    if (!selectedUserId) {
      toast({
        title: "Błąd",
        description: "Wybierz członka projektu",
        status: "warning",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    try {
      setLoading(true);
      const fileIds = selectedFiles.map((f) => f.id);
      const response = await projectApi.shareFiles(tenantId, projectId, fileIds, selectedUserId);

      if (response.ok) {
        const result = await response.json();
        
        if (result.successCount > 0) {
          toast({
            title: "Sukces",
            description: `Udostępniono ${result.successCount} plik(ów)`,
            status: "success",
            duration: 5000,
            isClosable: true,
          });
          onFilesShared();
          onClose();
        }

        if (result.failedCount > 0 && result.errors.length > 0) {
          toast({
            title: "Ostrzeżenie",
            description: `Nie udało się udostępnić ${result.failedCount} plik(ów): ${result.errors.join(", ")}`,
            status: "warning",
            duration: 7000,
            isClosable: true,
          });
        }
      } else if (response.status === 400) {
        // Bad Request - wyciągnij szczegółowe błędy z odpowiedzi
        try {
          const errorData = await response.json();
          
          // Sprawdź czy to struktura z errors array
          if (errorData.errors && Array.isArray(errorData.errors) && errorData.errors.length > 0) {
            toast({
              title: "Błąd walidacji",
              description: errorData.errors.join(", "),
              status: "error",
              duration: 7000,
              isClosable: true,
            });
          } 
          // Sprawdź czy to ModelState errors (format ASP.NET)
          else if (errorData.errors && typeof errorData.errors === 'object') {
            const allErrors = Object.values(errorData.errors).flat();
            toast({
              title: "Błąd walidacji",
              description: Array.isArray(allErrors) ? allErrors.join(", ") : "Nieprawidłowe dane",
              status: "error",
              duration: 7000,
              isClosable: true,
            });
          }
          // Sprawdź czy to prosty komunikat
          else if (errorData.message || errorData.title) {
            toast({
              title: "Błąd",
              description: errorData.message || errorData.title,
              status: "error",
              duration: 5000,
              isClosable: true,
            });
          }
          // Jeśli to zwykły tekst
          else if (typeof errorData === 'string') {
            toast({
              title: "Błąd",
              description: errorData,
              status: "error",
              duration: 5000,
              isClosable: true,
            });
          }
          else {
            toast({
              title: "Błąd walidacji",
              description: "Nieprawidłowe dane wejściowe",
              status: "error",
              duration: 5000,
              isClosable: true,
            });
          }
        } catch {
          // Jeśli nie można sparsować JSON, spróbuj jako tekst
          const errorText = await response.text();
          toast({
            title: "Błąd",
            description: errorText || "Nieprawidłowe dane wejściowe",
            status: "error",
            duration: 5000,
            isClosable: true,
          });
        }
      } else {
        // Inne błędy HTTP
        const errorText = await response.text().catch(() => "Nie udało się udostępnić plików");
        toast({
          title: `Błąd ${response.status}`,
          description: errorText,
          status: "error",
          duration: 5000,
          isClosable: true,
        });
      }
    } catch (error) {
      console.error("Błąd podczas udostępniania plików:", error);
      toast({
        title: "Błąd",
        description: error instanceof Error ? error.message : "Nie udało się udostępnić plików",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setLoading(false);
    }
  };

  const selectedMember = members.find((m) => m.userId === selectedUserId);

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
            <Text fontSize={{ base: "md", md: "lg" }}>Udostępnij pliki</Text>
          </HStack>
        </ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            {/* Lista wybranych plików */}
            <Box>
              <Text fontWeight="bold" mb={2}>
                Wybrane pliki ({selectedFiles.length}):
              </Text>
              <VStack spacing={2} align="stretch" maxH="200px" overflowY="auto">
                {selectedFiles.map((file) => (
                  <Box
                    key={file.id}
                    p={2}
                    bg="gray.50"
                    borderRadius="md"
                    fontSize="sm"
                  >
                    <Text fontWeight="medium">{file.displayName}</Text>
                    <HStack spacing={2} mt={1} flexWrap="wrap">
                      <Badge colorScheme="purple" fontSize="xs">
                        {file.packageName}
                      </Badge>
                      {file.currentVersion && (
                        <Badge colorScheme="blue" fontSize="xs">
                          {(file.currentVersion.fileSizeBytes / 1024).toFixed(2)} KB
                        </Badge>
                      )}
                      {file.totalVersions > 1 && (
                        <Badge colorScheme="green" fontSize="xs">
                          {file.totalVersions} wersji
                        </Badge>
                      )}
                    </HStack>
                  </Box>
                ))}
              </VStack>
            </Box>

            <Alert status="info" fontSize="xs">
              <AlertIcon />
              Udostępniasz pliki bazowe (wszystkie wersje). Członek będzie mieć dostęp do wszystkich wersji wybranych plików.
            </Alert>

            <Divider />

            {/* Wybór członka */}
            <Box>
              <Text fontWeight="bold" mb={2}>
                Udostępnij dla:
              </Text>
              {loadingMembers ? (
                <Text fontSize="sm" color="gray.500">
                  Ładowanie członków...
                </Text>
              ) : (
                <>
                  <Select
                    value={selectedUserId}
                    onChange={(e) => setSelectedUserId(e.target.value)}
                    icon={<User size={16} />}
                  >
                    <option value="" disabled hidden>
                      Wybierz członka projektu
                    </option>
                    {members.map((member) => (
                      <option key={member.userId} value={member.userId}>
                        {member.firstName} {member.lastName} ({member.email})
                      </option>
                    ))}
                  </Select>

                  {selectedMember && (
                    <Box mt={3} p={3} bg="blue.50" borderRadius="md">
                      <HStack>
                        <User size={18} />
                        <VStack align="start" spacing={0}>
                          <Text fontSize="sm" fontWeight="medium">
                            {selectedMember.firstName} {selectedMember.lastName}
                          </Text>
                          <Text fontSize="xs" color="gray.600">
                            {selectedMember.email}
                          </Text>
                        </VStack>
                      </HStack>
                    </Box>
                  )}
                </>
              )}
            </Box>

            <Alert status="info" fontSize="sm">
              <AlertIcon />
              Wybrany członek otrzyma dostęp do tych plików i będzie mógł je przeglądać oraz pobierać.
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
            isDisabled={!selectedUserId || loadingMembers}
            leftIcon={<Share2 size={18} />}
            width={{ base: "100%", md: "auto" }}
            order={{ base: 1, md: 2 }}
          >
            Udostępnij
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
