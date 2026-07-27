import { useMemo, useState } from "react";
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
import { AssignmentConflictAlertDialog } from "../AssignmentConflictAlertDialog";
import { AssigneeConflictWarningIcon } from "../AssigneeConflictWarningIcon";
import {
  diffNewAssignees,
  useAssignmentConflictCheck,
} from "../../../hooks/useAssignmentConflictCheck";
import { detectAssigneeConflicts } from "../../../utils/detectAssigneeConflicts";
import type { WorkScheduleAssignmentConflictWeb } from "../../../types/workSchedule.types";

interface AssignmentsModalProps {
  isOpen: boolean;
  onClose: () => void;
  stageId: string;
  work: WorkScheduleStageWorkWeb;
}

export default function AssignmentsModal({ isOpen, onClose, stageId, work }: AssignmentsModalProps) {
  const { members, contractors, setAssignments } = useGantt();
  const initialUserIds = (work.assignees ?? [])
    .map(a => a.userId)
    .filter((id): id is string => !!id);
  const initialContractorIds = (work.assignees ?? [])
    .map(a => a.contractorId)
    .filter((id): id is string => !!id);

  const [selectedUserIds, setSelectedUserIds] = useState<Set<string>>(new Set(initialUserIds));
  const [selectedContractorIds, setSelectedContractorIds] = useState<Set<string>>(
    new Set(initialContractorIds)
  );
  const [search, setSearch] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isConflictOpen, setIsConflictOpen] = useState(false);
  const [pendingUserIds, setPendingUserIds] = useState<string[]>([]);
  const [pendingContractorIds, setPendingContractorIds] = useState<string[]>([]);

  const { conflicts, checkConflicts, clearConflicts } = useAssignmentConflictCheck();

  const workPeriods = work.periods ?? [];

  const conflictsByUserId = useMemo(() => {
    const map = new Map<string, WorkScheduleAssignmentConflictWeb[]>();
    for (const m of members) {
      const name = `${m.firstName} ${m.lastName}`.trim() || m.email;
      const found = detectAssigneeConflicts({
        workId: work.id,
        workPeriods,
        candidates: [{
          userId: m.userId,
          assigneeName: m.companyName ? `${name} (${m.companyName})` : name,
          assignments: m.assignments ?? [],
        }],
      });
      if (found.length > 0) {
        map.set(m.userId, found);
      }
    }
    return map;
  }, [members, work.id, workPeriods]);

  const conflictsByContractorId = useMemo(() => {
    const map = new Map<string, WorkScheduleAssignmentConflictWeb[]>();
    for (const c of contractors) {
      const found = detectAssigneeConflicts({
        workId: work.id,
        workPeriods,
        candidates: [{
          contractorId: c.id,
          assigneeName: c.name,
          assignments: c.assignments ?? [],
        }],
      });
      if (found.length > 0) {
        map.set(c.id, found);
      }
    }
    return map;
  }, [contractors, work.id, workPeriods]);

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

  const persistAssignments = async (userIds: string[], contractorIds: string[]) => {
    setIsSubmitting(true);
    try {
      await setAssignments(stageId, work.id, userIds, contractorIds);
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSave = async () => {
    const userIds = Array.from(selectedUserIds);
    const contractorIds = Array.from(selectedContractorIds);
    const { newUserIds, newContractorIds } = diffNewAssignees(
      userIds,
      contractorIds,
      initialUserIds,
      initialContractorIds
    );

    const memberById = new Map(members.map(m => [m.userId, m]));
    const contractorById = new Map(contractors.map(c => [c.id, c]));
    const candidates = [
      ...newUserIds.map(id => {
        const m = memberById.get(id);
        const name = m
          ? [m.firstName, m.lastName].filter(Boolean).join(" ") || m.email
          : id;
        return {
          userId: id,
          assigneeName: name,
          assignments: m?.assignments ?? [],
        };
      }),
      ...newContractorIds.map(id => {
        const c = contractorById.get(id);
        return {
          contractorId: id,
          assigneeName: c?.name ?? id,
          assignments: c?.assignments ?? [],
        };
      }),
    ];

    const found = checkConflicts(candidates, work.id, work.periods ?? []);
    if (found.length > 0) {
      setPendingUserIds(userIds);
      setPendingContractorIds(contractorIds);
      setIsConflictOpen(true);
      return;
    }

    await persistAssignments(userIds, contractorIds);
  };

  const handleConfirmDespiteConflicts = async () => {
    setIsConflictOpen(false);
    clearConflicts();
    await persistAssignments(pendingUserIds, pendingContractorIds);
  };

  return (
    <>
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
                  <Avatar
                    name={`${member.firstName} ${member.lastName}`.trim() || member.email}
                    size="xs"
                  />
                  <Text fontSize="sm" fontWeight="medium" noOfLines={1} flex={1} minW={0}>
                    {`${member.firstName} ${member.lastName}`.trim() || member.email}
                    {member.companyName ? ` (${member.companyName})` : ""}
                  </Text>
                  <AssigneeConflictWarningIcon
                    conflicts={conflictsByUserId.get(member.userId) ?? []}
                  />
                </HStack>
              ))}

              <Text fontSize="xs" fontWeight="semibold" color="neutral.500" textTransform="uppercase" pt={2}>
                Kontrahenci
              </Text>
              {filteredContractors.length === 0 && (
                <Text fontSize="sm" color="neutral.400">Brak kontrahentów</Text>
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
                  <AssigneeConflictWarningIcon
                    conflicts={conflictsByContractorId.get(contractor.id) ?? []}
                  />
                </HStack>
              ))}
            </VStack>
          </DrawerBody>
          <DrawerFooter borderTopWidth="1px" gap={2}>
            <Button variant="ghost" colorScheme="gray" onClick={onClose}>Anuluj</Button>
            <Button
              colorScheme="primary"
              onClick={handleSave}
              isLoading={isSubmitting}
            >
              Zapisz ({selectedUserIds.size + selectedContractorIds.size})
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <AssignmentConflictAlertDialog
        isOpen={isConflictOpen}
        onClose={() => {
          setIsConflictOpen(false);
          clearConflicts();
        }}
        onConfirm={handleConfirmDespiteConflicts}
        conflicts={conflicts}
        isLoading={isSubmitting}
      />
    </>
  );
}
