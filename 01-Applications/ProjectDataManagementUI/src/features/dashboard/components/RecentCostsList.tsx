import React from 'react';
import { Box, HStack, Icon, Text, VStack } from '@chakra-ui/react';
import { Receipt } from 'lucide-react';
import type { TrackedCostWeb } from '../types/projectDashboard.types';
import { NetGrossAmount } from './shared/NetGrossAmount';
import { DATE } from '../utils/formatters';

const MAX_RECENT_COSTS = 5;

export interface RecentCostsListProps {
  costs: TrackedCostWeb[];
  onSelect: (cost: TrackedCostWeb) => void;
}

function costPath(cost: TrackedCostWeb): string {
  return cost.costEstimateItemPath ?? cost.workScheduleWorkPath ?? 'Koszt dodatkowy';
}

interface RecentCostRowProps {
  cost: TrackedCostWeb;
  onSelect: (cost: TrackedCostWeb) => void;
}

function RecentCostRow({ cost, onSelect }: RecentCostRowProps): React.ReactElement {
  const handleKeyDown = (event: React.KeyboardEvent): void => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      onSelect(cost);
    }
  };

  return (
    <HStack
      role="button"
      tabIndex={0}
      aria-label={`Otwórz koszt ${cost.name}`}
      onClick={() => onSelect(cost)}
      onKeyDown={handleKeyDown}
      cursor="pointer"
      spacing={3}
      align="flex-start"
      px={3}
      py={2.5}
      borderRadius="lg"
      _hover={{ bg: 'neutral.50' }}
    >
      <Icon as={Receipt} boxSize={4} color="orange.500" mt={0.5} aria-hidden="true" />
      <VStack align="flex-start" spacing={0.5} flex={1} minW={0}>
        <Text fontSize="sm" fontWeight="medium" color="neutral.800" noOfLines={1}>
          {cost.name}
        </Text>
        <Text fontSize="xs" color="neutral.600" noOfLines={1}>
          {costPath(cost)} · {DATE(cost.createdAt)}
        </Text>
      </VStack>
      <Box flexShrink={0}>
        <NetGrossAmount
          net={cost.net}
          gross={cost.gross}
          size="sm"
          align="right"
          accentColor="orange.600"
        />
      </Box>
    </HStack>
  );
}

/** Krótka lista 5 najnowszych kosztów projektu, klikalna do szczegółów. */
export function RecentCostsList({ costs, onSelect }: RecentCostsListProps): React.ReactElement {
  const recent = [...costs]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, MAX_RECENT_COSTS);

  if (recent.length === 0) {
    return (
      <Text fontSize="sm" color="neutral.600" fontStyle="italic" p={3}>
        Brak ostatnich kosztów.
      </Text>
    );
  }

  return (
    <VStack align="stretch" spacing={1}>
      {recent.map((cost) => (
        <RecentCostRow key={cost.id} cost={cost} onSelect={onSelect} />
      ))}
    </VStack>
  );
}

export default RecentCostsList;
