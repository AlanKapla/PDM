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
  const { members, contractors, setAssignments } = useGantt();
  const [selectedUserIds, setSelectedUserIds] = useState<Set<string>>(
    new Set(
      (work.assignees ?? [])
        .map(a => a.userId)
        .filter((id): id is string => !!id)
    )
  );
  const [selectedContractorIds, setSelectedContractorIds] = useState<Set<string>>(
    new Set(
      (work.assignees ?? [])
        .map(a => a.contractorId)
        .filter((id): id is string => !!id)
    )
  );
  const [search, setSearch] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const q = search.toLowerCase();
  const filteredMembers = members.filter(m =>
    `${m.firstName} ${m.lastName} ${m.email} ${m.companyName ?? ""}`.toLowerCase().includes(q)
  );
  const filteredContractors = contractors.filter(c =>
    c.name.toLowerCase().includes(q)
  );

  const toggleUser = (userId: string) => {
    setSelectedUserIds(prev => {
      const next = new Set(prev);
      if (next.has(userId)) next.delete(userId);
      else next.add(userId);
      return next;
    });
  };

  const toggleContractor = (contractorId: string) => {
    setSelectedContractorIds(prev => {
      const next = new Set(prev);
      if (next.has(contractorId)) next.delete(contractorId);
      else next.add(contractorId);
      return next;
    });
  };

  const handleSave = async () => {
    setIsSubmitting(true);
    try {
      await setAssignments(
        stageId,
        work.id,
        Array.from(selectedUserIds),
        Array.from(selectedContractorIds),
      );
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
          <VStack spacing={3} align="stretch">
            <InputGroup size="sm">
              <InputLeftElement>
                <Search size={14} color="var(--chakra-colors-neutral-400)" />
              </InputLeftElement>
              <Input
                placeholder="Szukaj uczestnika lub kontahenta..."
                value={search}
                onChange={e => setSearch(e.target.value)}
                pl={8}
              />
            </InputGroup>

            <Text fontSize="xs" fontWeight="semibold" color="neutral.500" textTransform="uppercase">
              Zespół projektu
            </Text>
            {filteredMembers.length === 0 && (
              <Text fontSize="sm" color="neutral.400">Brak uczestników projektu</Text>
            )}
            {filteredMembers.map(member => (
              <HStack key={member.userId} w="100%" spacing={3}>
                <Checkbox
                  isChecked={selectedUserIds.has(member.userId)}
                  onChange={() => toggleUser(member.userId)}
                  colorScheme="primary"
                />
                <Avatar name={`${member.firstName} ${member.lastName}`} size="xs" />
                <VStack align="start" spacing={0} flex={1} minW={0}>
                  <Text fontSize="sm" fontWeight="medium" noOfLines={1}>
                    {member.firstName} {member.lastName}
                    {member.companyName?.trim() ? ` (${member.companyName.trim()})` : ""}
                  </Text>
                  <Text fontSize="xs" color="neutral.400" noOfLines={1}>{member.email}</Text>
                </VStack>
              </HStack>
            ))}

            <Text fontSize="xs" fontWeight="semibold" color="neutral.500" textTransform="uppercase" pt={2}>
              Kontahenci
            </Text>
            {filteredContractors.length === 0 && (
              <Text fontSize="sm" color="neutral.400">Brak kontahentów</Text>
            )}
            {filteredContractors.map(contractor => (
              <HStack key={contractor.id} w="100%" spacing={3}>
                <Checkbox
                  isChecked={selectedContractorIds.has(contractor.id)}
                  onChange={() => toggleContractor(contractor.id)}
                  colorScheme="primary"
                />
                <Avatar name={contractor.name} size="xs" />
                <Text fontSize="sm" fontWeight="medium" noOfLines={1} flex={1} minW={0}>
                  {contractor.name}
                </Text>
              </HStack>
            ))}
          </VStack>
        </DrawerBody>
        <DrawerFooter borderTopWidth="1px" gap={2}>
          <Button variant="ghost" colorScheme="gray" onClick={onClose}>Anuluj</Button>
          <Button colorScheme="primary" onClick={handleSave} isLoading={isSubmitting}>
            Zapisz ({selectedUserIds.size + selectedContractorIds.size})
          </Button>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
