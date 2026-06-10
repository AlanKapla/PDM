import {
  Box,
  HStack,
  Text,
  Badge,
  VStack,
  Avatar,
  useColorModeValue,
} from "@chakra-ui/react";
import type { ChatWeb } from "../../types/chat.types";

interface ChatListItemProps {
  chat: ChatWeb;
  isActive: boolean;
  currentUserId: string;
  onClick: () => void;
}

export default function ChatListItem({
  chat,
  isActive,
  currentUserId,
  onClick,
}: ChatListItemProps) {
  const activeBg = useColorModeValue("primary.50", "primary.900");
  const hoverBg = useColorModeValue("gray.100", "gray.700");
  const mutedColor = useColorModeValue("gray.500", "gray.400");

  const displayName = chat.isGroupChat
    ? chat.name
    : chat.members
        .filter((m) => m.userId !== currentUserId)
        .map((m) => `${m.firstName} ${m.lastName}`)
        .join(", ") || chat.name;

  const lastMessageText = chat.lastMessage
    ? chat.lastMessage.isDeleted
      ? "Wiadomość usunięta"
      : chat.lastMessage.content
    : "Brak wiadomości";

  const otherMember = !chat.isGroupChat
    ? chat.members.find((m) => m.userId !== currentUserId)
    : null;

  const initials = otherMember
    ? `${otherMember.firstName[0]}${otherMember.lastName[0]}`.toUpperCase()
    : displayName
        .split(" ")
        .slice(0, 2)
        .map((w) => w[0]?.toUpperCase() ?? "")
        .join("");

  return (
    <HStack
      px={3}
      py={2}
      cursor="pointer"
      bg={isActive ? activeBg : "transparent"}
      _hover={{ bg: isActive ? activeBg : hoverBg }}
      borderRadius="md"
      onClick={onClick}
      align="flex-start"
      spacing={3}
    >
      <Avatar name={displayName} size="sm" getInitials={() => initials} mt={0.5} />
      <Box flex={1} minW={0}>
        <HStack justify="space-between">
          <Text fontWeight={chat.unreadCount > 0 ? "bold" : "medium"} fontSize="sm" noOfLines={1}>
            {displayName}
          </Text>
          {chat.unreadCount > 0 && (
            <Badge colorScheme="primary" borderRadius="full" fontSize="xs" flexShrink={0}>
              {chat.unreadCount > 99 ? "99+" : chat.unreadCount}
            </Badge>
          )}
        </HStack>
        <Text fontSize="xs" color={mutedColor} noOfLines={1}>
          {lastMessageText}
        </Text>
      </Box>
    </HStack>
  );
}
