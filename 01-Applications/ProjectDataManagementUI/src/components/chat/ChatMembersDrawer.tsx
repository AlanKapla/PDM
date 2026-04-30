import {
  Drawer,
  DrawerOverlay,
  DrawerContent,
  DrawerCloseButton,
  DrawerHeader,
  DrawerBody,
  VStack,
  HStack,
  Text,
  Avatar,
  Badge,
  IconButton,
  Divider,
  Spinner,
  Center,
  Box,
  Tooltip,
  useColorModeValue,
} from "@chakra-ui/react";
import { X, UserPlus } from "lucide-react";
import { useState, useEffect } from "react";
import type { ChatWeb, ChatMemberWeb, AvailableMemberWeb } from "../../types/chat.types";
import { chatApi } from "../../api/chatApi";
import { useToastNotification } from "../../hooks/useToastNotification";

interface ChatMembersDrawerProps {
  chat: ChatWeb;
  currentUserId: string;
  isOpen: boolean;
  onClose: () => void;
  onMembersChange?: (members: ChatMemberWeb[]) => void;
}

export default function ChatMembersDrawer({
  chat,
  currentUserId,
  isOpen,
  onClose,
  onMembersChange,
}: ChatMembersDrawerProps) {
  const [members, setMembers] = useState<ChatMemberWeb[]>(chat.members);
  const [availableMembers, setAvailableMembers] = useState<AvailableMemberWeb[]>([]);
  const [loading, setLoading] = useState(false);
  const [actionUserId, setActionUserId] = useState<string | null>(null);
  const { showError, showSuccess } = useToastNotification();

  const sectionLabelColor = useColorModeValue("gray.500", "gray.400");

  const currentUserIsAdmin =
    chat.members.find((m) => m.userId === currentUserId)?.isAdmin ?? false;

  // Przy każdym otwarciu szuflady pobierz świeże dane
  useEffect(() => {
    if (!isOpen) return;

    const load = async () => {
      setLoading(true);
      try {
        const [freshMembers, available] = await Promise.all([
          chatApi.getMembers(chat.id),
          currentUserIsAdmin ? chatApi.getAvailableMembers(chat.id) : Promise.resolve([]),
        ]);
        setMembers(freshMembers);
        setAvailableMembers(available);
      } catch {
        // zostają poprzednie dane
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [isOpen, chat.id, currentUserIsAdmin]);

  const handleRemove = async (userId: string) => {
    setActionUserId(userId);
    try {
      await chatApi.removeMember(chat.id, userId);
      const removed = members.find((m) => m.userId === userId);
      const newMembers = members.filter((m) => m.userId !== userId);
      setMembers(newMembers);
      onMembersChange?.(newMembers);
      if (removed) {
        setAvailableMembers((prev) => [
          ...prev,
          { userId: removed.userId, firstName: removed.firstName, lastName: removed.lastName },
        ]);
      }
      showSuccess("Uczestnik usunięty");
    } catch {
      showError("Nie udało się usunąć uczestnika.");
    } finally {
      setActionUserId(null);
    }
  };

  const handleAdd = async (userId: string) => {
    setActionUserId(userId);
    try {
      await chatApi.addMember(chat.id, { userId });
      // Pobierz świeżą listę — backend uzupełni pola (joinedAt, isAdmin itp.)
      const freshMembers = await chatApi.getMembers(chat.id);
      setMembers(freshMembers);
      onMembersChange?.(freshMembers);
      setAvailableMembers((prev) => prev.filter((m) => m.userId !== userId));
      showSuccess("Uczestnik dodany");
    } catch {
      showError("Nie udało się dodać uczestnika.");
    } finally {
      setActionUserId(null);
    }
  };

  return (
    <Drawer isOpen={isOpen} onClose={onClose} placement="right" size="sm">
      <DrawerOverlay />
      <DrawerContent>
        <DrawerCloseButton />
        <DrawerHeader>Uczestnicy ({members.length})</DrawerHeader>

        <DrawerBody px={3} pb={4}>
          {loading ? (
            <Center py={10}>
              <Spinner />
            </Center>
          ) : (
            <VStack align="stretch" spacing={0}>
              {/* Lista obecnych uczestników */}
              {members.map((member) => (
                <HStack key={member.userId} px={2} py={2} borderRadius="md" spacing={3}>
                  <Avatar
                    name={`${member.firstName} ${member.lastName}`}
                    size="sm"
                    flexShrink={0}
                  />
                  <Box flex={1} minW={0}>
                    <HStack spacing={1.5} flexWrap="wrap">
                      <Text fontSize="sm" fontWeight="medium" noOfLines={1}>
                        {member.firstName} {member.lastName}
                      </Text>
                      {member.isAdmin && (
                        <Badge colorScheme="primary" fontSize="xs" flexShrink={0}>
                          Admin
                        </Badge>
                      )}
                      {member.userId === currentUserId && (
                        <Badge colorScheme="gray" fontSize="xs" flexShrink={0}>
                          Ty
                        </Badge>
                      )}
                    </HStack>
                  </Box>
                  {/* Admin może usunąć każdego nie-admina poza sobą */}
                  {currentUserIsAdmin &&
                    member.userId !== currentUserId &&
                    !member.isAdmin && (
                      <Tooltip label="Usuń uczestnika">
                        <IconButton
                          icon={<X size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="red"
                          aria-label="Usuń uczestnika"
                          isLoading={actionUserId === member.userId}
                          onClick={() => handleRemove(member.userId)}
                        />
                      </Tooltip>
                    )}
                </HStack>
              ))}

              {/* Sekcja dodawania — tylko dla adminów */}
              {currentUserIsAdmin && availableMembers.length > 0 && (
                <>
                  <Divider my={4} />
                  <Text
                    fontSize="xs"
                    fontWeight="semibold"
                    color={sectionLabelColor}
                    px={2}
                    mb={2}
                    textTransform="uppercase"
                    letterSpacing="wide"
                  >
                    Dodaj uczestników
                  </Text>
                  {availableMembers.map((member) => (
                    <HStack key={member.userId} px={2} py={2} borderRadius="md" spacing={3}>
                      <Avatar
                        name={`${member.firstName} ${member.lastName}`}
                        size="sm"
                        flexShrink={0}
                      />
                      <Text fontSize="sm" flex={1} noOfLines={1}>
                        {member.firstName} {member.lastName}
                      </Text>
                      <Tooltip label="Dodaj do grupy">
                        <IconButton
                          icon={<UserPlus size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="primary"
                          aria-label="Dodaj do grupy"
                          isLoading={actionUserId === member.userId}
                          onClick={() => handleAdd(member.userId)}
                        />
                      </Tooltip>
                    </HStack>
                  ))}
                </>
              )}

              {currentUserIsAdmin && availableMembers.length === 0 && !loading && (
                <>
                  <Divider my={4} />
                  <Text fontSize="sm" color={sectionLabelColor} px={2}>
                    Wszyscy dostępni użytkownicy są już w tej grupie.
                  </Text>
                </>
              )}
            </VStack>
          )}
        </DrawerBody>
      </DrawerContent>
    </Drawer>
  );
}
