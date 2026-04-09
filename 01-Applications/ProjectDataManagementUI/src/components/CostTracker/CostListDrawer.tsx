import { useState } from "react";
import {
  Drawer,
  DrawerOverlay,
  DrawerContent,
  DrawerHeader,
  DrawerBody,
  DrawerCloseButton,
  VStack,
  HStack,
  Text,
  IconButton,
  Box,
  Divider,
  Tooltip,
  useDisclosure,
  useBreakpointValue,
} from "@chakra-ui/react";
import { Edit2, Trash2 } from "lucide-react";
import { formatDate } from "../../utils/formatters";
import AttachmentList from "./AttachmentList";
import CostFormDrawer from "./CostFormDrawer";
import { DeleteCostConfirm } from "./PositionsTable";
import { costTrackerApi } from "../../api/costTrackerApi";
import { useToastNotification } from "../../hooks/useToastNotification";
import { handleApiError } from "../../utils/handleApiError";
import type { TrackedCostWeb } from "../../types/costTracker.types";

interface CostListDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  onMutated: () => void;
  tenantId: string;
  projectId: string;
  costs: TrackedCostWeb[];
  title: string;
}

function formatNet(value: number | null): string {
  if (value === null || value === undefined) return "—";
  return `${value.toLocaleString("pl-PL", { minimumFractionDigits: 2, maximumFractionDigits: 2 })} PLN`;
}

export default function CostListDrawer({
  isOpen,
  onClose,
  onMutated,
  tenantId,
  projectId,
  costs,
  title,
}: CostListDrawerProps) {
  const { showSuccess, showError } = useToastNotification();
  const size = useBreakpointValue({ base: "full", md: "lg" }) as string;

  const [editingCost, setEditingCost] = useState<TrackedCostWeb | null>(null);
  const [deletingCost, setDeletingCost] = useState<TrackedCostWeb | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();

  const handleDeleteConfirm = async () => {
    if (!deletingCost) return;
    setIsDeleting(true);
    try {
      await costTrackerApi.deleteCost(tenantId, projectId, deletingCost.id);
      showSuccess("Koszt usunięty");
      onDeleteClose();
      setDeletingCost(null);
      onMutated();
    } catch (err) {
      const { title: t, description } = handleApiError(err);
      showError(t, description);
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <>
      <Drawer isOpen={isOpen} onClose={onClose} placement="right" size={size}>
        <DrawerOverlay />
        <DrawerContent>
          <DrawerCloseButton />
          <DrawerHeader>{title}</DrawerHeader>
          <DrawerBody>
            {costs.length === 0 ? (
              <Text color="gray.500" fontSize="sm">
                Brak kosztów.
              </Text>
            ) : (
              <VStack spacing={3} align="stretch">
                {costs.map((cost, idx) => (
                  <Box key={cost.id}>
                    <HStack align="flex-start" spacing={3}>
                      <VStack align="stretch" flex={1} spacing={1}>
                        <Text fontWeight="semibold" fontSize="sm">
                          {cost.name}
                        </Text>
                        <HStack spacing={4} flexWrap="wrap">
                          <Text fontSize="xs" color="gray.600">
                            Netto: {formatNet(cost.net)}
                          </Text>
                          <Text fontSize="xs" color="gray.600">
                            Brutto: {formatNet(cost.gross)}
                          </Text>
                          {cost.date && (
                            <Text fontSize="xs" color="gray.600">
                              Data: {formatDate(cost.date, false)}
                            </Text>
                          )}
                          {cost.contractor && (
                            <Text fontSize="xs" color="gray.600">
                              Wykonawca: {cost.contractor}
                            </Text>
                          )}
                        </HStack>
                        {cost.description && (
                          <Text fontSize="xs" color="gray.500" noOfLines={2}>
                            {cost.description}
                          </Text>
                        )}
                        {cost.attachments.length > 0 && (
                          <AttachmentList attachments={cost.attachments} readonly />
                        )}
                      </VStack>

                      <HStack spacing={1} flexShrink={0}>
                        <Tooltip label="Edytuj">
                          <IconButton
                            aria-label="Edytuj koszt"
                            icon={<Edit2 size={14} />}
                            size="sm"
                            variant="ghost"
                            onClick={() => setEditingCost(cost)}
                            minH="44px"
                          />
                        </Tooltip>
                        <Tooltip label="Usuń">
                          <IconButton
                            aria-label="Usuń koszt"
                            icon={<Trash2 size={14} />}
                            size="sm"
                            variant="ghost"
                            colorScheme="red"
                            onClick={() => { setDeletingCost(cost); onDeleteOpen(); }}
                            minH="44px"
                          />
                        </Tooltip>
                      </HStack>
                    </HStack>
                    {idx < costs.length - 1 && <Divider mt={3} />}
                  </Box>
                ))}
              </VStack>
            )}
          </DrawerBody>
        </DrawerContent>
      </Drawer>

      {/* Drawer edycji kosztu */}
      {editingCost && (
        <CostFormDrawer
          isOpen={!!editingCost}
          onClose={() => setEditingCost(null)}
          onSuccess={() => { setEditingCost(null); onMutated(); }}
          tenantId={tenantId}
          projectId={projectId}
          cost={editingCost}
        />
      )}

      {/* Potwierdzenie usunięcia */}
      <DeleteCostConfirm
        isOpen={isDeleteOpen}
        onClose={() => { onDeleteClose(); setDeletingCost(null); }}
        onConfirm={handleDeleteConfirm}
        isLoading={isDeleting}
      />
    </>
  );
}
