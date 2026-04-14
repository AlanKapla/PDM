import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  Button,
  FormControl,
  FormLabel,
  Select,
  Input,
  VStack,
  Text,
  Spinner,
  Center,
  useColorModeValue,
  Box,
  HStack,
  Avatar,
  Checkbox,
  Tag,
  TagLabel,
  TagCloseButton,
  Wrap,
  WrapItem,
} from "@chakra-ui/react";
import { useState, useEffect, useMemo } from "react";
import { chatApi } from "../../api/chatApi";
import type { ProjectContactsGroupWeb } from "../../types/chat.types";
import { useToastNotification } from "../../hooks/useToastNotification";

interface CreateGroupChatModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: (chatId: string) => void;
}

export default function CreateGroupChatModal({
  isOpen,
  onClose,
  onCreated,
}: CreateGroupChatModalProps) {
  const [contactGroups, setContactGroups] = useState<ProjectContactsGroupWeb[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState("");
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([]);
  const [groupName, setGroupName] = useState("");
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const { showError, showSuccess } = useToastNotification();

  const hoverBg = useColorModeValue("gray.50", "gray.700");

  useEffect(() => {
    if (!isOpen) return;
    setSelectedProjectId("");
    setSelectedUserIds([]);
    setGroupName("");

    const load = async () => {
      setLoading(true);
      try {
        const groups = await chatApi.getContacts();
        setContactGroups(groups);
      } catch {
        showError("Nie udało się załadować listy kontaktów.");
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [isOpen]);

  // Wyzeruj wybranych członków przy zmianie projektu
  useEffect(() => {
    setSelectedUserIds([]);
  }, [selectedProjectId]);

  const availableMembers = useMemo(() => {
    const group = contactGroups.find((g) => g.projectId === selectedProjectId);
    return group?.members ?? [];
  }, [contactGroups, selectedProjectId]);

  const selectedMembers = useMemo(
    () => availableMembers.filter((m) => selectedUserIds.includes(m.userId)),
    [availableMembers, selectedUserIds]
  );

  const toggleMember = (userId: string) => {
    setSelectedUserIds((prev) =>
      prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]
    );
  };

  const removeMember = (userId: string) => {
    setSelectedUserIds((prev) => prev.filter((id) => id !== userId));
  };

  const handleSubmit = async () => {
    if (!groupName.trim() || selectedUserIds.length === 0) return;
    setSubmitting(true);
    try {
      const result = await chatApi.createChat({
        projectId: selectedProjectId || null,
        memberUserIds: selectedUserIds,
        name: groupName.trim(),
      });
      showSuccess("Grupa utworzona");
      onCreated(result.id);
      onClose();
    } catch (err: any) {
      const status = err?.response?.status;
      if (status === 403) {
        showError("Nie wszyscy wybrani użytkownicy mają wspólny projekt.");
      } else {
        showError("Nie udało się utworzyć grupy.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} isCentered size={{ base: "full", md: "md" }}>
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>Nowa grupa</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            <FormControl isRequired>
              <FormLabel fontSize="sm">Nazwa grupy</FormLabel>
              <Input
                placeholder="np. Zespół projektowy"
                value={groupName}
                onChange={(e) => setGroupName(e.target.value)}
                size="sm"
              />
            </FormControl>

            <FormControl isRequired>
              <FormLabel fontSize="sm">Projekt (źródło członków)</FormLabel>
              {loading ? (
                <Center py={3}>
                  <Spinner size="sm" />
                </Center>
              ) : (
                <Select
                  placeholder="Wybierz projekt..."
                  value={selectedProjectId}
                  onChange={(e) => setSelectedProjectId(e.target.value)}
                  size="sm"
                >
                  {contactGroups.map((g) => (
                    <option key={g.projectId} value={g.projectId}>
                      {g.projectName}
                    </option>
                  ))}
                </Select>
              )}
            </FormControl>

            {/* Wybrani członkowie */}
            {selectedMembers.length > 0 && (
              <Wrap spacing={2}>
                {selectedMembers.map((m) => (
                  <WrapItem key={m.userId}>
                    <Tag size="sm" colorScheme="primary" borderRadius="full">
                      <TagLabel>
                        {m.firstName} {m.lastName}
                      </TagLabel>
                      <TagCloseButton onClick={() => removeMember(m.userId)} />
                    </Tag>
                  </WrapItem>
                ))}
              </Wrap>
            )}

            {selectedProjectId && (
              <FormControl>
                <FormLabel fontSize="sm">Dodaj członków</FormLabel>
                {availableMembers.length === 0 ? (
                  <Text fontSize="sm" color="gray.500">
                    Brak innych członków w tym projekcie.
                  </Text>
                ) : (
                  <VStack spacing={1} align="stretch" maxH="200px" overflowY="auto">
                    {availableMembers.map((m) => (
                      <HStack
                        key={m.userId}
                        px={3}
                        py={2}
                        borderRadius="md"
                        cursor="pointer"
                        _hover={{ bg: hoverBg }}
                        onClick={() => toggleMember(m.userId)}
                        spacing={3}
                      >
                        <Checkbox
                          isChecked={selectedUserIds.includes(m.userId)}
                          pointerEvents="none"
                        />
                        <Avatar name={`${m.firstName} ${m.lastName}`} size="xs" />
                        <Box>
                          <Text fontSize="sm" fontWeight="medium">
                            {m.firstName} {m.lastName}
                          </Text>
                        </Box>
                      </HStack>
                    ))}
                  </VStack>
                )}
              </FormControl>
            )}
          </VStack>
        </ModalBody>
        <ModalFooter>
          <Button variant="ghost" mr={3} onClick={onClose}>
            Anuluj
          </Button>
          <Button
            colorScheme="primary"
            isDisabled={!groupName.trim() || selectedUserIds.length === 0}
            isLoading={submitting}
            onClick={handleSubmit}
          >
            Utwórz grupę
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

