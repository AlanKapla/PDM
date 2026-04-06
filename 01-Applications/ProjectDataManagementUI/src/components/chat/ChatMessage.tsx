import {
  Box,
  HStack,
  Text,
  Avatar,
  useColorModeValue,
  IconButton,
  Tooltip,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
} from "@chakra-ui/react";
import { MoreVertical, Pencil, Trash2, CornerUpLeft } from "lucide-react";
import type { MessageWeb } from "../../types/chat.types";

interface ChatMessageProps {
  message: MessageWeb;
  isOwn: boolean;
  replyTarget: MessageWeb | null;
  onReply: () => void;
  onEdit: () => void;
  onDelete: () => void;
}

export default function ChatMessage({
  message,
  isOwn,
  replyTarget,
  onReply,
  onEdit,
  onDelete,
}: ChatMessageProps) {
  const ownBg = useColorModeValue("primary.500", "primary.400");
  const otherBg = useColorModeValue("gray.100", "gray.700");
  const mutedColor = useColorModeValue("gray.500", "gray.400");
  const replyBg = useColorModeValue("blackAlpha.100", "whiteAlpha.100");

  const senderName = `${message.senderFirstName} ${message.senderLastName}`;
  const sentAt = new Date(message.sentAt).toLocaleTimeString("pl-PL", {
    hour: "2-digit",
    minute: "2-digit",
  });

  // Wiadomość usunięta — placeholder
  if (message.isDeleted) {
    return (
      <Box
        alignSelf={isOwn ? "flex-end" : "flex-start"}
        px={3}
        py={1.5}
        maxW="70%"
        opacity={0.6}
      >
        <Text fontSize="sm" fontStyle="italic" color={mutedColor}>
          Ta wiadomość została usunięta
        </Text>
      </Box>
    );
  }

  return (
    <Box
      alignSelf={isOwn ? "flex-end" : "flex-start"}
      maxW={{ base: "88%", md: "70%" }}
      role="group"
    >
      {/* Nazwa nadawcy (tylko dla cudzych wiadomości) */}
      {!isOwn && (
        <Text fontSize="xs" color={mutedColor} mb={0.5} ml={1}>
          {senderName}
        </Text>
      )}

      <HStack spacing={1} align="flex-end" flexDirection={isOwn ? "row-reverse" : "row"}>
        {!isOwn && (
          <Avatar name={senderName} size="xs" flexShrink={0} mb={0.5} />
        )}

        <Box
          bg={isOwn ? ownBg : otherBg}
          color={isOwn ? "white" : undefined}
          px={3}
          py={2}
          borderRadius="lg"
          borderBottomRightRadius={isOwn ? "sm" : "lg"}
          borderBottomLeftRadius={isOwn ? "lg" : "sm"}
        >
          {/* Cytowana wiadomość */}
          {replyTarget && !replyTarget.isDeleted && (
            <Box
              bg={replyBg}
              borderLeft="3px solid"
              borderColor={isOwn ? "whiteAlpha.600" : "primary.300"}
              px={2}
              py={1}
              mb={1.5}
              borderRadius="sm"
            >
              <Text fontSize="xs" fontWeight="semibold" opacity={0.8}>
                {replyTarget.senderFirstName} {replyTarget.senderLastName}
              </Text>
              <Text fontSize="xs" opacity={0.7} noOfLines={2}>
                {replyTarget.content}
              </Text>
            </Box>
          )}

          <Text fontSize="sm" whiteSpace="pre-wrap" wordBreak="break-word">
            {message.content}
          </Text>

          {/* Czas i status edycji */}
          <HStack spacing={1} justify="flex-end" mt={0.5}>
            {message.isEdited && (
              <Text fontSize="xs" opacity={0.7}>
                (edytowano)
              </Text>
            )}
            <Text fontSize="xs" opacity={0.7}>
              {sentAt}
            </Text>
          </HStack>
        </Box>

        {/* Akcje — zawsze widoczne na mobile, po najechaniu na desktop */}
        <Box
          opacity={{ base: 1, md: 0 }}
          _groupHover={{ opacity: 1 }}
          transition="opacity 0.15s"
        >
          <Menu isLazy>
            <MenuButton
              as={IconButton}
              icon={<MoreVertical size={14} />}
              size="xs"
              variant="ghost"
              aria-label="Opcje wiadomości"
            />
            <MenuList>
              <MenuItem icon={<CornerUpLeft size={14} />} onClick={onReply}>
                Odpowiedz
              </MenuItem>
              {isOwn && (
                <>
                  <MenuItem icon={<Pencil size={14} />} onClick={onEdit}>
                    Edytuj
                  </MenuItem>
                  <MenuItem
                    icon={<Trash2 size={14} />}
                    onClick={onDelete}
                    color="red.500"
                  >
                    Usuń
                  </MenuItem>
                </>
              )}
            </MenuList>
          </Menu>
        </Box>
      </HStack>
    </Box>
  );
}
