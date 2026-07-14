import React from 'react';
import {
  Badge,
  HStack,
  Icon,
  Tab,
  TabList,
  TabPanels,
  Tabs,
  Text,
  useBreakpointValue,
} from '@chakra-ui/react';
import { Calendar, DollarSign, LayoutDashboard, Receipt } from 'lucide-react';

export const DASHBOARD_TAB_INDEX = {
  general: 0,
  finance: 1,
  schedules: 2,
  costs: 3,
} as const;

export type DashboardMainTab = keyof typeof DASHBOARD_TAB_INDEX;

export interface DashboardMainTabsProps {
  tabIndex: number;
  onTabChange: (index: number) => void;
  estimatesCount: number;
  schedulesCount: number;
  costsCount: number;
  children: React.ReactNode;
}

const TAB_ITEMS: Array<{
  key: DashboardMainTab;
  label: string;
  icon: React.ElementType;
  countKey: 'estimatesCount' | 'schedulesCount' | 'costsCount' | null;
  badgeColor: string;
}> = [
  { key: 'general', label: 'Ogólne', icon: LayoutDashboard, countKey: null, badgeColor: 'primary' },
  { key: 'finance', label: 'Finanse', icon: DollarSign, countKey: 'estimatesCount', badgeColor: 'level2' },
  { key: 'schedules', label: 'Harmonogramy', icon: Calendar, countKey: 'schedulesCount', badgeColor: 'level2' },
  { key: 'costs', label: 'Koszty', icon: Receipt, countKey: 'costsCount', badgeColor: 'primary' },
];

export function DashboardMainTabs({
  tabIndex,
  onTabChange,
  estimatesCount,
  schedulesCount,
  costsCount,
  children,
}: DashboardMainTabsProps): React.ReactElement {
  const counts = { estimatesCount, schedulesCount, costsCount };
  const showTabBadges = useBreakpointValue({ base: false, md: true }) ?? true;

  return (
    <Tabs
      colorScheme="primary"
      variant="enclosed"
      index={tabIndex}
      onChange={onTabChange}
      isLazy
      w="100%"
    >
      <TabList flexWrap="wrap" w="100%" overflow="visible">
        {TAB_ITEMS.map(({ key, label, icon, countKey, badgeColor }) => {
          const count = countKey != null ? counts[countKey] : null;

          return (
            <Tab key={key} fontWeight="bold" whiteSpace="nowrap" flex={{ base: '1 1 auto', md: 1 }}>
              <HStack spacing={2}>
                <Icon as={icon} boxSize={4} aria-hidden="true" />
                <Text>{label}</Text>
                {showTabBadges && count != null && (
                  <Badge colorScheme={badgeColor} ml={1} borderRadius="full">
                    {count}
                  </Badge>
                )}
              </HStack>
            </Tab>
          );
        })}
      </TabList>
      <TabPanels>{children}</TabPanels>
    </Tabs>
  );
}

export default DashboardMainTabs;
