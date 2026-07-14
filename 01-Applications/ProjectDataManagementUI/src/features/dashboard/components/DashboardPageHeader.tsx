import React from 'react';
import { Heading, HStack, Icon, Text, VStack } from '@chakra-ui/react';
import { LayoutDashboard } from 'lucide-react';
import { DashboardAddCostToolbar } from './DashboardAddCostToolbar';

export interface DashboardPageHeaderProps {
  projectName?: string;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

export function DashboardPageHeader({
  projectName,
  tenantId,
  projectId,
  onRefetch,
}: DashboardPageHeaderProps): React.ReactElement {
  return (
    <HStack justify="space-between" mb={6} flexWrap="wrap" gap={4} w="100%">
      <HStack spacing={3}>
        <Icon as={LayoutDashboard} boxSize={8} color="primary.600" />
        <VStack align="flex-start" spacing={0}>
          <Heading size="lg">Dashboard projektu</Heading>
          {projectName && (
            <Text fontSize="sm" color="neutral.600" noOfLines={1}>
              {projectName}
            </Text>
          )}
        </VStack>
      </HStack>
      <DashboardAddCostToolbar tenantId={tenantId} projectId={projectId} onRefetch={onRefetch} />
    </HStack>
  );
}

export default DashboardPageHeader;
