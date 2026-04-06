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
  HStack,
  Text,
  Alert,
  AlertIcon,
  Box,
  Badge,
  Checkbox,
  Spinner,
  Avatar,
  useToast,
  Divider,
} from "@chakra-ui/react";
import { Share2, Users, Lock } from "lucide-react";
import { projectApi } from "../api/projectApi";
import { costEstimateApi } from "../api/costEstimateApi";
import { handleApiError } from "../utils/handleApiError";
import type { ProjectMemberWeb } from "../types/project.types";
import type { CostEstimateShareWeb } from "../types/costEstimate.types.new";

interface ShareCostEstimateModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  costEstimateId: string;
  costEstimateName: string;
  /** Właściciel kosztorysu — wykluczany z listy wyboru */
  ownerId: string;
  /** Id zalogowanego użytkownika — wykluczany z listy wyboru */
  currentUserId: string;
  /** Aktualna lista udostępnień (z CostEstimateDetailsWeb lub CostEstimateListItemWeb) */
  currentSharedUsers: CostEstimateShareWeb[];
  onShareUpdated: () => void;
}

export default function ShareCostEstimateModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  costEstimateId,
  costEstimateName,
  ownerId,
  currentUserId,
  currentSharedUsers,
  onShareUpdated,
}: ShareCostEstimateModalProps) {
  const toast = useToast();

  const [members, setMembers] = useState<ProjectMemberWeb[]>([]);
  const [selectedUserIds, setSelectedUserIds] = useState<Set<string>>(new Set());
  const [loadingMembers, setLoadingMembers] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!isOpen) return;
    // Pre-zaznacz aktualnie udostępnione osoby
    setSelectedUserIds(new Set(currentSharedUsers.map((s) => s.userId)));
    fetchMembers();
  }, [isOpen]);

  const fetchMembers = async () => {
    setLoadingMembers(true);
    try {
      const response = await projectApi.getProjectMembers(tenantId, projectId);
      const data: ProjectMemberWeb[] = response.data;
      // Wyklucz właściciela kosztorysu i bieżącego użytkownika (jeśli to owner)
      const excludeIds = new Set([ownerId, currentUserId].filter(Boolean));
      setMembers(data.filter((m) => m.userId && !excludeIds.has(m.userId)));
    } catch {
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać listy członków projektu",
        status: "error",
        duration: 4000,
        isClosable: true,
      });
    } finally {
      setLoadingMembers(false);
    }
  };

  const toggleUser = (userId: string) => {
    setSelectedUserIds((prev) => {
      const next = new Set(prev);
      if (next.has(userId)) {
        next.delete(userId);
      } else {
        next.add(userId);
      }
      return next;
    });
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      // PUT — zastępuje pełną listę, wysyła aktualnie zaznaczonych
      await costEstimateApi.updateCostEstimateShares(
        tenantId,
        projectId,
        costEstimateId,
        Array.from(selectedUserIds)
      );
      toast({
        title: "Sukces",
        description: "Udostępnienie kosztorysu zaktualizowane",
        status: "success",
        duration: 3000,
        isClosable: true,
      });
      onShareUpdated();
      onClose();
    } catch (err) {
      const { title, description } = handleApiError(err);
      toast({ title, description, status: "error", duration: 5000, isClosable: true });
    } finally {
      setSaving(false);
    }
  };

  const handleClose = () => {
    setSelectedUserIds(new Set());
    onClose();
  };

  const changesCount = (() => {
    const currentIds = new Set(currentSharedUsers.map((s) => s.userId));
    const added = [...selectedUserIds].filter((id) => !currentIds.has(id)).length;
    const removed = [...currentIds].filter((id) => !selectedUserIds.has(id)).length;
    return { added, removed };
  })();

  return (
    <Modal isOpen={isOpen} onClose={handleClose} size={{ base: "full", md: "md" }} isCentered>
      <ModalOverlay />
      <ModalContent mx={{ base: 0, md: "auto" }}>
        <ModalHeader>
          <HStack spacing={2}>
            <Share2 size={20} />
            <Text>Udostępnij kosztorys</Text>
          </HStack>
        </ModalHeader>
        <ModalCloseButton />

        <ModalBody>
          <VStack align="stretch" spacing={4}>
            {/* Nazwa kosztorysu */}
            <Box>
              <Text fontSize="sm" color="gray.500" mb={1}>
                Kosztorys:
              </Text>
              <Badge colorScheme="primary" fontSize="sm" px={3} py={1} borderRadius="md">
                {costEstimateName}
              </Badge>
            </Box>

            <Divider />

            {/* Lista członków projektu */}
            {loadingMembers ? (
              <HStack justify="center" py={4}>
                <Spinner size="md" />
                <Text fontSize="sm" color="gray.500">
                  Ładowanie członków...
                </Text>
              </HStack>
            ) : members.length === 0 ? (
              <Alert status="info" borderRadius="md">
                <AlertIcon />
                Brak innych członków projektu do udostępnienia
              </Alert>
            ) : (
              <Box>
                <HStack mb={2} justify="space-between">
                  <Text fontSize="sm" fontWeight="semibold" color="gray.700">
                    <HStack spacing={1}>
                      <Users size={14} />
                      <span>Wybierz użytkowników ({members.length})</span>
                    </HStack>
                  </Text>
                  {selectedUserIds.size > 0 && (
                    <Badge colorScheme="primary" fontSize="xs">
                      Wybrano: {selectedUserIds.size}
                    </Badge>
                  )}
                </HStack>

                <VStack
                  align="stretch"
                  spacing={1}
                  maxH="300px"
                  overflowY="auto"
                  borderWidth="1px"
                  borderRadius="md"
                  borderColor="gray.200"
                  p={2}
                >
                  {members.map((member) => {
                    const isSelected = selectedUserIds.has(member.userId);
                    return (
                      <HStack
                        key={member.userId}
                        p={2}
                        borderRadius="md"
                        cursor="pointer"
                        bg={isSelected ? "primary.50" : "transparent"}
                        _hover={{ bg: isSelected ? "primary.100" : "gray.50" }}
                        onClick={() => toggleUser(member.userId)}
                        spacing={3}
                      >
                        <Checkbox
                          isChecked={isSelected}
                          onChange={() => toggleUser(member.userId)}
                          colorScheme="primary"
                          onClick={(e) => e.stopPropagation()}
                        />
                        <Avatar
                          size="xs"
                          name={`${member.firstName} ${member.lastName}`}
                        />
                        <VStack align="start" spacing={0} flex={1} minW={0}>
                          <Text fontSize="sm" fontWeight="medium" noOfLines={1}>
                            {member.firstName} {member.lastName}
                          </Text>
                          <Text fontSize="xs" color="gray.500" noOfLines={1}>
                            {member.email}
                          </Text>
                        </VStack>
                        {isSelected && (
                          <Badge colorScheme="green" fontSize="xs">
                            Dostęp
                          </Badge>
                        )}
                      </HStack>
                    );
                  })}
                </VStack>
              </Box>
            )}

            {/* Podgląd zmian */}
            {(changesCount.added > 0 || changesCount.removed > 0) && (
              <Alert status="warning" borderRadius="md" fontSize="sm">
                <AlertIcon />
                <VStack align="start" spacing={0}>
                  {changesCount.added > 0 && (
                    <Text>
                      Dodano dostęp dla {changesCount.added}{" "}
                      {changesCount.added === 1 ? "osoby" : "osób"}
                    </Text>
                  )}
                  {changesCount.removed > 0 && (
                    <Text>
                      Cofnięto dostęp dla {changesCount.removed}{" "}
                      {changesCount.removed === 1 ? "osoby" : "osób"}
                    </Text>
                  )}
                </VStack>
              </Alert>
            )}

            {/* Info o uprawnieniach */}
            <Alert status="info" borderRadius="md" fontSize="xs">
              <AlertIcon as={Lock} />
              Udostępniony użytkownik może edytować pola nieoznaczone jako tylko do odczytu, ale nie może modyfikować struktury kosztorysu.
            </Alert>
          </VStack>
        </ModalBody>

        <ModalFooter>
          <Button variant="ghost" mr={3} onClick={handleClose} isDisabled={saving}>
            Anuluj
          </Button>
          <Button
            colorScheme="primary"
            leftIcon={<Share2 size={16} />}
            onClick={handleSave}
            isLoading={saving}
            isDisabled={loadingMembers}
          >
            Zapisz
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
