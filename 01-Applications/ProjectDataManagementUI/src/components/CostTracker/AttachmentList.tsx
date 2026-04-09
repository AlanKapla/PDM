import {
  VStack,
  HStack,
  Text,
  IconButton,
  Link,
  Tooltip,
} from "@chakra-ui/react";
import { Download, Trash2, FileText } from "lucide-react";
import { formatFileSize } from "../../utils/formatters";
import type { TrackedCostAttachmentWeb } from "../../types/costTracker.types";

interface AttachmentListProps {
  attachments: TrackedCostAttachmentWeb[];
  removedIds?: string[];
  onRemove?: (id: string) => void;
  readonly?: boolean;
}

export default function AttachmentList({
  attachments,
  removedIds = [],
  onRemove,
  readonly = false,
}: AttachmentListProps) {
  if (attachments.length === 0) return null;

  const visible = attachments.filter((a) => !removedIds.includes(a.id));
  if (visible.length === 0) return null;

  return (
    <VStack align="stretch" spacing={1}>
      {visible.map((att) => (
        <HStack
          key={att.id}
          px={2}
          py={1}
          borderRadius="md"
          bg="gray.50"
          _dark={{ bg: "gray.700" }}
          spacing={2}
        >
          <FileText size={14} />
          <Text fontSize="sm" flex={1} noOfLines={1}>
            <Link href={att.fileUrl} isExternal color="blue.600">
              {att.originalFileName}
            </Link>
          </Text>
          <Text fontSize="xs" color="gray.500" whiteSpace="nowrap">
            {formatFileSize(att.fileSize)}
          </Text>
          <Tooltip label="Pobierz">
            <IconButton
              as="a"
              href={att.fileUrl}
              download={att.originalFileName}
              aria-label="Pobierz załącznik"
              icon={<Download size={14} />}
              size="xs"
              variant="ghost"
            />
          </Tooltip>
          {!readonly && onRemove && (
            <Tooltip label="Usuń załącznik">
              <IconButton
                aria-label="Usuń załącznik"
                icon={<Trash2 size={14} />}
                size="xs"
                variant="ghost"
                colorScheme="red"
                onClick={() => onRemove(att.id)}
              />
            </Tooltip>
          )}
        </HStack>
      ))}
    </VStack>
  );
}
