import { useState } from "react";
import {
  Drawer,
  DrawerOverlay,
  DrawerContent,
  DrawerHeader,
  DrawerBody,
  DrawerFooter,
  DrawerCloseButton,
  VStack,
  HStack,
  Text,
  Checkbox,
  Button,
  Avatar,
  Input,
  InputGroup,
  InputLeftElement,
} from "@chakra-ui/react";
import { Search } from "lucide-react";
import { useGantt } from "../GanttContext";
import type { WorkScheduleStageWorkWeb } from "../../../types/workSchedule.types";

interface AssignmentsModalProps {
  isOpen: boolean;
  onClose: () => void;
  stageId: string;
  work: WorkScheduleStageWorkWeb;
}

export default function AssignmentsModal({ isOpen, onClose, stageId, work }: AssignmentsModalProps) {
  const { members, setAssignments } = useGantt();
  const [selectedIds, setSelectedIds] = useState<Set<string>>(
    new Set((work.assignees ?? []).map(a => a.userId))
  );
  const [search, setSearch] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const filteredMembers = members.filter(m =>
    `${m.firstName} ${m.lastName} ${m.email}`.toLowerCase().includes(search.toLowerCase())
  );

  const toggleMember = (userId: string) => {
    setSelectedIds(prev => {
      const next = new Set(prev);
      if (next.has(userId)) next.delete(userId);
      else next.add(userId);
      return next;
    });
  };

  const handleSave = async () => {
    setIsSubmitting(true);
    try {
      await setAssignments(stageId, work.id, Array.from(selectedIds));
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Drawer isOpen={isOpen} onClose={onClose} placement="bottom" size="md">
      <DrawerOverlay />
      <DrawerContent borderTopRadius="lg" maxH="70vh">
        <DrawerCloseButton />
        <DrawerHeader borderBottomWidth="1px">Przypisani — {work.name}</DrawerHeader>
        <DrawerBody py={4} overflowY="auto">
          <VStack spacing={3}>
            <InputGroup size="sm">
              <InputLeftElement>
                <Search size={14} color="var(--chakra-colors-neutral-400)" />
              </InputLeftElement>
              <Input
                placeholder="Szukaj uczestnika..."
                value={search}
                onChange={e => setSearch(e.target.value)}
                pl={8}
              />
            </InputGroup>
            {filteredMembers.length === 0 && (
              <Text fontSize="sm" color="neutral.400">Brak uczestników projektu</Text>
            )}
            {filteredMembers.map(member => (
              <HStack key={member.userId} w="100%" spacing={3}>
                <Checkbox
                  isChecked={selectedIds.has(member.userId)}
                  onChange={() => toggleMember(member.userId)}
                  colorScheme="primary"
                />
                <Avatar name={`${member.firstName} ${member.lastName}`} size="xs" />
                <VStack align="start" spacing={0} flex={1} minW={0}>
                  <Text fontSize="sm" fontWeight="medium" noOfLines={1}>
                    {member.firstName} {member.lastName}
                  </Text>
                  <Text fontSize="xs" color="neutral.400" noOfLines={1}>{member.email}</Text>
                </VStack>
              </HStack>
            ))}
          </VStack>
        </DrawerBody>
        <DrawerFooter borderTopWidth="1px" gap={2}>
          <Button variant="ghost" colorScheme="gray" onClick={onClose}>Anuluj</Button>
          <Button colorScheme="primary" onClick={handleSave} isLoading={isSubmitting}>
            Zapisz ({selectedIds.size})
          </Button>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
