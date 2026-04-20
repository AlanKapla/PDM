import { useState } from "react";
import {
  Box,
  HStack,
  VStack,
  Text,
  IconButton,
  Badge,
  useColorModeValue,
  useDisclosure,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  Button,
  Spinner,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
} from "@chakra-ui/react";
import { ChevronDown, ChevronRight, Plus, Trash2, MoreVertical, Move, Pencil } from "lucide-react";
import { useRef } from "react";
import { useGantt } from "./GanttContext";
import MobileWorkRow from "./MobileWorkRow";
import type { WorkScheduleStageWeb } from "../../types/workSchedule.types";

interface MobileStageRowProps {
  stage: WorkScheduleStageWeb;
  depth: number;
}

export default function MobileStageRow({ stage, depth }: MobileStageRowProps) {
  const { mode, expandedStages, toggleStage, deleteStage, isMutating, openMobileModal, canEdit } = useGantt();
  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);
  const [isHovered, setIsHovered] = useState(false);

  const isExpanded = expandedStages.has(stage.id);
  const isEditing = mode === "edit";
  const isDeleting = isMutating.has(`deleteStage-${stage.id}`);
  const works = [...(stage.works ?? [])].sort((a, b) => a.order - b.order);
  const childStages = [...(stage.childStages ?? [])].sort((a, b) => a.order - b.order);

  const bgStage = useColorModeValue("primary.50", "primary.900");
  const borderColor = useColorModeValue("primary.200", "primary.700");

  const handleDeleteConfirm = async () => {
    onDeleteClose();
    await deleteStage(stage.id);
  };

  return (
    <>
      {/* Wiersz etapu */}
      <Box
        borderLeftWidth={depth === 0 ? "3px" : "2px"}
        borderLeftColor={depth === 0 ? "primary.400" : "primary.200"}
        borderBottomWidth="1px"
        borderBottomColor={borderColor}
        bg={bgStage}
        ml={`${depth * 12}px`}
      >
        <HStack px={3} py={2} spacing={2} justify="space-between">
          <HStack spacing={2} flex={1} minW={0} onClick={() => toggleStage(stage.id)} cursor="pointer">
            <Box color="primary.500" flexShrink={0}>
              {isExpanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
            </Box>
            <Text fontWeight="semibold" fontSize="sm" noOfLines={1} flex={1}>
              {stage.name || <Text as="span" color="gray.400" fontStyle="italic">Bez nazwy</Text>}
            </Text>
            <Badge colorScheme="gray" variant="subtle" fontSize="9px">
              {works.length > 0 ? `${works.length}` : "0"}
            </Badge>
          </HStack>

          {isEditing && canEdit && (
            <Menu>
              <MenuButton
                as={IconButton}
                aria-label="Więcej"
                icon={<MoreVertical size={14} />}
                size="xs"
                variant="ghost"
              />
              <MenuList>
                <MenuItem
                  icon={<Pencil size={14} />}
                  onClick={() => openMobileModal({ type: "renameStage", stageId: stage.id, initialName: stage.name })}
                >
                  Zmień nazwę
                </MenuItem>
                <MenuItem
                  icon={<Plus size={14} />}
                  onClick={() => openMobileModal({ type: "workForm", stageId: stage.id })}
                >
                  Zakres pracy
                </MenuItem>
                <MenuItem
                  icon={<Plus size={14} />}
                  onClick={() => openMobileModal({ type: "stageForm", stageId: stage.id })}
                >
                  Podetap
                </MenuItem>
                <MenuItem
                  icon={<Move size={14} />}
                  onClick={() => openMobileModal({ type: "moveStage", stageId: stage.id })}
                >
                  Przenieś etap
                </MenuItem>
                <MenuItem
                  icon={isDeleting ? <Spinner size="xs" /> : <Trash2 size={14} />}
                  color="red.500"
                  isDisabled={isDeleting}
                  onClick={onDeleteOpen}
                >
                  Usuń etap
                </MenuItem>
              </MenuList>
            </Menu>
          )}
        </HStack>
      </Box>

      {/* Zakresy prac i podetapy (gdy rozwinięte) */}
      {isExpanded && (
        <>
          {works.map(work => (
            <MobileWorkRow key={work.id} work={work} stageId={stage.id} depth={depth + 1} />
          ))}
          {childStages.map(child => (
            <MobileStageRow key={child.id} stage={child} depth={depth + 1} />
          ))}
          {isEditing && canEdit && (
            <Box
              ml={`${(depth + 1) * 12}px`}
              borderBottomWidth="1px"
              borderBottomColor={useColorModeValue("gray.100", "gray.700")}
              px={3}
              py={1}
            >
              <Button
                size="xs"
                leftIcon={<Plus size={12} />}
                variant="ghost"
                colorScheme="green"
                onClick={() => openMobileModal({ type: "workForm", stageId: stage.id })}
              >
                Zakres pracy
              </Button>
            </Box>
          )}
        </>
      )}

      {/* Dialog potwierdzenia usunięcia */}
      <AlertDialog isOpen={isDeleteOpen} leastDestructiveRef={cancelRef} onClose={onDeleteClose}>
        <AlertDialogOverlay>
          <AlertDialogContent>
            <AlertDialogHeader>Usuń etap</AlertDialogHeader>
            <AlertDialogBody>
              Czy na pewno chcesz usunąć etap <strong>{stage.name}</strong>?
            </AlertDialogBody>
            <AlertDialogFooter>
              <Button ref={cancelRef} onClick={onDeleteClose}>Anuluj</Button>
              <Button colorScheme="red" onClick={handleDeleteConfirm} ml={3}>Usuń</Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>
    </>
  );
}
