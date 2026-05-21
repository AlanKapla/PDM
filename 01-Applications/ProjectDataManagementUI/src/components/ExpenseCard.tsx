import {
  Badge,
  Box,
  Checkbox,
  HStack,
  IconButton,
  Text,
  Tooltip,
  useColorModeValue,
} from "@chakra-ui/react";
import { Download, Edit2, Share2, Trash2 } from "lucide-react";
import { formatCurrency, formatDate } from "../utils/formatters";
import type { ProjectCostListItemWeb } from "../types/project.types";

interface ExpenseCardProps {
  cost: ProjectCostListItemWeb;
  showOwner?: boolean;
  canEdit?: boolean;
  canDelete?: boolean;
  canManageShare?: boolean;
  canToggleAccepted?: boolean;
  isTogglingAccepted?: boolean;
  isDeleting?: boolean;
  onEdit?: () => void;
  onDelete?: () => void;
  onManageShare?: () => void;
  onToggleAccepted?: () => void;
}

export default function ExpenseCard({
  cost,
  showOwner = false,
  canEdit = false,
  canDelete = false,
  canManageShare = false,
  canToggleAccepted = false,
  isTogglingAccepted = false,
  isDeleting = false,
  onEdit,
  onDelete,
  onManageShare,
  onToggleAccepted,
}: ExpenseCardProps) {
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const bg = useColorModeValue("white", "gray.800");
  const subTextColor = useColorModeValue("gray.500", "gray.400");

  const metaParts = [
    showOwner && cost.userName,
    cost.contractorName,
    formatDate(cost.date, false),
  ].filter(Boolean) as string[];

  return (
    <Box
      bg={bg}
      borderWidth="1px"
      borderColor={borderColor}
      borderRadius="lg"
      p={3}
      shadow="sm"
    >
      {/* Wiersz 1: Nazwa + kwota brutto */}
      <HStack justify="space-between" align="flex-start" mb={1}>
        <Text fontWeight="bold" fontSize="sm" flex={1}>
          {cost.name}
        </Text>
        <Text
          fontWeight="bold"
          fontSize="sm"
          color="green.600"
          flexShrink={0}
          ml={2}
        >
          {formatCurrency(cost.gross ?? 0)}
        </Text>
      </HStack>

      {/* Wiersz 2: Właściciel, miejsce, data */}
      {metaParts.length > 0 && (
        <Text fontSize="xs" color={subTextColor} mb={2}>
          {metaParts.join(" · ")}
        </Text>
      )}

      {/* Wiersz 3: Badges + ikony akcji */}
      <HStack justify="space-between" align="center" flexWrap="wrap" gap={1}>
        <HStack spacing={2} flexWrap="wrap">
          {/* Checkbox Zaakceptowane */}
          <Checkbox
            isChecked={cost.isAccepted}
            onChange={canToggleAccepted && !isTogglingAccepted ? onToggleAccepted : undefined}
            colorScheme="green"
            isDisabled={!canToggleAccepted || isTogglingAccepted}
            size="sm"
          >
            <Text fontSize="xs">{cost.isAccepted ? "Zaakceptowane" : "Niezaakceptowane"}</Text>
          </Checkbox>

          {/* Chip dokumentu */}
          {cost.hasDocument && cost.previewSasUrl && (
            <Badge
              colorScheme="primary"
              fontSize="xs"
              cursor="pointer"
              onClick={() => window.open(cost.previewSasUrl, "_blank")}
              px={2}
              py={0.5}
              borderRadius="full"
              title={cost.documentFileName}
            >
              📄 {cost.documentFileName?.substring(0, 12) ?? "Dokument"}
            </Badge>
          )}
        </HStack>

        {/* Ikony akcji */}
        <HStack spacing={1}>
          {cost.hasDocument && cost.downloadSasUrl && (
            <Tooltip label="Pobierz dokument">
              <IconButton
                aria-label="Pobierz dokument"
                icon={<Download size={14} />}
                size="xs"
                variant="ghost"
                colorScheme="green"
                onClick={() => window.open(cost.downloadSasUrl, "_blank")}
              />
            </Tooltip>
          )}
          {canManageShare && (
            <Tooltip label="Udostępnij">
              <IconButton
                aria-label="Udostępnij koszt"
                icon={<Share2 size={14} />}
                size="xs"
                variant="ghost"
                colorScheme="orange"
                onClick={onManageShare}
              />
            </Tooltip>
          )}
          {canEdit && (
            <Tooltip label="Edytuj">
              <IconButton
                aria-label="Edytuj koszt"
                icon={<Edit2 size={14} />}
                size="xs"
                variant="ghost"
                colorScheme="primary"
                onClick={onEdit}
              />
            </Tooltip>
          )}
          {canDelete && (
            <Tooltip label="Usuń">
              <IconButton
                aria-label="Usuń koszt"
                icon={<Trash2 size={14} />}
                size="xs"
                variant="ghost"
                colorScheme="red"
                onClick={onDelete}
                isLoading={isDeleting}
              />
            </Tooltip>
          )}
        </HStack>
      </HStack>
    </Box>
  );
}
