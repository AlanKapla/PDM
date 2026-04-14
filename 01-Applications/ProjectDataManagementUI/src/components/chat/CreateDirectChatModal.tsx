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
  VStack,
  Text,
  Spinner,
  Center,
  useColorModeValue,
  Box,
  HStack,
  Avatar,
  Input,
} from "@chakra-ui/react";
import { useState, useEffect, useMemo } from "react";
import { chatApi } from "../../api/chatApi";
import type { ProjectMateWeb, ProjectContactsGroupWeb } from "../../types/chat.types";
import { useToastNotification } from "../../hooks/useToastNotification";

interface CreateDirectChatModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: (chatId: string) => void;
}

export default function CreateDirectChatModal({
  isOpen,
  onClose,
  onCreated,
}: CreateDirectChatModalProps) {
  const [contactGroups, setContactGroups] = useState<ProjectContactsGroupWeb[]>([]);
  const [selectedUserId, setSelectedUserId] = useState("");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const { showError, showSuccess } = useToastNotification();

  const hoverBg = useColorModeValue("primary.50", "primary.900");
  const selectedBg = useColorModeValue("primary.100", "primary.800");

  useEffect(() => {
    if (!isOpen) return;
    setSelectedUserId("");
    setSearch("");

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

  // Spłaszcz wszystkich kontaktów — deduplikacja po userId
  const allContacts = useMemo<ProjectMateWeb[]>(() => {
    const seen = new Set<string>();
    const result: ProjectMateWeb[] = [];
    for (const group of contactGroups) {
      for (const member of group.members) {
        if (!seen.has(member.userId)) {
          seen.add(member.userId);
          result.push(member);
        }
      }
    }
    return result;
  }, [contactGroups]);

  const filteredContacts = useMemo(() => {
    const q = search.toLowerCase();
    if (!q) return allContacts;
    return allContacts.filter(
      (m) =>
        m.firstName.toLowerCase().includes(q) ||
        m.lastName.toLowerCase().includes(q)
    );
  }, [allContacts, search]);

  const handleSubmit = async () => {
    if (!selectedUserId) return;
    setSubmitting(true);
    try {
      const result = await chatApi.createChat({ memberUserIds: [selectedUserId] });
      showSuccess("Rozmowa utworzona");
      onCreated(result.id);
      onClose();
    } catch (err: any) {
      const status = err?.response?.status;
      if (status === 403) {
        showError("Brak wspólnego projektu z wybranym użytkownikiem.");
      } else {
        showError("Nie udało się utworzyć rozmowy.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} isCentered size={{ base: "full", md: "md" }}>
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>Nowa wiadomość bezpośrednia</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            {loading ? (
              <Center py={6}>
                <Spinner size="md" />
              </Center>
            ) : allContacts.length === 0 ? (
              <Text fontSize="sm" color="gray.500">
                Brak dostępnych kontaktów.
              </Text>
            ) : (
              <FormControl isRequired>
                <FormLabel fontSize="sm">Wybierz użytkownika</FormLabel>
                <Input
                  placeholder="Szukaj po nazwisku..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  size="sm"
                  mb={2}
                />
                <VStack spacing={1} align="stretch" maxH="280px" overflowY="auto">
                  {filteredContacts.map((m) => (
                    <HStack
                      key={m.userId}
                      px={3}
                      py={2}
                      borderRadius="md"
                      cursor="pointer"
                      bg={selectedUserId === m.userId ? selectedBg : "transparent"}
                      _hover={{ bg: selectedUserId === m.userId ? selectedBg : hoverBg }}
                      onClick={() => setSelectedUserId(m.userId)}
                      spacing={3}
                    >
                      <Avatar name={`${m.firstName} ${m.lastName}`} size="sm" />
                      <Box>
                        <Text fontSize="sm" fontWeight="medium">
                          {m.firstName} {m.lastName}
                        </Text>
                      </Box>
                    </HStack>
                  ))}
                  {filteredContacts.length === 0 && (
                    <Text fontSize="sm" color="gray.500" px={3}>
                      Brak wyników dla &quot;{search}&quot;.
                    </Text>
                  )}
                </VStack>
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
            isDisabled={!selectedUserId}
            isLoading={submitting}
            onClick={handleSubmit}
          >
            Rozpocznij rozmowę
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
